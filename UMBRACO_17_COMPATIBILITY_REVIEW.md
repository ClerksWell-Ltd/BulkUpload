# Umbraco 17 Compatibility Review - BulkUpload Package

**Review Date:** 2025-12-26
**Implementation Date:** 2025-12-27 ✅
**Current Target:** Umbraco 13
**Future Target:** Umbraco 17 (with shared codebase)
**Goal:** Maximize code reuse and minimize changes needed for Umbraco 17 migration

> **📢 UPDATE (2025-12-27):** The **Core package architecture** has been **implemented**!
> See [CORE_PACKAGE_IMPLEMENTATION.md](CORE_PACKAGE_IMPLEMENTATION.md) for details.
>
> **What's Done:**
> - ✅ `BulkUpload.Core` project created with multi-targeting (net8.0 + net10.0)
> - ✅ All shared code (85%+) moved to Core
> - ✅ `BulkUpload` (v13) updated to reference Core
> - ✅ Ready for `BulkUpload.V17` when needed
>
> The architecture described in "Part 3: Multi-Targeting Strategy" below has been **fully implemented** using the cleaner Core + version-specific package approach.

---

## Executive Summary

### Current Status: 🟢 **Well Positioned for Migration**

The BulkUpload package is **already well-prepared** for Umbraco 17 migration:

- ✅ **Frontend**: ~90% ready (Phases 1-4 complete - framework-agnostic refactoring done)
- ⚠️ **Backend**: Requires moderate changes (~30-40% of code)
- 🎯 **Estimated Total Migration Effort**: 3-5 days

### Key Achievements

The recent refactoring work has successfully:
- Extracted all frontend business logic into framework-agnostic JavaScript services
- Implemented adapter pattern for HTTP abstraction
- Created pure utility functions for file and result processing
- Reduced AngularJS controller to a thin ~50-line wrapper

---

## Part 1: Frontend Analysis (JavaScript/HTML)

### ✅ Status: Migration-Ready (Phase 1-4 Complete)

#### What's Already Done

The frontend has been **completely refactored** to be framework-agnostic:

**1. Framework-Agnostic Services** ✅
- `BulkUploadService.js` (370 lines) - All business logic
- `BulkUploadApiClient.js` (149 lines) - HTTP abstraction with adapter pattern
- State management independent of UI framework

**2. Pure JavaScript Utilities** ✅
- `fileUtils.js` - File validation, formatting, extension detection
- `resultUtils.js` - Result statistics, CSV export, filtering

**3. HTTP Adapter Pattern** ✅
- `AngularHttpAdapter` - For Umbraco 13 ($http + ng-file-upload)
- `FetchHttpAdapter` - For Umbraco 17 (native Fetch API)

**4. Thin UI Layer** ✅
- Current AngularJS controller is only ~50 lines (wrapper around services)
- Easy to replace with Lit component

#### What's Needed for Umbraco 17 (Phase 5)

**Frontend Changes Required:**

1. **Create Lit Component** (~1-2 days)
   ```typescript
   // src/dashboard.element.ts
   import { LitElement, html, css } from 'lit';
   import { BulkUploadService } from './services/BulkUploadService.js';
   import { BulkUploadApiClient } from './services/BulkUploadApiClient.js';

   export class BulkUploadDashboard extends LitElement {
     private service: BulkUploadService;

     constructor() {
       super();
       // Use FetchHttpAdapter (default) instead of AngularHttpAdapter
       const apiClient = new BulkUploadApiClient();
       this.service = new BulkUploadService(apiClient, this.handleNotification);
     }

     render() {
       return html`
         <uui-box>
           <!-- Same UI structure, different template syntax -->
         </uui-box>
       `;
     }
   }
   ```

