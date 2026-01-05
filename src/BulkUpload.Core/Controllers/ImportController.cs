
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
using Umbraco.Cms.Web.BackOffice.Controllers;
using BulkUpload.Core.Models;
using BulkUpload.Core.Services;


namespace BulkUpload.Core.Controllers;

public class BulkUploadController : UmbracoAuthorizedApiController
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

    [HttpPost]
    public async Task<IActionResult> ImportAll([FromForm] IFormFile file)
    {
        // Clear all caches at the start of each import to ensure fresh state
        _parentLookupCache.Clear();
        _mediaItemCache.Clear();
        _legacyIdCache.Clear();
        _logger.LogInformation("Bulk Upload: Cleared all caches for new import");

        string? tempDirectory = null;

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload: Uploaded file is not valid");
                return BadRequest("Uploaded file not valid.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".zip" && extension != ".csv")
            {
                _logger.LogError("Bulk Upload: File is not a ZIP or CSV file");
                return BadRequest("Please upload either a CSV file (content only) or a ZIP file (content + media files).");
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
                    return BadRequest("No CSV file found in ZIP archive.");
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
                return Ok(new { totalCount = 0, successCount = 0, failureCount = 0, results = new List<ContentImportResult>() });
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

            return Ok(new
            {
                totalCount = allResults.Count,
                successCount = totalSuccessCount,
                failureCount = totalFailureCount,
                results = allResults,
                mediaPreprocessingResults = allMediaPreprocessingResults
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Case Studies: Error occurred while importing case studies from CSV");
            return BadRequest("\r\n" + "Something went wrong while processing the records. Please try after some time.");
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

    [HttpPost]
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

    [HttpPost]
    public IActionResult ExportMediaPreprocessingResults([FromBody] List<MediaPreprocessingResult> results)
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
            return BadRequest("Error exporting media preprocessing results.");
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
