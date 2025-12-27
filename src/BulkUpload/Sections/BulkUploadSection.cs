using Umbraco.Cms.Core.Sections;

namespace BulkUpload.Sections;

public class BulkUploadSection : ISection
{
    public string Alias => "bulkUploadSection";

    public string Name => "Bulk Upload";
}