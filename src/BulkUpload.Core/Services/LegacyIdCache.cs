using System.Collections.Concurrent;

namespace BulkUpload.Core.Services;

/// <summary>
/// Thread-safe cache for mapping legacy CMS identifiers to Umbraco content GUIDs.
/// Enables parent relationship resolution during bulk imports from legacy systems.
/// </summary>
public class LegacyIdCache : ILegacyIdCache
{
    private readonly ConcurrentDictionary<string, Guid> _cache;

    public LegacyIdCache()
    {
        // Use case-insensitive comparison for legacy IDs to be more forgiving
        _cache = new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool TryAdd(string legacyId, Guid umbracoGuid)
    {
        if (string.IsNullOrWhiteSpace(legacyId))
        {
            return false;
        }

        return _cache.TryAdd(legacyId, umbracoGuid);
    }

    /// <inheritdoc />
    public bool TryGetGuid(string legacyId, out Guid umbracoGuid)
    {
        if (string.IsNullOrWhiteSpace(legacyId))
        {
            umbracoGuid = Guid.Empty;
            return false;
        }

        return _cache.TryGetValue(legacyId, out umbracoGuid);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
    }

    /// <inheritdoc />
    public int Count => _cache.Count;
}