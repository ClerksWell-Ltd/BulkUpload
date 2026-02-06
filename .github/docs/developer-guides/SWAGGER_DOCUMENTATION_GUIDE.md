# Swagger/OpenAPI Documentation Guide for BulkUpload

This guide shows how to add comprehensive Swagger documentation to BulkUpload API controllers, following Umbraco v17 best practices.

## Overview

BulkUpload uses conditional compilation to support both Umbraco 13 (v13 - no Swagger) and Umbraco 17 (v17 - with Swagger/OpenAPI). All Swagger-related code is wrapped in `#if !NET8_0` or `#if NET10_0` preprocessor directives.

## Key Patterns from Umbraco v17

Based on analysis of the Umbraco v17 source code, here are the key patterns to follow:

### 1. Controller Base Class Setup

```csharp
#if NET8_0
public class BulkUploadController : UmbracoAuthorizedApiController
#else
/// <summary>
/// BulkUpload API for importing content from CSV/ZIP files
/// </summary>
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Content")]
[MapToApi("bulk-upload")]
[ApiController]
public class BulkUploadController : ControllerBase
#endif
```

**Key Attributes:**
- `[ApiVersion("1.0")]` - API versioning
- `[ApiExplorerSettings(GroupName = "...")]` - Groups endpoints in Swagger UI
- `[MapToApi("bulk-upload")]` - Maps to specific API documentation
- `[Route("api/v{version:apiVersion}/...")]` - Versioned route template

### 2. XML Documentation Structure

All public methods should have comprehensive XML documentation:

```csharp
/// <summary>
/// Brief one-line description of what the endpoint does
/// </summary>
/// <param name="parameterName">Description of the parameter and its purpose</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation</param>
/// <returns>
/// Detailed description of what is returned, including success and error cases.
/// </returns>
/// <remarks>
/// <para>Detailed explanation of the endpoint's behavior.</para>
/// <list type="bullet">
///   <item><description>Feature 1</description></item>
///   <item><description>Feature 2</description></item>
/// </list>
/// <para><strong>Important:</strong> Any critical notes or warnings.</para>
/// </remarks>
/// <example>
/// Example usage or request format (optional but helpful).
/// </example>
```

### 3. Response Type Attributes

Every endpoint should document all possible response types:

```csharp
[HttpPost]
[Route("importall")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(ContentImportResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> ImportAll(...)
```

**Common Response Types:**
- `200 OK` - Success response with typed model
- `400 Bad Request` - Validation errors (ProblemDetails)
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Authorization failed
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Unexpected errors

### 4. File Download Responses

For endpoints that return files:

```csharp
[HttpPost]
[Route("exportresults")]
[Consumes("application/json")]
[Produces("text/csv", "application/zip")]
[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public IActionResult ExportResults(...)
{
    // Return file with proper content type and disposition
    return File(bytes, "text/csv", "filename.csv");
}
```

### 5. CancellationToken Support

Always include `CancellationToken` parameter for async operations:

```csharp
public async Task<IActionResult> ImportAll(
    [FromForm] IFormFile file,
    CancellationToken cancellationToken = default)
{
    // Pass to async methods
    await someService.DoWorkAsync(cancellationToken);
}
```

### 6. Structured Error Responses

Use `ProblemDetails` for error responses instead of plain strings:

```csharp
if (file == null || file.Length == 0)
{
    return BadRequest(new ProblemDetails
    {
        Title = "Invalid File",
        Detail = "Uploaded file is not valid or is empty.",
        Status = StatusCodes.Status400BadRequest,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    });
}
```

## Response Models

Create explicit response models instead of anonymous objects for better Swagger documentation:

### ContentImportResponse.cs

```csharp
namespace BulkUpload.Models;

/// <summary>
/// Response model for content import operations
/// </summary>
public class ContentImportResponse
{
    /// <summary>
    /// Total number of records processed from all CSV files
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported content items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported content item
    /// </summary>
    public List<ContentImportResult> Results { get; set; } = new();

    /// <summary>
    /// Media preprocessing results (if media files were included)
    /// </summary>
    public List<MediaPreprocessingResult>? MediaPreprocessingResults { get; set; }
}
```

