namespace Umbraco.Community.BulkUpload.Models;

public class ContentImportResult
{
    public required string BulkUploadContentName { get; set; }
    public bool BulkUploadSuccess { get; set; }
    public Guid? BulkUploadContentGuid { get; set; }
    public Guid? BulkUploadParentGuid { get; set; }
    public string? BulkUploadErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }
}