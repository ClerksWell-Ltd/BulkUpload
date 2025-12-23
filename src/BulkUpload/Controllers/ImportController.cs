
using System.Globalization;
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

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload: Uploaded csv file is not valid");
                return BadRequest("Uploaded CSV file not valid.");
            }
            else
            {

                using (var reader = new StreamReader(file.OpenReadStream()))
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
                        _logger.LogDebug("Preprocessing media items from CSV records");
                        var mediaPreprocessingResults = _mediaPreprocessorService.PreprocessMediaItems(records);

                        // Step 2: Create all ImportObjects from CSV records
                        var importObjects = new List<ImportObject>();
                        foreach (var item in records)
                        {
                            ImportObject importObject = _importUtilityService.CreateImportObject(item);
                            if (importObject.CanImport)
                            {
                                importObjects.Add(importObject);
                            }
                        }

                        // Step 3: Validate and sort based on legacy hierarchy (if present)
                        var sortedImportObjects = _hierarchyResolver.ValidateAndSort(importObjects);
                        _logger.LogDebug("Sorted {Count} import objects for processing", sortedImportObjects.Count);

                        // Step 4: Import in sorted order (parents before children) and collect results
                        var results = new List<ContentImportResult>();
                        foreach (var importObject in sortedImportObjects)
                        {
                            var result = _importUtilityService.ImportSingleItem(importObject);
                            results.Add(result);
                        }

                        var successCount = results.Count(r => r.Success);
                        var failureCount = results.Count(r => !r.Success);

                        _logger.LogInformation("Bulk Upload: Processed {Total} records - {Success} successful, {Failed} failed",
                            results.Count, successCount, failureCount);

                        return Ok(new
                        {
                            totalCount = results.Count,
                            successCount = successCount,
                            failureCount = failureCount,
                            results = results,
                            mediaPreprocessingResults = mediaPreprocessingResults
                        });
                    }

                    _logger.LogInformation("Bulk Upload: No valid records found in CSV");
                    return Ok(new { totalCount = 0, successCount = 0, failureCount = 0, results = new List<ContentImportResult>() });
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Case Studies: Error occurred while importing case studies from CSV");
            return BadRequest("\r\n" + "Something went wrong while processing the records. Please try after some time.");
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

            var csv = new StringBuilder();
            csv.AppendLine("contentName,success,contentId,contentGuid,contentUdi,errorMessage,bulkUploadLegacyId");

            foreach (var result in results)
            {
                var escapedErrorMessage = result.ErrorMessage?.Replace("\"", "\"\"") ?? "";
                var escapedLegacyId = result.BulkUploadLegacyId?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"\"{result.ContentName}\",{result.Success},{result.ContentId ?? 0},\"{result.ContentGuid}\",\"{result.ContentUdi}\",\"{escapedErrorMessage}\",\"{escapedLegacyId}\"");
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
}