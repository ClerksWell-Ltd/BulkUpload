namespace BulkUpload.Core.Services;

/// <summary>
/// Provides caching for legacy CMS identifiers to Umbraco content GUIDs.
/// Used during bulk import to resolve parent relationships based on legacy IDs.
/// </summary>
public interface ILegacyIdCache
{
    /// <summary>
    /// Attempts to add a legacy ID to Umbraco GUID mapping to the cache.
    /// </summary>
    /// <param name="legacyId">The legacy CMS identifier.</param>
    /// <param name="umbracoGuid">The Umbraco content GUID (Key).</param>
    /// <returns>True if the mapping was added; false if the legacy ID already exists.</returns>
    bool TryAdd(string legacyId, Guid umbracoGuid);

    /// <summary>
    /// Attempts to retrieve the Umbraco GUID for a given legacy ID.
    /// </summary>
    /// <param name="legacyId">The legacy CMS identifier to look up.</param>
    /// <param name="umbracoGuid">When this method returns, contains the Umbraco GUID if found; otherwise, Guid.Empty.</param>
    /// <returns>True if the legacy ID was found; false otherwise.</returns>
    bool TryGetGuid(string legacyId, out Guid umbracoGuid);

    /// <summary>
    /// Clears all cached legacy ID mappings.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of cached legacy ID mappings.
    /// </summary>
    int Count { get; }
}