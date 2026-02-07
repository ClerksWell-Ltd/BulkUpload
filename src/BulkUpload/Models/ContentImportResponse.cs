using System.Text.Json.Serialization;

namespace BulkUpload.Models;

/// <summary>
/// Response model for content import operations
/// </summary>
public class ContentImportResponse
{
    /// <summary>
    /// Total number of records processed from all CSV files
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported content items
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported content item, including GUIDs, parent references, and error messages
    /// </summary>
    [JsonPropertyName("results")]
    public List<ContentImportResult> Results { get; set; } = new();

    /// <summary>
    /// Media preprocessing results (only populated if media files were included in ZIP upload)
    /// </summary>
    [JsonPropertyName("mediaPreprocessingResults")]
    public List<MediaPreprocessingResult>? MediaPreprocessingResults { get; set; }
}
