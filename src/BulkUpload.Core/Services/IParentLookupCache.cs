namespace Umbraco.Community.BulkUpload.Core.Services;

/// <summary>
/// Provides caching for parent lookups by path to improve performance
/// during bulk operations.
/// </summary>
public interface IParentLookupCache
{
    /// <summary>
    /// Gets a media folder GUID by path, using cache if available.
    /// Creates folders if they don't exist.
    /// </summary>
    /// <param name="path">The folder path (e.g., "/Blog/Images/")</param>
    /// <returns>The media folder GUID if found or created, null otherwise</returns>
    Guid? GetOrCreateMediaFolderByPath(string path);

    /// <summary>
    /// Gets a content folder GUID by path, using cache if available.
    /// Resolves to existing folders (does not create).
    /// </summary>
    /// <param name="path">The folder path (e.g., "/Blog/Articles/")</param>
    /// <returns>The content folder GUID if found, null otherwise</returns>
    Guid? GetOrCreateContentFolderByPath(string path);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();
}