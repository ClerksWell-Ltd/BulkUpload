#if NET8_0
// Umbraco 13 only - v17 uses umbraco-package.json for section registration
using Umbraco.Cms.Core.Sections;

namespace BulkUpload.Sections;

public class BulkUploadSection : ISection
{
    public string Alias => "bulkUploadSection";

    public string Name => "Bulk Upload";
}
#endif