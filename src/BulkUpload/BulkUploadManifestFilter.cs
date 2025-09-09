using Umbraco.Cms.Core.Manifest;

namespace Umbraco.Community.BulkUpload
{
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
                    // List any Script files
                    // Urls should start '/App_Plugins/BulkUpload/' not '/wwwroot/BulkUpload/', e.g.
                    // "/App_Plugins/BulkUpload/Scripts/scripts.js"
                },
                Stylesheets = new string[]
                {
                    // List any Stylesheet files
                    // Urls should start '/App_Plugins/BulkUpload/' not '/wwwroot/BulkUpload/', e.g.
                    // "/App_Plugins/BulkUpload/Styles/styles.css"
                }
            });
        }
    }
}
