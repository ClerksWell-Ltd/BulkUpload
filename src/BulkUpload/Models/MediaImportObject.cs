namespace BulkUpload.Models;

public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// Parent folder specification - can be:
    /// - Integer ID (e.g., "1234")
    /// - GUID (e.g., "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
    /// - Path (e.g., "/Blog/Images/") - auto-creates folders
    /// </summary>
    public string? Parent { get; set; }

    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    // Support for external media sources (file paths and URLs)
    public MediaSource? ExternalSource { get; set; }

    /// <summary>
    /// Optional: Legacy CMS identifier for this media item.
    /// Used to preserve identifiers from legacy systems for tracking purposes.
    /// This is NOT persisted as a media property.
    /// </summary>
    public string? BulkUploadLegacyId { get; set; }

    /// <summary>
    /// Optional: Umbraco media GUID for updating existing media.
    /// When present with BulkUploadShouldUpdate=true, the import will update the existing media item.
    /// This is NOT persisted as a media property.
    /// </summary>
    public Guid? BulkUploadMediaGuid { get; set; }

    /// <summary>
    /// Per-row flag indicating whether to update this specific media item.
    /// When false, creates new media (default behavior).
    /// When true with BulkUploadMediaGuid, updates the existing media item.
    /// This is NOT persisted as a media property.
    /// </summary>
    public bool BulkUploadShouldUpdate { get; set; } = false;

    /// <summary>
    /// Per-file flag tracking whether the bulkUploadShouldUpdate column existed in the CSV.
    /// When true, the import file supports update mode (column is present).
    /// Individual rows still use BulkUploadShouldUpdate value to determine update vs create.
    /// This is NOT persisted as a media property.
    /// </summary>
    public bool BulkUploadShouldUpdateColumnExisted { get; set; } = false;

    public bool CanImport =>
        (BulkUploadShouldUpdate && BulkUploadMediaGuid.HasValue) || // Update mode: needs GUID
        (!BulkUploadShouldUpdate && // Create mode: needs file source
         ((!string.IsNullOrWhiteSpace(FileName) || ExternalSource != null) &&
          !string.IsNullOrWhiteSpace(Parent)));

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FileName;
}