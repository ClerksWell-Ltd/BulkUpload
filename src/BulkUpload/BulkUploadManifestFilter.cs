#if NET8_0
// Umbraco 13 only - v17 uses umbraco-package.json for manifest registration
using Umbraco.Cms.Core.Manifest;

namespace BulkUpload;

internal class BulkUploadManifestFilter : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        var assembly = typeof(BulkUploadManifestFilter).Assembly;

        manifests.Add(new PackageManifest
        {
            PackageName = "Bulk Upload ",
            Version = assembly.GetName()?.Version?.ToString(3) ?? "0.1.0",
            AllowPackageTelemetry = true,
            Scripts = new string[] {
                // Utilities (no dependencies)
                "/App_Plugins/BulkUpload/utils/fileUtils.js",
                "/App_Plugins/BulkUpload/utils/resultUtils.js",
                // HTTP Adapters (no dependencies)
                "/App_Plugins/BulkUpload/services/httpAdapters.js",
                // API Client (depends on httpAdapters)
                "/App_Plugins/BulkUpload/services/BulkUploadApiClient.js",
                // Service (depends on resultUtils)
                "/App_Plugins/BulkUpload/services/BulkUploadService.js",
                // AngularJS service wrapper (depends on BulkUploadApiClient)
                "/App_Plugins/BulkUpload/bulkUploadImportApiService.js",
                // AngularJS controller (depends on all of the above)
                "/App_Plugins/BulkUpload/bulkUpload.Controller.js"
            },
            Stylesheets = new string[]
            {
            }
        });
    }
}
#endif