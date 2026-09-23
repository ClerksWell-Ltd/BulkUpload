namespace BulkUpload.Constants;

/// <summary>
/// Defines reserved column names that are used for import metadata
/// and must not be mapped to Umbraco content properties.
/// </summary>
public static class ReservedColumns
{
    /// <summary>
    /// Column containing the legacy CMS identifier for the current item.
    /// Used to preserve identifiers from legacy systems during import.
    /// </summary>
    public const string BulkUploadLegacyId = "bulkUploadLegacyId";

    /// <summary>
    /// Column containing the legacy CMS parent identifier.
    /// Used to resolve parent-child relationships from legacy systems.
    /// </summary>
    public const string BulkUploadLegacyParentId = "bulkUploadLegacyParentId";

    /// <summary>
    /// Column indicating whether to publish the content item.
    /// Governs publishing only, independently of bulkUploadShouldUpdate: a truthy value publishes the
    /// item, an absent or falsy value leaves its publish state unchanged (new content is saved as a draft).
    /// Accepts true/yes/1 as truthy.
    /// </summary>
    public const string BulkUploadShouldPublish = "bulkUploadShouldPublish";

    /// <summary>
    /// Column indicating whether to unpublish the content item.
    /// Governs unpublishing only, independently of bulkUploadShouldUpdate: a truthy value unpublishes
    /// the item if it is published, an absent or falsy value leaves its publish state unchanged.
    /// Wins over bulkUploadShouldPublish when both are truthy. New content is saved as a draft.
    /// Accepts true/yes/1 as truthy.
    /// </summary>
    public const string BulkUploadShouldUnpublish = "bulkUploadShouldUnpublish";

    /// <summary>
    /// Column containing the Umbraco content GUID for updating existing content.
    /// When present, the import will update the existing content item instead of creating new.
    /// </summary>
    public const string BulkUploadContentGuid = "bulkUploadContentGuid";

    /// <summary>
    /// Column containing the Umbraco parent content GUID for moving content.
    /// When present with bulkUploadContentGuid, the content will be moved to this parent.
    /// </summary>
    public const string BulkUploadParentGuid = "bulkUploadParentGuid";

    /// <summary>
    /// Column containing the Umbraco media GUID for updating existing media.
    /// When present with bulkUploadShouldUpdate=true, the import will update the existing media item.
    /// </summary>
    public const string BulkUploadMediaGuid = "bulkUploadMediaGuid";

    /// <summary>
    /// Column indicating whether to write data to an existing item (per-row decision).
    /// Content: the only column that lets the name, parent and property values of an existing item
    /// (identified by bulkUploadContentGuid) be written. On a row without bulkUploadContentGuid a
    /// falsy value skips the row entirely.
    /// Media: when true with bulkUploadMediaGuid, updates the existing media item; when false or
    /// missing bulkUploadMediaGuid, creates new media.
    /// </summary>
    public const string BulkUploadShouldUpdate = "bulkUploadShouldUpdate";

    /// <summary>
    /// Result column indicating whether the import of this row was successful.
    /// Added to the results CSV after import. Contains true/false.
    /// </summary>
    public const string BulkUploadSuccess = "bulkUploadSuccess";

    /// <summary>
    /// Gets all reserved column names that should be excluded from property mapping.
    /// </summary>
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        BulkUploadLegacyId,
        BulkUploadLegacyParentId,
        BulkUploadShouldPublish,
        BulkUploadShouldUnpublish,
        BulkUploadContentGuid,
        BulkUploadParentGuid,
        BulkUploadMediaGuid,
        BulkUploadShouldUpdate,
        BulkUploadSuccess
    };

    /// <summary>
    /// Checks if a column name is reserved and should not be mapped to a content property.
    /// </summary>
    /// <param name="columnName">The column name to check (case-insensitive).</param>
    /// <returns>True if the column is reserved, false otherwise.</returns>
    public static bool IsReserved(string columnName)
    {
        return All.Contains(columnName);
    }
}