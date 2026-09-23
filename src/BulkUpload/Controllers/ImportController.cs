
using System.IO.Compression;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

#if NET8_0
using Umbraco.Cms.Web.BackOffice.Controllers;

#else
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Asp.Versioning;
#endif
using BulkUpload.Models;
using BulkUpload.Services;


namespace BulkUpload.Controllers;

#if NET8_0
public class BulkUploadController : UmbracoAuthorizedApiController
#else
/// <summary>
/// BulkUpload API for importing content from CSV/ZIP files into Umbraco CMS.
/// Supports single and multi-CSV imports, media deduplication, legacy CMS migration, and update mode.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Content")]
[MapToApi("bulk-upload")]
[ApiController]
public class BulkUploadController : ControllerBase
#endif
{
    private readonly ILogger<BulkUploadController> _logger;
    private readonly IBulkImportService _bulkImportService;

    public BulkUploadController(
        ILogger<BulkUploadController> logger,
        IBulkImportService bulkImportService)
    {
        _logger = logger;
        _bulkImportService = bulkImportService;
    }

    /// <summary>
    /// Imports content from a CSV file or ZIP archive containing CSV and media files.
    /// </summary>
    /// <param name="model">Request model containing the file to import.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the import operation.</param>
    /// <returns>
    /// Import results containing total counts, success/failure counts, and detailed information for each imported content item.
    /// Includes media preprocessing results if media files were included in the upload.
    /// </returns>
    /// <remarks>
    /// <para>This endpoint supports multiple import scenarios:</para>
    /// <list type="bullet">
    ///   <item><description><strong>Single CSV:</strong> Upload a CSV file containing content data only (no media files)</description></item>
    ///   <item><description><strong>ZIP with CSV and media:</strong> Upload a ZIP containing CSV(s) and media files</description></item>
    ///   <item><description><strong>Multi-CSV imports:</strong> ZIP with multiple CSV files supporting cross-file parent-child hierarchy</description></item>
    ///   <item><description><strong>Legacy migration:</strong> Use bulkUploadLegacyId and bulkUploadLegacyParentId for legacy CMS migration</description></item>
    /// </list>
    ///
    /// <para><strong>Update Mode and Publish State:</strong></para>
    /// <para>Three independent columns control what happens to each row. 'bulkUploadShouldUpdate' is the only column that writes data (name, parent and property values) to existing content identified by 'bulkUploadContentGuid'. 'bulkUploadShouldPublish' publishes and 'bulkUploadShouldUnpublish' unpublishes, whether or not data is written; when both are true, unpublish wins. A row for existing content with none of the three set to true is skipped. A row for new content is skipped when 'bulkUploadShouldUpdate' is present and false.</para>
    ///
    /// <para><strong>Media Deduplication:</strong></para>
    /// <para>When importing multiple CSVs, media files referenced across different CSVs are automatically deduplicated - each unique file is created only once.</para>
    ///
    /// <para><strong>Required CSV Columns:</strong></para>
    /// <list type="bullet">
    ///   <item><description>name - Content node name</description></item>
    ///   <item><description>docTypeAlias - Content type alias (must exist in Umbraco)</description></item>
    ///   <item><description>parent - Parent node ID, GUID, or path</description></item>
    /// </list>
    ///
    /// <para><strong>Optional CSV Columns:</strong></para>
    /// <list type="bullet">
    ///   <item><description>bulkUploadLegacyId - Legacy CMS identifier for this content item</description></item>
    ///   <item><description>bulkUploadLegacyParentId - Legacy CMS parent identifier (enables cross-file hierarchy)</description></item>
    ///   <item><description>bulkUploadContentGuid - GUID of existing content to update, publish or unpublish</description></item>
    ///   <item><description>bulkUploadShouldUpdate - Set to 'true' to write this row's data to the existing content identified by bulkUploadContentGuid</description></item>
    ///   <item><description>bulkUploadShouldPublish - Set to 'true' to publish the content (default: false, publish state unchanged)</description></item>
    ///   <item><description>bulkUploadShouldUnpublish - Set to 'true' to unpublish existing content (default: false, publish state unchanged)</description></item>
    ///   <item><description>propertyAlias|resolverAlias - Content properties using resolver syntax (e.g., heroImage|zipFileToMedia)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Example ZIP structure:
    /// <code>
    /// upload.zip
    /// ├── content.csv
    /// ├── categories.csv
    /// └── media/
    ///     ├── hero-image.jpg
    ///     └── thumbnail.jpg
    /// </code>
    ///
    /// Example CSV content:
    /// <code>
    /// name,docTypeAlias,parent,heroImage|zipFileToMedia,publishDate|dateTime,bulkUploadShouldPublish
    /// "Article 1","article","umb://document/1234","media/hero-image.jpg","2024-01-01T00:00:00Z","true"
    /// "Article 2","article","umb://document/1234","media/thumbnail.jpg","2024-01-02T00:00:00Z","false"
    /// </code>
    /// </example>
    [HttpPost]
    [Consumes("multipart/form-data")]
#if !NET8_0
    [IgnoreAntiforgeryToken]
    [Route("importall")]
    [ProducesResponseType(typeof(ContentImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
#endif
    public async Task<IActionResult> ImportAll([FromForm] ImportContentRequestModel model, CancellationToken cancellationToken = default)
    {
        string? tempDirectory = null;

        try
        {
            var file = model.File;
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload: Uploaded file is not valid");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File",
                    Detail = "Uploaded file is not valid or is empty.",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".zip" && extension != ".csv")
            {
                _logger.LogError("Bulk Upload: File is not a ZIP or CSV file");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File Type",
                    Detail = "Please upload either a CSV file (content only) or a ZIP file (content + media files).",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
            }

            List<string> csvFilePaths;
            bool isZipUpload = extension == ".zip";

            if (isZipUpload)
            {
                // Create temporary directory for extraction
                tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);

                // Extract ZIP file
                using (var fileStream = file.OpenReadStream())
                {
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                    archive.ExtractToDirectory(tempDirectory);
                }

                // Find all CSV files in extracted contents
                var csvFiles = Directory.GetFiles(tempDirectory, "*.csv", SearchOption.AllDirectories);
                if (csvFiles.Length == 0)
                {
                    _logger.LogError("Bulk Upload: No CSV file found in ZIP archive");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "No CSV Found",
                        Detail = "No CSV file found in ZIP archive. Please ensure your ZIP contains at least one .csv file.",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    });
                }

                csvFilePaths = csvFiles.ToList();
                _logger.LogInformation("Bulk Upload: Processing ZIP file with {CsvCount} CSV file(s) and media files", csvFilePaths.Count);
            }
            else
            {
                // CSV file uploaded directly - save to temporary location for consistent handling
                tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                var csvFilePath = Path.Combine(tempDirectory, file.FileName);

                using (var stream = new FileStream(csvFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                csvFilePaths = new List<string> { csvFilePath };
                _logger.LogInformation("Bulk Upload: Processing CSV file (no media files in archive)");
            }

            // Everything from here on is shared with the in-process callers of IBulkImportService:
            // cache clear, CSV read, media preprocessing, resolver mapping, hierarchy sort and save.
            var response = await _bulkImportService.ImportCsvFilesAsync(csvFilePaths, tempDirectory, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload: Error occurred while importing content from CSV/ZIP");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Import Failed",
                Detail = "An unexpected error occurred while processing the import. Please check the logs for details.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            });
        }
        finally
        {
            // Clean up temporary directory
            if (tempDirectory != null && Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary directory: {TempDirectory}", tempDirectory);
                }
            }
        }
    }

    /// <summary>
    /// Exports content import results to a CSV file or ZIP archive for review and further processing.
    /// </summary>
    /// <param name="results">Array of content import result objects from a previous ImportAll operation.</param>
    /// <returns>
    /// A CSV file (for single-source imports) or ZIP archive (for multi-CSV imports) containing the import results.
    /// Each file includes success status, GUIDs, error messages, and original CSV data.
    /// </returns>
    /// <remarks>
    /// <para>The exported CSV file(s) include the following columns:</para>
    /// <list type="bullet">
    ///   <item><description><strong>bulkUploadSuccess</strong> - true/false indicating if the import was successful</description></item>
    ///   <item><description><strong>bulkUploadContentGuid</strong> - GUID of the created/updated content item</description></item>
    ///   <item><description><strong>bulkUploadParentGuid</strong> - GUID of the parent content item</description></item>
    ///   <item><description><strong>bulkUploadErrorMessage</strong> - Error details (only included if errors occurred)</description></item>
    ///   <item><description><strong>bulkUploadLegacyId</strong> - Legacy CMS identifier (only if used in import)</description></item>
    ///   <item><description><strong>bulkUploadShouldPublish</strong> - Publish flag (only if used in import)</description></item>
    ///   <item><description><strong>bulkUploadShouldUnpublish</strong> - Unpublish flag (only if used in import)</description></item>
    ///   <item><description><strong>bulkUploadShouldUpdate</strong> - Update flag value</description></item>
    ///   <item><description><strong>Original columns</strong> - All original CSV columns are preserved</description></item>
    /// </list>
    ///
    /// <para><strong>Multi-CSV Imports:</strong></para>
    /// <para>If the import contained multiple CSV files, this endpoint returns a ZIP archive with separate result files for each source CSV.
    /// Each result file is named {originalFileName}-import-results.csv.</para>
    ///
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description>Audit trail of import operations</description></item>
    ///   <item><description>Error analysis and debugging</description></item>
    ///   <item><description>Preparing update imports (use bulkUploadContentGuid for subsequent updates)</description></item>
    ///   <item><description>Legacy ID mapping for future imports</description></item>
    /// </list>
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("text/csv", "application/zip")]
#if !NET8_0
    [IgnoreAntiforgeryToken]
    [Route("exportresults")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
#endif
    public IActionResult ExportResults([FromBody] List<ContentImportResult> results)
    {
        try
        {
            if (results == null || !results.Any())
            {
                return BadRequest("No results to export.");
            }

            // Group results by source CSV file
            var groupedResults = results
                .GroupBy(r => r.SourceCsvFileName ?? "unknown.csv")
                .OrderBy(g => g.Key)
                .ToList();

            // If only one source file, return a single CSV
            if (groupedResults.Count == 1)
            {
                var singleCsv = GenerateCsvForResults(groupedResults[0].ToList());
                var bytes = Encoding.UTF8.GetBytes(singleCsv);
                var fileName = Path.GetFileNameWithoutExtension(groupedResults[0].Key);
                return File(bytes, "text/csv", $"{fileName}-import-results.csv");
            }

            // Multiple source files - create a ZIP with separate CSV files
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var group in groupedResults)
                {
                    var csvContent = GenerateCsvForResults(group.ToList());
                    var fileName = Path.GetFileNameWithoutExtension(group.Key);
                    var zipEntryName = $"{fileName}-import-results.csv";

                    var zipEntry = archive.CreateEntry(zipEntryName, CompressionLevel.Optimal);
                    using var zipEntryStream = zipEntry.Open();
                    using var writer = new StreamWriter(zipEntryStream, Encoding.UTF8);
                    writer.Write(csvContent);
                }
            }

            memoryStream.Position = 0;
            var zipBytes = memoryStream.ToArray();

            return File(zipBytes, "application/zip", "content-import-results.zip");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload: Error exporting results");
            return BadRequest("Error exporting results.");
        }
    }

    /// <summary>
    /// Generates CSV content for a list of results
    /// </summary>
    private string GenerateCsvForResults(List<ContentImportResult> results)
    {
        // Collect all unique original column names from results (preserving order from first occurrence)
        // Exclude any columns that start with "bulkUpload" prefix to avoid duplicates with system columns
        var originalColumns = new List<string>();
        foreach (var result in results)
        {
            if (result.OriginalCsvData != null)
            {
                foreach (var key in result.OriginalCsvData.Keys)
                {
                    if (!originalColumns.Contains(key) && !key.StartsWith("bulkUpload", StringComparison.OrdinalIgnoreCase))
                    {
                        originalColumns.Add(key);
                    }
                }
            }
        }

        // Determine which optional columns to include
        bool hasAnyErrors = results.Any(r => !string.IsNullOrWhiteSpace(r.BulkUploadErrorMessage));
        bool hadLegacyIdColumn = results.Any(r => r.OriginalCsvData != null &&
            r.OriginalCsvData.Keys.Any(k => k.Split('|')[0].Equals("bulkUploadLegacyId", StringComparison.OrdinalIgnoreCase)));

        bool hadShouldPublishColumn = results.Any(r => r.OriginalCsvData != null &&
            r.OriginalCsvData.Keys.Any(k => k.Split('|')[0].Equals("bulkUploadShouldPublish", StringComparison.OrdinalIgnoreCase)));

        bool hadShouldUnpublishColumn = results.Any(r => r.OriginalCsvData != null &&
            r.OriginalCsvData.Keys.Any(k => k.Split('|')[0].Equals("bulkUploadShouldUnpublish", StringComparison.OrdinalIgnoreCase)));

        var csv = new StringBuilder();

        // Build header: BulkUpload columns + original columns
        var headerParts = new List<string>
        {
            "bulkUploadSuccess",
            "bulkUploadContentGuid",
            "bulkUploadParentGuid"
        };

        if (hasAnyErrors)
        {
            headerParts.Add("bulkUploadErrorMessage");
        }

        if (hadLegacyIdColumn)
        {
            headerParts.Add("bulkUploadLegacyId");
        }

        if (hadShouldPublishColumn)
        {
            headerParts.Add("bulkUploadShouldPublish");
        }

        if (hadShouldUnpublishColumn)
        {
            headerParts.Add("bulkUploadShouldUnpublish");
        }

        headerParts.Add("bulkUploadShouldUpdate");

        headerParts.AddRange(originalColumns);
        csv.AppendLine(string.Join(",", headerParts));

        // Build each row: BulkUpload values + original values
        foreach (var result in results)
        {
            var rowParts = new List<string>();

            // BulkUpload columns
            rowParts.Add(result.BulkUploadSuccess.ToString());
            rowParts.Add($"\"{result.BulkUploadContentGuid}\"");
            rowParts.Add($"\"{result.BulkUploadParentGuid}\"");

            // Optional BulkUpload columns (only if needed)
            if (hasAnyErrors)
            {
                rowParts.Add($"\"{(result.BulkUploadErrorMessage?.Replace("\"", "\"\"") ?? "")}\"");
            }

            if (hadLegacyIdColumn)
            {
                rowParts.Add($"\"{(result.BulkUploadLegacyId?.Replace("\"", "\"\"") ?? "")}\"");
            }

            if (hadShouldPublishColumn)
            {
                // If column existed in original upload, use the value; otherwise use false
                var shouldPublishValue = result.BulkUploadShouldPublishColumnExisted
                    ? result.BulkUploadShouldPublish.ToString()
                    : "false";
                rowParts.Add(shouldPublishValue);
            }

            if (hadShouldUnpublishColumn)
            {
                // If column existed in original upload, use the value; otherwise use false
                var shouldUnpublishValue = result.BulkUploadShouldUnpublishColumnExisted
                    ? result.BulkUploadShouldUnpublish.ToString()
                    : "false";
                rowParts.Add(shouldUnpublishValue);
            }

            // If column existed in original upload, use the value; otherwise use false
            var shouldUpdateValue = result.BulkUploadShouldUpdateColumnExisted
                ? result.BulkUploadShouldUpdate.ToString()
                : "false";
            rowParts.Add(shouldUpdateValue);

            // Original CSV columns
            foreach (var columnName in originalColumns)
            {
                string value = "";
                if (result.OriginalCsvData != null && result.OriginalCsvData.TryGetValue(columnName, out var csvValue))
                {
                    value = csvValue?.Replace("\"", "\"\"") ?? "";
                }
                rowParts.Add($"\"{value}\"");
            }

            csv.AppendLine(string.Join(",", rowParts));
        }

        return csv.ToString();
    }

    /// <summary>
    /// Exports media preprocessing results to CSV or ZIP file
    /// </summary>
    /// <param name="results">Array of media preprocessing result objects</param>
    /// <returns>CSV file (single source) or ZIP file (multiple sources) containing media import results</returns>
    /// <remarks>
    /// Returns a CSV file with columns:
    /// - bulkUploadMediaGuid: GUID of created media item
    /// - bulkUploadFileName: Original filename
    /// - bulkUploadStatus: Success/Failed
    /// - bulkUploadErrorMessage: Error details if failed
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("text/csv", "application/zip")]
#if !NET8_0
    [IgnoreAntiforgeryToken]
    [Route("exportmediapreprocessingresults")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
#endif
    public IActionResult ExportMediaPreprocessingResults([FromBody] List<MediaPreprocessingResult> results)
    {
        try
        {
            if (results == null || !results.Any())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "No Results",
                    Detail = "No media preprocessing results to export. Please provide a non-empty array of results.",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
            }

            // Group results by source CSV file
            var groupedResults = results
                .GroupBy(r => r.SourceCsvFileName ?? "unknown.csv")
                .OrderBy(g => g.Key)
                .ToList();

            // If only one source file, return a single CSV
            if (groupedResults.Count == 1)
            {
                var singleCsv = GenerateCsvForMediaResults(groupedResults[0].ToList());
                var bytes = Encoding.UTF8.GetBytes(singleCsv);
                var fileName = Path.GetFileNameWithoutExtension(groupedResults[0].Key);
                return File(bytes, "text/csv", $"{fileName}-media-import-results.csv");
            }

            // Multiple source files - create a ZIP with separate CSV files
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var group in groupedResults)
                {
                    var csvContent = GenerateCsvForMediaResults(group.ToList());
                    var fileName = Path.GetFileNameWithoutExtension(group.Key);
                    var zipEntryName = $"{fileName}-media-import-results.csv";

                    var zipEntry = archive.CreateEntry(zipEntryName, CompressionLevel.Optimal);
                    using var zipEntryStream = zipEntry.Open();
                    using var writer = new StreamWriter(zipEntryStream, Encoding.UTF8);
                    writer.Write(csvContent);
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", "media-import-results.zip");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload: Error exporting media preprocessing results");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Export Failed",
                Detail = "An unexpected error occurred while exporting the media preprocessing results. Please check the logs for details.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            });
        }
    }

    /// <summary>
    /// Generates CSV content for media preprocessing results
    /// </summary>
    private string GenerateCsvForMediaResults(List<MediaPreprocessingResult> results)
    {
        var csv = new StringBuilder();

        // Build header with user-requested column names
        csv.AppendLine("bulkUploadMediaGuid,bulkUploadFileName,bulkUploadStatus,bulkUploadErrorMessage");

        // Build each row
        foreach (var result in results)
        {
            var mediaGuid = result.Value?.ToString() ?? "";
            var fileName = (result.FileName?.Replace("\"", "\"\"") ?? "");
            var status = result.Success ? "Success" : "Failed";
            var errorMessage = (result.ErrorMessage?.Replace("\"", "\"\"") ?? "");

            csv.AppendLine($"\"{mediaGuid}\",\"{fileName}\",\"{status}\",\"{errorMessage}\"");
        }

        return csv.ToString();
    }
}