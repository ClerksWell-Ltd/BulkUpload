using Microsoft.AspNetCore.Http;

namespace BulkUpload.Models;

/// <summary>
/// Request model for media import operations.
/// </summary>
public class ImportMediaRequestModel
{
    /// <summary>
    /// The CSV file or ZIP archive containing media definitions and files.
    /// </summary>
    public required IFormFile File { get; set; }
}
