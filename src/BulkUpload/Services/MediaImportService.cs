using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Resolvers;
using Umbraco.Extensions;

namespace Umbraco.Community.BulkUpload.Services;

public class MediaImportService : IMediaImportService
{
    private readonly IMediaService _mediaService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IResolverFactory _resolverFactory;
    private readonly ILogger<MediaImportService> _logger;

    public MediaImportService(
        IMediaService mediaService,
        IMediaTypeService mediaTypeService,
        MediaFileManager mediaFileManager,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IResolverFactory resolverFactory,
        ILogger<MediaImportService> logger)
    {
        _mediaService = mediaService;
        _mediaTypeService = mediaTypeService;
        _mediaFileManager = mediaFileManager;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _resolverFactory = resolverFactory;
        _logger = logger;
    }

    public virtual MediaImportObject CreateMediaImportObject(dynamic? record)
    {
        if (record == null)
        {
            _logger.LogError("Bulk Upload Media: Record is null");
            throw new ArgumentNullException(nameof(record));
        }

        var dynamicProperties = (IDictionary<string, object>)record;

        var propertiesToCreate = new Dictionary<string, object>();

        var fileName = "";
        if (dynamicProperties.TryGetValue("fileName", out object? fileNameValue))
        {
            fileName = fileNameValue?.ToString() ?? "";
        }

        string? name = null;
        if (dynamicProperties.TryGetValue("name", out object? nameValue))
        {
            var nameStr = nameValue?.ToString();
            if (!string.IsNullOrWhiteSpace(nameStr))
            {
                name = nameStr;
            }
        }

        // Support both "parent" (new) and "parentId" (legacy) columns
        string? parent = null;
        if (dynamicProperties.TryGetValue("parent", out object? parentValue))
        {
            var parentStr = parentValue?.ToString();
            if (!string.IsNullOrWhiteSpace(parentStr))
            {
                parent = ValidateParentValue(parentStr);
            }
        }
        // Fallback to legacy "parentId" column for backward compatibility
        else if (dynamicProperties.TryGetValue("parentId", out object? parentIdValue))
        {
            var parentIdStr = parentIdValue?.ToString();
            if (!string.IsNullOrWhiteSpace(parentIdStr))
            {
                parent = ValidateParentValue(parentIdStr);
            }
        }

        string? mediaTypeAlias = null;
        if (dynamicProperties.TryGetValue("mediaTypeAlias", out object? mediaTypeAliasValue))
        {
            var aliasStr = mediaTypeAliasValue?.ToString();
            if (!string.IsNullOrWhiteSpace(aliasStr))
            {
                mediaTypeAlias = aliasStr;
            }
        }

        MediaImportObject importObject = new MediaImportObject()
        {
            FileName = fileName,
            Name = name,
            Parent = parent,
            MediaTypeAlias = mediaTypeAlias
        };

        MediaSource? externalSource = null;

        foreach (var property in dynamicProperties)
        {
            var columnDetails = property.Key.Split('|');
            var columnName = columnDetails.First();
            string? aliasValue = null;
            if (columnDetails.Length > 1)
            {
                aliasValue = columnDetails.Last();
            }

            if (new string[] { "fileName", "name", "parent", "parentId", "mediaTypeAlias" }.Contains(property.Key.Split('|')[0]))
                continue;

            var resolverAlias = aliasValue ?? "text";

            // Check for external source resolvers (pathToStream, urlToStream)
            // These resolvers return MediaSource objects instead of property values
            if (resolverAlias.Contains("pathToStream") || resolverAlias.Contains("urlToStream"))
            {
                var resolver = _resolverFactory.GetByAlias(resolverAlias);
                if (resolver != null)
                {
                    var resolvedValue = resolver.Resolve(property.Value);
                    if (resolvedValue is MediaSource mediaSource)
                    {
                        externalSource = mediaSource;
                        _logger.LogInformation("Detected external media source: {Type} - {Value}",
                            mediaSource.Type, mediaSource.Value);
                    }
                }
                continue; // Don't add to properties
            }

            var normalResolver = _resolverFactory.GetByAlias(resolverAlias);
            object? propertyValue = null;
            if (normalResolver != null)
            {
                propertyValue = normalResolver.Resolve(property.Value);
            }

            propertiesToCreate.Add(columnName, propertyValue);
        }

        importObject.Properties = propertiesToCreate;
        importObject.ExternalSource = externalSource;
        return importObject;
    }

