namespace BulkUpload.Core.Resolvers;

using BulkUpload.Core.Services;

/// <summary>
/// Interface for resolvers that depend on the LegacyIdCache and must be resolved
/// after dependent content items are created. Used for content picker and multi-node
/// tree picker properties that reference other content by legacy ID.
/// </summary>
public interface IDeferredResolver : IResolver
{
    /// <summary>
    /// Extracts legacy IDs that this property depends on from the raw value.
    /// Called during import object creation to build the dependency graph.
    /// </summary>
    /// <param name="value">The raw value from the CSV (may be string or other type)</param>
    /// <returns>List of legacy IDs that must be created before this property can be resolved</returns>
    List<string> ExtractDependencies(object value);

    /// <summary>
    /// Resolves the property value using the LegacyIdCache to look up content GUIDs.
    /// Called during content creation after all dependencies have been satisfied.
    /// </summary>
    /// <param name="value">The raw value from the CSV</param>
    /// <param name="legacyIdCache">Cache mapping legacy IDs to Umbraco content GUIDs</param>
    /// <returns>The resolved property value (typically a content UDI or array of UDIs)</returns>
    object ResolveDeferred(object value, ILegacyIdCache legacyIdCache);
}
