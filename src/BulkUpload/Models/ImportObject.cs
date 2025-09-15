namespace Umbraco.Community.BulkUpload.Models;

public class ImportObject
{
    public required string ContentTypeAlais { get; set; }
    public required string Name { get; set; }
    public int ParentId { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public bool CanImport => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(ContentTypeAlais)
        && ParentId > 0;


}