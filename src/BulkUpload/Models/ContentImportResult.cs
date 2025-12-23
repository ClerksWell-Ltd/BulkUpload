namespace Umbraco.Community.BulkUpload.Models;

public class ContentImportResult
{
    public required string ContentName { get; set; }
    public bool Success { get; set; }
    public int? ContentId { get; set; }
    public Guid? ContentGuid { get; set; }
    public string? ContentUdi { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BulkUploadLegacyId { get; set; }
}
