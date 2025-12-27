namespace Umbraco.Community.BulkUpload.Core.Models;

public class MediaImportResult
{
    public required string BulkUploadFileName { get; set; }
    public bool BulkUploadSuccess { get; set; }
    public Guid? BulkUploadMediaGuid { get; set; }
    public string? BulkUploadMediaUdi { get; set; }
    public string? BulkUploadErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }

    /// <summary>
    /// Original CSV row data with column names including resolver syntax (e.g., "tags|stringArray")
    /// </summary>
    public Dictionary<string, string>? OriginalCsvData { get; set; }
}