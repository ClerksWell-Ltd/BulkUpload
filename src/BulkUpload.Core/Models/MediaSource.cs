namespace BulkUpload.Core.Models;

public class MediaSource
{
    public MediaSourceType Type { get; set; }
    public required string Value { get; set; }  // File path or URL
    public string? Parameter { get; set; }  // Optional parameter (e.g., parent folder path)
}

public enum MediaSourceType
{
    ZipFile,      // File from uploaded ZIP (current behavior)
    FilePath,     // File from local/network path
    Url           // File from URL download
}