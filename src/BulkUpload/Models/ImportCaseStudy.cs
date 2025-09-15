using CsvHelper.Configuration.Attributes;

namespace Umbraco.Community.BulkUpload.Models;

public class ImportCaseStudy
{
    [Name("Start date")]
    public string? StartDate { get; set; }
    [Name("Project Title")]
    public string? ProjectTitle { get; set; }
    [Name("Project Types")]
    public string? ProjectTypes { get; set; }
    [Name("Project Status")]
    public string? ProjectStatus { get; set; }
    [Name("Non clinical/Non therapy area")]
    public string? NonClinicalNonTherapyArea { get; set; }
    [Name("Clinical/Therapy area")]
    public string? ClinicalTherapyArea { get; set; }
    [Name("NHS Organisation")]
    public string? NHSOrganisation { get; set; }
    [Name("Pharmaceutical company")]
    public string? PharmaceuticalCompany { get; set; }
    [Name("Project focus")]
    public string? ProjectFocus { get; set; }
    [Name("Case study URL")]
    public string? CaseStudyURL { get; set; }
    [Name("External scanning")]
    public string? ExternalScanning { get; set; }
}