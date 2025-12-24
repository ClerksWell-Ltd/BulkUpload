namespace Umbraco.Community.BulkUpload.Models;

public class ImportObject
{
    public required string ContentTypeAlais { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// Parent folder specification - can be:
    /// - Integer ID (e.g., "1234")
    /// - GUID (e.g., "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
    /// - Path (e.g., "/Blog/Articles/") - resolves to existing folders
    /// </summary>
    public string? Parent { get; set; }

    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Optional: Legacy CMS identifier for this item.
    /// Used to preserve identifiers from legacy systems and enable hierarchy mapping.
    /// This is NOT persisted as a content property.
    /// </summary>
    public string? LegacyId { get; set; }

    /// <summary>
    /// Optional: Legacy CMS parent identifier.
    /// Used to resolve parent relationships when importing from legacy systems.
    /// Overrides the Parent property if present and valid.
    /// This is NOT persisted as a content property.
    /// </summary>
    public string? LegacyParentId { get; set; }

    /// <summary>
    /// Indicates whether the content item should be published after saving.
    /// Defaults to false if not specified in the CSV.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool ShouldPublish { get; set; } = false;

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }

    public bool CanImport => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(ContentTypeAlais);
}