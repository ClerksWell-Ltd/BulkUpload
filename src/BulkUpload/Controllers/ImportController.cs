
using System.Globalization;

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
        IMediaPreprocessorService mediaPreprocessorService)
    {
        _logger = logger;
        _importUtilityService = importUtilityService;
        _hierarchyResolver = hierarchyResolver;
        _mediaPreprocessorService = mediaPreprocessorService;
    }

    [HttpPost]
    public async Task<IActionResult> ImportAll([FromForm] IFormFile file)
    {
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
                        _mediaPreprocessorService.PreprocessMediaItems(records);

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

                        // Step 4: Import in sorted order (parents before children)
                        foreach (var importObject in sortedImportObjects)
                        {
                            _importUtilityService.ImportSingleItem(importObject);
                        }

                    }

                    _logger.LogInformation("Bulk Upload: Successfully imported {Count} records from CSV", records.Count);

                    return Ok(new { Count = records.Count, Sample = records.FirstOrDefault() });
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Upload Case Studies: Error occurred while importing case studies from CSV");
            return BadRequest("\r\n" + "Something went wrong while processing the records. Please try after some time.");
        }
    }
}