2. **Create Extension Manifest** (~0.5 days)
   ```json
   // umbraco-package.json
   {
     "$schema": "https://json.schemastore.org/umbraco-package.json",
     "name": "Umbraco.Community.BulkUpload",
     "version": "2.0.0",
     "extensions": [
       {
         "type": "dashboard",
         "alias": "bulkUpload.dashboard",
         "name": "Bulk Upload Dashboard",
         "element": "/App_Plugins/BulkUpload/dist/bulk-upload-dashboard.js",
         "weight": -10,
         "meta": {
           "label": "Bulk Upload",
           "pathname": "bulk-upload"
         },
         "conditions": [
           {
             "alias": "Umb.Condition.SectionAlias",
             "match": "Umb.Section.Content"
           }
         ]
       }
     ]
   }
   ```

3. **Build Configuration** (~0.5 days)
   - Add `package.json` with Vite build setup
   - Add `tsconfig.json` for TypeScript
   - Configure bundling for Lit component

**Code Reuse:** ~90% of frontend JavaScript can be directly imported and reused!

---

## Part 2: Backend Analysis (C#)

### ⚠️ Status: Requires Changes (~30-40% of backend code)

#### Areas Requiring Changes

### 1. Dashboard & Section Registration (Breaking Change) 🔴

**Current Implementation (Umbraco 13):**

```csharp
// BulkUploadDashboard.cs - WILL NOT WORK IN v17
[Weight(0)]
public class BulkUploadDashboard : IDashboard
{
    public string Alias => "bulkUploadDashboard";
    public string[] Sections => new[] { "bulkUploadSection" };
    public string View => "/App_Plugins/BulkUpload/bulkUploadDashboard.html";
    public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();
}
```

**Status:**
- ❌ `IDashboard` interface **removed in Umbraco 14** (not available in v17)
- ❌ C#-based dashboard registration **deprecated**

**Migration Required:**
- Remove `BulkUploadDashboard.cs` entirely
- Remove `BulkUploadSection.cs` (sections registered differently)
- Move to JSON-based extension manifest (see Frontend section)

**File Impact:**
- `src/BulkUpload/Dashboards/BulkUploadDashboard.cs` - **DELETE**
- `src/BulkUpload/Sections/BulkUploadSection.cs` - **DELETE**
- `src/BulkUpload/BulkUploadComposer.cs:20` - Remove section registration

---

### 2. Manifest Filter (Breaking Change) 🔴

**Current Implementation:**

```csharp
// BulkUploadManifestFilter.cs - WILL NOT WORK IN v17
internal class BulkUploadManifestFilter : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        manifests.Add(new PackageManifest
        {
            PackageName = "Bulk Upload",
            Scripts = new string[] {
                "/App_Plugins/BulkUpload/bulkUpload.controller.js",
                "/App_Plugins/BulkUpload/bulkUploadImportApiService.js"
            }
        });
    }
}
```

**Status:**
- ❌ `package.manifest` system **replaced** with `umbraco-package.json`
- ❌ `IManifestFilter` still exists but used differently

**Migration Required:**
- Replace with `umbraco-package.json` file (JSON-based)
- Remove `BulkUploadManifestFilter.cs` or adapt for v17

**File Impact:**
- `src/BulkUpload/BulkUploadManifestFilter.cs` - **ADAPT or DELETE**
- `src/BulkUpload/BulkUploadComposer.cs:18` - Remove or update manifest filter

---

### 3. Core Services (Likely Compatible) 🟢

**Current Implementation:**

The package heavily uses these Umbraco services:
- `IContentService` - Create/update content
- `IMediaService` - Create/update media
- `IContentTypeService` - Get content type definitions
- `IMediaTypeService` - Get media type definitions
- `ICoreScopeProvider` - Database transactions
- `ILocalizationService` - Multi-language support
- `ILanguageRepository` - Language data
- `MediaFileManager` - Media file uploads
- `IShortStringHelper` - String utilities

