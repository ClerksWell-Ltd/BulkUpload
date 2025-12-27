# BulkUpload Core Package Implementation

**Implementation Date:** 2025-12-27
**Status:** ✅ **Phase 1 Complete** - Core package structure implemented for Umbraco 13
**Next Phase:** Add Umbraco 17 support when ready

---

## 🎯 Architecture Overview

The BulkUpload package has been refactored into a **multi-project structure** to support both Umbraco 13 and future Umbraco 17 versions with maximum code reuse:

```
BulkUpload/
├── src/
│   ├── BulkUpload.Core/              # ✅ Multi-targeted shared library (NEW)
│   │   ├── BulkUpload.Core.csproj    # Targets: net8.0, net10.0
│   │   ├── Services/                  # All business logic services
│   │   ├── Resolvers/                 # All 25+ resolver implementations
│   │   ├── Models/                    # All data models
│   │   ├── Controllers/               # API controllers (import endpoints)
│   │   └── Constants/                 # Reserved column names
│   │
│   ├── BulkUpload/                    # ✅ Umbraco 13 package (UPDATED)
│   │   ├── BulkUpload.csproj          # Targets: net8.0 only
│   │   ├── Dashboards/                # IDashboard (v13-specific)
│   │   ├── Sections/                  # ISection (v13-specific)
│   │   ├── BulkUploadComposer.cs      # v13 DI registration
│   │   ├── BulkUploadManifestFilter.cs # v13 manifest
│   │   └── App_Plugins/BulkUpload/    # AngularJS frontend + services
│   │
│   └── BulkUpload.V17/                # ⏳ Umbraco 17 package (FUTURE)
│       └── (to be created when ready for v17 support)
```

---

## ✅ What Was Implemented (Phase 1)

### 1. Created BulkUpload.Core Project

**Purpose:** Multi-targeted shared library containing all framework-agnostic business logic.

**Key Features:**
- **Multi-targeting:** Compiles for both `net8.0` (Umbraco 13) and `net10.0` (Umbraco 17)
- **Shared dependencies:** CsvHelper, SourceLink
- **Conditional dependencies:** Umbraco packages per target framework
- **Package metadata:** Ready for NuGet distribution

**File:** `src/BulkUpload.Core/BulkUpload.Core.csproj`

### 2. Moved Shared Code to Core

**Moved from BulkUpload → BulkUpload.Core:**
- ✅ `/Services/` (8 service implementations + interfaces)
- ✅ `/Resolvers/` (25+ resolver implementations + factory)
- ✅ `/Models/` (ImportObject, MediaImportObject, result models, etc.)
- ✅ `/Controllers/` (ImportController, MediaImportController)
- ✅ `/Constants/` (ReservedColumns)

**Total Lines Moved:** ~3,500 lines of core business logic

### 3. Updated Namespaces

**All Core files now use:**
```csharp
namespace Umbraco.Community.BulkUpload.Core.Services;
namespace Umbraco.Community.BulkUpload.Core.Resolvers;
namespace Umbraco.Community.BulkUpload.Core.Models;
namespace Umbraco.Community.BulkUpload.Core.Controllers;
namespace Umbraco.Community.BulkUpload.Core.Constants;
```

### 4. Updated BulkUpload (v13) Package

**Changes Made:**
- ✅ Added project reference to `BulkUpload.Core`
- ✅ Removed CsvHelper dependency (now in Core)
- ✅ Updated version to 2.0.0
- ✅ Updated description to mention Umbraco 13 specifically
- ✅ Updated all using statements to reference Core namespaces
- ✅ Removed moved directories (Services, Resolvers, Models, Controllers, Constants)

**What Remains in BulkUpload (v13-specific only):**
- Dashboards/BulkUploadDashboard.cs (IDashboard registration)
- Sections/BulkUploadSection.cs (ISection registration)
- BulkUploadManifestFilter.cs (package.manifest)
- BulkUploadComposer.cs (DI registration for v13)
- App_Plugins/BulkUpload/ (AngularJS frontend)

### 5. Updated BulkUpload.Tests

**Changes Made:**
- ✅ Updated project reference from `BulkUpload` → `BulkUpload.Core`
- ✅ Updated all using statements to reference Core namespaces
- ✅ Tests now validate Core business logic directly

### 6. Updated Solution File

