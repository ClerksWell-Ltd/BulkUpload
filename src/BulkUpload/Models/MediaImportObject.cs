namespace Umbraco.Community.BulkUpload.Models;

public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }
    public int ParentId { get; set; }
    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    // Support for external media sources (file paths and URLs)
    public MediaSource? ExternalSource { get; set; }

    public bool CanImport =>
        (!string.IsNullOrWhiteSpace(FileName) || ExternalSource != null)
        && ParentId > 0;

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FileName;
}
