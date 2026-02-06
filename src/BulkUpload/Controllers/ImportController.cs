
using System.Globalization;
using System.IO.Compression;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
#if NET8_0
using Umbraco.Cms.Web.BackOffice.Controllers;

#else
using Umbraco.Cms.Api.Common.Attributes;
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
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Content")]
[MapToApi("bulk-upload")]
[ApiController]
public class BulkUploadController : ControllerBase
#endif
{
    private readonly ILogger<BulkUploadController> _logger;
    private readonly IImportUtilityService _importUtilityService;
    private readonly IHierarchyResolver _hierarchyResolver;
    private readonly IMediaPreprocessorService _mediaPreprocessorService;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILegacyIdCache _legacyIdCache;

    public BulkUploadController(
        IContentService contentService,
        IUmbracoContextAccessor umbracoContextAccessor,
        IContentTypeService contentTypeService,
        IJsonSerializer jsonSerializer,
        ILocalizationService localizationService,
        ILanguageRepository languageRepository,
        ICoreScopeProvider coreScopeProvider,
        ILogger<BulkUploadController> logger,
        IImportUtilityService importUtilityService,
        IHierarchyResolver hierarchyResolver,
        IMediaPreprocessorService mediaPreprocessorService,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILegacyIdCache legacyIdCache)
    {
        _logger = logger;
        _importUtilityService = importUtilityService;
        _hierarchyResolver = hierarchyResolver;
        _mediaPreprocessorService = mediaPreprocessorService;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _legacyIdCache = legacyIdCache;
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
    /// <para><strong>Update Mode:</strong></para>
    /// <para>Include a 'bulkUploadShouldUpdate' column (true/false) in your CSV to enable update mode. Rows with 'true' will update existing content, while 'false' rows are skipped.</para>
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
    ///   <item><description>bulkUploadShouldUpdate - Set to 'true' to update existing content instead of creating new</description></item>
    ///   <item><description>bulkUploadShouldPublish - Set to 'true' to publish content after creation (default: false)</description></item>
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
#if !NET8_0
    [Route("importall")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
#endif
    public async Task<IActionResult> ImportAll([FromForm] ImportContentRequestModel model, CancellationToken cancellationToken = default)
    {
        // Clear all caches at the start of each import to ensure fresh state
        _parentLookupCache.Clear();
        _mediaItemCache.Clear();
        _legacyIdCache.Clear();
        _logger.LogInformation("Bulk Upload: Cleared all caches for new import");

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

            // Step 1: Read all CSV files and collect all records with their source file
            var allRecordsWithSource = new List<(dynamic record, string sourceFileName)>();
            foreach (var csvFilePath in csvFilePaths)
            {
                var sourceFileName = Path.GetFileName(csvFilePath);
                _logger.LogInformation("Bulk Upload: Reading CSV file: {CsvFile}", sourceFileName);

                using (var reader = new StreamReader(csvFilePath, Encoding.UTF8))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                }))
                {
                    await foreach (var record in csv.GetRecordsAsync<dynamic>())
                    {
                        allRecordsWithSource.Add((record, sourceFileName));
                    }
                }

                _logger.LogInformation("Bulk Upload: Read {RecordCount} total records so far (current file: {CsvFile})",
                    allRecordsWithSource.Count, sourceFileName);
            }

            if (!allRecordsWithSource.Any())
            {
                _logger.LogInformation("Bulk Upload: No valid records found in any CSV file");
                return Ok(new ContentImportResponse
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0,
                    Results = new List<ContentImportResult>()
                });
            }

            // Detect if this import supports update mode (per-file detection)
            if (allRecordsWithSource != null && allRecordsWithSource.Any())
            {
                var firstRecord = (IDictionary<string, object>)allRecordsWithSource.First().record;
                var hasUpdateColumn = firstRecord.Keys.Any(k =>
                    k.Split('|')[0].Equals("bulkUploadShouldUpdate", StringComparison.OrdinalIgnoreCase));
                if (hasUpdateColumn)
                {
                    _logger.LogInformation("Bulk Upload: Import file contains 'bulkUploadShouldUpdate' column - update mode is available. Each row's value will determine update vs create.");
                }
                else
                {
                    _logger.LogInformation("Bulk Upload: Import file does not contain 'bulkUploadShouldUpdate' column - all items will be created.");
                }
            }

            // Step 2: Preprocess media items from all CSV files to avoid duplicates
            _logger.LogDebug("Preprocessing media items from all CSV records across {CsvCount} file(s)", csvFilePaths.Count);
            var allMediaPreprocessingResults = _mediaPreprocessorService.PreprocessMediaItems(allRecordsWithSource, tempDirectory);

            // Step 3: Create all ImportObjects from all CSV records with source tracking
            var allImportObjects = new List<ImportObject>();
            var skippedCount = 0;
            foreach (var (record, sourceFileName) in allRecordsWithSource)
            {
                ImportObject importObject = _importUtilityService.CreateImportObject(record);
                importObject.OriginalCsvData = ConvertCsvRecordToDictionary(record);
                importObject.SourceCsvFileName = sourceFileName;

                // In UPDATE MODE, skip rows where bulkUploadShouldUpdate = false
                if (importObject.BulkUploadShouldUpdateColumnExisted && !importObject.BulkUploadShouldUpdate)
                {
                    skippedCount++;
                    _logger.LogDebug("Skipping row '{Name}' - bulkUploadShouldUpdate is false", importObject.Name);
                    continue;
                }

                if (importObject.CanImport)
                {
                    allImportObjects.Add(importObject);
                }
            }

            if (skippedCount > 0)
            {
                _logger.LogInformation("Bulk Upload: Skipped {SkippedCount} row(s) where bulkUploadShouldUpdate was false", skippedCount);
            }

            // Step 4: Validate and sort ALL import objects across all CSV files based on legacy hierarchy
            // This ensures parent-child relationships work correctly even when spread across different CSV files
            var sortedImportObjects = _hierarchyResolver.ValidateAndSort(allImportObjects);
            _logger.LogDebug("Sorted {Count} import objects for processing across {CsvCount} CSV file(s)",
                sortedImportObjects.Count, csvFilePaths.Count);

            // Step 5: Import in sorted order (parents before children) and collect results
            var allResults = new List<ContentImportResult>();
            foreach (var importObject in sortedImportObjects)
            {
                var result = _importUtilityService.ImportSingleItem(importObject, importObject.BulkUploadShouldPublish);
                allResults.Add(result);
            }

            var totalSuccessCount = allResults.Count(r => r.BulkUploadSuccess);
            var totalFailureCount = allResults.Count(r => !r.BulkUploadSuccess);

            _logger.LogInformation("Bulk Upload: Completed processing {CsvCount} CSV file(s) - {Total} total records, {Success} successful, {Failed} failed",
                csvFilePaths.Count, allResults.Count, totalSuccessCount, totalFailureCount);

            return Ok(new ContentImportResponse
            {
                TotalCount = allResults.Count,
                SuccessCount = totalSuccessCount,
                FailureCount = totalFailureCount,
                Results = allResults,
                MediaPreprocessingResults = allMediaPreprocessingResults
            });
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
#if !NET8_0
    [Route("exportresults")]
    [Consumes("application/json")]
    [Produces("text/csv", "application/zip")]
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
#if !NET8_0
    [Route("exportmediapreprocessingresults")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [Consumes("application/json")]
    [Produces("text/csv", "application/zip")]
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

    /// <summary>
    /// Converts a dynamic CSV record to a dictionary preserving column names with resolver syntax
    /// </summary>
    private Dictionary<string, string> ConvertCsvRecordToDictionary(dynamic record)
    {
        var dictionary = new Dictionary<string, string>();
        var recordDict = (IDictionary<string, object>)record;

        foreach (var kvp in recordDict)
        {
            // Store the value as a string, handling nulls
            dictionary[kvp.Key] = kvp.Value?.ToString() ?? "";
        }

        return dictionary;
    }
}