namespace BulkUpload.Models;

/// <summary>
/// Response model for media import operations
/// </summary>
public class MediaImportResponse
{
    /// <summary>
    /// Total number of media items processed from the CSV file
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported media items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported media item, including GUIDs, UDIs, and error messages
    /// </summary>
    public List<MediaImportResult> Results { get; set; } = new();
}
