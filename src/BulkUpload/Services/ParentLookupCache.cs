using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.BulkUpload.Services;

/// <summary>
/// Provides in-memory caching for parent lookups to optimize bulk operations.
/// Uses ConcurrentDictionary for thread-safe access.
/// </summary>
public class ParentLookupCache : IParentLookupCache
{
    private readonly IMediaService _mediaService;
    private readonly IContentService _contentService;
    private readonly ILogger<ParentLookupCache> _logger;

    // Cache for GUID to ID mappings
    private readonly ConcurrentDictionary<Guid, int> _mediaGuidCache = new();
    private readonly ConcurrentDictionary<Guid, int> _contentGuidCache = new();

    // Cache for path to ID mappings
    private readonly ConcurrentDictionary<string, int> _mediaPathCache = new();
    private readonly ConcurrentDictionary<string, int> _contentPathCache = new();

    public ParentLookupCache(
        IMediaService mediaService,
        IContentService contentService,
        ILogger<ParentLookupCache> logger)
    {
        _mediaService = mediaService;
        _contentService = contentService;
        _logger = logger;
    }

    public int? GetMediaIdByGuid(Guid guid)
    {
        // Check cache first
        if (_mediaGuidCache.TryGetValue(guid, out var cachedId))
        {
            _logger.LogDebug("Cache hit: Media GUID {Guid} -> ID {Id}", guid, cachedId);
            return cachedId;
        }

        // Not in cache, look up from service
        var media = _mediaService.GetById(guid);
        if (media != null)
        {
            _mediaGuidCache.TryAdd(guid, media.Id);
            _logger.LogDebug("Cache miss: Media GUID {Guid} -> ID {Id} (cached)", guid, media.Id);
            return media.Id;
        }

        _logger.LogWarning("Media GUID {Guid} not found", guid);
        return null;
    }

    public int? GetContentIdByGuid(Guid guid)
    {
        // Check cache first
        if (_contentGuidCache.TryGetValue(guid, out var cachedId))
        {
            _logger.LogDebug("Cache hit: Content GUID {Guid} -> ID {Id}", guid, cachedId);
            return cachedId;
        }

        // Not in cache, look up from service
        var content = _contentService.GetById(guid);
        if (content != null)
        {
            _contentGuidCache.TryAdd(guid, content.Id);
            _logger.LogDebug("Cache miss: Content GUID {Guid} -> ID {Id} (cached)", guid, content.Id);
            return content.Id;
        }

        _logger.LogWarning("Content GUID {Guid} not found", guid);
        return null;
    }

    public int? GetOrCreateMediaFolderByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path for cache key consistency
        var normalizedPath = NormalizePath(path);

        // Check cache first
        if (_mediaPathCache.TryGetValue(normalizedPath, out var cachedId))
        {
            _logger.LogDebug("Cache hit: Media path '{Path}' -> ID {Id}", normalizedPath, cachedId);
            return cachedId;
        }

        // Not in cache, resolve or create folder structure
        var folderId = ResolveOrCreateMediaFolderPath(normalizedPath);
        if (folderId.HasValue)
        {
            _mediaPathCache.TryAdd(normalizedPath, folderId.Value);
            _logger.LogDebug("Cache miss: Media path '{Path}' -> ID {Id} (cached)", normalizedPath, folderId.Value);
        }