**Status:**
- ✅ These core services **remain available** in Umbraco 17
- ⚠️ API signatures may have minor changes (not breaking for most usage)
- ℹ️ According to Umbraco policy: "Changes to interfaces in Umbraco.Core.Services are not considered breaking changes"

**Migration Required:**
- ✅ **Minimal to none** - Most service usage should work as-is
- ⚠️ May need minor adjustments based on deprecation warnings
- Test thoroughly after upgrading dependencies

**File Impact:**
- `src/BulkUpload/Services/ImportUtilityService.cs` - Minor updates expected
- `src/BulkUpload/Services/MediaImportService.cs` - Minor updates expected
- `src/BulkUpload/Controllers/ImportController.cs` - Minor updates expected
- `src/BulkUpload/Controllers/MediaImportController.cs` - Minor updates expected

---

### 4. Controller Authorization (Likely Compatible) 🟢

**Current Implementation:**

```csharp
public class BulkUploadController : UmbracoAuthorizedApiController
{
    [HttpPost]
    public async Task<IActionResult> ImportAll([FromForm] IFormFile file)
    {
        // ...
    }
}
```

**Status:**
- ✅ `UmbracoAuthorizedApiController` **still available** in Umbraco 17
- ✅ Authorization attributes remain the same
- ℹ️ Management API is new, but backoffice API controllers still supported

**Migration Required:**
- ✅ **None expected** - Controllers should work as-is

**File Impact:**
- `src/BulkUpload/Controllers/ImportController.cs` - No changes expected
- `src/BulkUpload/Controllers/MediaImportController.cs` - No changes expected

---

### 5. Dependency Injection & Composition (Compatible) 🟢

**Current Implementation:**

```csharp
internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IImportUtilityService, ImportUtilityService>();
        builder.Services.AddSingleton<IMediaImportService, MediaImportService>();
        // ... 40+ service registrations
    }
}
```

**Status:**
- ✅ `IComposer` interface **still available** in Umbraco 17
- ✅ DI registration pattern unchanged
- ✅ Service lifetime management (Singleton, Transient) unchanged

**Migration Required:**
- ✅ **None** - Composer will work as-is (minus dashboard/manifest registration lines)

**File Impact:**
- `src/BulkUpload/BulkUploadComposer.cs` - Remove lines 18-20 only (manifest & dashboard)

---

### 6. Breaking Changes from Umbraco 14→17

Based on official documentation, these changes affect migration from v13 to v17:

#### **Umbraco 14 Changes (Included in 13→17 Upgrade)**

1. **Snapshot API Removed** 🟢 (Not used in BulkUpload)
   - `IPublishedSnapshot` and `IPublishedSnapshotAccessor` removed
   - Must use `IPublishedContentCache` or `IPublishedMediaCache` instead
   - ✅ **Impact: None** - BulkUpload doesn't use snapshot APIs

2. **AngularJS Removed** ✅ (Already addressed)
   - Frontend completely rewritten as Web Components
   - ✅ **Impact: Mitigated** - Framework-agnostic services already created

3. **Management API Introduced** 🟢 (Not used in BulkUpload)
   - New REST API for backoffice operations
   - Old backoffice API controllers still supported
   - ✅ **Impact: None** - Can continue using current API pattern

#### **Umbraco 16 Changes (Included in 13→17 Upgrade)**

1. **TinyMCE Removed** 🟢 (Not used in BulkUpload)
   - TipTap is now the only rich text editor
   - ✅ **Impact: None** - BulkUpload doesn't configure rich text editors

#### **Umbraco 17 Changes**

1. **System Dates to UTC** ⚠️ (Minor impact)
   - All system dates stored in UTC
   - Migration runs automatically on upgrade
   - ⚠️ **Impact: Low** - May affect import date handling

2. **Extension Methods Removal** ⚠️ (Unknown impact)
   - Some extension methods removed
   - Need to test for compilation errors
   - ⚠️ **Impact: Unknown** - Must test after upgrade

