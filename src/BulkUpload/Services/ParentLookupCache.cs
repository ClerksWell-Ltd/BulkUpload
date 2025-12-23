using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.BulkUpload.Services;

/// <summary>
/// Provides in-memory caching for parent lookups to optimize bulk operations.
/// Uses ConcurrentDictionary for thread-safe access.
/// Stores GUIDs for compatibility with modern Umbraco versions.
/// </summary>
public class ParentLookupCache : IParentLookupCache
{
    private readonly IMediaService _mediaService;
    private readonly IContentService _contentService;
    private readonly ILogger<ParentLookupCache> _logger;

    // Cache for path to GUID mappings
    private readonly ConcurrentDictionary<string, Guid> _mediaPathCache = new();
    private readonly ConcurrentDictionary<string, Guid> _contentPathCache = new();

    public ParentLookupCache(
        IMediaService mediaService,
        IContentService contentService,
        ILogger<ParentLookupCache> logger)
    {
        _mediaService = mediaService;
        _contentService = contentService;
        _logger = logger;
    }

    public Guid? GetOrCreateMediaFolderByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path for cache key consistency
        var normalizedPath = NormalizePath(path);

        // Check cache first
        if (_mediaPathCache.TryGetValue(normalizedPath, out var cachedGuid))
        {
            _logger.LogDebug("Cache hit: Media path '{Path}' -> GUID {Guid}", normalizedPath, cachedGuid);
            return cachedGuid;
        }

        // Not in cache, resolve or create folder structure
        var folderGuid = ResolveOrCreateMediaFolderPath(normalizedPath);
        if (folderGuid.HasValue)
        {
            _mediaPathCache.TryAdd(normalizedPath, folderGuid.Value);
            _logger.LogDebug("Cache miss: Media path '{Path}' -> GUID {Guid} (cached)", normalizedPath, folderGuid.Value);
        }

        return folderGuid;
    }

    public Guid? GetOrCreateContentFolderByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path for cache key consistency
        var normalizedPath = NormalizePath(path);

        // Check cache first
        if (_contentPathCache.TryGetValue(normalizedPath, out var cachedGuid))
        {
            _logger.LogDebug("Cache hit: Content path '{Path}' -> GUID {Guid}", normalizedPath, cachedGuid);
            return cachedGuid;
        }

        // Not in cache, resolve folder structure
        var folderGuid = ResolveOrCreateContentFolderPath(normalizedPath);
        if (folderGuid.HasValue)
        {
            _contentPathCache.TryAdd(normalizedPath, folderGuid.Value);
            _logger.LogDebug("Cache miss: Content path '{Path}' -> GUID {Guid} (cached)", normalizedPath, folderGuid.Value);
        }

        return folderGuid;
    }

    public void Clear()
    {
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
    /// Resolves a media folder path like "/Blog/Header Images/" to a media folder GUID.
    /// Creates folders if they don't exist.
    /// </summary>
    private Guid? ResolveOrCreateMediaFolderPath(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Guid currentParentGuid = Guid.Empty; // Root uses Guid.Empty
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
            if (_mediaPathCache.TryGetValue(currentPath.ToLowerInvariant(), out var cachedSubPathGuid))
            {
                currentParentGuid = cachedSubPathGuid;
                continue;
            }

            // Look for existing folder with this name under current parent
            // Convert GUID to ID for querying (GetPagedChildren doesn't support GUID in all versions)
            int currentParentId;
            if (currentParentGuid == Guid.Empty)
            {
                currentParentId = Constants.System.Root;
            }
            else
            {
                var parentMedia = _mediaService.GetById(currentParentGuid);
                if (parentMedia == null)
                {
                    _logger.LogError("Could not find media with GUID {Guid}", currentParentGuid);
                    return null;
                }
                currentParentId = parentMedia.Id;
            }

            var children = _mediaService.GetPagedChildren(currentParentId, 0, int.MaxValue, out _);
            var existingFolder = children.FirstOrDefault(x =>
                x.ContentType.Alias == "Folder" &&
                string.Equals(x.Name, folderName, StringComparison.InvariantCultureIgnoreCase));

            if (existingFolder != null)
            {
                currentParentGuid = existingFolder.Key;
                // Cache this sub-path
                _mediaPathCache.TryAdd(currentPath.ToLowerInvariant(), existingFolder.Key);
                _logger.LogDebug("Found existing media folder '{FolderName}' with GUID {Guid}", folderName, existingFolder.Key);
            }
            else
            {
                // Create new folder using GUID (for modern Umbraco compatibility)
                IMedia newFolder;
                if (currentParentGuid == Guid.Empty)
                {
                    newFolder = _mediaService.CreateMedia(folderName, Constants.System.Root, "Folder");
                }
                else
                {
                    newFolder = _mediaService.CreateMedia(folderName, currentParentGuid, "Folder");
                }

                var saveResult = _mediaService.Save(newFolder);

                if (saveResult.Success)
                {
                    currentParentGuid = newFolder.Key;
                    // Cache this sub-path
                    _mediaPathCache.TryAdd(currentPath.ToLowerInvariant(), newFolder.Key);
                    _logger.LogInformation("Created new media folder '{FolderName}' with GUID {Guid}", folderName, newFolder.Key);
                }
                else
                {
                    _logger.LogError("Failed to create media folder '{FolderName}'", folderName);
                    return null;
                }
            }
        }

        return currentParentGuid == Guid.Empty ? null : currentParentGuid;
    }

    /// <summary>
    /// Resolves a content folder path like "/Blog/Articles/" to a content folder GUID.
    /// Resolves to existing folders (does not create).
    /// </summary>
    private Guid? ResolveOrCreateContentFolderPath(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Guid currentParentGuid = Guid.Empty; // Root uses Guid.Empty
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
            if (_contentPathCache.TryGetValue(currentPath.ToLowerInvariant(), out var cachedSubPathGuid))
            {
                currentParentGuid = cachedSubPathGuid;
                continue;
            }

            // Look for existing folder with this name under current parent
            // Convert GUID to ID for querying (GetPagedChildren doesn't support GUID in all versions)
            int currentParentId;
            if (currentParentGuid == Guid.Empty)
            {
                currentParentId = Constants.System.Root;
            }
            else
            {
                var parentContent = _contentService.GetById(currentParentGuid);
                if (parentContent == null)
                {
                    _logger.LogError("Could not find content with GUID {Guid}", currentParentGuid);
                    return null;
                }
                currentParentId = parentContent.Id;
            }

            var children = _contentService.GetPagedChildren(currentParentId, 0, int.MaxValue, out _);
            var existingFolder = children.FirstOrDefault(x =>
                string.Equals(x.Name, folderName, StringComparison.InvariantCultureIgnoreCase));

            if (existingFolder != null)
            {
                currentParentGuid = existingFolder.Key;
                // Cache this sub-path
                _contentPathCache.TryAdd(currentPath.ToLowerInvariant(), existingFolder.Key);
                _logger.LogDebug("Found existing content folder '{FolderName}' with GUID {Guid}", folderName, existingFolder.Key);
            }
            else
            {
                // For content, we'll just log a warning and return null
                // Content folder creation is more complex as we need to know the content type
                _logger.LogWarning("Content folder '{FolderName}' not found and auto-creation not supported for content", folderName);
                return null;
            }
        }

        return currentParentGuid == Guid.Empty ? null : currentParentGuid;
    }
}