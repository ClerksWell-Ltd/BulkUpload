using BulkUpload.Models;
using BulkUpload.Services;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that downloads media from a URL (or decodes a base64 data URI) and
/// uploads it to Umbraco, returning the UDI of the created media item.
///
/// Supports flexible parent folder specification:
/// - Alias parameter: imageUrl|urlToMedia:1234 or imageUrl|urlToMedia:/Folder/Path/
/// - Value parameter: https://example.com/image.jpg|1234 or https://example.com/image.jpg|/Folder/Path/
/// - Fallback hierarchy: value parameter > alias parameter > root folder
///
/// Parent formats supported:
/// - Integer ID: 1234
/// - GUID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
/// - Path: /Blog/Header Images/ (creates folders if they don't exist)
///
/// Data URI support:
/// - Values starting with "data:" followed by a base64-encoded image payload are
///   decoded and uploaded without a network request, e.g.
///   data:image/png;base64,iVBORw0KGgo...|urlToMedia[:parentParameter]
/// </summary>
public class UrlToMediaResolver : IResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILogger<UrlToMediaResolver> _logger;

    public UrlToMediaResolver(
        IHttpClientFactory httpClientFactory,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILogger<UrlToMediaResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _logger = logger;
    }

    public string Alias() => "urlToMedia";

    public object Resolve(object value)
    {
        // Extract alias parameter if value is wrapped
        string? aliasParameter = null;
        object actualValue = value;

        if (value is ParameterizedValue parameterizedValue)
        {
            aliasParameter = parameterizedValue.Parameter;
            actualValue = parameterizedValue.Value;
        }

        // Parse the actual value to extract URL and optional value parameter
        string? valueParameter = null;
        string? urlString = null;

        if (actualValue is string str && !string.IsNullOrWhiteSpace(str))
        {
            var parts = str.Split('|', 2);
            urlString = parts[0]?.Trim();
            valueParameter = parts.Length > 1 ? parts[1]?.Trim() : null;
        }

        if (string.IsNullOrWhiteSpace(urlString))
        {
            _logger.LogWarning("UrlToMediaResolver received null or empty URL");
            return string.Empty;
        }

        // Check cache first to avoid creating duplicate media items
        if (_mediaItemCache.TryGetGuid(urlString, out var cachedMediaGuid))
        {
            var cachedUdi = Udi.Create(UmbracoConstants.UdiEntityType.Media, cachedMediaGuid);
            _logger.LogDebug("Found cached media for URL: {Url}, UDI: {Udi}", urlString, cachedUdi);
            return cachedUdi.ToString();
        }

        if (DataUriParser.IsDataUri(urlString))
            return ResolveDataUri(urlString, valueParameter, aliasParameter);

        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("UrlToMediaResolver received invalid URL: {Url}", urlString);
            return string.Empty;
        }

        try
        {
            // Determine parent folder using fallback hierarchy:
            // 1. Value parameter (highest priority)
            // 2. Alias parameter
            // 3. Root folder (default)
            var parent = ResolveParent(valueParameter, aliasParameter);

            // Download the media file from the URL
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = httpClient.GetAsync(uri).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download media from URL: {Url}, Status: {StatusCode}",
                    urlString, response.StatusCode);
                return string.Empty;
            }

            var fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("Downloaded file from URL is empty: {Url}", urlString);
                return string.Empty;
            }

            // Extract filename from URL, using Content-Type to detect extension if needed
            var fileName = GetFileNameFromUrl(uri, response);

            return CreateAndSaveMedia(fileBytes, fileName, parent, urlString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving URL to media: {Url}", urlString);
            return string.Empty;
        }
    }

    private string ResolveDataUri(string dataUri, string? valueParameter, string? aliasParameter)
    {
        if (!DataUriParser.TryParse(dataUri, out var mimeType, out var fileBytes))
        {
            _logger.LogWarning("UrlToMediaResolver received malformed or unsupported data URI");
            return string.Empty;
        }

        try
        {
            var parent = ResolveParent(valueParameter, aliasParameter);
            var fileName = DataUriParser.GenerateFileName(mimeType, fileBytes);
            return CreateAndSaveMedia(fileBytes, fileName, parent, dataUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving data URI to media");
            return string.Empty;
        }
    }

    private string CreateAndSaveMedia(byte[] fileBytes, string fileName, object parent, string cacheKey)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mediaTypeAlias = DetermineMediaTypeFromExtension(extension);

        var mediaType = _mediaTypeService.Get(mediaTypeAlias);
        if (mediaType == null)
        {
            _logger.LogWarning("Media type '{MediaType}' not found", mediaTypeAlias);
            return string.Empty;
        }

        IMedia mediaItem;
        if (parent is Guid guid)
        {
            mediaItem = guid == Guid.Empty
                ? _mediaService.CreateMedia(fileName, UmbracoConstants.System.Root, mediaTypeAlias)
                : _mediaService.CreateMedia(fileName, guid, mediaTypeAlias);
        }
        else if (parent is int id)
        {
            mediaItem = _mediaService.CreateMedia(fileName, id, mediaTypeAlias);
        }
        else
        {
            _logger.LogWarning("Invalid parent type resolved for media: {FileName}", fileName);
            return string.Empty;
        }

        using (var fileStream = new MemoryStream(fileBytes))
        {
            var propertyType = mediaItem.Properties["umbracoFile"]?.PropertyType;
            if (propertyType == null)
            {
                _logger.LogWarning("Media type does not have umbracoFile property");
                return string.Empty;
            }

            var mediaPath = _mediaFileManager.GetMediaPath(fileName, propertyType.Key, mediaItem.Key);
            _mediaFileManager.FileSystem.AddFile(mediaPath, fileStream, true);

            // Store the URL-form path ("/media/abc/logo.jpg") on umbracoFile. GetMediaPath
            // returns a filesystem-relative path; ImageCropper + v17 MediaPicker3 need the
            // /media/-prefixed URL to build link/preview.
            var umbracoFileValue = _mediaFileManager.FileSystem.GetUrl(mediaPath);
            mediaItem.SetValue("umbracoFile", umbracoFileValue);
        }

        var saveResult = _mediaService.Save(mediaItem);
        if (!saveResult.Success)
        {
            _logger.LogWarning("Failed to save media item for: {FileName}", fileName);
            return string.Empty;
        }

        _mediaItemCache.TryAdd(cacheKey, mediaItem.Key);

        var udi = Udi.Create(UmbracoConstants.UdiEntityType.Media, mediaItem.Key);
        var udiString = udi.ToString();

        _logger.LogInformation("Successfully created media: {FileName}, Parent: {Parent}, UDI: {Udi}",
            fileName, parent, udiString);
        return udiString;
    }

    /// <summary>
    /// Resolves the parent folder using the fallback hierarchy:
    /// 1. Value parameter (from URL|parameter syntax)
    /// 2. Alias parameter (from resolver:parameter syntax)
    /// 3. Root folder (default)
    /// Uses caching for improved performance.
    /// Returns either Guid or int depending on parameter format and framework version.
    /// </summary>
    private object ResolveParent(string? valueParameter, string? aliasParameter)
    {
        // Try value parameter first (highest priority)
        var parameter = valueParameter ?? aliasParameter;

        if (string.IsNullOrWhiteSpace(parameter))
        {
            return UmbracoConstants.System.Root;
        }

        // Try to parse as GUID - use directly without lookup for modern Umbraco compatibility
        if (Guid.TryParse(parameter, out var guid))
        {
            _logger.LogDebug("Using parent GUID {Guid} directly", guid);
            return guid;
        }

        // Try to parse as integer ID - only use for .NET 8 compatibility
        if (int.TryParse(parameter, out var parentId))
        {
#if NET8_0
            _logger.LogDebug("Using parent integer ID {Id} (.NET 8)", parentId);
            return parentId;
#else
            // For non-.NET 8, look up the GUID from the integer ID
            var media = _mediaService.GetById(parentId);
            if (media != null)
            {
                _logger.LogDebug("Resolved parent integer ID {Id} to GUID {Guid}", parentId, media.Key);
                return media.Key;
            }
            _logger.LogWarning("Parent ID {Id} not found, using root folder", parentId);
            return UmbracoConstants.System.Root;
#endif
        }

        // Treat as path - resolve or create folder structure using cache (returns GUID)
        var folderGuid = _parentLookupCache.GetOrCreateMediaFolderByPath(parameter);
        if (folderGuid.HasValue)
        {
            _logger.LogDebug("Resolved parent path '{Path}' to GUID {Guid}", parameter, folderGuid.Value);
            return folderGuid.Value;
        }

        _logger.LogWarning("Could not resolve parent path '{Path}', using root folder", parameter);
        return UmbracoConstants.System.Root;
    }

    private string GetFileNameFromUrl(Uri uri, HttpResponseMessage response)
    {
        var segments = uri.Segments;
        var lastSegment = segments.Length > 0 ? segments[^1] : "downloaded-file";

        // Decode URL-encoded characters
        var fileName = Uri.UnescapeDataString(lastSegment);

        // Remove query strings if present
        var queryIndex = fileName.IndexOf('?');
        if (queryIndex >= 0)
        {
            fileName = fileName.Substring(0, queryIndex);
        }

        // If no extension, detect from Content-Type header
        if (!Path.HasExtension(fileName))
        {
            var extension = GetFileExtensionFromContentType(response.Content.Headers.ContentType?.MediaType);
            fileName += extension;
        }

        return fileName;
    }

    private static string GetFileExtensionFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return ".bin";

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "video/mp4" => ".mp4",
            "video/avi" or "video/x-msvideo" => ".avi",
            "video/quicktime" => ".mov",
            "video/x-ms-wmv" => ".wmv",
            "video/webm" => ".webm",
            "audio/mpeg" => ".mp3",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/x-ms-wma" => ".wma",
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "text/plain" => ".txt",
            _ => ".bin"
        };
    }

    private string DetermineMediaTypeFromExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => "Image",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => "File",
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".webm" => "Video",
            ".mp3" or ".wav" or ".ogg" or ".wma" => "Audio",
            _ => "File"
        };
    }
}