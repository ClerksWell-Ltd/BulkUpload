using System.Globalization;
using System.IO.Compression;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
public class MediaImportController : UmbracoAuthorizedApiController
#else
/// <summary>
/// Media Import API for importing media files from CSV/ZIP files, URLs, or server file paths into Umbraco CMS.
/// Supports auto-folder creation, media deduplication, and update mode for property-only updates.
/// </summary>
[Route("api/v{version:apiVersion}/media")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Media")]
[MapToApi("bulk-upload")]
[ApiController]
public class MediaImportController : ControllerBase
#endif
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

    /// <summary>
    /// Imports media files from a CSV file or ZIP archive into the Umbraco media library.
    /// </summary>
    /// <param name="model">Request model containing the file to import.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the import operation.</param>
    /// <returns>
    /// Import results containing total counts, success/failure counts, and detailed information for each imported media item.
    /// Includes media GUIDs, UDIs, filenames, and error messages if any imports failed.
    /// </returns>
    /// <remarks>
    /// <para>This endpoint supports multiple media source types:</para>
    /// <list type="bullet">
    ///   <item><description><strong>ZIP file:</strong> Media files included in the archive (use fileName column in CSV)</description></item>
    ///   <item><description><strong>URL download:</strong> Download media from URL (use mediaSource|urlToStream column in CSV)</description></item>
    ///   <item><description><strong>Server file path:</strong> Import from server file path (use mediaSource|pathToStream column in CSV)</description></item>
    /// </list>
    ///
    /// <para><strong>Key Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Auto-folder creation:</strong> Parent folders are automatically created based on the parent column</description></item>
    ///   <item><description><strong>Media deduplication:</strong> Same media file referenced multiple times is created only once</description></item>
    ///   <item><description><strong>Auto-type detection:</strong> Media type is automatically detected from file extension</description></item>
    ///   <item><description><strong>Update mode:</strong> Use bulkUploadShouldUpdate column to update existing media properties without replacing files</description></item>
    ///   <item><description><strong>SSRF protection:</strong> URL downloads are validated to prevent Server-Side Request Forgery attacks</description></item>
    /// </list>
    ///
    /// <para><strong>Required CSV Columns:</strong></para>
    /// <list type="bullet">
    ///   <item><description>fileName - Media filename (for ZIP uploads) or desired name</description></item>
    /// </list>
    ///
    /// <para><strong>Optional CSV Columns:</strong></para>
    /// <list type="bullet">
    ///   <item><description>parent - Parent folder ID, GUID, or path (creates folders automatically)</description></item>
    ///   <item><description>name - Display name for the media item (defaults to fileName)</description></item>
    ///   <item><description>mediaTypeAlias - Media type alias (auto-detected from extension if not provided)</description></item>
    ///   <item><description>mediaSource|urlToStream - URL to download media from</description></item>
    ///   <item><description>mediaSource|pathToStream - Server file path to import media from</description></item>
    ///   <item><description>bulkUploadLegacyId - Legacy CMS identifier for this media item</description></item>
    ///   <item><description>bulkUploadShouldUpdate - Set to 'true' to update properties without replacing file</description></item>
    ///   <item><description>Additional property columns using propertyAlias|resolverAlias syntax</description></item>
    /// </list>
    ///
    /// <para><strong>Security:</strong></para>
    /// <para>URL downloads are protected against SSRF attacks by blocking localhost and private IP ranges (192.168.x.x, 10.x.x.x, 172.16-31.x.x).
    /// File path imports are validated to prevent path traversal attacks to system directories.</para>
    /// </remarks>
    /// <example>
    /// Example ZIP structure for media import:
    /// <code>
    /// media-upload.zip
    /// ├── media.csv
    /// └── files/
    ///     ├── product-image-1.jpg
    ///     ├── product-image-2.jpg
    ///     └── brochure.pdf
    /// </code>
    ///
    /// Example CSV content (ZIP-based import):
    /// <code>
    /// fileName,name,parent,altText
    /// "files/product-image-1.jpg","Product Image 1","umb://media-folder/1234","Our flagship product"
    /// "files/product-image-2.jpg","Product Image 2","umb://media-folder/1234","Secondary product view"
    /// "files/brochure.pdf","Product Brochure","umb://media-folder/5678",""
    /// </code>
    ///
    /// Example CSV content (URL-based import):
    /// <code>
    /// fileName,mediaSource|urlToStream,parent
    /// "hero-image.jpg","https://example.com/images/hero.jpg","umb://media-folder/1234"
    /// "logo.png","https://example.com/branding/logo.png","umb://media-folder/5678"
    /// </code>
    /// </example>
    [HttpPost]
#if !NET8_0
    [Route("importmedia")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
#endif
    public async Task<IActionResult> ImportMedia([FromForm] ImportMediaRequestModel model, CancellationToken cancellationToken = default)
    {
        // Clear all caches at the start of each import to ensure fresh state
        _parentLookupCache.Clear();
        _mediaItemCache.Clear();
        _legacyIdCache.Clear();
        _logger.LogInformation("Bulk Upload Media: Cleared all caches for new import");

        string? tempDirectory = null;

        try
        {
            var file = model.File;
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload Media: Uploaded file is not valid");
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
                _logger.LogError("Bulk Upload Media: File is not a ZIP or CSV file");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File Type",
                    Detail = "Please upload either a ZIP file (containing CSV and media files) or a CSV file (for URL-based media).",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
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
                    return BadRequest(new ProblemDetails
                    {
                        Title = "No CSV Found",
                        Detail = "No CSV file found in ZIP archive. Please ensure your ZIP contains at least one .csv file.",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    });
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

            using (var reader = new StreamReader(csvFilePath, Encoding.UTF8))
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

                            // Auto-detect ZIP file source when fileName is provided but no explicit mediaSource
                            // This handles the common case where users only specify fileName in the CSV for ZIP uploads
                            if (isZipUpload && importObject.ExternalSource == null && !string.IsNullOrWhiteSpace(importObject.FileName))
                            {
                                importObject.ExternalSource = new MediaSource
                                {
                                    Type = MediaSourceType.ZipFile,
                                    Value = importObject.FileName
                                };
                                _logger.LogDebug("Auto-detected fileName '{FileName}' as ZipFile source", importObject.FileName);
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

                                    case Models.MediaSourceType.ZipFile:
                                        {
                                            // Get the relative path from the mediaSource value
                                            var zipFileName = importObject.ExternalSource.Value;

                                            if (string.IsNullOrWhiteSpace(tempDirectory))
                                            {
                                                results.Add(new MediaImportResult
                                                {
                                                    BulkUploadFileName = zipFileName,
                                                    BulkUploadSuccess = false,
                                                    BulkUploadErrorMessage = "ZipFile media source requires a ZIP upload, but no ZIP file was provided",
                                                    BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                    OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                                });
                                                _logger.LogWarning("Bulk Upload Media: ZipFile source specified but no ZIP uploaded");
                                                continue;
                                            }

                                            string[]? mediaFiles = null;

                                            // Check if fileName contains a folder path (/ or \)
                                            if (zipFileName.Contains('/') || zipFileName.Contains('\\'))
                                            {
                                                // Normalize path separators to system separator
                                                var normalizedFileName = zipFileName.Replace('/', Path.DirectorySeparatorChar)
                                                                                     .Replace('\\', Path.DirectorySeparatorChar);

                                                // Try to find file at specific relative path first
                                                var specificPath = Path.Combine(tempDirectory, normalizedFileName);
                                                if (System.IO.File.Exists(specificPath))
                                                {
                                                    mediaFiles = new[] { specificPath };
                                                    _logger.LogDebug("Bulk Upload Media: Found file at specific path: {Path}", specificPath);
                                                }
                                                else
                                                {
                                                    // Fall back to searching by just the filename anywhere in the ZIP
                                                    var fileNameOnly = Path.GetFileName(normalizedFileName);
                                                    mediaFiles = Directory.GetFiles(tempDirectory, fileNameOnly, SearchOption.AllDirectories);
                                                    if (mediaFiles.Length > 0)
                                                    {
                                                        _logger.LogWarning("Bulk Upload Media: File '{FileName}' not found at specified path, but found '{FileNameOnly}' elsewhere in ZIP",
                                                            zipFileName, fileNameOnly);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // No folder path specified - search anywhere in ZIP
                                                mediaFiles = Directory.GetFiles(tempDirectory, zipFileName, SearchOption.AllDirectories);
                                            }

                                            if (mediaFiles == null || mediaFiles.Length == 0)
                                            {
                                                // Only error if NOT in update mode (update mode allows property-only updates)
                                                if (!importObject.BulkUploadShouldUpdate)
                                                {
                                                    results.Add(new MediaImportResult
                                                    {
                                                        BulkUploadFileName = zipFileName,
                                                        BulkUploadSuccess = false,
                                                        BulkUploadErrorMessage = $"File not found in ZIP archive: {zipFileName}",
                                                        BulkUploadLegacyId = importObject.BulkUploadLegacyId,
                                                        BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                                        BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                                        OriginalCsvData = ConvertCsvRecordToDictionary(item)
                                                    });
                                                    _logger.LogWarning("Bulk Upload Media: File not found in ZIP: {FileName}", zipFileName);
                                                    continue;
                                                }
                                                // For update mode, null fileStream is OK (property-only update)
                                                _logger.LogDebug("Bulk Upload Media: File not found in ZIP but in update mode, proceeding with property-only update: {FileName}", zipFileName);
                                            }
                                            else
                                            {
                                                var mediaFilePath = mediaFiles[0];
                                                fileStream = new FileStream(mediaFilePath, FileMode.Open, FileAccess.Read);
                                                actualFileName = zipFileName;
                                                _logger.LogInformation("Bulk Upload Media: Reading from ZIP file: {FilePath}", zipFileName);
                                            }
                                            break;
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

                return Ok(new MediaImportResponse
                {
                    TotalCount = totalCount,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Results = results
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Media: Error occurred while importing media from CSV/ZIP");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Import Failed",
                Detail = "An unexpected error occurred while processing the media files. Please check the logs for details.",
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
    /// Exports media import results to a CSV file for review and further processing.
    /// </summary>
    /// <param name="results">Array of media import result objects from a previous ImportMedia operation.</param>
    /// <returns>
    /// A CSV file containing the media import results with success status, GUIDs, UDIs, filenames, error messages, and original CSV data.
    /// </returns>
    /// <remarks>
    /// <para>The exported CSV file includes the following columns:</para>
    /// <list type="bullet">
    ///   <item><description><strong>bulkUploadFileName</strong> - Original filename of the media item</description></item>
    ///   <item><description><strong>bulkUploadSuccess</strong> - true/false indicating if the import was successful</description></item>
    ///   <item><description><strong>bulkUploadMediaGuid</strong> - GUID of the created/updated media item</description></item>
    ///   <item><description><strong>bulkUploadMediaUdi</strong> - UDI of the created/updated media item (for use in content references)</description></item>
    ///   <item><description><strong>bulkUploadErrorMessage</strong> - Error details (only included if errors occurred)</description></item>
    ///   <item><description><strong>bulkUploadLegacyId</strong> - Legacy CMS identifier (only if used in import)</description></item>
    ///   <item><description><strong>bulkUploadShouldUpdate</strong> - Update flag value</description></item>
    ///   <item><description><strong>Original columns</strong> - All original CSV columns are preserved</description></item>
    /// </list>
    ///
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description>Audit trail of media import operations</description></item>
    ///   <item><description>Error analysis and debugging</description></item>
    ///   <item><description>Getting media UDIs for use in content property values</description></item>
    ///   <item><description>Preparing update imports (use bulkUploadMediaGuid for subsequent updates)</description></item>
    ///   <item><description>Legacy ID mapping for future imports</description></item>
    /// </list>
    /// </remarks>
    [HttpPost]
#if !NET8_0
    [Route("exportresults")]
    [Consumes("application/json")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
#endif
    public IActionResult ExportResults([FromBody] List<MediaImportResult> results)
    {
        try
        {
            if (results == null || !results.Any())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "No Results",
                    Detail = "No results to export. Please provide a non-empty array of media import results.",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
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
            _logger.LogError(ex, "Bulk Upload Media: Error exporting media import results");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Export Failed",
                Detail = "An unexpected error occurred while exporting the results. Please check the logs for details.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            });
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