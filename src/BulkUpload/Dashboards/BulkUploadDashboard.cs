using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dashboards;

namespace Umbraco.Community.BulkUpload.Dashboards;
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