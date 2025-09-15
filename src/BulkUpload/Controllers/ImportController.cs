
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
    private readonly IContentService _contentService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IContentTypeService _contentTypeService;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageRepository _languageRepository;
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly ILogger<BulkUploadController> _logger;
    private readonly IImportUtilityService _importUtilityService;
    public BulkUploadController(IContentService contentService, IUmbracoContextAccessor umbracoContextAccessor, IContentTypeService contentTypeService, IJsonSerializer jsonSerializer, ILocalizationService localizationService, ILanguageRepository languageRepository, ICoreScopeProvider coreScopeProvider, ILogger<BulkUploadController> logger, IImportUtilityService importUtilityService)
    {
        _contentService = contentService;
        _umbracoContextAccessor = umbracoContextAccessor;
        _contentTypeService = contentTypeService;
        _jsonSerializer = jsonSerializer;
        _localizationService = localizationService;
        _languageRepository = languageRepository;
        _coreScopeProvider = coreScopeProvider;
        _logger = logger;
        _importUtilityService = importUtilityService;
    }

    private DateTime? TryParseCsvDate(string csvValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(csvValue))
            {
                return null;
            }
            string[] acceptedFormats = new[]
            {
                // Year-first formats
                "yyyy-MM-dd", // 2025-05-27
                "yyyy/MM/dd", // 2025/05/27
                "yyyy.MM.dd", // 2025.05.27
                "yyyy-M-d", // 2025-5-7
                "yyyy/M/d", // 2025/5/7
                "yyyy.M.d", // 2025.5.7

                // UK-style (day first)
                "dd-MM-yyyy", // 27-05-2025
                "d-M-yyyy", // 7-5-2025
                "dd/MM/yyyy", // 27/05/2025
                "d/M/yyyy", // 7/5/2025
                "dd.MM.yyyy", // 27.05.2025
                "d.M.yyyy", // 7.5.2025

                // US-style (month first)
                "MM-dd-yyyy", // 05-27-2025
                "M-d-yyyy", // 5-7-2025
                "MM/dd/yyyy", // 05/27/2025
                "M/d/yyyy", // 5/7/2025
                "MM.dd.yyyy", // 05.27.2025
                "M.d.yyyy", // 5.7.2025

                // Year first with short/long month names
                "yyyy-MMM-dd", // 2025-May-27
                "yyyy-MMMM-dd", // 2025-May-27

                // UK-style with month names
                "dd MMM yyyy", // 27 May 2025
                "d MMM yyyy", // 7 May 2025
                "dd MMMM yyyy", // 27 May 2025
                "d MMMM yyyy", // 7 May 2025

                // US-style with month names
                "MMM dd, yyyy", // May 27, 2025
                "MMMM dd, yyyy", // May 27, 2025
                "MMM d, yyyy", // May 7, 2025
                "MMMM d, yyyy", // May 7, 2025

                // Month-name first, no comma (less common but valid)
                "MMM dd yyyy", // May 27 2025
                "MMMM dd yyyy", // May 27 2025
                "MMM d yyyy", // May 7 2025
                "MMMM d yyyy", // May 7 2025

                // Partial formats (requires you to append year manually if missing)
                "dd MMM", // 27 May
                "d MMM", // 7 May
                "MMM dd", // May 27
                "MMM d", // May 7

                // Full weekday name
                "dddd, dd MMM yyyy", // Tuesday, 27 May 2025
                "dddd, d MMM yyyy", // Tuesday, 7 May 2025
                "dddd, dd MMMM yyyy", // Tuesday, 27 May 2025
                "dddd, d MMMM yyyy", // Tuesday, 7 May 2025

                // With time (24h)
                "yyyy-MM-dd HH:mm:ss", // 2025-05-27 14:30:00
                "dd-MM-yyyy HH:mm:ss", // 27-05-2025 14:30:00
                "d-M-yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "d/M/yyyy HH:mm:ss",
                "MM-dd-yyyy HH:mm:ss", // 05-27-2025 14:30:00
                "dd MMM yyyy HH:mm:ss", // 27 May 2025 14:30:00
                "MMM dd, yyyy HH:mm:ss", // May 27, 2025 14:30:00
                "MMMM dd, yyyy HH:mm:ss", // May 27, 2025 14:30:00

                // ISO formats
                "yyyy-MM-ddTHH:mm:ss", // 2025-05-27T14:30:00
                "yyyy-MM-ddTHH:mm:ssZ", // 2025-05-27T14:30:00Z
                "yyyy-MM-ddTHH:mm:ss.fff", // 2025-05-27T14:30:00.123
                "yyyy-MM-ddTHH:mm:ss.fffZ", // 2025-05-27T14:30:00.123Z

                // Compact numeric
                "yyyyMMdd", // 20250527
                "yyyyMMddHHmmss", // 20250527143000

                // Short formats (risky if year isn't enforced manually)
                "d-M-yy", // 7-5-25
                "dd-MM-yy", // 27-05-25
                "M/d/yy", // 5/7/25
                "MM/dd/yy", // 05/27/25
                "dd/MM/yy", // 27/05/25

                //Day-Month
                "d MMM",
                "d MMMM",
                "dd MMM",
                "dd MMMM",

                //Month-Day
                "MMM d",
                "MMMM d",
                "MMM dd",
                "MMMM dd",

                //with dashes 
                "d-MMM",
                "d-MMMM",
                "dd-MMM",
                "dd-MMMM",
                "MMM-d",
                "MMMM-d",
                "MMM-dd",
                "MMMM-dd",

                //with slashes
                "d/MMM",
                "d/MMMM",
                "dd/MMM",
                "dd/MMMM",
                "MMM/d",
                "MMMM/d",
                "MMM/dd",
                "MMMM/dd",

                // Just year and month
                "yyyy-MM", // 2025-05
                "MM/yyyy",
                "M/yyyy",
                "MM-yy",
                "MM-yyyy", // 05-2025
                "MMM-yy", //May-23
                "MMMM-yyyy", //May-2023
                "MMM yyyy", // May 2025
                "MMMM yyyy", // May 2025
                "MMMM yy", //May 23
                "MMM yy", //May 23
            };

            if (DateTime.TryParseExact(csvValue, acceptedFormats, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDate))
            {
                //if the original value didn't include a day, assume list
                if (parsedDate.Day == 1 && (csvValue.Length <= 7 || csvValue.Contains("-") || csvValue.Contains("/")))
                {
                    parsedDate = new DateTime(parsedDate.Year, parsedDate.Month, 1);
                }
                return parsedDate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Bulk Upload Case Studies: Failed to parse date: '{csvValue}'");
        }
        return null; //unparseable
    }

    [HttpPost]
    public async Task<IActionResult> ImportAll([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Bulk Upload Case Studies: Uploaded csv file is not valid");
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
                        foreach (var item in records)
                        {
                            ImportObject importObject = _importUtilityService.CreateImportObject(item);
                            if (importObject.CanImport)
                            {
                                _importUtilityService.ImportSingleItem(importObject);
                            }
                        }

                    }

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

    private async Task<string> GetHtmlFromUrl(string url)
    {
        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bulk Upload Case Studies: Failed to retrieve HTML content from the provided URL.");
            return "";
        }
    }
}