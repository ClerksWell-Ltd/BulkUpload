namespace Umbraco.Community.BulkUpload.Models;

public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }
    public int ParentId { get; set; }
    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public bool CanImport => !string.IsNullOrWhiteSpace(FileName) && ParentId > 0;

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FileName;
}
