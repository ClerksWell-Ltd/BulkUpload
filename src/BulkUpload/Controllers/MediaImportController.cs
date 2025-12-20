using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Controllers;

public class MediaImportController : UmbracoAuthorizedApiController
{
    private readonly ILogger<MediaImportController> _logger;
    private readonly IMediaImportService _mediaImportService;

    public MediaImportController(
        ILogger<MediaImportController> logger,
        IMediaImportService mediaImportService)
    {
        _logger = logger;
        _mediaImportService = mediaImportService;
    }

    [HttpPost]
    public async Task<IActionResult> ImportMedia([FromForm] IFormFile file)
    {
        string? tempDirectory = null;

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload Media: Uploaded ZIP file is not valid");
                return BadRequest("Uploaded ZIP file not valid.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".zip")
            {
                _logger.LogError("Bulk Upload Media: File is not a ZIP archive");
                return BadRequest("Please upload a ZIP file containing CSV and media files.");
            }

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

            var csvFilePath = csvFiles[0]; // Use first CSV found
            if (csvFiles.Length > 1)
            {
                _logger.LogWarning("Bulk Upload Media: Multiple CSV files found, using first: {CsvFile}", Path.GetFileName(csvFilePath));
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

                if (records != null && records.Any())
                {
                    foreach (var item in records)
                    {
                        try
                        {
                            MediaImportObject importObject = _mediaImportService.CreateMediaImportObject(item);

                            if (!importObject.CanImport)
                            {
                                results.Add(new MediaImportResult
                                {
                                    FileName = importObject.FileName,
                                    Success = false,
                                    ErrorMessage = "Invalid import object: Missing required fields"
                                });
                                continue;
                            }

                            // Find the media file in extracted contents
                            var mediaFiles = Directory.GetFiles(tempDirectory, importObject.FileName, SearchOption.AllDirectories);

                            if (mediaFiles.Length == 0)
                            {
                                results.Add(new MediaImportResult
                                {
                                    FileName = importObject.FileName,
                                    Success = false,
                                    ErrorMessage = $"File not found in ZIP archive: {importObject.FileName}"
                                });
                                _logger.LogWarning("Bulk Upload Media: File not found: {FileName}", importObject.FileName);
                                continue;
                            }

                            var mediaFilePath = mediaFiles[0];

                            // Import the media item
                            using var fileStream = new FileStream(mediaFilePath, FileMode.Open, FileAccess.Read);
                            var result = _mediaImportService.ImportSingleMediaItem(importObject, fileStream);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Bulk Upload Media: Error importing media item");
                            results.Add(new MediaImportResult
                            {
                                FileName = "Unknown",
                                Success = false,
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                }

                var successCount = results.Count(r => r.Success);
                var failureCount = results.Count(r => !r.Success);

                _logger.LogInformation("Bulk Upload Media: Imported {SuccessCount} of {TotalCount} media items ({FailureCount} failed)",
                    successCount, records.Count, failureCount);

                return Ok(new
                {
                    TotalCount = records.Count,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Results = results
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

            var csv = new StringBuilder();
            csv.AppendLine("fileName,success,mediaId,mediaGuid,mediaUdi,errorMessage");

            foreach (var result in results)
            {
                csv.AppendLine($"\"{result.FileName}\",{result.Success},{result.MediaId ?? 0},\"{result.MediaGuid}\",\"{result.MediaUdi}\",\"{result.ErrorMessage}\"");
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
}
