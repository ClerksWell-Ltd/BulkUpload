namespace Umbraco.Community.BulkUpload.Models;

public class MediaImportResult
{
    public required string FileName { get; set; }
    public bool Success { get; set; }
    public int? MediaId { get; set; }
    public Guid? MediaGuid { get; set; }
    public string? MediaUdi { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }
}
