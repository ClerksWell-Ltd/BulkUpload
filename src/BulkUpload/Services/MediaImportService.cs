using BulkUpload.Models;
using BulkUpload.Resolvers;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using ReservedColumns = BulkUpload.Constants.ReservedColumns;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Services;

public class MediaImportService : IMediaImportService
{
    private readonly IMediaService _mediaService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IResolverFactory _resolverFactory;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly ILogger<MediaImportService> _logger;

    public MediaImportService(
        IMediaService mediaService,
        IMediaTypeService mediaTypeService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IResolverFactory resolverFactory,
        IParentLookupCache parentLookupCache,
        ILogger<MediaImportService> logger)
    {
        _mediaService = mediaService;
        _mediaTypeService = mediaTypeService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _resolverFactory = resolverFactory;
        _parentLookupCache = parentLookupCache;
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

        // Extract reserved column for legacy tracking
        string? bulkUploadLegacyId = null;
        var legacyIdKey = dynamicProperties.Keys.FirstOrDefault(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadLegacyId, StringComparison.OrdinalIgnoreCase));
        if (legacyIdKey != null && dynamicProperties.TryGetValue(legacyIdKey, out object? legacyIdValue))
        {
            var legacyIdStr = legacyIdValue?.ToString();
            if (!string.IsNullOrWhiteSpace(legacyIdStr))
            {
                bulkUploadLegacyId = legacyIdStr;
            }
        }

