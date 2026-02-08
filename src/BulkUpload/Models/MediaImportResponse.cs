using System.Text.Json.Serialization;

namespace BulkUpload.Models;

/// <summary>
/// Response model for media import operations
/// </summary>
public class MediaImportResponse
{
    /// <summary>
    /// Total number of media items processed from the CSV file
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported media items
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported media item, including GUIDs, UDIs, and error messages
    /// </summary>
    [JsonPropertyName("results")]
    public List<MediaImportResult> Results { get; set; } = new();
}
