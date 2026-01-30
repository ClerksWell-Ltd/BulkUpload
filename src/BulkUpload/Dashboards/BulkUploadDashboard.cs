#if NET8_0
// Umbraco 13 only - v17 uses umbraco-package.json for dashboard registration
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dashboards;

namespace BulkUpload.Dashboards;

[Weight(0)]
public class BulkUploadDashboard : IDashboard
{
    public string Alias => "bulkUploadDashboard";

    public string[] Sections => new[]
    {
        "bulkUploadSection"
    };

    public string View => "/App_Plugins/BulkUpload/bulkUploadDashboard.html";

    public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();

}
#endif