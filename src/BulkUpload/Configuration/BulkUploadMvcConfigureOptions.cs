#if NET10_0
// Umbraco 17 only - configures MVC for controller discovery
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BulkUpload.Configuration;

/// <summary>
/// Configures MVC to include controllers from BulkUpload.Core assembly
/// </summary>
internal class BulkUploadMvcConfigureOptions : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options)
    {
        // Controllers will be discovered automatically from BulkUpload.Core
        // This class ensures the assembly is included in the application parts
    }
}

/// <summary>
/// Configures ApplicationPartManager to include BulkUpload.Core assembly
/// </summary>
internal class BulkUploadApplicationPartManagerConfigureOptions : IConfigureOptions<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager>
{
    public void Configure(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager options)
    {
        // Add BulkUpload.Core assembly as an application part
        var assembly = typeof(BulkUpload.Core.Controllers.BulkUploadController).Assembly;
        var partFactory = Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartFactory.GetApplicationPartFactory(assembly);
        foreach (var part in partFactory.GetApplicationParts(assembly))
        {
            options.ApplicationParts.Add(part);
        }
    }
}
#endif