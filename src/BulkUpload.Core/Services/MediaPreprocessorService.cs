using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using BulkUpload.Core.Models;
using BulkUpload.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Core.Services;

/// <summary>
/// Service responsible for preprocessing media items before content import.
/// Identifies all unique media references, creates them once, and caches them.
/// </summary>
public class MediaPreprocessorService : IMediaPreprocessorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILogger<MediaPreprocessorService> _logger;

    public MediaPreprocessorService(
        IHttpClientFactory httpClientFactory,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILogger<MediaPreprocessorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _logger = logger;
    }

    /// <summary>
    /// Preprocesses media items from CSV records.
    /// Extracts all unique media references, creates them, and caches them.
    /// </summary>
    /// <param name="csvRecordsWithSource">CSV records with their source filenames to extract media references from</param>
    /// <param name="zipExtractDirectory">Optional directory where ZIP was extracted (for zipFileToMedia resolver)</param>
    /// <returns>List of media preprocessing results containing cache keys and values</returns>
    public List<MediaPreprocessingResult> PreprocessMediaItems(List<(dynamic record, string sourceFileName)> csvRecordsWithSource, string? zipExtractDirectory = null)
    {
        _logger.LogInformation("Starting media preprocessing for {Count} CSV records", csvRecordsWithSource.Count);

        // Extract all unique media references
        var mediaReferences = ExtractMediaReferences(csvRecordsWithSource, zipExtractDirectory);

        _logger.LogInformation("Found {Count} unique media references to create", mediaReferences.Count);

        // Create media items and cache them
        var results = new List<MediaPreprocessingResult>();
        int successCount = 0;
        int failureCount = 0;

        foreach (var mediaRef in mediaReferences)
        {
            try
            {
                Guid mediaGuid = mediaRef.Type switch
                {
                    MediaReferenceType.Url => CreateMediaFromUrl(mediaRef),
                    MediaReferenceType.Path => CreateMediaFromPath(mediaRef),
                    MediaReferenceType.ZipFile => CreateMediaFromZipFile(mediaRef, zipExtractDirectory!),
                    _ => throw new InvalidOperationException($"Unknown media reference type: {mediaRef.Type}")
                };

                if (mediaGuid != Guid.Empty)
                {
                    _mediaItemCache.TryAdd(mediaRef.OriginalValue, mediaGuid);
                    successCount++;
                    _logger.LogDebug("Created and cached media: {Value} → {Guid}", mediaRef.OriginalValue, mediaGuid);

                    results.Add(new MediaPreprocessingResult
                    {
                        Key = mediaRef.OriginalValue,
                        Value = mediaGuid,
                        Success = true,
                        FileName = mediaRef.FileName,
                        SourceCsvFileName = mediaRef.SourceCsvFileName
                    });
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning("Failed to create media for: {Value}", mediaRef.OriginalValue);

                    results.Add(new MediaPreprocessingResult
                    {
                        Key = mediaRef.OriginalValue,
                        Value = null,
                        Success = false,
                        ErrorMessage = "Failed to create media item",
                        FileName = mediaRef.FileName,
                        SourceCsvFileName = mediaRef.SourceCsvFileName
                    });
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Error creating media for: {Value}", mediaRef.OriginalValue);

                results.Add(new MediaPreprocessingResult
                {
                    Key = mediaRef.OriginalValue,
                    Value = null,
                    Success = false,
                    ErrorMessage = ex.Message,
                    FileName = mediaRef.FileName,
                    SourceCsvFileName = mediaRef.SourceCsvFileName
                });
            }
        }

        _logger.LogInformation("Media preprocessing completed. Success: {Success}, Failures: {Failures}",
            successCount, failureCount);

        return results;
    }

    /// <summary>
    /// Extracts all unique media references from CSV records.
    /// </summary>
    private List<MediaReference> ExtractMediaReferences(List<(dynamic record, string sourceFileName)> csvRecordsWithSource, string? zipExtractDirectory)
    {
        var mediaReferences = new Dictionary<string, MediaReference>(StringComparer.OrdinalIgnoreCase);

        foreach (var (record, sourceFileName) in csvRecordsWithSource)
        {
            var dynamicProperties = (IDictionary<string, object>)record;

            foreach (var property in dynamicProperties)
            {
                var columnDetails = property.Key.Split('|');

                // Skip standard columns
                var standardColumns = new string[] { "name", "parent", "parentId", "docTypeAlias" };
                if (standardColumns.Contains(columnDetails[0]))
                    continue;

                // Extract resolver alias
                string? resolverAlias = null;
                string? aliasParameter = null;

                if (columnDetails.Length > 1)
                {
                    var resolverPart = columnDetails.Last();
                    var resolverParts = resolverPart.Split(':', 2, StringSplitOptions.None);
                    resolverAlias = resolverParts[0];
                    aliasParameter = resolverParts.Length > 1 ? resolverParts[1] : null;
                }

                // Check if this is a media resolver
                if (resolverAlias != "urlToMedia" && resolverAlias != "pathToMedia" && resolverAlias != "zipFileToMedia")
                    continue;

                // Extract the value
                var valueStr = property.Value?.ToString();
                if (string.IsNullOrWhiteSpace(valueStr))
                    continue;

                // Parse value to extract actual media reference and optional parent parameter
                var valueParts = valueStr.Split('|', 2);
                var mediaValue = valueParts[0]?.Trim();
                var valueParameter = valueParts.Length > 1 ? valueParts[1]?.Trim() : null;

                if (string.IsNullOrWhiteSpace(mediaValue))
                    continue;

                // Determine parent (value parameter takes precedence over alias parameter)
                var parent = valueParameter ?? aliasParameter;

                // Add to unique collection if not already present
                if (!mediaReferences.ContainsKey(mediaValue))
                {
                    var refType = resolverAlias switch
                    {
                        "urlToMedia" => MediaReferenceType.Url,
                        "pathToMedia" => MediaReferenceType.Path,
                        "zipFileToMedia" => MediaReferenceType.ZipFile,
                        _ => MediaReferenceType.Path // Default to Path for unknown
                    };

                    // Extract filename from the original value
                    string? fileName = null;
                    try
                    {
                        if (refType == MediaReferenceType.Url && Uri.TryCreate(mediaValue, UriKind.Absolute, out var uri))
                        {
                            fileName = Path.GetFileName(uri.LocalPath);
                        }
                        else
                        {
                            fileName = Path.GetFileName(mediaValue);
                        }
                    }
                    catch
                    {
                        fileName = mediaValue; // Fallback to original value if extraction fails
                    }

                    mediaReferences[mediaValue] = new MediaReference
                    {
                        OriginalValue = mediaValue,
                        Type = refType,
                        Parent = parent,
                        FileName = fileName,
                        SourceCsvFileName = sourceFileName
                    };
                }
            }
        }

        return mediaReferences.Values.ToList();
    }

    /// <summary>
    /// Creates a media item from a URL.
    /// </summary>
    private Guid CreateMediaFromUrl(MediaReference mediaRef)
    {
        if (!Uri.TryCreate(mediaRef.OriginalValue, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid URL: {Url}", mediaRef.OriginalValue);
            return Guid.Empty;
        }

        try
        {
            // Download file
            using var httpClient = _httpClientFactory.CreateClient();
            var response = httpClient.GetAsync(uri).Result;
            response.EnsureSuccessStatusCode();

            var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"download_{Guid.NewGuid():N}{GetFileExtension(response.Content.Headers.ContentType?.MediaType)}";
            }

            // Resolve parent
            var parent = ResolveParent(mediaRef.Parent);

            // Determine media type
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var mediaTypeAlias = GetMediaTypeAlias(extension);

            // Create media item
            var mediaItem = CreateMediaItem(fileName, parent, mediaTypeAlias);
            if (mediaItem == null)
                return Guid.Empty;

            // Upload file
            UploadMediaFile(mediaItem, fileName, fileBytes);

            _mediaService.Save(mediaItem);

            return mediaItem.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading media from URL: {Url}", mediaRef.OriginalValue);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Creates a media item from a file path.
    /// </summary>
    private Guid CreateMediaFromPath(MediaReference mediaRef)
    {
        if (!System.IO.File.Exists(mediaRef.OriginalValue))
        {
            _logger.LogWarning("File not found: {Path}", mediaRef.OriginalValue);
            return Guid.Empty;
        }

        try
        {
            var fileBytes = System.IO.File.ReadAllBytes(mediaRef.OriginalValue);
            var fileName = Path.GetFileName(mediaRef.OriginalValue);

            // Resolve parent
            var parent = ResolveParent(mediaRef.Parent);

            // Determine media type
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var mediaTypeAlias = GetMediaTypeAlias(extension);

            // Create media item
            var mediaItem = CreateMediaItem(fileName, parent, mediaTypeAlias);
            if (mediaItem == null)
                return Guid.Empty;

            // Upload file
            UploadMediaFile(mediaItem, fileName, fileBytes);

            _mediaService.Save(mediaItem);

            return mediaItem.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading media from path: {Path}", mediaRef.OriginalValue);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Creates a media item from a file in the extracted ZIP archive.
    /// </summary>
    private Guid CreateMediaFromZipFile(MediaReference mediaRef, string zipExtractDirectory)
    {
        // Search for the file in the ZIP extract directory
        var filePaths = Directory.GetFiles(zipExtractDirectory, mediaRef.OriginalValue, SearchOption.AllDirectories);

        if (filePaths.Length == 0)
        {
            _logger.LogWarning("File not found in ZIP archive: {FileName}", mediaRef.OriginalValue);
            return Guid.Empty;
        }

        var filePath = filePaths[0]; // Use first match
        if (filePaths.Length > 1)
        {
            _logger.LogWarning("Multiple files found with name {FileName}, using first: {Path}",
                mediaRef.OriginalValue, filePath);
        }

        try
        {
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileName = Path.GetFileName(filePath);

            // Resolve parent
            var parent = ResolveParent(mediaRef.Parent);

            // Determine media type
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var mediaTypeAlias = GetMediaTypeAlias(extension);

            // Create media item
            var mediaItem = CreateMediaItem(fileName, parent, mediaTypeAlias);
            if (mediaItem == null)
                return Guid.Empty;

            // Upload file
            UploadMediaFile(mediaItem, fileName, fileBytes);

            _mediaService.Save(mediaItem);

            _logger.LogDebug("Created media from ZIP file: {FileName} → {MediaKey}", fileName, mediaItem.Key);

            return mediaItem.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating media from ZIP file: {FileName}", mediaRef.OriginalValue);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Resolves parent folder specification to a GUID or integer ID.
    /// </summary>
    private object ResolveParent(string? parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return UmbracoConstants.System.Root;
        }

        // Try GUID
        if (Guid.TryParse(parent, out var guid))
        {
            return guid;
        }

        // Try integer ID
        if (int.TryParse(parent, out var parentId))
        {
#if NET8_0
            return parentId;
#else
            var media = _mediaService.GetById(parentId);
            if (media != null)
            {
                return media.Key;
            }
            return UmbracoConstants.System.Root;
#endif
        }

        // Treat as path
        var folderGuid = _parentLookupCache.GetOrCreateMediaFolderByPath(parent);
        if (folderGuid.HasValue)
        {
            return folderGuid.Value;
        }

        return UmbracoConstants.System.Root;
    }

    /// <summary>
    /// Creates a media item.
    /// </summary>
    private IMedia? CreateMediaItem(string fileName, object parent, string mediaTypeAlias)
    {
        try
        {
            if (parent is Guid guid)
            {
                return guid == Guid.Empty
                    ? _mediaService.CreateMedia(fileName, UmbracoConstants.System.Root, mediaTypeAlias)
                    : _mediaService.CreateMedia(fileName, guid, mediaTypeAlias);
            }
            else if (parent is int id)
            {
                return _mediaService.CreateMedia(fileName, id, mediaTypeAlias);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating media item: {FileName}", fileName);
            return null;
        }
    }

    /// <summary>
    /// Uploads a file to a media item.
    /// </summary>
    private void UploadMediaFile(IMedia mediaItem, string fileName, byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);

        // Use the proper Umbraco extension method for setting media files
        // This handles file upload, path generation, and proper JSON structure creation
        mediaItem.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper,
            _contentTypeBaseServiceProvider, UmbracoConstants.Conventions.Media.File,
            fileName, stream);
    }

    /// <summary>
    /// Gets the media type alias based on file extension.
    /// </summary>
    private string GetMediaTypeAlias(string extension)
    {
        var imageExtensions = new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp", "svg" };
        var videoExtensions = new[] { "mp4", "avi", "mov", "wmv", "flv", "webm" };
        var audioExtensions = new[] { "mp3", "wav", "ogg", "wma", "flac" };

        if (imageExtensions.Contains(extension.ToLowerInvariant()))
            return "Image";

        if (videoExtensions.Contains(extension.ToLowerInvariant()))
            return "Video";

        if (audioExtensions.Contains(extension.ToLowerInvariant()))
            return "Audio";

        return "File";
    }

    /// <summary>
    /// Gets file extension from content type.
    /// </summary>
    private string GetFileExtension(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return ".bin";

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "video/mp4" => ".mp4",
            "audio/mpeg" => ".mp3",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }
}

/// <summary>
/// Represents a media reference found in the CSV.
/// </summary>
internal class MediaReference
{
    public required string OriginalValue { get; set; }
    public required MediaReferenceType Type { get; set; }
    public string? Parent { get; set; }
    public string? FileName { get; set; }
    public string? SourceCsvFileName { get; set; }
}

/// <summary>
/// Type of media reference.
/// </summary>
internal enum MediaReferenceType
{
    Url,
    Path,
    ZipFile
}