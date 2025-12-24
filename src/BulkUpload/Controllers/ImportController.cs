
using System.Globalization;
using System.IO.Compression;
using System.Text;

using BulkUpload.Services;

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
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Services;


namespace Umbraco.Community.BulkUpload.Controllers;

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

            // Aggregate results from all CSV files
            var allResults = new List<ContentImportResult>();
            var allMediaPreprocessingResults = new List<MediaPreprocessingResult>();

            // Process each CSV file
            foreach (var csvFilePath in csvFilePaths)
            {
                _logger.LogInformation("Bulk Upload: Processing CSV file: {CsvFile}", Path.GetFileName(csvFilePath));

                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                }))
                {
                    var records = new List<dynamic>();

                    await foreach (var record in csv.GetRecordsAsync<dynamic>())
                    {
                        records.Add(record);
                    }

                    if (records != null && records.Any())
                    {
                        // Step 1: Preprocess media items to avoid duplicates
                        _logger.LogDebug("Preprocessing media items from CSV records in {CsvFile}", Path.GetFileName(csvFilePath));
                        var mediaPreprocessingResults = _mediaPreprocessorService.PreprocessMediaItems(records, tempDirectory);
                        allMediaPreprocessingResults.AddRange(mediaPreprocessingResults);

                        // Step 2: Create all ImportObjects from CSV records
                        var importObjects = new List<ImportObject>();
                        foreach (var item in records)
                        {
                            ImportObject importObject = _importUtilityService.CreateImportObject(item);
                            importObject.OriginalCsvData = ConvertCsvRecordToDictionary(item);
                            if (importObject.CanImport)
                            {
                                importObjects.Add(importObject);
                            }
                        }

                        // Step 3: Validate and sort based on legacy hierarchy (if present)
                        var sortedImportObjects = _hierarchyResolver.ValidateAndSort(importObjects);
                        _logger.LogDebug("Sorted {Count} import objects for processing from {CsvFile}", sortedImportObjects.Count, Path.GetFileName(csvFilePath));

                        // Step 4: Import in sorted order (parents before children) and collect results
                        var results = new List<ContentImportResult>();
                        foreach (var importObject in sortedImportObjects)
                        {
                            var result = _importUtilityService.ImportSingleItem(importObject, importObject.ShouldPublish);
                            results.Add(result);
                        }

                        var successCount = results.Count(r => r.BulkUploadSuccess);
                        var failureCount = results.Count(r => !r.BulkUploadSuccess);

                        _logger.LogInformation("Bulk Upload: Processed {Total} records from {CsvFile} - {Success} successful, {Failed} failed",
                            results.Count, Path.GetFileName(csvFilePath), successCount, failureCount);

                        allResults.AddRange(results);
                    }
                    else
                    {
                        _logger.LogInformation("Bulk Upload: No valid records found in {CsvFile}", Path.GetFileName(csvFilePath));
                    }
                }
            }

            // Return aggregated results from all CSV files
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

            // Collect all unique original column names from all results (preserving order from first occurrence)
            var originalColumns = new List<string>();
            foreach (var result in results)
            {
                if (result.OriginalCsvData != null)
                {
                    foreach (var key in result.OriginalCsvData.Keys)
                    {
                        if (!originalColumns.Contains(key))
                        {
                            originalColumns.Add(key);
                        }
                    }
                }
            }

            var csv = new StringBuilder();

            // Build header: BulkUpload columns + original columns
            var headerParts = new List<string>
            {
                "bulkUploadContentName",
                "bulkUploadSuccess",
                "bulkUploadContentGuid",
                "bulkUploadParentGuid",
                "bulkUploadErrorMessage",
                "bulkUploadLegacyId"
            };
            headerParts.AddRange(originalColumns);
            csv.AppendLine(string.Join(",", headerParts));

            // Build each row: BulkUpload values + original values
            foreach (var result in results)
            {
                var rowParts = new List<string>();

                // BulkUpload columns
                rowParts.Add($"\"{result.BulkUploadContentName}\"");
                rowParts.Add(result.BulkUploadSuccess.ToString());
                rowParts.Add($"\"{result.BulkUploadContentGuid}\"");
                rowParts.Add($"\"{result.BulkUploadParentGuid}\"");
                rowParts.Add($"\"{(result.BulkUploadErrorMessage?.Replace("\"", "\"\"") ?? "")}\"");
                rowParts.Add($"\"{(result.BulkUploadLegacyId?.Replace("\"", "\"\"") ?? "")}\"");

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

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "content-import-results.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload: Error exporting results");
            return BadRequest("Error exporting results.");
        }
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

            var csv = new StringBuilder();
            csv.AppendLine("key,value");

            foreach (var result in results)
            {
                var escapedKey = result.Key?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"\"{escapedKey}\",\"{result.Value}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "media-preprocessing-results.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload: Error exporting media preprocessing results");
            return BadRequest("Error exporting media preprocessing results.");
        }
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