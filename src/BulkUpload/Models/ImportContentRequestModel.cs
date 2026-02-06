using Microsoft.AspNetCore.Http;

namespace BulkUpload.Models;

/// <summary>
/// Request model for content import operations.
/// </summary>
public class ImportContentRequestModel
{
    /// <summary>
    /// The CSV file or ZIP archive containing content data and optional media files.
    /// </summary>
    public required IFormFile File { get; set; }
}
