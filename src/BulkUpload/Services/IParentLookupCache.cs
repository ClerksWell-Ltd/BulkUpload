namespace Umbraco.Community.BulkUpload.Services;

/// <summary>
/// Provides caching for parent lookups by GUID and path to improve performance
/// during bulk operations.
/// </summary>
public interface IParentLookupCache
{
    /// <summary>
    /// Gets a media ID by GUID, using cache if available.
    /// </summary>
    /// <param name="guid">The GUID to look up</param>
    /// <returns>The media ID if found, null otherwise</returns>
    int? GetMediaIdByGuid(Guid guid);

    /// <summary>
    /// Gets a content ID by GUID, using cache if available.
    /// </summary>
    /// <param name="guid">The GUID to look up</param>
    /// <returns>The content ID if found, null otherwise</returns>
    int? GetContentIdByGuid(Guid guid);

    /// <summary>
    /// Gets a media folder ID by path, using cache if available.
    /// Creates folders if they don't exist.
    /// </summary>
    /// <param name="path">The folder path (e.g., "/Blog/Images/")</param>
    /// <returns>The media folder ID if found or created, null otherwise</returns>
    int? GetOrCreateMediaFolderByPath(string path);

    /// <summary>
    /// Gets a content folder ID by path, using cache if available.
    /// Creates folders if they don't exist.
    /// </summary>
    /// <param name="path">The folder path (e.g., "/Blog/Articles/")</param>
    /// <returns>The content folder ID if found or created, null otherwise</returns>
    int? GetOrCreateContentFolderByPath(string path);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();
}
