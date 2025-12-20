using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.BulkUpload.Models;

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
    private readonly ILogger<UrlToMediaResolver> _logger;

    public UrlToMediaResolver(
        IHttpClientFactory httpClientFactory,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ILogger<UrlToMediaResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
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

        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("UrlToMediaResolver received invalid URL: {Url}", urlString);
            return string.Empty;
        }

        try
        {
            // Determine parent folder ID using fallback hierarchy:
            // 1. Value parameter (highest priority)
            // 2. Alias parameter
            // 3. Root folder (default)
            var parentId = ResolveParentId(valueParameter, aliasParameter);

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

            // Create media item in the determined parent folder
            var mediaItem = _mediaService.CreateMedia(fileName, parentId, mediaTypeAlias);

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

            // Return the UDI
            var udi = Udi.Create(Constants.UdiEntityType.Media, mediaItem.Key);
            var udiString = udi.ToString();

            _logger.LogInformation("Successfully created media from URL: {Url}, Parent: {ParentId}, UDI: {Udi}",
                urlString, parentId, udiString);
            return udiString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving URL to media: {Url}", urlString);
            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves the parent folder ID using the fallback hierarchy:
    /// 1. Value parameter (from URL|parameter syntax)
    /// 2. Alias parameter (from resolver:parameter syntax)
    /// 3. Root folder (default)
    /// </summary>
    private int ResolveParentId(string? valueParameter, string? aliasParameter)
    {
        // Try value parameter first (highest priority)
        var parameter = valueParameter ?? aliasParameter;

        if (string.IsNullOrWhiteSpace(parameter))
        {
            return Constants.System.Root;
        }

        // Try to parse as integer ID
        if (int.TryParse(parameter, out var parentId))
        {
            return parentId;
        }

        // Try to parse as GUID and resolve to media ID
        if (Guid.TryParse(parameter, out var guid))
        {
            var media = _mediaService.GetById(guid);
            if (media != null)
            {
                _logger.LogDebug("Resolved parent GUID {Guid} to media ID {Id}", guid, media.Id);
                return media.Id;
            }
            _logger.LogWarning("Parent GUID {Guid} not found, using root folder", guid);
            return Constants.System.Root;
        }

        // Treat as path - resolve or create folder structure
        var folderId = ResolveOrCreateFolderPath(parameter);
        return folderId ?? Constants.System.Root;
    }

    /// <summary>
    /// Resolves a folder path like "/Blog/Header Images/" to a media folder ID.
    /// Creates folders if they don't exist.
    /// </summary>
    private int? ResolveOrCreateFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path - remove leading/trailing slashes and split
        path = path.Trim('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return Constants.System.Root;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentParentId = Constants.System.Root;

        foreach (var segment in segments)
        {
            var folderName = segment.Trim();
            if (string.IsNullOrWhiteSpace(folderName))
            {
                continue;
            }

            // Look for existing folder with this name under current parent
            var existingFolder = _mediaService
                .GetPagedChildren(currentParentId, 0, int.MaxValue, out _)
                .FirstOrDefault(x =>
                    x.ContentType.Alias == "Folder" &&
                    string.Equals(x.Name, folderName, StringComparison.InvariantCultureIgnoreCase));

            if (existingFolder != null)
            {
                currentParentId = existingFolder.Id;
                _logger.LogDebug("Found existing folder '{FolderName}' with ID {Id}", folderName, existingFolder.Id);
            }
            else
            {
                // Create new folder
                var newFolder = _mediaService.CreateMedia(folderName, currentParentId, "Folder");
                var saveResult = _mediaService.Save(newFolder);

                if (saveResult.Success)
                {
                    currentParentId = newFolder.Id;
                    _logger.LogInformation("Created new folder '{FolderName}' with ID {Id}", folderName, newFolder.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to create folder '{FolderName}', using parent ID {ParentId}",
                        folderName, currentParentId);
                    return currentParentId;
                }
            }
        }

        return currentParentId;
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
