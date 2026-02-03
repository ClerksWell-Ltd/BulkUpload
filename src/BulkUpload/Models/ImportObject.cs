namespace BulkUpload.Models;

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
    /// Optional: List of legacy IDs that this content item depends on via content picker properties.
    /// Used to determine import order - all referenced content must be created before this item.
    /// This is NOT persisted as a content property.
    /// </summary>
    public List<string>? ContentPickerDependencies { get; set; }

    /// <summary>
    /// Optional: Properties that use deferred resolvers (e.g., content pickers by legacy ID).
    /// These properties are resolved during ImportSingleItem after dependencies are satisfied.
    /// Key: property alias, Value: tuple of (raw CSV value, resolver alias)
    /// This is NOT persisted as a content property.
    /// </summary>
    public Dictionary<string, (object value, string resolverAlias)>? DeferredProperties { get; set; }

    /// <summary>
    /// Optional: Umbraco content GUID for updating existing content.
    /// When present, the import will update the existing content item instead of creating new.
    /// This is NOT persisted as a content property.
    /// </summary>
    public Guid? BulkUploadContentGuid { get; set; }

    /// <summary>
    /// Optional: Umbraco parent content GUID for moving content.
    /// When present with BulkUploadContentGuid, the content will be moved to this parent.
    /// This is NOT persisted as a content property.
    /// </summary>
    public Guid? BulkUploadParentGuid { get; set; }

    /// <summary>
    /// Indicates whether the content item should be published after saving.
    /// Defaults to false if not specified in the CSV.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool ShouldPublish { get; set; } = false;

    /// <summary>
    /// Per-row flag indicating whether to update this specific content item.
    /// When false, creates new content (default behavior).
    /// When true with BulkUploadContentGuid, updates the existing content item.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool BulkUploadShouldPublish { get; set; } = false;

    /// <summary>
    /// Per-file flag tracking whether the bulkUploadShouldUpdate column existed in the CSV.
    /// When true, the import file supports update mode (column is present).
    /// Individual rows still use BulkUploadShouldUpdate value to determine update vs create.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool BulkUploadShouldPublishColumnExisted { get; set; } = false;

    /// <summary>
    /// Per-row flag indicating whether to update this specific content item.
    /// When false, creates new content (default behavior).
    /// When true with BulkUploadContentGuid, updates the existing content item.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool BulkUploadShouldUpdate { get; set; } = false;

    /// <summary>
    /// Per-file flag tracking whether the bulkUploadShouldUpdate column existed in the CSV.
    /// When true, the import file supports update mode (column is present).
    /// Individual rows still use BulkUploadShouldUpdate value to determine update vs create.
    /// This is NOT persisted as a content property.
    /// </summary>
    public bool BulkUploadShouldUpdateColumnExisted { get; set; } = false;

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }

    /// <summary>
    /// Source CSV filename (without path) that this record came from.
    /// Used for grouping results by source file in exports.
    /// </summary>
    public string? SourceCsvFileName { get; set; }


    // Updated: allow import when updating by GUID and update flag even if Name or ContentTypeAlais are missing
    public bool CanImport => (BulkUploadContentGuid.HasValue && BulkUploadShouldUpdate)
        || (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ContentTypeAlais));
}