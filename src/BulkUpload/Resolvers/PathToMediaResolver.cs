using BulkUpload.Models;
using BulkUpload.Services;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using IOFile = System.IO.File;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that reads a media file from a local or network file path and uploads it to Umbraco,
/// returning the UDI of the created media item.
///
/// Supports flexible parent folder specification:
/// - Alias parameter: imageFile|pathToMedia:1234 or imageFile|pathToMedia:/Folder/Path/
/// - Value parameter: C:/Images/photo.jpg|1234 or /mnt/share/images/photo.jpg|/Folder/Path/
/// - Fallback hierarchy: value parameter > alias parameter > root folder
///
/// Parent formats supported:
/// - Integer ID: 1234
/// - GUID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
/// - Path: /Blog/Header Images/ (creates folders if they don't exist)
///
/// File path formats supported:
/// - Absolute paths: C:/Images/photo.jpg, /mnt/share/images/photo.jpg
/// - Network paths: \\server\share\images\photo.jpg
/// - Relative paths: ./images/photo.jpg (relative to application root)
/// </summary>
public class PathToMediaResolver : IResolver
{
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILogger<PathToMediaResolver> _logger;

    public PathToMediaResolver(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILogger<PathToMediaResolver> logger)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _logger = logger;
    }

    public string Alias() => "pathToMedia";

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

        // Parse the actual value to extract file path and optional value parameter
        string? valueParameter = null;
        string? filePath = null;

        if (actualValue is string str && !string.IsNullOrWhiteSpace(str))
        {
            var parts = str.Split('|', 2);
            filePath = parts[0]?.Trim();
            valueParameter = parts.Length > 1 ? parts[1]?.Trim() : null;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("PathToMediaResolver received null or empty file path");
            return string.Empty;
        }

        // Check cache first to avoid creating duplicate media items
        if (_mediaItemCache.TryGetGuid(filePath, out var cachedMediaGuid))
        {
            var cachedUdi = Udi.Create(UmbracoConstants.UdiEntityType.Media, cachedMediaGuid);
            _logger.LogDebug("Found cached media for path: {Path}, UDI: {Udi}", filePath, cachedUdi);
            return cachedUdi.ToString();
        }

        try
        {
            // Validate and resolve file path
            if (!IOFile.Exists(filePath))
            {
                _logger.LogWarning("File not found at path: {FilePath}", filePath);
                return string.Empty;
            }

            // Determine parent folder using fallback hierarchy:
            // 1. Value parameter (highest priority)
            // 2. Alias parameter
            // 3. Root folder (default)
            var parent = ResolveParent(valueParameter, aliasParameter);

            // Read the file from disk
            byte[] fileBytes;
            try
            {
                fileBytes = IOFile.ReadAllBytes(filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied reading file: {FilePath}", filePath);
                return string.Empty;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error reading file: {FilePath}", filePath);
                return string.Empty;
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("File is empty: {FilePath}", filePath);
                return string.Empty;
            }

            // Extract filename from path
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "uploaded-file";
            }

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
                    ? _mediaService.CreateMedia(fileName, UmbracoConstants.System.Root, mediaTypeAlias)
                    : _mediaService.CreateMedia(fileName, guid, mediaTypeAlias);
            }
            else if (parent is int id)
            {
                mediaItem = _mediaService.CreateMedia(fileName, id, mediaTypeAlias);
            }
            else
            {
                _logger.LogWarning("Invalid parent type resolved for file: {FilePath}", filePath);
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
                _logger.LogWarning("Failed to save media item for file: {FilePath}", filePath);
                return string.Empty;
            }

            // Cache the created media item to avoid duplicates in subsequent imports
            _mediaItemCache.TryAdd(filePath, mediaItem.Key);

            // Return the UDI
            var udi = Udi.Create(UmbracoConstants.UdiEntityType.Media, mediaItem.Key);
            var udiString = udi.ToString();

            _logger.LogInformation("Successfully created media from file: {FilePath}, Parent: {Parent}, UDI: {Udi}",
                filePath, parent, udiString);
            return udiString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving file path to media: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves the parent folder using the fallback hierarchy:
    /// 1. Value parameter (from filePath|parameter syntax)
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