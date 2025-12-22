using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Services;
using BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Resolvers;

/// <summary>
/// Resolver that downloads media from a URL and uploads it to Umbraco,
/// returning the UDI of the created media item.
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
            var cachedUdi = Udi.Create(Constants.UdiEntityType.Media, cachedMediaGuid);
            _logger.LogDebug("Found cached media for URL: {Url}, UDI: {Udi}", urlString, cachedUdi);
            return cachedUdi.ToString();
        }

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

            // Extract filename from URL
            var fileName = GetFileNameFromUrl(uri);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            // Determine media type from extension
            var mediaTypeAlias = DetermineMediaTypeFromExtension(extension);

            // Verify media type exists
            var mediaType = _mediaTypeService.Get(mediaTypeAlias);
            if (mediaType == null)
            {
                _logger.LogWarning("Media type '{MediaType}' not found", mediaTypeAlias);
                return string.Empty;
            }

            // Create media item in the determined parent folder using GUID or int
            IMedia mediaItem;
            if (parent is Guid guid)
            {
                mediaItem = guid == Guid.Empty
                    ? _mediaService.CreateMedia(fileName, Constants.System.Root, mediaTypeAlias)
                    : _mediaService.CreateMedia(fileName, guid, mediaTypeAlias);
            }
            else if (parent is int id)
            {
                mediaItem = _mediaService.CreateMedia(fileName, id, mediaTypeAlias);
            }
            else
            {
                _logger.LogWarning("Invalid parent type resolved for URL: {Url}", urlString);
                return string.Empty;
            }

            // Upload the file
            using (var fileStream = new MemoryStream(fileBytes))
            {
                // Get the property type for umbracoFile
                var propertyType = mediaItem.Properties["umbracoFile"]?.PropertyType;
                if (propertyType == null)
                {
                    _logger.LogWarning("Media type does not have umbracoFile property");
                    return string.Empty;
                }

                // Get the media path
                var mediaPath = _mediaFileManager.GetMediaPath(fileName, propertyType.Key, mediaItem.Key);

                // Save the file to the media file system
                _mediaFileManager.FileSystem.AddFile(mediaPath, fileStream, true);

                // Set the file path on the media item
                mediaItem.SetValue("umbracoFile", mediaPath);
            }

            // Save the media item
            var saveResult = _mediaService.Save(mediaItem);
            if (!saveResult.Success)
            {
                _logger.LogWarning("Failed to save media item for URL: {Url}", urlString);
                return string.Empty;
            }

            // Cache the created media item to avoid duplicates in subsequent imports
            _mediaItemCache.TryAdd(urlString, mediaItem.Key);

            // Return the UDI
            var udi = Udi.Create(Constants.UdiEntityType.Media, mediaItem.Key);
            var udiString = udi.ToString();

            _logger.LogInformation("Successfully created media from URL: {Url}, Parent: {Parent}, UDI: {Udi}",
                urlString, parent, udiString);
            return udiString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving URL to media: {Url}", urlString);
            return string.Empty;
        }
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
            return Constants.System.Root;
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
            return Constants.System.Root;
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
        return Constants.System.Root;
    }

    private string GetFileNameFromUrl(Uri uri)
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

        // Ensure we have an extension
        if (!Path.HasExtension(fileName))
        {
            fileName += ".jpg"; // Default to .jpg if no extension
        }

        return fileName;
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