**Changes Made:**
- ✅ Added `BulkUpload.Core.csproj` to solution
- ✅ Configured Debug and Release build configurations

---

## 📊 Code Distribution

### BulkUpload.Core (Shared - ~85% of codebase)

| Component | File Count | Purpose |
|-----------|-----------|---------|
| Services | 8 files | Import logic, media processing, hierarchy resolution, caching |
| Resolvers | 25+ files | CSV column resolvers (media, content, data types) |
| Models | 8 files | Import objects, results, block list/grid models |
| Controllers | 2 files | API endpoints for content and media import |
| Constants | 1 file | Reserved column names |

**Total:** ~3,500 lines of shared business logic

### BulkUpload (v13-specific - ~15% of codebase)

| Component | File Count | Purpose |
|-----------|-----------|---------|
| Dashboards | 1 file | IDashboard registration (v13 only) |
| Sections | 1 file | ISection registration (v13 only) |
| Manifest Filter | 1 file | package.manifest registration |
| Composer | 1 file | DI container setup |
| Frontend | ~10 files | AngularJS UI + framework-agnostic services |

**Total:** ~600 lines of v13-specific code

---

## 🎯 Benefits Achieved

### 1. Code Reuse
- ✅ **85%+ of code** is now shared between v13 and future v17
- ✅ All business logic changes only need to be made once
- ✅ Bug fixes in Core automatically benefit both versions

### 2. Clear Separation of Concerns
- ✅ Core = Pure business logic (framework-agnostic)
- ✅ Version packages = UI registration only (thin wrappers)
- ✅ No conditional compilation (`#if` directives) needed in most code

### 3. Easier Maintenance
- ✅ Version-specific code is isolated and obvious
- ✅ Fewer merge conflicts when cherry-picking commits
- ✅ Clearer project structure for new contributors

### 4. Git Workflow Improvements
- ✅ Backend changes → Commit to `BulkUpload.Core/`
- ✅ v13 UI changes → Commit to `BulkUpload/`
- ✅ v17 UI changes → Commit to `BulkUpload.V17/` (when created)
- ✅ Clean cherry-picking with minimal conflicts

### 5. Testing Improvements
- ✅ Test Core once against both frameworks
- ✅ Shared test suite for business logic
- ✅ Version-specific tests for UI registration

---

## 🚀 Next Steps: Adding Umbraco 17 Support

When ready to add Umbraco 17 support, follow these steps:

### Step 1: Create BulkUpload.V17 Project (~0.5 days)

```bash
# Create new project
mkdir src/BulkUpload.V17
```

**Create:** `src/BulkUpload.V17/BulkUpload.V17.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>Umbraco.Community.BulkUpload</PackageId>
    <Version>2.0.0</Version>
    <Description>BulkUpload for Umbraco 17 - bulk content and media import</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\BulkUpload.Core\BulkUpload.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Cms.Web.Website" Version="17.0.0" />
    <PackageReference Include="Umbraco.Cms.Web.BackOffice" Version="17.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="App_Plugins\BulkUpload\**\*"
             ExcludeFromSingleFile="true"
             CopyToOutputDirectory="Always" />
  </ItemGroup>
</Project>
```

### Step 2: Create Minimal Composer (~0.25 days)

**Create:** `src/BulkUpload.V17/BulkUploadComposer.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.BulkUpload.Core.Services;
using Umbraco.Community.BulkUpload.Core.Resolvers;

namespace Umbraco.Community.BulkUpload;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // No dashboard/section registration - handled by umbraco-package.json

        // Register all Core services (same as v13)
        builder.Services.AddSingleton<IResolver, TextResolver>();
        builder.Services.AddSingleton<IResolver, BooleanResolver>();
        // ... all other service registrations from v13 composer
        builder.Services.AddSingleton<IImportUtilityService, ImportUtilityService>();
        builder.Services.AddSingleton<IMediaImportService, MediaImportService>();
        builder.Services.AddSingleton<IMediaPreprocessorService, MediaPreprocessorService>();
    }
}
```

### Step 3: Create Lit Component Frontend (~1-2 days)

