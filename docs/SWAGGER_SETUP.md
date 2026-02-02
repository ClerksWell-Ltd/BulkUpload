# Swagger/OpenAPI Setup for Umbraco 17

This document describes the Swagger/OpenAPI configuration for the BulkUpload V17 package.

## Overview

The BulkUpload V17 APIs are automatically documented using Umbraco 17's built-in OpenAPI/Swagger support. The Management API endpoints are accessible and documented at `/umbraco/swagger`.

## Configuration

### 1. Controller Registration

Controllers from `BulkUpload.Core` assembly are registered via `BulkUploadApplicationPartManagerConfigureOptions` in the `BulkUploadComposer`:

```csharp
// BulkUploadComposer.cs
builder.Services.ConfigureOptions<BulkUploadApplicationPartManagerConfigureOptions>();
```

This ensures controllers are discovered by ASP.NET Core MVC and included in the OpenAPI documentation.

### 2. XML Documentation

XML documentation comments are enabled in `BulkUpload.Core.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

The generated `BulkUpload.Core.xml` file is automatically copied to the V17 output directory where Umbraco's OpenAPI system can read it.

### 3. Controller Attributes

Controllers use OpenAPI-friendly attributes:

- `[Tags("BulkUpload")]` / `[Tags("MediaImport")]` - Groups endpoints in Swagger UI
- `[ProducesResponseType]` - Documents response types and status codes
- `[Consumes]` / `[Produces]` - Documents request/response content types
- XML comments - Provide detailed descriptions, parameter info, and remarks

## Available Endpoints

### BulkUpload Controller

**Base Path:** `/umbraco/management/api/bulkupload`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/importall` | POST | Import content from CSV/ZIP file |
| `/exportresults` | POST | Export import results to CSV/ZIP |
| `/exportmediapreprocessingresults` | POST | Export media preprocessing results |

### MediaImport Controller

**Base Path:** `/umbraco/management/api/mediaimport`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/importmedia` | POST | Import media from CSV/ZIP file |
| `/exportresults` | POST | Export media import results to CSV |

## Accessing Swagger UI

1. Start your Umbraco 17 site (e.g., `https://localhost:44340`)
2. Navigate to: `https://localhost:44340/umbraco/swagger`
3. Authenticate using the "Authorize" button
4. Browse and test the BulkUpload and MediaImport endpoints

## Testing Endpoints

### Using Swagger UI

1. Find the endpoint you want to test (e.g., `POST /umbraco/management/api/bulkupload/importall`)
2. Click "Try it out"
3. Upload a test CSV or ZIP file
4. Click "Execute"
5. View the response with import results

### Using curl

```bash
# Import content
curl -X POST https://localhost:44340/umbraco/management/api/bulkupload/importall \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@content.csv"

# Import media
curl -X POST https://localhost:44340/umbraco/management/api/mediaimport/importmedia \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@media.zip"
```

### Using the Frontend Client

The TypeScript client (`BulkUploadApiClient`) automatically handles authentication and requests:

```typescript
import { BulkUploadApiClient } from './api/bulk-upload-api.js';

const client = new BulkUploadApiClient();
const response = await client.importContent(file);
```

## Documentation Details

Each endpoint includes:

- **Summary**: Brief description of what the endpoint does
- **Parameters**: Detailed parameter documentation with types
- **Request Body**: Expected request format (multipart/form-data or JSON)
- **Responses**: Possible response types and status codes
- **Remarks**: Additional details about features, supported formats, and behavior

## Example Swagger Documentation

### ImportAll Endpoint

```
POST /umbraco/management/api/bulkupload/importall

Summary:
Imports content from a CSV file or ZIP archive containing CSV and media files

Parameters:
- file (formData, required): CSV file or ZIP archive containing CSV files and optional media files

Responses:
- 200 OK: Import results with success/failure counts and details
- 400 Bad Request: Invalid file or processing error

Remarks:
Supports:
- Single CSV file upload (content only)
- ZIP file with CSV(s) and media files
- Multi-CSV imports with cross-file hierarchy
- Legacy content migration via bulkUploadLegacyId
- Automatic media deduplication
- Update mode via bulkUploadShouldUpdate column
```

## Troubleshooting

### Endpoints not appearing in Swagger

1. **Verify controllers are registered**: Check that `BulkUploadApplicationPartManagerConfigureOptions` is configured in the composer
2. **Check XML documentation**: Ensure `BulkUpload.Core.xml` exists in the output directory
3. **Rebuild the project**: Run `dotnet build -c Release` to regenerate XML documentation
4. **Clear browser cache**: Force refresh Swagger UI (Ctrl+F5)

### 404 Errors

If you get 404 errors when calling endpoints:

1. **Check the full URL**: Should be `/umbraco/management/api/bulkupload/importall` (not `/api/bulkupload/importall`)
2. **Verify authentication**: Management API requires authentication
3. **Check controller attributes**: Ensure `[MapToApi("management")]` is present (NET10.0 only)
4. **Test in Swagger**: Try the endpoint in Swagger UI first to verify it's registered

### Authentication Issues

The Management API requires authentication. Use one of:

1. **Umbraco backoffice authentication**: Browser session cookie
2. **Bearer token**: API key or JWT token
3. **Swagger UI authorization**: Click "Authorize" button and log in

## Files Modified

- `src/BulkUpload.Core/Controllers/ImportController.cs` - Added XML comments and OpenAPI attributes
- `src/BulkUpload.Core/Controllers/MediaImportController.cs` - Added XML comments and OpenAPI attributes
- `src/BulkUpload.Core/BulkUpload.Core.csproj` - Enabled XML documentation generation
- `src/BulkUpload.V17/BulkUpload.V17.csproj` - Configured XML file copying
- `src/BulkUpload.V17/BulkUploadMvcConfigureOptions.cs` - Controller registration

## See Also

- [Umbraco 17 Management API Documentation](https://docs.umbraco.com/umbraco-cms/extending/management-api)
- [OpenAPI Specification](https://swagger.io/specification/)
- [BulkUpload User Guide](./README.md)
