using Umbraco.Cms.Core.Sections;

namespace Umbraco.Community.BulkUpload.Sections;

public class BulkUploadSection : ISection
{
    public string Alias => "bulkUploadSection";

    public string Name => "Bulk Upload";
}