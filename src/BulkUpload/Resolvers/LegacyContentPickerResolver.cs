using BulkUpload.Services;

using Umbraco.Cms.Core;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver for content picker properties that reference content by legacy ID.
/// Converts a legacy ID to an Umbraco content UDI after the referenced content is created.
/// Use with column format: propertyAlias|legacyContentPicker
/// </summary>
public class LegacyContentPickerResolver : IDeferredResolver
{
    public string Alias() => "legacyContentPicker";

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
    /// Extracts the legacy ID dependency from the CSV value.
    /// </summary>
    public List<string> ExtractDependencies(object value)
    {
        var dependencies = new List<string>();

        if (value == null)
            return dependencies;

        var legacyId = value.ToString()?.Trim();

        if (!string.IsNullOrWhiteSpace(legacyId))
        {
            dependencies.Add(legacyId);
        }

        return dependencies;
    }

    /// <summary>
    /// Resolves the legacy ID to a content UDI using the LegacyIdCache.
    /// </summary>
    public object ResolveDeferred(object value, ILegacyIdCache legacyIdCache)
    {
        if (value == null)
            return string.Empty;

        var legacyId = value.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(legacyId))
            return string.Empty;

        // Look up the content GUID for this legacy ID
        if (legacyIdCache.TryGetGuid(legacyId, out var contentGuid))
        {
            // Convert GUID to content UDI
            var udi = Udi.Create("document", contentGuid);
            return udi.ToString();
        }

        // If the legacy ID is not found, return empty
        // This could happen if the referenced content failed to import or doesn't exist
        return string.Empty;
    }
}