3. **Swashbuckle v10** 🟢 (Not used)
   - ✅ **Impact: None** - BulkUpload doesn't use Swagger

4. **HTTPS Required by Default** 🟢 (Configuration)
   - ✅ **Impact: None** - Infrastructure setting

---

## Part 3: Multi-Targeting Strategy

### Recommended Approach: **Conditional Compilation with Shared Code**

To support both Umbraco 13 and 17 from a single codebase:

### Option A: Multi-Targeted NuGet Package (Recommended)

**Create two builds from one codebase:**

```xml
<!-- BulkUpload.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Multi-target both frameworks -->
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <PackageId>Umbraco.Community.BulkUpload</PackageId>
    <Version>2.0.0</Version>
  </PropertyGroup>

  <!-- Umbraco 13 dependencies (net8.0) -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Umbraco.Cms.Web.Website" Version="13.13.0" />
    <PackageReference Include="Umbraco.Cms.Web.BackOffice" Version="13.13.0" />
  </ItemGroup>

  <!-- Umbraco 17 dependencies (net10.0) -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Umbraco.Cms.Web.Website" Version="17.0.0" />
    <PackageReference Include="Umbraco.Cms.Web.BackOffice" Version="17.0.0" />
  </ItemGroup>

  <!-- Include v13 frontend assets for net8.0 -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <Content Include="App_Plugins\BulkUpload\**\*"
             ExcludeFromSingleFile="true"
             CopyToOutputDirectory="Always" />
  </ItemGroup>

  <!-- Include v17 frontend assets for net10.0 -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <Content Include="App_Plugins\BulkUpload.v17\**\*"
             ExcludeFromSingleFile="true"
             CopyToOutputDirectory="Always" />
  </ItemGroup>
</Project>
```

**Use conditional compilation for version-specific code:**

```csharp
namespace Umbraco.Community.BulkUpload;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
#if NET8_0
        // Umbraco 13 specific registrations
        builder.ManifestFilters().Append<BulkUploadManifestFilter>();
        builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#endif

        // Shared service registrations (work in both versions)
        builder.Services.AddSingleton<IResolver, TextResolver>();
        builder.Services.AddSingleton<IImportUtilityService, ImportUtilityService>();
        // ... all other services
    }
}
```

**Create version-specific files:**

```
src/BulkUpload/
├── Dashboards/
│   └── BulkUploadDashboard.v13.cs      # Only compiled for net8.0
├── Sections/
│   └── BulkUploadSection.v13.cs        # Only compiled for net8.0
├── BulkUploadManifestFilter.v13.cs     # Only compiled for net8.0
└── App_Plugins/
    ├── BulkUpload/                      # v13 (AngularJS)
    │   ├── bulkUpload.controller.js
    │   ├── bulkUploadDashboard.html
    │   └── services/                     # Framework-agnostic (shared!)
    │       ├── BulkUploadService.js
    │       ├── BulkUploadApiClient.js
    │       └── httpAdapters.js
    └── BulkUpload.v17/                  # v17 (Lit)
        ├── umbraco-package.json
        ├── dist/
        │   └── bulk-upload-dashboard.js
        └── services/                     # Same as v13 (symlink or copy)
            ├── BulkUploadService.js
            ├── BulkUploadApiClient.js
            └── httpAdapters.js
```

**Configure conditional compilation:**

```xml
<!-- Only compile v13-specific files for net8.0 -->
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <Compile Include="Dashboards\BulkUploadDashboard.v13.cs" />
  <Compile Include="Sections\BulkUploadSection.v13.cs" />
  <Compile Include="BulkUploadManifestFilter.v13.cs" />
</ItemGroup>

<!-- v17-specific files would go here when needed -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <!-- Currently none needed - all C# code is shared! -->
</ItemGroup>
```

---

### Option B: Separate Packages (Alternative)

