using System.Globalization;
using System.Text;

using BulkUpload.Constants;
using BulkUpload.Models;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

namespace BulkUpload.Services;

/// <inheritdoc cref="IBulkImportService" />
public class BulkImportService : IBulkImportService
{
    // The three import caches below are process-wide singletons that every import clears before it starts.
    // Overlapping imports would therefore wipe each other's parent/media/legacy-id lookups mid-run, so the
    // whole sequence runs under this gate and a second import queues behind the first.
    private readonly SemaphoreSlim _importGate = new(1, 1);

    private readonly IImportUtilityService _importUtilityService;
    private readonly IHierarchyResolver _hierarchyResolver;
    private readonly IMediaPreprocessorService _mediaPreprocessorService;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILegacyIdCache _legacyIdCache;
    private readonly ILogger<BulkImportService> _logger;

    public BulkImportService(
        IImportUtilityService importUtilityService,
        IHierarchyResolver hierarchyResolver,
        IMediaPreprocessorService mediaPreprocessorService,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILegacyIdCache legacyIdCache,
        ILogger<BulkImportService> logger)
    {
        _importUtilityService = importUtilityService;
        _hierarchyResolver = hierarchyResolver;
        _mediaPreprocessorService = mediaPreprocessorService;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _legacyIdCache = legacyIdCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContentImportResponse> ImportCsvFilesAsync(
        IReadOnlyList<string> csvFilePaths, string? mediaDirectory = null, CancellationToken ct = default)
    {
        await _importGate.WaitAsync(ct);
        try
        {
            ClearCaches();

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
                    await foreach (var record in csv.GetRecordsAsync<dynamic>(ct))
                    {
                        allRecordsWithSource.Add((record, sourceFileName));
                    }
                }

                _logger.LogInformation("Bulk Upload: Read {RecordCount} total records so far (current file: {CsvFile})",
                    allRecordsWithSource.Count, sourceFileName);
            }

            return Import(allRecordsWithSource, csvFilePaths.Count, mediaDirectory);
        }
        finally
        {
            _importGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ContentImportResponse> ImportRecordsAsync(
        IReadOnlyList<(IDictionary<string, object?> Record, string SourceFileName)> records,
        string? mediaDirectory = null, CancellationToken ct = default)
    {
        await _importGate.WaitAsync(ct);
        try
        {
            ClearCaches();

            var allRecordsWithSource = records
                .Select(r => ((dynamic)r.Record, r.SourceFileName))
                .ToList();

            var sourceFileCount = records
                .Select(r => r.SourceFileName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            _logger.LogInformation("Bulk Upload: Read {RecordCount} in-memory record(s) from {CsvCount} source(s)",
                allRecordsWithSource.Count, sourceFileCount);

            return Import(allRecordsWithSource, sourceFileCount, mediaDirectory);
        }
        finally
        {
            _importGate.Release();
        }
    }

    private void ClearCaches()
    {
        // Clear all caches at the start of each import to ensure fresh state
        _parentLookupCache.Clear();
        _mediaItemCache.Clear();
        _legacyIdCache.Clear();
        _logger.LogInformation("Bulk Upload: Cleared all caches for new import");
    }

    /// <summary>
    /// The import sequence itself, shared by both entry points. Deliberately has no try/catch: a resolver
    /// or hierarchy failure must reach the caller with its real message. The HTTP controller wraps this in
    /// its own catch to produce a 500.
    /// </summary>
    private ContentImportResponse Import(
        List<(dynamic record, string sourceFileName)> allRecordsWithSource, int csvCount, string? mediaDirectory)
    {
        if (!allRecordsWithSource.Any())
        {
            _logger.LogInformation("Bulk Upload: No valid records found in any CSV file");
            return new ContentImportResponse
            {
                TotalCount = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Results = new List<ContentImportResult>()
            };
        }

        // Log whether the import can write data to existing content (per-file detection, logging only)
        var firstRecord = (IDictionary<string, object>)allRecordsWithSource.First().record;
        var hasUpdateColumn = firstRecord.Keys.Any(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadShouldUpdate, StringComparison.OrdinalIgnoreCase));
        if (hasUpdateColumn)
        {
            _logger.LogInformation("Bulk Upload: Import file contains 'bulkUploadShouldUpdate' column - rows with a content GUID and a true value will have their data written.");
        }
        else
        {
            _logger.LogInformation("Bulk Upload: Import file does not contain 'bulkUploadShouldUpdate' column - data will not be written to existing content; rows with a content GUID can only change publish state.");
        }

        // Step 2: Preprocess media items from all CSV files to avoid duplicates
        _logger.LogDebug("Preprocessing media items from all CSV records across {CsvCount} file(s)", csvCount);
        var allMediaPreprocessingResults = _mediaPreprocessorService.PreprocessMediaItems(allRecordsWithSource, mediaDirectory);

        // Step 3: Create all ImportObjects from all CSV records with source tracking
        var allImportObjects = new List<ImportObject>();
        var skippedCount = 0;
        foreach (var (record, sourceFileName) in allRecordsWithSource)
        {
            ImportObject importObject = _importUtilityService.CreateImportObject(record);
            importObject.OriginalCsvData = ConvertCsvRecordToDictionary(record);
            importObject.SourceCsvFileName = sourceFileName;

            if (ShouldSkip(importObject, out var skipReason))
            {
                skippedCount++;
                _logger.LogDebug("Skipping row '{Name}' - {Reason}", importObject.Name, skipReason);
                continue;
            }

            if (importObject.CanImport)
            {
                allImportObjects.Add(importObject);
            }
        }

        if (skippedCount > 0)
        {
            _logger.LogInformation("Bulk Upload: Skipped {SkippedCount} row(s) with nothing to do", skippedCount);
        }

        // Step 4: Validate and sort ALL import objects across all CSV files based on legacy hierarchy
        // This ensures parent-child relationships work correctly even when spread across different CSV files
        var sortedImportObjects = _hierarchyResolver.ValidateAndSort(allImportObjects);
        _logger.LogDebug("Sorted {Count} import objects for processing across {CsvCount} CSV file(s)",
            sortedImportObjects.Count, csvCount);

        // Step 5: Import in sorted order (parents before children) and collect results
        var allResults = new List<ContentImportResult>();
        foreach (var importObject in sortedImportObjects)
        {
            var result = _importUtilityService.ImportSingleItem(importObject, importObject.BulkUploadShouldPublish, importObject.BulkUploadShouldUnpublish);
            allResults.Add(result);
        }

        var totalSuccessCount = allResults.Count(r => r.BulkUploadSuccess);
        var totalFailureCount = allResults.Count(r => !r.BulkUploadSuccess);

        _logger.LogInformation("Bulk Upload: Completed processing {CsvCount} CSV file(s) - {Total} total records, {Success} successful, {Failed} failed",
            csvCount, allResults.Count, totalSuccessCount, totalFailureCount);

        return new ContentImportResponse
        {
            TotalCount = allResults.Count,
            SuccessCount = totalSuccessCount,
            FailureCount = totalFailureCount,
            Results = allResults,
            MediaPreprocessingResults = allMediaPreprocessingResults
        };
    }

    /// <summary>
    /// Whether a row has nothing to do and is skipped silently, producing no result.
    /// A row for existing content is skipped when it neither writes data nor changes publish state.
    /// A row for new content is skipped when the bulkUploadShouldUpdate column is present and falsy.
    /// </summary>
    private static bool ShouldSkip(ImportObject importObject, out string reason)
    {
        if (importObject.BulkUploadContentGuid.HasValue)
        {
            if (!importObject.BulkUploadShouldUpdate
                && !importObject.BulkUploadShouldPublish
                && !importObject.BulkUploadShouldUnpublish)
            {
                reason = "none of bulkUploadShouldUpdate, bulkUploadShouldPublish or bulkUploadShouldUnpublish is true";
                return true;
            }
        }
        else if (importObject.BulkUploadShouldUpdateColumnExisted && !importObject.BulkUploadShouldUpdate)
        {
            reason = "bulkUploadShouldUpdate is false on a row without bulkUploadContentGuid";
            return true;
        }

        reason = "";
        return false;
    }

    /// <summary>
    /// Converts a dynamic CSV record to a dictionary preserving column names with resolver syntax
    /// </summary>
    private static Dictionary<string, string> ConvertCsvRecordToDictionary(dynamic record)
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
