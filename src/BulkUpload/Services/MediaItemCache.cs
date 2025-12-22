using System.Collections.Concurrent;

namespace BulkUpload.Services;

/// <summary>
/// Thread-safe cache for storing media item references to avoid duplicate creation.
/// Maps original column values (URLs or file paths) to their created media item GUIDs.
/// </summary>
public class MediaItemCache : IMediaItemCache
{
    private readonly ConcurrentDictionary<string, Guid> _cache;

    public MediaItemCache()
    {
        _cache = new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to add a media reference to the cache.
    /// </summary>
    /// <param name="originalValue">The original value from the CSV column (URL or file path)</param>
    /// <param name="mediaGuid">The GUID of the created media item</param>
    /// <returns>True if the value was added, false if it already existed</returns>
    public bool TryAdd(string originalValue, Guid mediaGuid)
    {
        if (string.IsNullOrWhiteSpace(originalValue))
            return false;

        return _cache.TryAdd(originalValue.Trim(), mediaGuid);
    }

    /// <summary>
    /// Attempts to retrieve a media item GUID from the cache.
    /// </summary>
    /// <param name="originalValue">The original value from the CSV column (URL or file path)</param>
    /// <param name="mediaGuid">The GUID of the previously created media item</param>
    /// <returns>True if found in cache, false otherwise</returns>
    public bool TryGetGuid(string originalValue, out Guid mediaGuid)
    {
        mediaGuid = Guid.Empty;

        if (string.IsNullOrWhiteSpace(originalValue))
            return false;

        return _cache.TryGetValue(originalValue.Trim(), out mediaGuid);
    }

    /// <summary>
    /// Clears all cached media references.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the number of cached media references.
    /// </summary>
    public int Count => _cache.Count;
}
