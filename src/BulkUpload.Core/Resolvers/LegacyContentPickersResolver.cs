using Umbraco.Cms.Core;
using BulkUpload.Core.Services;

namespace BulkUpload.Core.Resolvers;

/// <summary>
/// Resolver for multi-node tree picker properties that reference multiple content items by legacy ID.
/// Converts comma-separated legacy IDs to comma-separated Umbraco content UDIs.
/// Use with column format: propertyAlias|legacyContentPickers
/// </summary>
public class LegacyContentPickersResolver : IDeferredResolver
{
    public string Alias() => "legacyContentPickers";

    /// <summary>
    /// Standard resolve method - not used for deferred resolvers.
    /// Returns empty string as resolution requires the LegacyIdCache.
    /// </summary>
    public object Resolve(object value)
    {
        // Deferred resolvers cannot resolve without the cache
        // Resolution happens in ResolveDeferred() during content creation
        return string.Empty;
    }

    /// <summary>
    /// Extracts all legacy ID dependencies from the comma-separated CSV value.
    /// </summary>
    public List<string> ExtractDependencies(object value)
    {
        var dependencies = new List<string>();

        if (value == null)
            return dependencies;

        var str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return dependencies;

        // Split by comma and extract each legacy ID
        foreach (var item in str.Split(','))
        {
            var legacyId = item.Trim();

            if (!string.IsNullOrWhiteSpace(legacyId))
            {
                dependencies.Add(legacyId);
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Resolves comma-separated legacy IDs to comma-separated content UDIs using the LegacyIdCache.
    /// </summary>
    public object ResolveDeferred(object value, ILegacyIdCache legacyIdCache)
    {
        if (value == null)
            return string.Empty;

        var str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var udis = new List<string>();

        // Process each comma-separated legacy ID
        foreach (var item in str.Split(','))
        {
            var legacyId = item.Trim();

            if (string.IsNullOrWhiteSpace(legacyId))
                continue;

            // Look up the content GUID for this legacy ID
            if (legacyIdCache.TryGetGuid(legacyId, out var contentGuid))
            {
                // Convert GUID to content UDI
                var udi = Udi.Create(Constants.UdiEntityType.Document, contentGuid);
                udis.Add(udi.ToString());
            }
            // If a legacy ID is not found, skip it
            // This could happen if the referenced content failed to import or doesn't exist
        }

        return string.Join(",", udis);
    }
}
