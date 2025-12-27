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
    /// Column indicating whether to publish the content item after saving.
    /// Accepts true/false values. Defaults to false if not present.
    /// </summary>
    public const string BulkUploadShouldPublish = "bulkUploadShouldPublish";

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
    /// Gets all reserved column names that should be excluded from property mapping.
    /// </summary>
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        BulkUploadLegacyId,
        BulkUploadLegacyParentId,
        BulkUploadShouldPublish,
        BulkUploadContentGuid,
        BulkUploadParentGuid
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