**Directory structure:**
```
src/BulkUpload.V17/App_Plugins/BulkUpload/
├── umbraco-package.json          # Extension manifest (replaces IDashboard)
├── package.json                   # npm dependencies
├── tsconfig.json                  # TypeScript config
├── vite.config.ts                 # Build config
├── src/
│   └── dashboard.element.ts       # Lit component
├── services/                      # Copy from v13 (framework-agnostic!)
│   ├── BulkUploadService.js
│   ├── BulkUploadApiClient.js
│   └── httpAdapters.js
└── utils/                         # Copy from v13
    ├── fileUtils.js
    └── resultUtils.js
```

**Create:** `src/BulkUpload.V17/App_Plugins/BulkUpload/umbraco-package.json`
```json
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

**Create:** `src/BulkUpload.V17/App_Plugins/BulkUpload/src/dashboard.element.ts`
```typescript
import { LitElement, html, css } from 'lit';
import { customElement } from '@umbraco-cms/backoffice/external/lit';
// Import framework-agnostic services (DIRECTLY REUSED from v13!)
import { BulkUploadService } from '../services/BulkUploadService.js';
import { BulkUploadApiClient } from '../services/BulkUploadApiClient.js';

@customElement('bulk-upload-dashboard')
export class BulkUploadDashboard extends LitElement {
  private service: BulkUploadService;

  constructor() {
    super();
    // Use FetchHttpAdapter (default) instead of AngularHttpAdapter
    const apiClient = new BulkUploadApiClient();
    const notificationHandler = (n) => {
      // Use Umbraco 17 notification system
      this.dispatchEvent(new CustomEvent('notification', { detail: n }));
    };
    this.service = new BulkUploadService(apiClient, notificationHandler);
  }

  render() {
    return html`
      <uui-box>
        <!-- Same UI structure as v13, different template syntax -->
        <h2>Bulk Upload</h2>
        <!-- ... rest of UI -->
      </uui-box>
    `;
  }
}
```

### Step 4: Add to Solution and Build (~0.25 days)

```bash
# Add to solution (manually edit src/BulkUpload.sln)
# Build both packages
# Test on v13 and v17 sites
```

---

## 📦 Package Release Strategy

### Option A: Same Package ID (Recommended)

Both v13 and v17 use the same `PackageId: Umbraco.Community.BulkUpload`

**How it works:**
- NuGet automatically selects the correct package based on target framework
- v13 sites (`net8.0`) → Get BulkUpload package referencing Core (`net8.0`)
- v17 sites (`net10.0`) → Get BulkUpload.V17 package referencing Core (`net10.0`)

**User experience:**
```bash
# Same command for both versions!
dotnet add package Umbraco.Community.BulkUpload --version 2.0.0
```

### Option B: Separate Package IDs

**Alternative approach:**
- `Umbraco.Community.BulkUpload` (v13)
- `Umbraco.Community.BulkUpload.V17` (v17)
- `Umbraco.Community.BulkUpload.Core` (shared, installed automatically)

---

## 🔄 Git Commit Workflow

### Recommended Commit Organization

**Backend bug fix (affects both v13 and v17):**
```bash
git add src/BulkUpload.Core/
git commit -m "fix(core): resolve null reference in ImportUtilityService"
```

**v13 UI improvement:**
```bash
git add src/BulkUpload/
git commit -m "feat(v13): add file upload progress indicator"
```

**v17 UI improvement (when ready):**
```bash
git add src/BulkUpload.V17/
git commit -m "feat(v17): implement drag-drop file upload"
```

**Shared service improvement:**
```bash
git add src/BulkUpload.Core/Services/
git add src/BulkUpload/App_Plugins/BulkUpload/services/  # Update v13 frontend
# In future: Also update v17 frontend
git commit -m "feat(core): add batch import support"
```

### Cherry-Picking Strategy

**Scenario: Bug fix made in v13, need to apply to v17**

Since Core is shared, most bug fixes automatically work in both versions!

```bash
# Core fix
git checkout v13-branch
git add src/BulkUpload.Core/
git commit -m "fix(core): resolve CSV parsing issue"