    public virtual MediaImportResult ImportSingleMediaItem(MediaImportObject importObject, Stream fileStream, bool publish = false)
    {
        var result = new MediaImportResult
        {
            FileName = importObject.FileName,
            Success = false
        };

        try
        {
            if (!importObject.CanImport)
            {
                result.ErrorMessage = "Invalid import object: Missing required fields (fileName or parent)";
                return result;
            }

            // Resolve parent specification to media ID
            var parentId = ResolveParentId(importObject.Parent);
            _logger.LogDebug("Resolved parent '{Parent}' to ID {ParentId}", importObject.Parent, parentId);

            // Determine media type alias if not provided
            var mediaTypeAlias = importObject.MediaTypeAlias;
            if (string.IsNullOrWhiteSpace(mediaTypeAlias))
            {
                mediaTypeAlias = DetermineMediaTypeFromExtension(importObject.FileName);
            }

            // Verify media type exists
            var mediaType = _mediaTypeService.Get(mediaTypeAlias);
            if (mediaType == null)
            {
                result.ErrorMessage = $"Media type '{mediaTypeAlias}' not found";
                return result;
            }

            // Check if media already exists with same name under parent
            var existingMedia = _mediaService
                .GetPagedChildren(parentId, 0, int.MaxValue, out _)
                .FirstOrDefault(x => string.Equals(x.Name, importObject.DisplayName, StringComparison.InvariantCultureIgnoreCase));

            IMedia mediaItem;
            if (existingMedia != null)
            {
                mediaItem = existingMedia;
                _logger.LogInformation("Updating existing media: {Name}", importObject.DisplayName);
            }
            else
            {
                mediaItem = _mediaService.CreateMedia(importObject.DisplayName, parentId, mediaTypeAlias);
                _logger.LogInformation("Creating new media: {Name}", importObject.DisplayName);
            }

            // Upload the file
            if (fileStream != null && fileStream.Length > 0)
            {
                fileStream.Position = 0;

                // Get the media path using the property type and media item's key
                var propertyType = mediaItem.Properties["umbracoFile"]?.PropertyType;
                if (propertyType == null)
                {
                    result.ErrorMessage = "Media type does not have umbracoFile property";
                    return result;
                }

                var mediaPath = _mediaFileManager.GetMediaPath(importObject.FileName, propertyType.Key, mediaItem.Key);

                // Save the file to the media file system
                _mediaFileManager.FileSystem.AddFile(mediaPath, fileStream, true);

                // Set the file path on the media item
                mediaItem.SetValue("umbracoFile", mediaPath);
            }
            else
            {
                result.ErrorMessage = "File stream is null or empty";
                return result;
            }

            // Set additional properties
            if (importObject.Properties != null && importObject.Properties.Any())
            {
                foreach (var property in importObject.Properties)
                {
                    try
                    {
                        mediaItem.SetValue(property.Key, property.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set property {PropertyKey} on media {MediaName}", property.Key, importObject.DisplayName);
                    }
                }
            }

            // Save the media item
            var saveResult = _mediaService.Save(mediaItem);

            if (saveResult.Success)
            {
                result.Success = true;
                result.MediaId = mediaItem.Id;
                result.MediaGuid = mediaItem.Key;
                result.MediaUdi = Udi.Create(Constants.UdiEntityType.Media, mediaItem.Key).ToString();
                _logger.LogInformation("Successfully imported media: {Name} (ID: {Id})", importObject.DisplayName, mediaItem.Id);
            }
            else
            {
                result.ErrorMessage = "Failed to save media item";
                _logger.LogError("Failed to save media: {Name}", importObject.DisplayName);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error importing media: {FileName}", importObject.FileName);
        }

        return result;
    }

    /// <summary>
    /// Validates that the parent value is in a supported format.
    /// Returns the value if it's a valid integer, GUID, or path; otherwise returns null.
    /// </summary>
    private string? ValidateParentValue(string parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return null;
        }

        // Check if it's a valid integer
        if (int.TryParse(parent, out _))
        {
            return parent;
        }

        // Check if it's a valid GUID
        if (Guid.TryParse(parent, out _))
        {
            return parent;
        }

        // Check if it's a path (starts with /)
        if (parent.TrimStart().StartsWith("/"))
        {
            return parent;
        }

        // Invalid format
        return null;
    }

    /// <summary>
    /// Resolves the parent folder specification to a media ID.
    /// Supports integer ID, GUID, or path (with auto-creation).
    /// </summary>
    public int ResolveParentId(string? parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return Constants.System.Root;
        }

        // Try to parse as integer ID
        if (int.TryParse(parent, out var parentId))
        {
            return parentId;
        }

        // Try to parse as GUID and resolve to media ID
        if (Guid.TryParse(parent, out var guid))
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
        var folderId = ResolveOrCreateFolderPath(parent);
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
                    _logger.LogError("Failed to create folder '{FolderName}'", folderName);
                    return null;
                }
            }
        }

        return currentParentId;
    }

    private string DetermineMediaTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => "Image",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => "File",
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".webm" => "Video",
            ".mp3" or ".wav" or ".ogg" or ".wma" => "Audio",
            _ => "File" // Default to File type for unknown extensions
        };
    }
}