### MediaImportResponse.cs

```csharp
namespace BulkUpload.Models;

/// <summary>
/// Response model for media import operations
/// </summary>
public class MediaImportResponse
{
    /// <summary>
    /// Total number of media items processed
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported media items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed import attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed results for each imported media item
    /// </summary>
    public List<MediaImportResult> Results { get; set; } = new();
}
```

## Project Configuration

### Enable XML Documentation Generation

Add to `BulkUpload.csproj`:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

### Configure Swagger in Composer

```csharp
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace BulkUpload;

public class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
#if NET10_0
        // Register Swagger configuration
        builder.Services.ConfigureOptions<ConfigureBulkUploadSwaggerGenOptions>();
#endif
        // ... rest of your registrations
    }
}

#if NET10_0
/// <summary>
/// Configures Swagger/OpenAPI generation options for BulkUpload API
/// </summary>
public class ConfigureBulkUploadSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.SwaggerDoc(
            "bulk-upload",
            new OpenApiInfo
            {
                Title = "BulkUpload API",
                Version = "v1.0",
                Description = "API for bulk importing content and media from CSV/ZIP files into Umbraco CMS. " +
                              "Supports multi-CSV imports, media deduplication, legacy content migration, and update mode.",
                Contact = new OpenApiContact
                {
                    Name = "BulkUpload Package",
                    Url = new Uri("https://github.com/CarlSargunar/BulkUpload")
                }
            });

        // Include XML comments from the assembly
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            swaggerGenOptions.IncludeXmlComments(xmlPath);
        }

        // Optionally include XML comments from BulkUpload.Core if it generates them
        var coreXmlFile = "BulkUpload.Core.xml";
        var coreXmlPath = Path.Combine(AppContext.BaseDirectory, coreXmlFile);
        if (File.Exists(coreXmlPath))
        {
            swaggerGenOptions.IncludeXmlComments(coreXmlPath);
        }
    }
}
#endif
```

## Complete Example: Enhanced ImportAll Endpoint

Here's a complete example showing all best practices:

```csharp
/// <summary>
/// Imports content from a CSV file or ZIP archive containing CSV and media files
/// </summary>
/// <param name="file">CSV file or ZIP archive. ZIP can contain multiple CSVs and media files.</param>
/// <param name="cancellationToken">Cancellation token to cancel the import operation</param>
/// <returns>
/// Import results containing success/failure counts and detailed information for each imported content item.
/// Includes media preprocessing results if media files were included in the upload.
/// </returns>
/// <remarks>
/// <para>This endpoint supports multiple import scenarios:</para>
/// <list type="bullet">
///   <item><description><strong>Single CSV:</strong> Upload a CSV file containing content data only</description></item>
///   <item><description><strong>ZIP with CSV and media:</strong> Upload a ZIP file containing CSV(s) and media files</description></item>
///   <item><description><strong>Multi-CSV imports:</strong> ZIP with multiple CSV files with cross-file parent-child hierarchy</description></item>
///   <item><description><strong>Legacy migration:</strong> Use bulkUploadLegacyId and bulkUploadLegacyParentId for CMS migration</description></item>
/// </list>
///
/// <para><strong>Update Mode:</strong></para>
/// <para>Include a 'bulkUploadShouldUpdate' column in your CSV to enable update mode. Set to 'true' for rows you want to update.</para>
///
/// <para><strong>Media Deduplication:</strong></para>
/// <para>When importing multiple CSVs, media files are deduplicated automatically. The same media file referenced
/// in multiple CSVs will only be created once.</para>
///
/// <para><strong>CSV Format Requirements:</strong></para>
/// <list type="bullet">
///   <item><description>name (required) - Content node name</description></item>
///   <item><description>docTypeAlias (required) - Content type alias</description></item>
///   <item><description>parent (required) - Parent ID, GUID, or path</description></item>
///   <item><description>Additional columns for content properties using propertyAlias|resolverAlias syntax</description></item>
/// </list>
/// </remarks>
/// <example>
/// Example ZIP structure:
/// <code>
/// upload.zip
/// ├── content.csv
/// ├── categories.csv
/// └── media/
///     ├── hero-image.jpg
///     └── thumbnail.jpg
/// </code>
///
/// Example CSV content:
/// <code>
/// name,docTypeAlias,parent,heroImage|zipFileToMedia,publishDate|dateTime
/// "Article 1","article","umb://document/1234","media/hero-image.jpg","2024-01-01T00:00:00Z"
/// </code>
/// </example>
[HttpPost]
#if !NET8_0
[Route("importall")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(ContentImportResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
#endif
public async Task<IActionResult> ImportAll(
    [FromForm] IFormFile file,
    CancellationToken cancellationToken = default)
{
    // Implementation...
}
```

