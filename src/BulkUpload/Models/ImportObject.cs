namespace Umbraco.Community.BulkUpload.Models;

public class ImportObject
{
    public required string ContentTypeAlais { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// Parent folder specification - can be:
    /// - Integer ID (e.g., "1234")
    /// - GUID (e.g., "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
    /// - Path (e.g., "/Blog/Articles/") - resolves to existing folders
    /// </summary>
    public string? Parent { get; set; }

    public Dictionary<string, object>? Properties { get; set; }

    public bool CanImport => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(ContentTypeAlais)
        && !string.IsNullOrWhiteSpace(Parent);
}