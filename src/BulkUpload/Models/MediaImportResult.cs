namespace BulkUpload.Models;

public class MediaImportResult
{
    public required string BulkUploadFileName { get; set; }
    public bool BulkUploadSuccess { get; set; }
    public Guid? BulkUploadMediaGuid { get; set; }
    public string? BulkUploadMediaUdi { get; set; }
    public string? BulkUploadErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }
    public bool BulkUploadShouldUpdate { get; set; }
    public bool BulkUploadShouldUpdateColumnExisted { get; set; }

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }

    /// <summary>
    /// Informational message about the import (e.g., "No properties were updated")
    /// </summary>
    public string? BulkUploadInfoMessage { get; set; }
}