namespace BulkUpload.Core.Models;

/// <summary>
/// Represents the result of preprocessing a media item during bulk content upload.
/// </summary>
public class MediaPreprocessingResult
{
    /// <summary>
    /// The original value from CSV (URL or file path) - this is the cache key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The GUID of the created media item in Umbraco - this is the cache value.
    /// </summary>
    public Guid? Value { get; set; }

    /// <summary>
    /// Indicates whether the media item was successfully created.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the media creation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The filename of the media item (extracted from URL, path, or zip file).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Source CSV filename (without path) that this media reference came from.
    /// </summary>
    public string? SourceCsvFileName { get; set; }
}