## Advanced: Operation Filters (Optional)

For advanced customization, you can create operation filters similar to Umbraco:

```csharp
#if NET10_0
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Adds custom headers to BulkUpload API responses
/// </summary>
public class BulkUploadResponseHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Only apply to BulkUpload API
        if (!context.MethodInfo.DeclaringType?.Name.Contains("BulkUpload") ?? true)
            return;

        // Add custom headers to successful responses
        if (operation.Responses.TryGetValue("200", out var response))
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();

            // Example: Add X-Import-Duration header
            response.Headers["X-Import-Duration"] = new OpenApiHeader
            {
                Description = "Duration of the import operation in milliseconds",
                Schema = new OpenApiSchema { Type = "integer" }
            };
        }
    }
}

// Register in composer
public class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<ConfigureBulkUploadSwaggerGenOptions>();

        // Add operation filter
        builder.Services.Configure<SwaggerGenOptions>(options =>
        {
            options.OperationFilter<BulkUploadResponseHeaderFilter>();
        });
    }
}
#endif
```

## Testing Your Documentation

1. **Build the project:**
   ```bash
   cd src/BulkUpload
   dotnet build -f net10.0
   ```

2. **Run the test site:**
   ```bash
   cd src/BulkUpload.TestSite
   dotnet run
   ```

3. **Access Swagger UI:**
   - Navigate to: `https://localhost:xxxxx/umbraco/swagger`
   - You should see "BulkUpload API" in the API selector dropdown
   - All your endpoints should be visible with full documentation

## Checklist

Use this checklist when documenting API endpoints:

- [ ] Class has XML `<summary>` documentation
- [ ] All public methods have XML `<summary>` documentation
- [ ] All parameters have `<param>` documentation
- [ ] Methods have `<returns>` documentation
- [ ] Complex endpoints have `<remarks>` with details
- [ ] All HTTP methods have `[ProducesResponseType]` for 200 OK
- [ ] All HTTP methods have `[ProducesResponseType]` for error cases (400, 401, 500)
- [ ] File operations use `[Consumes]` and `[Produces]` attributes
- [ ] Async methods accept `CancellationToken`
- [ ] Response types use explicit models (not anonymous objects)
- [ ] Error responses use `ProblemDetails`
- [ ] XML documentation generation is enabled in `.csproj`
- [ ] Swagger configuration is registered in composer

## Common Pitfalls

1. **Anonymous objects in responses** - These don't generate good Swagger docs
   - ❌ `return Ok(new { count = 5, items = list });`
   - ✅ `return Ok(new MyResponse { Count = 5, Items = list });`

2. **Missing error response types** - Document all possible responses
   - ❌ Only documenting 200 OK
   - ✅ Document 200, 400, 401, 500

3. **Plain string errors** - Use structured error responses
   - ❌ `return BadRequest("Invalid file");`
   - ✅ `return BadRequest(new ProblemDetails { ... });`

4. **No XML documentation** - Swagger uses XML comments
   - ❌ No XML comments on methods
   - ✅ Complete XML documentation with `<summary>`, `<param>`, `<returns>`

5. **Forgetting conditional compilation** - v13 doesn't support these attributes
   - ❌ Using Swagger attributes without `#if` guards
   - ✅ Wrap Swagger-specific code in `#if !NET8_0`

## References

- [Umbraco v17 Management API](https://github.com/umbraco/Umbraco-CMS/tree/contrib/src/Umbraco.Cms.Api.Management)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Microsoft XML Documentation Comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [OpenAPI Specification](https://swagger.io/specification/)