# Automatically available to v17 (no cherry-pick needed!)
git checkout v17-branch
git merge main  # Core fix comes along
```

**Scenario: UI fix in v13 to port to v17**

```bash
# May need manual adaptation for Lit vs AngularJS
git checkout v17-branch
git cherry-pick <commit-hash>
# Adapt AngularJS syntax to Lit if needed
```

---

## 📋 Migration Checklist for v17 Support

When ready to add Umbraco 17 support:

### Phase 1: Project Setup (0.5 days)
- [ ] Create `src/BulkUpload.V17/` directory
- [ ] Create `BulkUpload.V17.csproj` with Core reference
- [ ] Create minimal `BulkUploadComposer.cs` (DI only)
- [ ] Add to solution file

### Phase 2: Frontend Setup (0.5 days)
- [ ] Create `App_Plugins/BulkUpload/` directory
- [ ] Add `package.json` with Lit dependencies
- [ ] Add `tsconfig.json`
- [ ] Add `vite.config.ts`
- [ ] Create `umbraco-package.json` extension manifest

### Phase 3: Copy Framework-Agnostic Services (0.25 days)
- [ ] Copy `services/BulkUploadService.js` from v13
- [ ] Copy `services/BulkUploadApiClient.js` from v13
- [ ] Copy `services/httpAdapters.js` from v13
- [ ] Copy `utils/fileUtils.js` from v13
- [ ] Copy `utils/resultUtils.js` from v13
- [ ] **No changes needed - direct copy!**

### Phase 4: Build Lit Component (1-2 days)
- [ ] Create `src/dashboard.element.ts`
- [ ] Import framework-agnostic services
- [ ] Build UI using UUI components
- [ ] Wire up service methods
- [ ] Configure Vite build

### Phase 5: Testing (1 day)
- [ ] Build BulkUpload.V17 package
- [ ] Install on Umbraco 17 test site
- [ ] Test content import
- [ ] Test media import
- [ ] Test CSV export
- [ ] Test error handling

### Phase 6: Documentation (0.5 days)
- [ ] Update README with v17 installation
- [ ] Update CHANGELOG
- [ ] Create migration guide for v13→v17 users
- [ ] Update NuGet package descriptions

**Total Estimated Effort:** 4-5 days

---

## 🎯 Key Advantages of This Architecture

### 1. Maximum Code Reuse
- ✅ 85%+ of code shared between versions
- ✅ Framework-agnostic services work in both AngularJS and Lit
- ✅ Core business logic tested once, works everywhere

### 2. Clean Separation
- ✅ No `#if NET8_0` conditional compilation in most code
- ✅ Version-specific code clearly isolated
- ✅ Easy to understand what's shared vs specific

### 3. Flexible Maintenance
- ✅ Can update v13 without touching v17
- ✅ Can sunset v13 later by just removing BulkUpload project
- ✅ Core package can be versioned independently if needed

### 4. Better Git Workflow
- ✅ Isolated commits per project
- ✅ Clean cherry-picking with minimal conflicts
- ✅ Clear history of what changed where

### 5. Easier Testing
- ✅ Test Core against both frameworks
- ✅ Version-specific tests for UI only
- ✅ Shared test suite reduces duplication

---

## 📚 References

### Internal Documentation
- [UMBRACO_17_MIGRATION_STRATEGY.md](UMBRACO_17_MIGRATION_STRATEGY.md) - Frontend refactoring strategy (Phases 1-5)
- [UMBRACO_17_COMPATIBILITY_REVIEW.md](UMBRACO_17_COMPATIBILITY_REVIEW.md) - Comprehensive compatibility analysis

### Umbraco Documentation
- [Extension Manifest Introduction](https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-registry/extension-manifest)
- [Dashboard Extension Type](https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-types/dashboard)
- [Version Specific Upgrades](https://docs.umbraco.com/umbraco-cms/fundamentals/setup/upgrading/version-specific)

---

## ✅ Summary

**Phase 1 Status:** ✅ **COMPLETE**

The BulkUpload package has been successfully refactored into a Core + version-specific architecture:

- ✅ **BulkUpload.Core** - Multi-targeted shared library (net8.0 + net10.0)
- ✅ **BulkUpload** - Umbraco 13 package (references Core)
- ✅ **BulkUpload.Tests** - Updated to test Core directly
- ✅ **Solution** - All projects added and configured

**Ready for v17:** The architecture is now in place. When ready to add Umbraco 17 support, simply create the `BulkUpload.V17` project following the checklist above. The Core package containing 85%+ of the code is already multi-targeted and ready to work with both versions!

---

**Document Version:** 1.0
**Last Updated:** 2025-12-27
**Implementation Status:** Phase 1 Complete ✅