        return folderId;
    }

    public int? GetOrCreateContentFolderByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path for cache key consistency
        var normalizedPath = NormalizePath(path);

        // Check cache first
        if (_contentPathCache.TryGetValue(normalizedPath, out var cachedId))
        {
            _logger.LogDebug("Cache hit: Content path '{Path}' -> ID {Id}", normalizedPath, cachedId);
            return cachedId;
        }

        // Not in cache, resolve or create folder structure
        var folderId = ResolveOrCreateContentFolderPath(normalizedPath);
        if (folderId.HasValue)
        {
            _contentPathCache.TryAdd(normalizedPath, folderId.Value);
            _logger.LogDebug("Cache miss: Content path '{Path}' -> ID {Id} (cached)", normalizedPath, folderId.Value);
        }

        return folderId;
    }

    public void Clear()
    {
        _mediaGuidCache.Clear();
        _contentGuidCache.Clear();
        _mediaPathCache.Clear();
        _contentPathCache.Clear();
        _logger.LogInformation("Parent lookup cache cleared");
    }

    /// <summary>
    /// Normalizes a path for consistent cache key usage.
    /// </summary>
    private string NormalizePath(string path)
    {
        return path.Trim().Trim('/').ToLowerInvariant();
    }

    /// <summary>
    /// Resolves a media folder path like "/Blog/Header Images/" to a media folder ID.
    /// Creates folders if they don't exist.
    /// </summary>
    private int? ResolveOrCreateMediaFolderPath(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return Constants.System.Root;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentParentId = Constants.System.Root;
        var currentPath = "";

        foreach (var segment in segments)
        {
            var folderName = segment.Trim();
            if (string.IsNullOrWhiteSpace(folderName))
            {
                continue;
            }

            // Build cumulative path for sub-path caching
            currentPath = string.IsNullOrEmpty(currentPath) ? folderName : $"{currentPath}/{folderName}";

            // Check if we have this sub-path in cache
            if (_mediaPathCache.TryGetValue(currentPath.ToLowerInvariant(), out var cachedSubPathId))
            {
                currentParentId = cachedSubPathId;
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
                // Cache this sub-path
                _mediaPathCache.TryAdd(currentPath.ToLowerInvariant(), existingFolder.Id);
                _logger.LogDebug("Found existing media folder '{FolderName}' with ID {Id}", folderName, existingFolder.Id);
            }
            else
            {
                // Create new folder
                var newFolder = _mediaService.CreateMedia(folderName, currentParentId, "Folder");
                var saveResult = _mediaService.Save(newFolder);

                if (saveResult.Success)
                {
                    currentParentId = newFolder.Id;
                    // Cache this sub-path
                    _mediaPathCache.TryAdd(currentPath.ToLowerInvariant(), newFolder.Id);
                    _logger.LogInformation("Created new media folder '{FolderName}' with ID {Id}", folderName, newFolder.Id);
                }
                else
                {
                    _logger.LogError("Failed to create media folder '{FolderName}'", folderName);
                    return null;
                }
            }
        }

        return currentParentId;
    }

    /// <summary>
    /// Resolves a content folder path like "/Blog/Articles/" to a content folder ID.
    /// Creates folders if they don't exist.
    /// </summary>
    private int? ResolveOrCreateContentFolderPath(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return Constants.System.Root;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentParentId = Constants.System.Root;
        var currentPath = "";

        foreach (var segment in segments)
        {
            var folderName = segment.Trim();
            if (string.IsNullOrWhiteSpace(folderName))
            {
                continue;
            }

            // Build cumulative path for sub-path caching
            currentPath = string.IsNullOrEmpty(currentPath) ? folderName : $"{currentPath}/{folderName}";

            // Check if we have this sub-path in cache
            if (_contentPathCache.TryGetValue(currentPath.ToLowerInvariant(), out var cachedSubPathId))
            {
                currentParentId = cachedSubPathId;
                continue;
            }

            // Look for existing folder with this name under current parent
            // Note: For content, we need to check for actual folder content types
            // This assumes there's a "Folder" content type, adjust if needed
            var existingFolder = _contentService
                .GetPagedChildren(currentParentId, 0, int.MaxValue, out _)
                .FirstOrDefault(x =>
                    string.Equals(x.Name, folderName, StringComparison.InvariantCultureIgnoreCase));

            if (existingFolder != null)
            {
                currentParentId = existingFolder.Id;
                // Cache this sub-path
                _contentPathCache.TryAdd(currentPath.ToLowerInvariant(), existingFolder.Id);
                _logger.LogDebug("Found existing content folder '{FolderName}' with ID {Id}", folderName, existingFolder.Id);
            }
            else
            {
                // For content, we'll just log a warning and return null
                // Content folder creation is more complex as we need to know the content type
                _logger.LogWarning("Content folder '{FolderName}' not found and auto-creation not supported for content", folderName);
                return null;
            }
        }

        return currentParentId;
    }
}
