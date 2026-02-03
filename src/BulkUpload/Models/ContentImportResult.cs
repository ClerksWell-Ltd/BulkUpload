namespace BulkUpload.Models;

public class ContentImportResult
{
    public bool BulkUploadSuccess { get; set; }
    public Guid? BulkUploadContentGuid { get; set; }
    public Guid? BulkUploadParentGuid { get; set; }
    public string? BulkUploadErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }
    public bool BulkUploadShouldUpdate { get; set; }
    public bool BulkUploadShouldUpdateColumnExisted { get; set; }
    public bool BulkUploadShouldPublish { get; set; }
    public bool BulkUploadShouldPublishColumnExisted { get; set; }

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }

    /// <summary>
    /// Source CSV filename (without path) that this record came from
    /// </summary>
    public string? SourceCsvFileName { get; set; }
}