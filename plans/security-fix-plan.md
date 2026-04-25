# Security Fix Plan: Unauthorized API Access on .NET 10+

## Summary

A security vulnerability exists in the BulkUpload package where the management API endpoints are completely unauthenticated on .NET 10+ (Umbraco 17+). Any user — including anonymous, unauthenticated users — can call the import endpoints over HTTP and upload content or media into the Umbraco site without ever logging in to the back-office.

This was reported by an external security researcher who demonstrated the issue on Umbraco 17.3.4 using a plain `curl` request with no auth token or cookie.

---

## Affected Versions

| .NET Version | Umbraco Version | Status |
|---|---|---|
| .NET 8.0 | Umbraco 13 | **Not affected** — controllers inherit `UmbracoAuthorizedApiController` |
| .NET 10.0 | Umbraco 17+ | **Affected** — controllers inherit `ControllerBase` with no auth |

---

## Root Cause

Both controllers use a `#if NET8_0 / #else` conditional compilation pattern to support dual target frameworks:

```csharp
// ImportController.cs (line 31)
#if NET8_0
public class BulkUploadController : UmbracoAuthorizedApiController  // ✅ protected
#else
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
// ... other attributes ...
[ApiController]
public class BulkUploadController : ControllerBase                  // ❌ unprotected
#endif
```

On `.NET 8.0`, inheriting `UmbracoAuthorizedApiController` automatically enforces back-office authentication. On `.NET 10.0+`, `ControllerBase` is a plain ASP.NET Core base class with no authorization whatsoever. No `[Authorize]` attribute was added to compensate.

The same pattern exists in `MediaImportController.cs` (line 24).

---

## Affected Endpoints

### `BulkUploadController` — `src/BulkUpload/Controllers/ImportController.cs`

| Method | Route | Risk |
|---|---|---|
| `ImportAll` | `POST /api/v1/content/importall` | **Critical** — creates/updates published content |
| `ExportResults` | `POST /api/v1/content/exportresults` | Medium — reads import results |
| `ExportMediaPreprocessingResults` | `POST /api/v1/content/exportmediapreprocessingresults` | Medium — reads media preprocessing data |

### `MediaImportController` — `src/BulkUpload/Controllers/MediaImportController.cs`

| Method | Route | Risk |
|---|---|---|
| `ImportMedia` | `POST /api/v1/media/importmedia` | **Critical** — imports media from URLs, file paths, or ZIPs |
| `ExportResults` | `POST /api/v1/media/exportresults` | Medium — reads media import results |
| `ImportMediaFromZipOnly` | `POST /api/v1/media/importmediafromzip` | **Critical** — imports media directly from ZIP |

> Note: `ImportMedia` already contains runtime SSRF and path-traversal guards (`IsAllowedUrl`, `IsAllowedFilePath`), but those are rendered irrelevant while the endpoint is entirely unauthenticated.

---

## Proposed Fix

Add `[Authorize]` attributes to both controller class declarations inside the `#if !NET8_0` block. The security researcher recommended using Umbraco's section-access policies, which restrict access to back-office users who have been granted access to the relevant section — following least-privilege.

### Authorization policies to use

| Controller | Recommended Policy | Rationale |
|---|---|---|
| `BulkUploadController` | `SectionAccessContent` | Content import requires content section access |
| `MediaImportController` | `SectionAccessMedia` | Media import requires media section access |

These policy names are defined as constants in `Umbraco.Cms.Web.Common.Authorization.AuthorizationPolicies`.

---

## Implementation

### 1. `ImportController.cs`

Add `using Microsoft.AspNetCore.Authorization;` and `using Umbraco.Cms.Web.Common.Authorization;` to the `#else` using block, and add `[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]` to the class declaration.

