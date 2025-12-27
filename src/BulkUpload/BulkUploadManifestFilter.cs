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
                "/App_Plugins/BulkUpload/bulkUpload.controller.js",
                "/App_Plugins/BulkUpload/bulkUploadImportApiService.js"
            },
            Stylesheets = new string[]
            {
            }
        });
    }
}