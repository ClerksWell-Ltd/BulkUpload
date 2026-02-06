namespace BulkUpload.Models;

/// <summary>
/// Response model for content import operations
/// </summary>
public class ContentImportResponse
{
    /// <summary>
    /// Total number of records processed from all CSV files
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported content items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported content item, including GUIDs, parent references, and error messages
    /// </summary>
    public List<ContentImportResult> Results { get; set; } = new();

    /// <summary>
    /// Media preprocessing results (only populated if media files were included in ZIP upload)
    /// </summary>
    public List<MediaPreprocessingResult>? MediaPreprocessingResults { get; set; }
}