        // Extract bulkUploadShouldUpdate flag
        // PER-FILE: Check if the column exists in the CSV (indicates update mode is supported)
        // PER-ROW: Get the value for this specific row (determines if this row should update or create)
        bool bulkUploadShouldUpdate = false;
        bool bulkUploadShouldUpdateColumnExisted = false;
        var shouldUpdateKey = dynamicProperties.Keys.FirstOrDefault(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadShouldUpdate, StringComparison.OrdinalIgnoreCase));
        if (shouldUpdateKey != null)
        {
            bulkUploadShouldUpdateColumnExisted = true; // Column exists in CSV (per-file indicator)
            if (dynamicProperties.TryGetValue(shouldUpdateKey, out object? shouldUpdateValue))
            {
                var shouldUpdateStr = shouldUpdateValue?.ToString()?.Trim().ToLowerInvariant();
                bulkUploadShouldUpdate = shouldUpdateStr == "true" || shouldUpdateStr == "yes" || shouldUpdateStr == "1";
            }
        }

        // Extract bulkUploadMediaGuid for updating existing media
        Guid? bulkUploadMediaGuid = null;
        var mediaGuidKey = dynamicProperties.Keys.FirstOrDefault(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadMediaGuid, StringComparison.OrdinalIgnoreCase));
        if (mediaGuidKey != null && dynamicProperties.TryGetValue(mediaGuidKey, out object? mediaGuidValue))
        {
            var mediaGuidStr = mediaGuidValue?.ToString();
            if (!string.IsNullOrWhiteSpace(mediaGuidStr) && Guid.TryParse(mediaGuidStr, out var parsedMediaGuid))
            {
                bulkUploadMediaGuid = parsedMediaGuid;
            }
        }

        MediaImportObject importObject = new MediaImportObject()
        {
            FileName = fileName,
            Name = name,
            Parent = parent,
            MediaTypeAlias = mediaTypeAlias,
            BulkUploadLegacyId = bulkUploadLegacyId,
            BulkUploadMediaGuid = bulkUploadMediaGuid,
            BulkUploadShouldUpdate = bulkUploadShouldUpdate,
            BulkUploadShouldUpdateColumnExisted = bulkUploadShouldUpdateColumnExisted
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

            // Skip standard columns and reserved columns
            var standardColumns = new string[] { "fileName", "name", "parent", "parentId", "mediaTypeAlias" };
            if (standardColumns.Contains(property.Key.Split('|')[0]) || ReservedColumns.IsReserved(property.Key.Split('|')[0]))
            {
                continue;
            }

            var resolverAlias = aliasValue ?? "text";

            // Auto-detect mediaSource type if no resolver alias is specified
            if (columnName.Equals("mediaSource", StringComparison.OrdinalIgnoreCase) && aliasValue == null)
            {
                var mediaSourceValue = property.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(mediaSourceValue))
                {
                    var detectedSourceType = DetectMediaSourceType(mediaSourceValue);
                    externalSource = new MediaSource
                    {
                        Type = detectedSourceType,
                        Value = mediaSourceValue
                    };
                    _logger.LogInformation("Auto-detected media source: {Type} - {Value}",
                        detectedSourceType, mediaSourceValue);
                }
                continue; // Don't add to properties
            }

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

            if (propertyValue != null)
            {
                propertiesToCreate.Add(columnName, propertyValue);
            }
        }

        importObject.Properties = propertiesToCreate;
        importObject.ExternalSource = externalSource;

        // Infer fileName from mediaSource if not provided in CSV
        if (string.IsNullOrWhiteSpace(importObject.FileName) && externalSource != null)
        {
            importObject.FileName = InferFileNameFromMediaSource(externalSource);
            _logger.LogDebug("Inferred fileName '{FileName}' from mediaSource", importObject.FileName);
        }

        return importObject;
    }

    public virtual MediaImportResult ImportSingleMediaItem(MediaImportObject importObject, Stream fileStream, bool publish = false)
    {
        var result = new MediaImportResult
        {
            BulkUploadFileName = importObject.FileName,
            BulkUploadSuccess = false,
            BulkUploadLegacyId = importObject.BulkUploadLegacyId,
            BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
            BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted
        };

        try
        {
            if (!importObject.CanImport)
            {
                result.BulkUploadErrorMessage = "Invalid import object: Missing required fields";
                return result;
            }

            IMedia? mediaItem;

            // Determine operation mode based on row values
            // If bulkUploadShouldUpdate column existed in CSV (per-file), then update mode is available
            // The row's bulkUploadShouldUpdate value (per-row) determines if THIS row should update
            if (importObject.BulkUploadShouldUpdate && importObject.BulkUploadMediaGuid.HasValue)
            {
                // ===== UPDATE MODE (for this row) =====
                mediaItem = _mediaService.GetById(importObject.BulkUploadMediaGuid.Value);
                if (mediaItem == null)
                {
                    result.BulkUploadErrorMessage = $"Media with GUID {importObject.BulkUploadMediaGuid.Value} not found";
                    return result;
                }

                _logger.LogInformation("Updating existing media: {Name} (GUID: {Guid})",
                    mediaItem.Name, importObject.BulkUploadMediaGuid.Value);

                // Update name if provided and different
                if (!string.IsNullOrWhiteSpace(importObject.Name) && mediaItem.Name != importObject.Name)
                {
                    var oldName = mediaItem.Name;
                    mediaItem.Name = importObject.Name;
                    _logger.LogInformation("Updated media name from '{OldName}' to '{NewName}'",
                        oldName, importObject.Name);
                }

                // Move to new parent if specified
                if (!string.IsNullOrWhiteSpace(importObject.Parent))
                {
                    var newParent = ResolveParent(importObject.Parent);
                    int newParentId;

                    if (newParent is Guid parentGuid)
                    {
                        if (parentGuid == Guid.Empty)
                        {
                            newParentId = UmbracoConstants.System.Root;
                        }
                        else
                        {
                            var newParentMedia = _mediaService.GetById(parentGuid);
                            if (newParentMedia == null)
                            {
                                result.BulkUploadErrorMessage = $"Parent with GUID {parentGuid} not found";
                                return result;
                            }
                            newParentId = newParentMedia.Id;
                        }
                    }
                    else if (newParent is int parentId)
                    {
                        newParentId = parentId;
                    }
                    else
                    {
                        result.BulkUploadErrorMessage = "Invalid parent type resolved";
                        return result;
                    }

                    // Move media to new parent if different
                    if (mediaItem.ParentId != newParentId)
                    {
                        _mediaService.Move(mediaItem, newParentId);
                        _logger.LogInformation("Moved media '{Name}' to new parent {ParentId}",
                            mediaItem.Name, newParentId);
                    }
                }

                // Upload file if provided (optional for property-only updates)
                if (fileStream != null && fileStream.Length > 0)
                {
                    fileStream.Position = 0;
                    mediaItem.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper,
                        _contentTypeBaseServiceProvider, UmbracoConstants.Conventions.Media.File,
                        importObject.FileName, fileStream);
                    _logger.LogDebug("Updated media file for '{Name}'", mediaItem.Name);
                }
                else
                {
                    _logger.LogDebug("Skipping file upload for '{Name}' (property-only update)", mediaItem.Name);
                }
            }
            else
            {
                // ===== CREATE MODE (for this row) =====
                // Resolve parent specification to media parent (GUID or int)
                var parent = ResolveParent(importObject.Parent);
                _logger.LogDebug("Resolved parent '{Parent}' to {ParentType} {ParentValue}",
                    importObject.Parent, parent.GetType().Name, parent);

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
                    result.BulkUploadErrorMessage = $"Media type '{mediaTypeAlias}' not found";
                    return result;
                }

                // Check if media already exists with same name under parent
                // For querying, we need to use integer ID (GetPagedChildren doesn't support GUID in all versions)
                int queryParentId;
                if (parent is Guid parentGuid)
                {
                    if (parentGuid == Guid.Empty)
                    {
                        queryParentId = UmbracoConstants.System.Root;
                    }
                    else
                    {
                        var parentMedia = _mediaService.GetById(parentGuid);
                        if (parentMedia == null)
                        {
                            result.BulkUploadErrorMessage = $"Parent with GUID {parentGuid} not found";
                            return result;
                        }
                        queryParentId = parentMedia.Id;
                    }
                }
                else if (parent is int parentId)
                {
                    queryParentId = parentId;
                }
                else
                {
                    result.BulkUploadErrorMessage = "Invalid parent type resolved";
                    return result;
                }

                // Use GUID-based or int-based Create depending on parent type
                if (parent is Guid guid)
                {
                    mediaItem = guid == Guid.Empty
                        ? _mediaService.CreateMedia(importObject.DisplayName, UmbracoConstants.System.Root, mediaTypeAlias)
                        : _mediaService.CreateMedia(importObject.DisplayName, guid, mediaTypeAlias);
                }
                else if (parent is int id)
                {
                    mediaItem = _mediaService.CreateMedia(importObject.DisplayName, id, mediaTypeAlias);
                }
                else
                {
                    result.BulkUploadErrorMessage = "Invalid parent type for media creation";
                    return result;
                }
                _logger.LogInformation("Creating new media: {Name}", importObject.DisplayName);

                // Upload the file (required for create mode)
                if (fileStream != null && fileStream.Length > 0)
                {
                    fileStream.Position = 0;

                    // Use the proper Umbraco extension method for setting media files
                    // This handles file upload, path generation, and proper JSON structure creation
                    mediaItem.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper,
                        _contentTypeBaseServiceProvider, UmbracoConstants.Conventions.Media.File,
                        importObject.FileName, fileStream);
                }
                else
                {
                    result.BulkUploadErrorMessage = "File stream is null or empty (required for new media)";
                    return result;
                }
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
                result.BulkUploadSuccess = true;
                result.BulkUploadMediaGuid = mediaItem.Key;
                result.BulkUploadMediaUdi = Udi.Create(UmbracoConstants.UdiEntityType.Media, mediaItem.Key).ToString();
                _logger.LogInformation("Successfully imported media: {Name} (ID: {Id}), Umbraco Media Name: {SavedName}", importObject.DisplayName, mediaItem.Id, mediaItem.Name);
            }
            else
            {
                result.BulkUploadErrorMessage = "Failed to save media item";
                _logger.LogError("Failed to save media: {Name}", importObject.DisplayName);
            }
        }
        catch (Exception ex)
        {
            result.BulkUploadErrorMessage = ex.Message;
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
    /// Resolves the parent folder specification to a media parent (GUID or integer ID for .NET 8).
    /// Supports integer ID (.NET 8 only), GUID, or path (with auto-creation).
    /// Uses caching for improved performance.
    /// Returns either Guid or int depending on input and framework version.
    /// </summary>
    public object ResolveParent(string? parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return UmbracoConstants.System.Root;
        }

        // Try to parse as GUID - use directly without lookup for modern Umbraco compatibility
        if (Guid.TryParse(parent, out var guid))
        {
            _logger.LogDebug("Using parent GUID {Guid} directly", guid);
            return guid;
        }

        // Try to parse as integer ID - only use for .NET 8 compatibility
        if (int.TryParse(parent, out var parentId))
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
        var folderGuid = _parentLookupCache.GetOrCreateMediaFolderByPath(parent);
        if (folderGuid.HasValue)
        {
            _logger.LogDebug("Resolved parent path '{Path}' to GUID {Guid}", parent, folderGuid.Value);
            return folderGuid.Value;
        }

        _logger.LogWarning("Could not resolve parent path '{Path}', using root folder", parent);
        return UmbracoConstants.System.Root;
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

    /// <summary>
    /// Auto-detects the MediaSourceType from the value pattern.
    /// URLs (http:// or https://) are detected as Url.
    /// Absolute paths (contains : for Windows or starts with / for Unix) are detected as FilePath.
    /// Everything else (relative paths) is detected as ZipFile.
    /// </summary>
    private MediaSourceType DetectMediaSourceType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MediaSourceType.ZipFile;
        }

        var trimmedValue = value.Trim();

        // Check for URL (http:// or https://)
        if (trimmedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return MediaSourceType.Url;
        }

        // Check for absolute file path
        // Windows: Contains colon (e.g., C:\, D:\)
        // Unix/Linux: Starts with forward slash (e.g., /home/user/)
        if (trimmedValue.Contains(':') || trimmedValue.StartsWith('/'))
        {
            return MediaSourceType.FilePath;
        }

        // Default to ZipFile for relative paths (e.g., "image.jpg", "folder/image.jpg")
        return MediaSourceType.ZipFile;
    }

    /// <summary>
    /// Infers the fileName from a MediaSource value.
    /// For URLs: extracts the filename from the URL path.
    /// For FilePaths: extracts the filename from the path.
    /// For ZipFile: uses the value directly as it's already the filename/relative path.
    /// </summary>
    private string InferFileNameFromMediaSource(MediaSource mediaSource)
    {
        if (mediaSource == null || string.IsNullOrWhiteSpace(mediaSource.Value))
        {
            return "";
        }

        switch (mediaSource.Type)
        {
            case MediaSourceType.Url:
                try
                {
                    var uri = new Uri(mediaSource.Value);
                    var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.LocalPath));
                    return !string.IsNullOrWhiteSpace(fileName) ? fileName : "download.jpg";
                }
                catch
                {
                    return "download.jpg";
                }

            case MediaSourceType.FilePath:
                // Handle both Windows (\) and Unix (/) path separators
                return GetFileNameFromPath(mediaSource.Value);

            case MediaSourceType.ZipFile:
                // For zip files, the value is already the filename or relative path
                // Extract just the filename if it contains path separators
                return GetFileNameFromPath(mediaSource.Value);

            default:
                return "";
        }
    }

    /// <summary>
    /// Extracts the filename from a path, handling both Windows (\) and Unix (/) path separators.
    /// This is needed because Path.GetFileName() only recognizes the OS-specific separator.
    /// </summary>
    private string GetFileNameFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        // Find the last occurrence of either path separator
        var lastBackslash = path.LastIndexOf('\\');
        var lastForwardSlash = path.LastIndexOf('/');
        var lastSeparator = Math.Max(lastBackslash, lastForwardSlash);

        // If no separator found, the entire string is the filename
        if (lastSeparator == -1)
        {
            return path;
        }

        // Return everything after the last separator
        return path.Substring(lastSeparator + 1);
    }
}