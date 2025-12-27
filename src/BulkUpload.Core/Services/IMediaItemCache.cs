namespace BulkUpload.Core.Services;

/// <summary>
/// Interface for caching media item references to avoid duplicate creation.
/// </summary>
public interface IMediaItemCache
{
    /// <summary>
    /// Attempts to add a media reference to the cache.
    /// </summary>
    /// <param name="originalValue">The original value from the CSV column (URL or file path)</param>
    /// <param name="mediaGuid">The GUID of the created media item</param>
    /// <returns>True if the value was added, false if it already existed</returns>
    bool TryAdd(string originalValue, Guid mediaGuid);

    /// <summary>
    /// Attempts to retrieve a media item GUID from the cache.
    /// </summary>
    /// <param name="originalValue">The original value from the CSV column (URL or file path)</param>
    /// <param name="mediaGuid">The GUID of the previously created media item</param>
    /// <returns>True if found in cache, false otherwise</returns>
    bool TryGetGuid(string originalValue, out Guid mediaGuid);

    /// <summary>
    /// Clears all cached media references.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of cached media references.
    /// </summary>
    int Count { get; }
}