Create two separate NuGet packages:
- `Umbraco.Community.BulkUpload` (v13 - targets net8.0)
- `Umbraco.Community.BulkUpload` (v17 - targets net10.0)

**Pros:**
- Clearer separation
- Easier to maintain different frontend assets

**Cons:**
- Code duplication
- More complex release process
- Users must manually switch packages

---

### Option C: Single Package with Runtime Detection (Not Recommended)

Detect Umbraco version at runtime and load appropriate components.

**Cons:**
- Complex runtime logic
- Larger package size
- Harder to maintain
- Not recommended by Umbraco team

---

## Part 4: Migration Checklist

### 📋 Backend Changes Required

#### High Priority (Breaking Changes)

- [ ] **Remove v13-only dashboard registration**
  - Delete `src/BulkUpload/Dashboards/BulkUploadDashboard.cs` (or mark as v13-only)
  - Delete `src/BulkUpload/Sections/BulkUploadSection.cs` (or mark as v13-only)
  - Remove from `BulkUploadComposer.cs:20`

- [ ] **Replace manifest filter**
  - Create `App_Plugins/BulkUpload.v17/umbraco-package.json`
  - Delete or conditionally compile `BulkUploadManifestFilter.cs`
  - Remove from `BulkUploadComposer.cs:18`

#### Medium Priority (Testing Required)

- [ ] **Update NuGet package references**
  - `Umbraco.Cms.Web.Website` 13.13.0 → 17.0.0
  - `Umbraco.Cms.Web.BackOffice` 13.13.0 → 17.0.0
  - Test for compilation errors

- [ ] **Test core service APIs**
  - Verify `IContentService` methods still work
  - Verify `IMediaService` methods still work
  - Test media file upload pipeline
  - Test parent-child relationships

- [ ] **Review extension method usage**
  - Search for deprecated extension methods
  - Replace with supported alternatives
  - Run full test suite

#### Low Priority (Nice to Have)

- [ ] **UTC date handling**
  - Review date property imports
  - Ensure proper timezone handling
  - Test date-based imports

- [ ] **Code modernization**
  - Consider new Umbraco 17 APIs
  - Optimize for performance improvements
  - Add nullability annotations

---

### 📋 Frontend Changes Required

#### Phase 5: Lit Component Implementation

- [ ] **Create build configuration**
  - Add `package.json` with Vite
  - Add `tsconfig.json`
  - Configure bundling

- [ ] **Create Lit dashboard component**
  - Port UI from AngularJS template to Lit
  - Import existing `BulkUploadService.js`
  - Import existing `BulkUploadApiClient.js`
  - Use `FetchHttpAdapter` (default)

- [ ] **Create extension manifest**
  - Define dashboard in `umbraco-package.json`
  - Configure section placement
  - Set permissions

- [ ] **Test frontend**
  - Test file uploads
  - Test result display
  - Test CSV export
  - Test media preprocessing

---

### 📋 Testing Strategy

#### Unit Tests

- [ ] Run existing `BulkUpload.Tests` against v17
- [ ] Test all resolvers
- [ ] Test media import service
- [ ] Test hierarchy resolver

#### Integration Tests

- [ ] Create v17 test site
- [ ] Test content import end-to-end
- [ ] Test media import end-to-end
- [ ] Test ZIP upload with media files
- [ ] Test multi-CSV import
- [ ] Test legacy ID mapping

#### Compatibility Tests

- [ ] Build multi-targeted package
- [ ] Install on Umbraco 13 site (net8.0 build)
- [ ] Install on Umbraco 17 site (net10.0 build)
- [ ] Verify functionality on both

---

## Part 5: Effort Estimation

### Development Time Estimates

