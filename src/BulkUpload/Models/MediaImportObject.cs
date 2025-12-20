namespace Umbraco.Community.BulkUpload.Models;

public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// Parent folder specification - can be:
    /// - Integer ID (e.g., "1234")
    /// - GUID (e.g., "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
    /// - Path (e.g., "/Blog/Images/") - auto-creates folders
    /// </summary>
    public string? Parent { get; set; }

    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    // Support for external media sources (file paths and URLs)
    public MediaSource? ExternalSource { get; set; }

    public bool CanImport =>
        (!string.IsNullOrWhiteSpace(FileName) || ExternalSource != null)
        && !string.IsNullOrWhiteSpace(Parent);

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FileName;
}
