using System.Globalization;
using System.IO.Compression;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Web.BackOffice.Controllers;
using BulkUpload.Core.Models;
using BulkUpload.Core.Services;

namespace BulkUpload.Core.Controllers;

public class MediaImportController : UmbracoAuthorizedApiController
{
    private readonly ILogger<MediaImportController> _logger;
    private readonly IMediaImportService _mediaImportService;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILegacyIdCache _legacyIdCache;

    public MediaImportController(
        ILogger<MediaImportController> logger,
        IMediaImportService mediaImportService,
        IParentLookupCache parentLookupCache,
        IMediaItemCache mediaItemCache,
        ILegacyIdCache legacyIdCache)
    {
        _logger = logger;
        _mediaImportService = mediaImportService;
        _parentLookupCache = parentLookupCache;
        _mediaItemCache = mediaItemCache;
        _legacyIdCache = legacyIdCache;
    }

    [HttpPost]
    public async Task<IActionResult> ImportMedia([FromForm] IFormFile file)
    {
        // Clear all caches at the start of each import to ensure fresh state
        _parentLookupCache.Clear();
        _mediaItemCache.Clear();
        _legacyIdCache.Clear();
        _logger.LogInformation("Bulk Upload Media: Cleared all caches for new import");

        string? tempDirectory = null;

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload Media: Uploaded file is not valid");
                return BadRequest("Uploaded file not valid.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".zip" && extension != ".csv")
            {
                _logger.LogError("Bulk Upload Media: File is not a ZIP or CSV file");
                return BadRequest("Please upload either a ZIP file (containing CSV and media files) or a CSV file (for URL-based media).");
            }

            string csvFilePath;
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

                // Find CSV file in extracted contents
                var csvFiles = Directory.GetFiles(tempDirectory, "*.csv", SearchOption.AllDirectories);
                if (csvFiles.Length == 0)
                {
                    _logger.LogError("Bulk Upload Media: No CSV file found in ZIP archive");
                    return BadRequest("No CSV file found in ZIP archive.");
                }

                csvFilePath = csvFiles[0]; // Use first CSV found
                if (csvFiles.Length > 1)
                {
                    _logger.LogWarning("Bulk Upload Media: Multiple CSV files found, using first: {CsvFile}", Path.GetFileName(csvFilePath));
                }
            }
            else
            {
                // CSV file uploaded directly - save to temporary location
                tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                csvFilePath = Path.Combine(tempDirectory, file.FileName);

                using (var stream = new FileStream(csvFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Bulk Upload Media: Processing CSV file directly (no media files in archive)");
            }

            // Parse CSV and import media
            var results = new List<MediaImportResult>();

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

                // Detect if this import supports update mode (per-file detection)
                if (records != null && records.Any())
                {
                    var firstRecord = (IDictionary<string, object>)records.First();
                    var hasUpdateColumn = firstRecord.Keys.Any(k =>
                        k.Split('|')[0].Equals("bulkUploadShouldUpdate", StringComparison.OrdinalIgnoreCase));
                    if (hasUpdateColumn)
                    {
                        _logger.LogInformation("Bulk Upload Media: Import file contains 'bulkUploadShouldUpdate' column - update mode is available. Each row's value will determine update vs create.");
                    }
                    else
                    {
                        _logger.LogInformation("Bulk Upload Media: Import file does not contain 'bulkUploadShouldUpdate' column - all items will be created.");
                    }
                }

                if (records != null && records.Any())
                {
                    foreach (var item in records)
                    {
                        Stream? fileStream = null;
                        try
                        {
                            MediaImportObject importObject = _mediaImportService.CreateMediaImportObject(item);

                            if (!importObject.CanImport)
                            {
                                results.Add(new MediaImportResult
                                {
                                    BulkUploadFileName = importObject.FileName,
                                    BulkUploadSuccess = false,
                                    BulkUploadErrorMessage = "Invalid import object: Missing required fields",
                                    BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                    BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                    BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                    OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                });
                                continue;
                            }

                            string actualFileName = importObject.FileName;

                            // Handle external media sources
                            if (importObject.ExternalSource != null)
                            {
                                switch (importObject.ExternalSource.Type)
                                {
                                    case Models.MediaSourceType.FilePath:
                                        var filePath = importObject.ExternalSource.Value;

                                        // Security validation
                                        if (!IsAllowedFilePath(filePath))
                                        {
                                            results.Add(new MediaImportResult
                                            {
                                                BulkUploadFileName = filePath,
                                                BulkUploadSuccess = false,
                                                BulkUploadErrorMessage = "Access to this file path is not allowed for security reasons",
                                                BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                            });
                                            continue;
                                        }

                                        if (!System.IO.File.Exists(filePath))
                                        {
                                            results.Add(new MediaImportResult
                                            {
                                                BulkUploadFileName = filePath,
                                                BulkUploadSuccess = false,
                                                BulkUploadErrorMessage = $"File not found at path: {filePath}",
                                                BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                            });
                                            _logger.LogWarning("Bulk Upload Media: File not found at path: {FilePath}", filePath);
                                            continue;
                                        }
                                        fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                                        actualFileName = Path.GetFileName(filePath);
                                        _logger.LogInformation("Bulk Upload Media: Reading from file path: {FilePath}", filePath);
                                        break;

                                    case Models.MediaSourceType.Url:
                                        {
                                            var url = importObject.ExternalSource.Value;

                                            // Security validation
                                            if (!IsAllowedUrl(url))
                                            {
                                                results.Add(new MediaImportResult
                                                {
                                                    BulkUploadFileName = url,
                                                    BulkUploadSuccess = false,
                                                    BulkUploadErrorMessage = "Access to this URL is not allowed for security reasons",
                                                    BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                    OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                                });
                                                continue;
                                            }

                                            using var httpClient = new HttpClient();
                                            httpClient.Timeout = TimeSpan.FromSeconds(30);

                                            try
                                            {
                                                _logger.LogInformation("Bulk Upload Media: Downloading from URL: {Url}", url);
                                                var response = await httpClient.GetAsync(url);
                                                response.EnsureSuccessStatusCode();

                                                var memoryStream = new MemoryStream();
                                                await response.Content.CopyToAsync(memoryStream);
                                                memoryStream.Position = 0;
                                                fileStream = memoryStream;

                                                // Extract filename from URL
                                                var uri = new Uri(url);
                                                actualFileName = Path.GetFileName(Uri.UnescapeDataString(uri.LocalPath));
                                                if (string.IsNullOrWhiteSpace(Path.GetExtension(actualFileName)))
                                                {
                                                    actualFileName += ".jpg"; // Default extension
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                results.Add(new MediaImportResult
                                                {
                                                    BulkUploadFileName = url,
                                                    BulkUploadSuccess = false,
                                                    BulkUploadErrorMessage = $"Failed to download from URL: {ex.Message}",
                                                    BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                    OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                                });
                                                _logger.LogError(ex, "Bulk Upload Media: Failed to download from URL: {Url}", url);
                                                continue;
                                            }
                                            break;
                                        }
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(importObject.FileName))
                            {
                                // Find the media file in extracted ZIP contents (only for ZIP uploads)
                                // Skip this for property-only updates (update mode with no fileName or no file available)
                                if (!isZipUpload && !importObject.BulkUploadShouldUpdate)
                                {
                                    results.Add(new MediaImportResult
                                    {
                                        BulkUploadFileName = importObject.FileName,
                                        BulkUploadSuccess = false,
                                        BulkUploadErrorMessage = $"Media file '{importObject.FileName}' requires a source. For CSV-only uploads, use mediaSource|urlToStream (for URLs) or mediaSource|pathToStream (for file paths).",
                                        BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                        BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                        BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                        OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                    });
                                    _logger.LogWarning("Bulk Upload Media: CSV-only upload requires external source for file: {FileName}", importObject.FileName);
                                    continue;
                                }

                                if (isZipUpload)
                                {
                                    var mediaFiles = Directory.GetFiles(tempDirectory!, importObject.FileName, SearchOption.AllDirectories);

                                    if (mediaFiles.Length == 0)
                                    {
                                        // Only error if NOT in update mode (update mode allows property-only updates)
                                        if (!importObject.BulkUploadShouldUpdate)
                                        {
                                            results.Add(new MediaImportResult
                                            {
                                                BulkUploadFileName = importObject.FileName,
                                                BulkUploadSuccess = false,
                                                BulkUploadErrorMessage = $"File not found in ZIP archive: {importObject.FileName}",
                                                BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                                BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                                OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                            });
                                            _logger.LogWarning("Bulk Upload Media: File not found in ZIP: {FileName}", importObject.FileName);
                                            continue;
                                        }
                                        // For update mode, null fileStream is OK (property-only update)
                                        _logger.LogDebug("Bulk Upload Media: File not found in ZIP but in update mode, proceeding with property-only update: {FileName}", importObject.FileName);
                                    }
                                    else
                                    {
                                        var mediaFilePath = mediaFiles[0];
                                        fileStream = new FileStream(mediaFilePath, FileMode.Open, FileAccess.Read);
                                    }
                                }
                            }

                            // Update fileName if it came from external source
                            if (!string.IsNullOrWhiteSpace(actualFileName))
                            {
                                importObject.FileName = actualFileName;
                            }

                            // Import the media item (fileStream can be null for property-only updates)
                            if (fileStream == null && !importObject.BulkUploadShouldUpdate)
                            {
                                // Only error if NOT in update mode
                                results.Add(new MediaImportResult
                                {
                                    BulkUploadFileName = importObject.FileName,
                                    BulkUploadSuccess = false,
                                    BulkUploadErrorMessage = "No file stream available for import",
                                    BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                    BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                    BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                    OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                });
                                continue;
                            }

                            var result = fileStream != null
                                ? _mediaImportService.ImportSingleMediaItem(importObject, fileStream)
                                : _mediaImportService.ImportSingleMediaItem(importObject, Stream.Null);
                            result.OriginalCsvData = ConvertCsvRecordToDictionary(item);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Bulk Upload Media: Error importing media item");

                            // Try to extract legacy ID from item if possible
                            string? legacyId = null;
                            try
                            {
                                var dynamicProperties = (IDictionary<string, object>)item;
                                if (dynamicProperties.TryGetValue("bulkUploadLegacyId", out object? legacyIdValue))
                                {
                                    legacyId = legacyIdValue?.ToString();
                                }
                            }
                            catch
                            {
                                // Ignore errors when trying to extract legacy ID
                            }

                            results.Add(new MediaImportResult
                            {
                                BulkUploadFileName = "Unknown",
                                BulkUploadSuccess = false,
                                BulkUploadErrorMessage = ex.Message,
                                BulkUploadLegacyId = legacyId,
                                OriginalCsvData = ConvertCsvRecordToDictionary(item)
                            });
                        }
                        finally
                        {
                            // Clean up stream
                            fileStream?.Dispose();
                        }
                    }
                }

                var successCount = results.Count(r => r.BulkUploadSuccess);
                var failureCount = results.Count(r => !r.BulkUploadSuccess);
                var totalCount = records?.Count ?? 0;

                _logger.LogInformation("Bulk Upload Media: Imported {SuccessCount} of {TotalCount} media items ({FailureCount} failed)",
                    successCount, totalCount, failureCount);

                return Ok(new
                {
                    totalCount,
                    successCount,
                    failureCount,
                    results
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Media: Error occurred while importing media from ZIP");
            return BadRequest("\r\n" + "Something went wrong while processing the media files. Please try again later.");
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
    public IActionResult ExportResults([FromBody] List<MediaImportResult> results)
    {
        try
        {
            if (results == null || !results.Any())
            {
                return BadRequest("No results to export.");
            }

            // Collect all unique original column names from all results (preserving order from first occurrence)
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

            var csv = new StringBuilder();

            // Build header: BulkUpload columns + original columns
            // Only include optional columns if they have data or existed in original CSV
            var headerParts = new List<string>
            {
                "bulkUploadFileName",
                "bulkUploadSuccess",
                "bulkUploadMediaGuid",
                "bulkUploadMediaUdi"
            };

            if (hasAnyErrors)
            {
                headerParts.Add("bulkUploadErrorMessage");
            }

            if (hadLegacyIdColumn)
            {
                headerParts.Add("bulkUploadLegacyId");
            }

            headerParts.Add("bulkUploadShouldUpdate");

            headerParts.AddRange(originalColumns);
            csv.AppendLine(string.Join(",", headerParts));

            // Build each row: BulkUpload values + original values
            foreach (var result in results)
            {
                var rowParts = new List<string>
                {
                    // BulkUpload columns (always included)
                    $"\"{result.BulkUploadFileName}\"",
                    result.BulkUploadSuccess.ToString(),
                    $"\"{result.BulkUploadMediaGuid}\"",
                    $"\"{result.BulkUploadMediaUdi}\""
                };

                // Optional BulkUpload columns (only if needed)
                if (hasAnyErrors)
                {
                    rowParts.Add($"\"{(result.BulkUploadErrorMessage?.Replace("\"", "\"\"") ?? "")}\"");
                }

                if (hadLegacyIdColumn)
                {
                    rowParts.Add($"\"{(result.BulkUploadLegacyId?.Replace("\"", "\"\"") ?? "")}\"");
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

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "media-import-results.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Media: Error exporting results");
            return BadRequest("Error exporting results.");
        }
    }

    /// <summary>
    /// Validates that a URL is safe to download from (basic SSRF prevention).
    /// </summary>
    private bool IsAllowedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Bulk Upload Media: Invalid URL format: {Url}", url);
            return false;
        }

        var host = uri.Host.ToLowerInvariant();

        // Block localhost and private IP ranges to prevent SSRF attacks
        if (host == "localhost" ||
            host == "127.0.0.1" ||
            host == "::1" ||
            host.StartsWith("192.168.") ||
            host.StartsWith("10.") ||
            host.StartsWith("172.16.") ||
            host.StartsWith("172.17.") ||
            host.StartsWith("172.18.") ||
            host.StartsWith("172.19.") ||
            host.StartsWith("172.20.") ||
            host.StartsWith("172.21.") ||
            host.StartsWith("172.22.") ||
            host.StartsWith("172.23.") ||
            host.StartsWith("172.24.") ||
            host.StartsWith("172.25.") ||
            host.StartsWith("172.26.") ||
            host.StartsWith("172.27.") ||
            host.StartsWith("172.28.") ||
            host.StartsWith("172.29.") ||
            host.StartsWith("172.30.") ||
            host.StartsWith("172.31."))
        {
            _logger.LogWarning("Bulk Upload Media: Blocked URL to private/local address: {Url}", url);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a file path is safe to read from (basic path traversal prevention).
    /// </summary>
    private bool IsAllowedFilePath(string path)
    {
        try
        {
            // Get the full path to resolve relative paths and normalize
            var fullPath = Path.GetFullPath(path);

            // Basic validation: ensure the path doesn't try to escape to system directories
            // This is a basic check - you may want to configure allowed base paths
            var normalizedPath = fullPath.Replace("\\", "/").ToLowerInvariant();

            // Block access to common system directories
            var blockedPaths = new[]
            {
                "/windows/system32",
                "/windows/syswow64",
                "/etc/",
                "/var/",
                "/usr/",
                "/sys/",
                "/proc/"
            };

            foreach (var blocked in blockedPaths)
            {
                if (normalizedPath.Contains(blocked))
                {
                    _logger.LogWarning("Bulk Upload Media: Blocked access to system directory: {Path}", path);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk Upload Media: Invalid file path: {Path}", path);
            return false;
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