| Task | Complexity | Time Estimate |
|------|-----------|---------------|
| **Backend Changes** | | |
| Remove/adapt dashboard & section registration | Low | 0.5 days |
| Remove/adapt manifest filter | Low | 0.5 days |
| Update NuGet dependencies | Low | 0.25 days |
| Test & fix core service API changes | Medium | 0.5 days |
| Configure multi-targeting | Medium | 0.5 days |
| **Frontend Changes** | | |
| Create Lit component | Medium | 1-2 days |
| Create extension manifest | Low | 0.5 days |
| Build configuration (Vite, TypeScript) | Low | 0.5 days |
| **Testing** | | |
| Unit tests | Low | 0.5 days |
| Integration tests | Medium | 1 day |
| **Total** | | **5-7 days** |

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Core service API breaking changes | Low | High | Thorough testing; fallback to v13-specific implementations |
| Extension method deprecations | Medium | Medium | Search and replace; use static method equivalents |
| Frontend Lit component complexity | Low | Medium | Already have framework-agnostic services; mostly UI work |
| Multi-targeting build issues | Low | Low | Well-documented approach; use conditional compilation |
| User data migration issues | Low | High | Test with real data; provide migration guide |

---

## Part 6: Recommendations

### Short-Term (Next 2 Weeks)

1. ✅ **Continue using Umbraco 13**
   - Current implementation is stable
   - No urgent need to migrate yet

2. 📝 **Prepare multi-targeting structure**
   - Rename current files with `.v13.cs` suffix where needed
   - Set up conditional compilation directives
   - Document version-specific code

3. 🧪 **Create Umbraco 17 test environment**
   - Set up parallel v17 development site
   - Test compatibility early and often

### Medium-Term (Next 1-2 Months)

1. 🎨 **Implement Lit component (Phase 5)**
   - Start with simple dashboard shell
   - Import existing services
   - Build incrementally

2. 📦 **Create multi-targeted package**
   - Configure `.csproj` for both targets
   - Build separate frontend asset folders
   - Test package installation on both versions

3. 📚 **Update documentation**
   - Create v17 installation guide
   - Document differences between versions
   - Provide migration path for existing users

### Long-Term (Post-Release)

1. 🚀 **Release v2.0 with v17 support**
   - Publish multi-targeted NuGet package
   - Announce LTS-to-LTS upgrade support
   - Provide clear versioning guidance

2. 🔧 **Maintain both versions**
   - Bug fixes for both v13 and v17
   - New features target both versions
   - Eventually sunset v13 support (after v13 EOL)

3. 🎯 **Leverage v17 features**
   - Explore new Management API
   - Consider distributed background jobs for large imports
   - Optimize for performance improvements

---

## Part 7: Code Reuse Summary

### Backend Code Reuse: ~70%

| Component | Reusable? | Notes |
|-----------|-----------|-------|
| Controllers | ✅ 100% | No changes needed |
| Services | ✅ 95% | Minor API adjustments possible |
| Resolvers | ✅ 100% | Pure business logic, framework-agnostic |
| Models | ✅ 100% | No changes needed |
| Caches | ✅ 100% | No changes needed |
| Composer (DI) | ✅ 90% | Remove manifest/dashboard registration |
| Dashboard/Section | ❌ 0% | Must use new extension manifest |
| Manifest Filter | ❌ 0% | Must use JSON manifest |

### Frontend Code Reuse: ~90%

| Component | Reusable? | Notes |
|-----------|-----------|-------|
| BulkUploadService.js | ✅ 100% | Framework-agnostic, direct import |
| BulkUploadApiClient.js | ✅ 100% | Just use FetchHttpAdapter |
| httpAdapters.js | ✅ 100% | FetchHttpAdapter already implemented |
| fileUtils.js | ✅ 100% | Pure JavaScript utilities |
| resultUtils.js | ✅ 100% | Pure JavaScript utilities |
| AngularJS Controller | ❌ 0% | Replace with Lit component |
| AngularJS Template | ⚠️ 50% | UI structure reusable, syntax changes |

### Overall Code Reuse: ~85%

