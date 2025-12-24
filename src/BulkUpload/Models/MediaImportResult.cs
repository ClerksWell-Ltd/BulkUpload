namespace Umbraco.Community.BulkUpload.Models;

public class MediaImportResult
{
    public required string BulkUploadFileName { get; set; }
    public bool BulkUploadSuccess { get; set; }
    public Guid? BulkUploadMediaGuid { get; set; }
    public string? BulkUploadMediaUdi { get; set; }
    public string? BulkUploadErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }
}