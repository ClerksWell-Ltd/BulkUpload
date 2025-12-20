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
    private readonly ILogger<PathToMediaResolver> _logger;

    public PathToMediaResolver(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ILogger<PathToMediaResolver> logger)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
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

        try
        {
            // Validate and resolve file path
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found at path: {FilePath}", filePath);
                return string.Empty;
            }

            // Determine parent folder ID using fallback hierarchy:
            // 1. Value parameter (highest priority)
            // 2. Alias parameter
            // 3. Root folder (default)
            var parentId = ResolveParentId(valueParameter, aliasParameter);

            // Read the file from disk
            byte[] fileBytes;
            try
            {
                fileBytes = File.ReadAllBytes(filePath);
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
                _logger.LogWarning("Failed to save media item for file: {FilePath}", filePath);
                return string.Empty;
            }

            // Return the UDI
            var udi = Udi.Create(Constants.UdiEntityType.Media, mediaItem.Key);
            var udiString = udi.ToString();

            _logger.LogInformation("Successfully created media from file: {FilePath}, Parent: {ParentId}, UDI: {Udi}",
                filePath, parentId, udiString);
            return udiString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving file path to media: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves the parent folder ID using the fallback hierarchy:
    /// 1. Value parameter (from filePath|parameter syntax)
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