**Out of ~4,000 lines of code:**
- **~3,400 lines** can be reused directly or with minor changes
- **~600 lines** need to be rewritten (Lit component + manifest)

This is an **excellent position** for migration! 🎉

---

## Part 8: Breaking Changes Summary

### Changes Required for Umbraco 17

#### 🔴 **Critical (Must Change)**

1. **Dashboard Registration**
   - Old: `IDashboard` C# interface
   - New: `umbraco-package.json` extension manifest
   - Files: `BulkUploadDashboard.cs` → DELETE or conditionally compile

2. **Section Registration**
   - Old: `ISection` via Composer
   - New: Extension manifest or built-in sections
   - Files: `BulkUploadSection.cs` → DELETE or conditionally compile

3. **Package Manifest**
   - Old: `IManifestFilter` with `PackageManifest`
   - New: `umbraco-package.json` file
   - Files: `BulkUploadManifestFilter.cs` → DELETE or conditionally compile

4. **Frontend Framework**
   - Old: AngularJS controller
   - New: Lit Web Component
   - Files: `bulkUpload.controller.js` → Rewrite as Lit component

#### 🟡 **Moderate (Test & Verify)**

1. **Core Service APIs**
   - Changes not considered "breaking" by Umbraco
   - May have deprecations or signature changes
   - Files: All service classes → Test thoroughly

2. **Extension Methods**
   - Some removed in v17
   - Files: All `.cs` files using `Umbraco.Extensions` → Review and test

#### 🟢 **Low Risk (Likely Compatible)**

1. **Dependency Injection**
   - `IComposer` still supported
   - Files: `BulkUploadComposer.cs` → No changes expected

2. **API Controllers**
   - `UmbracoAuthorizedApiController` still supported
   - Files: `ImportController.cs`, `MediaImportController.cs` → No changes expected

3. **Models & Business Logic**
   - No framework dependencies
   - Files: All model classes, resolver classes → No changes expected

---

## Conclusion

The BulkUpload package is in an **excellent position** for Umbraco 17 migration thanks to the recent frontend refactoring work. The framework-agnostic service layer means:

- ✅ **90% of frontend code** can be directly imported into the new Lit component
- ✅ **70% of backend code** requires no changes
- ✅ **Total migration effort**: 5-7 days (much better than a complete rewrite!)

### Next Steps

1. **Immediate**: Set up multi-targeting in `.csproj`
2. **Short-term**: Create Lit component (Phase 5)
3. **Medium-term**: Build and test v17 package
4. **Long-term**: Release v2.0 with LTS-to-LTS upgrade support

The investment in extracting framework-agnostic services has paid off significantly! 🚀

---

## References

### Official Umbraco Documentation
- [Version Specific Upgrades](https://docs.umbraco.com/umbraco-cms/fundamentals/setup/upgrading/version-specific)
- [Umbraco 17 Release Notes](https://releases.umbraco.com/release/umbraco/Umbraco-CMS/17.0.0)
- [Extension Manifest Introduction](https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-registry/extension-manifest)
- [Dashboard Extension Type](https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-types/dashboard)

### Umbraco Blog Posts
- [Umbraco 17 LTS Release](https://umbraco.com/blog/umbraco-17-lts-release)
- [Umbraco 17 Beta Announcement](https://umbraco.com/blog/umbraco-17-beta-is-out/)
- [Release Candidate Umbraco 17](https://umbraco.com/blog/release-candidate-umbraco-17/)

### Community Resources
- [Upgrade v13 to v14 and IDashboard - Umbraco Forum](https://forum.umbraco.com/t/upgrade-v13-to-v14-and-idashboard/5734)
- [Service APIs Documentation](https://docs.umbraco.com/umbraco-cms/fundamentals/code/umbraco-services)

---

**Document Version:** 1.0
**Last Updated:** 2025-12-26
**Prepared By:** Claude (AI Assistant)
**Review Status:** Ready for Team Review