**Before (lines 21–44):**
```csharp
#else
using Umbraco.Cms.Api.Common.Attributes;
using Asp.Versioning;
#endif

// ...

#if NET8_0
public class BulkUploadController : UmbracoAuthorizedApiController
#else
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Content")]
[MapToApi("bulk-upload")]
[ApiController]
public class BulkUploadController : ControllerBase
#endif
```

**After:**
```csharp
#else
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Asp.Versioning;
#endif

// ...

#if NET8_0
public class BulkUploadController : UmbracoAuthorizedApiController
#else
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
[Route("api/v{version:apiVersion}/content")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Content")]
[MapToApi("bulk-upload")]
[ApiController]
public class BulkUploadController : ControllerBase
#endif
```

### 2. `MediaImportController.cs`

Same pattern — add the usings and add `[Authorize(Policy = AuthorizationPolicies.SectionAccessMedia)]`.

**Before (lines 15–37):**
```csharp
#else
using Umbraco.Cms.Api.Common.Attributes;
using Asp.Versioning;
#endif

// ...

#if NET8_0
public class MediaImportController : UmbracoAuthorizedApiController
#else
[Route("api/v{version:apiVersion}/media")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Media")]
[MapToApi("bulk-upload")]
[ApiController]
public class MediaImportController : ControllerBase
#endif
```

**After:**
```csharp
#else
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Asp.Versioning;
#endif

// ...

#if NET8_0
public class MediaImportController : UmbracoAuthorizedApiController
#else
[Authorize(Policy = AuthorizationPolicies.SectionAccessMedia)]
[Route("api/v{version:apiVersion}/media")]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Media")]
[MapToApi("bulk-upload")]
[ApiController]
public class MediaImportController : ControllerBase
#endif
```

### 3. Open questions / things to verify before implementation

- ~~**Policy names**: Confirm `AuthorizationPolicies.SectionAccessContent` and `AuthorizationPolicies.SectionAccessMedia` are available in the `Umbraco.Cms.Web.Website` 17.x transitive dependency chain, or whether an additional `PackageReference` is needed for net10.0.~~ **Resolved**: `AuthorizationPolicies` lives in `Umbraco.Cms.Web.Common.Authorization` — the `using` directive shown in the code examples above is correct and sufficient. Do **not** reach for `Constants.Security` (that is in `Umbraco.Cms.Core` and is a different namespace entirely).
- **Export endpoints**: Decide whether `ExportResults` and `ExportMediaPreprocessingResults` should share the same section-access policy as their respective import endpoints, or use a broader `BackOfficeAccess` policy (since they are read-only result exports, not content writes). Current proposal is to keep them under the same class-level attribute, which covers all methods.
- **Authentication scheme**: Confirm whether a specific `AuthenticationSchemes` value needs to be set on the `[Authorize]` attribute for Umbraco 17's OpenIddict-based back-office auth, or whether the default scheme resolution is sufficient.
- **`[IgnoreAntiforgeryToken]`**: These are present on individual action methods and should be retained — they are needed for multipart form-data file uploads. The `[Authorize]` sits at a higher (class) level and does not conflict.

---

## Testing Plan

1. **Build both target frameworks** — `dotnet build -f net8.0` and `dotnet build -f net10.0` — confirm no compile errors.
2. **On net10.0 / Umbraco 17**: Without logging in, make a `curl` request to `POST /api/v1/content/importall` and verify a `401 Unauthorized` response is returned.
3. **Authenticated, no section access**: Log in as a back-office user without Content section access and verify `403 Forbidden`.
4. **Authenticated, with section access**: Log in as a back-office user with Content section access and verify the endpoint processes the request normally.
5. **Repeat steps 2–4 for `MediaImportController`** using the media section.
6. **Confirm net8.0 is unaffected** — no regression in Umbraco 13 behavior.

---

## Release

- Bump `<Version>` in `BulkUpload.csproj` (currently `2.0.6`) — suggest `2.0.7` patch release.
- Add a security advisory or changelog note documenting the vulnerability and fix.
- Consider whether to file a CVE given this affects an open-source Umbraco community package.
