# Multi-Targeting Architecture

## Overview

BulkUpload uses .NET multi-targeting to support both Umbraco 13 and Umbraco 17 from a **single codebase** in a **single NuGet package**. This document explains how the multi-targeting architecture works and how to maintain it.

## What Changed

### Previous Architecture (v1.x)
- **Multi-branch strategy**: Separate `release/v13.x` and `release/v17.x` branches
- **Separate packages**: Different NuGet packages for each Umbraco version
- **Cherry-picking workflow**: Manual cherry-picking of changes between branches
- **Version-specific releases**: Release v1.x.x for Umbraco 13, v2.x.x for Umbraco 17

### Current Architecture (v2.0.0+)
- **Single codebase**: Both versions in the same solution and projects
- **Multi-targeting**: Projects target both `net8.0` and `net10.0`
- **Single package**: One NuGet package (`Umbraco.Community.BulkUpload`) contains both versions
- **Automatic framework selection**: NuGet automatically installs the correct version based on the target framework
- **Shared development**: Changes apply to both versions simultaneously

## Architecture Components

### 1. Project Structure

```
src/
├── BulkUpload.Core/                    # Core business logic
│   ├── BulkUpload.Core.csproj          # Multi-targets: net8.0, net10.0
│   ├── Controllers/                    # API controllers
│   ├── Services/                       # Core services
│   ├── Resolvers/                      # CSV resolvers
│   ├── Models/                         # Data models
│   └── Constants/                      # Shared constants
│
├── BulkUpload/                         # Umbraco integration (RCL)
│   ├── BulkUpload.csproj               # Multi-targets: net8.0, net10.0
│   ├── BulkUploadComposer.cs           # DI registration (with #if NET8_0)
│   ├── Sections/                       # Umbraco 13 sections (net8.0 only)
│   ├── Dashboards/                     # Umbraco 13 dashboards (net8.0 only)
│   ├── ClientV13/                      # V13 frontend (AngularJS) - NOT BUILT
│   ├── ClientV17/                      # V17 frontend (Lit + Vite)
│   └── wwwroot/                        # Static web assets (RCL)
│       ├── BulkUpload/                 # V13 AngularJS files (static)
│       ├── bulkupload.js               # V17 bundle (built from ClientV17)
│       └── umbraco-package.json        # V17 package manifest
│
├── BulkUpload.Tests/                   # Unit tests
├── BulkUpload.TestSite13/              # Umbraco 13 test site
└── BulkUpload.TestSite17/              # Umbraco 17 test site
```

### 2. Multi-Targeting Configuration

#### BulkUpload.Core.csproj

```xml
<PropertyGroup>
  <!-- Multi-targeting: Supports both Umbraco 13 (net8.0) and Umbraco 17 (net10.0) -->
  <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
  <Version>2.1.0</Version>
</PropertyGroup>

<!-- Shared dependencies (both versions) -->
<ItemGroup>
  <PackageReference Include="CsvHelper" Version="33.1.0" />
</ItemGroup>

<!-- Umbraco 13 dependencies (net8.0) -->
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <PackageReference Include="Umbraco.Cms.Web.Website" Version="13.13.0" />
  <PackageReference Include="Umbraco.Cms.Web.BackOffice" Version="13.13.0" />
</ItemGroup>

<!-- Umbraco 17 dependencies (net10.0) -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <PackageReference Include="Umbraco.Cms.Api.Common" Version="17.1.0" />
  <PackageReference Include="Umbraco.Cms.Core" Version="17.1.0" />
</ItemGroup>
```

**Key Points:**
- `<TargetFrameworks>` (plural) specifies multiple targets
- Conditional `ItemGroup` elements with `Condition="'$(TargetFramework)' == 'net8.0'"` for framework-specific dependencies
- Both frameworks share the same version number
- Separate build outputs: `bin/Debug/net8.0/` and `bin/Debug/net10.0/`

#### BulkUpload.csproj (Razor Class Library)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <!-- Multi-target for Umbraco 13 (net8.0) and Umbraco 17 (net10.0) -->
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <Version>2.0.0</Version>

    <!-- RCL settings -->
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <ContentTargetFolders>.</ContentTargetFolders>
  </PropertyGroup>

  <!-- Conditional StaticWebAssetBasePath per framework -->
  <PropertyGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <StaticWebAssetBasePath>App_Plugins</StaticWebAssetBasePath>
  </PropertyGroup>

  <PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <StaticWebAssetBasePath>/App_Plugins/BulkUpload</StaticWebAssetBasePath>
    <EnableDefaultContentItems>false</EnableDefaultContentItems>
  </PropertyGroup>

  <!-- Reference to Core package -->
  <ItemGroup>
    <ProjectReference Include="..\BulkUpload.Core\BulkUpload.Core.csproj" />
  </ItemGroup>

  <!-- Build V17 frontend only for net10.0 -->
  <Target Name="BuildFrontendV17" BeforeTargets="BeforeBuild"
          Condition="'$(TargetFramework)' == 'net10.0'">
    <Message Text="[BulkUpload V17] Building frontend bundle..." Importance="high" />
    <Exec Command="npm install" WorkingDirectory="ClientV17"
          Condition="!Exists('ClientV17/node_modules')" />
    <Exec Command="npm run build" WorkingDirectory="ClientV17" />
  </Target>
</Project>
```

**Key Points:**
- `Microsoft.NET.Sdk.Razor` SDK enables Razor Class Library (RCL) functionality
- `StaticWebAssetBasePath` differs between frameworks:
  - net8.0: `App_Plugins` (Umbraco 13 expects files at `/App_Plugins/BulkUpload/`)
  - net10.0: `/App_Plugins/BulkUpload` (explicit full path)
- Conditional build targets: `BuildFrontendV17` only runs for net10.0
- RCL automatically includes `wwwroot/` as static web assets

### 3. Conditional Compilation

Use preprocessor directives for framework-specific code:

#### BulkUploadComposer.cs

```csharp
using BulkUpload.Core.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

#if NET8_0
using BulkUpload.Sections;
using Umbraco.Cms.Core.Sections;
#endif

namespace BulkUpload;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
#if NET8_0
        // Umbraco 13: Register sections and dashboards via C# API
        builder.ManifestFilters().Append<BulkUploadManifestFilter>();
        builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#endif
        // Note: Umbraco 17 uses umbraco-package.json for section/dashboard registration

        // Shared service registrations (both versions)
        builder.Services.AddSingleton<IResolver, TextResolver>();
        builder.Services.AddTransient<IResolver, GuidToContentUdiResolver>();
        // ... more resolvers
    }
}
```

**Key Points:**
- `#if NET8_0` compiles code only for net8.0 (Umbraco 13)
- `#if NET10_0` compiles code only for net10.0 (Umbraco 17)
- `#else` and `#elif` supported for complex conditions
- Shared code (outside `#if` blocks) compiles for all frameworks
- Umbraco 13 uses C# APIs for registration, Umbraco 17 uses JSON manifests

### 4. Razor Class Library (RCL) for Static Assets

The `BulkUpload` project uses the Razor Class Library (RCL) pattern to serve static web assets.

#### How RCL Works

1. **wwwroot directory**: Any files in `wwwroot/` are automatically packaged as static web assets
2. **Framework-specific paths**: `StaticWebAssetBasePath` controls where assets are served
3. **Automatic inclusion**: Host applications automatically serve RCL static assets
4. **No manual copying**: Files don't need to be copied to the host application

#### Static Asset Structure

```
BulkUpload/wwwroot/
├── umbraco-package.json           # V17 package manifest
├── bulkupload.js                  # V17 bundle (built from ClientV17)
├── bulkupload.js.map              # V17 source map
└── BulkUpload/                    # V13 AngularJS files
    ├── bulkUpload.Controller.js
    ├── bulkUploadDashboard.html
    ├── bulkUploadDashboard.css
    ├── bulkUploadImportApiService.js
    ├── services/
    │   ├── httpAdapters.js
    │   ├── BulkUploadService.js
    │   └── BulkUploadApiClient.js
    ├── utils/
    │   ├── fileUtils.js
    │   └── resultUtils.js
    └── lang/
        └── en.xml
```

#### Asset Serving

**Umbraco 13 (net8.0)**:
- `StaticWebAssetBasePath`: `App_Plugins`
- Files served at: `/App_Plugins/BulkUpload/bulkUpload.Controller.js`
- Umbraco 13 expects backoffice plugins at `/App_Plugins/{PluginName}/`

**Umbraco 17 (net10.0)**:
- `StaticWebAssetBasePath`: `/App_Plugins/BulkUpload`
- Files served at: `/App_Plugins/BulkUpload/bulkupload.js`
- Explicit full path ensures correct asset resolution

### 5. Frontend Build Process

#### Umbraco 13 Frontend (AngularJS)

**Location**: `BulkUpload/wwwroot/BulkUpload/`
**Technology**: AngularJS 1.x (legacy Umbraco backoffice framework)
**Build**: Pre-built, static files (no build step)

Files:
- `bulkUpload.Controller.js` - Main AngularJS controller
- `bulkUploadDashboard.html` - Dashboard template
- `bulkUploadImportApiService.js` - API service
- Services, utilities, and language files

**No build required**: These files are static and directly included in the package.

#### Umbraco 17 Frontend (Lit + Vite)

**Location**: `BulkUpload/ClientV17/`
**Technology**:
- **Lit 3.0**: Web Components framework
- **TypeScript**: Type-safe development
- **Vite 5.0**: Modern frontend build tool

**Build Configuration** (`vite.config.ts`):
```typescript
import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: '../wwwroot',           // Output to wwwroot for RCL
    emptyOutDir: false,             // Don't delete V13 files
    lib: {
      entry: 'src/index.ts',
      formats: ['es'],
      fileName: 'bulkupload'        // Outputs: bulkupload.js
    },
    rollupOptions: {
      external: [
        '@umbraco-cms/backoffice'   // Externalize Umbraco deps
      ]
    }
  }
});
```

**Build Process**:
1. `npm install` - Install dependencies (if `node_modules/` doesn't exist)
2. `npm run build` - Run Vite build
3. Output: `wwwroot/bulkupload.js` and `wwwroot/bulkupload.js.map`

**MSBuild Integration** (from `BulkUpload.csproj`):
```xml
<Target Name="BuildFrontendV17" BeforeTargets="BeforeBuild"
        Condition="'$(TargetFramework)' == 'net10.0'">
  <Message Text="[BulkUpload V17] Cleaning previous V17 bundle..." Importance="high" />
  <Delete Files="wwwroot\bulkupload.js;wwwroot\bulkupload.js.map;wwwroot\umbraco-package.json" />

  <Message Text="[BulkUpload V17] Installing frontend dependencies..." Importance="high" />
  <Exec Command="npm install" WorkingDirectory="ClientV17"
        Condition="!Exists('ClientV17/node_modules')" />

  <Message Text="[BulkUpload V17] Building frontend bundle..." Importance="high" />
  <Exec Command="npm run build" WorkingDirectory="ClientV17" />
</Target>
```

**Key Points**:
- Build only runs when building for `net10.0` (Umbraco 17)
- `npm install` runs conditionally (skipped if `node_modules/` exists)
- Clean step removes only V17 files, preserves V13 `BulkUpload/` folder
- Build happens before MSBuild compiles the C# code

### 6. Package Manifest Registration

#### Umbraco 13 (C# API)

Sections and dashboards registered via C# in `BulkUploadComposer.cs`:

```csharp
#if NET8_0
builder.ManifestFilters().Append<BulkUploadManifestFilter>();
builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#endif
```

**Files**:
- `Sections/BulkUploadSection.cs` - Section definition
- `Dashboards/BulkUploadDashboard.cs` - Dashboard definition
- `BulkUploadManifestFilter.cs` - Manifest filter

#### Umbraco 17 (JSON Manifest)

Sections and dashboards registered via `umbraco-package.json`:

```json
{
  "$schema": "../../umbraco-package-schema.json",
  "name": "Bulk Upload",
  "version": "2.0.0",
  "extensions": [
    {
      "type": "backoffice",
      "alias": "BulkUpload.Backoffice",
      "name": "Bulk Upload Backoffice",
      "js": "/App_Plugins/BulkUpload/bulkupload.js"
    },
    {
      "type": "section",
      "alias": "BulkUpload.Section",
      "name": "Bulk Upload",
      "meta": {
        "label": "Bulk Upload",
        "pathname": "bulk-upload"
      }
    },
    {
      "type": "dashboard",
      "alias": "BulkUpload.Dashboard",
      "name": "Bulk Upload Dashboard",
      "element": "/App_Plugins/BulkUpload/bulkupload.js",
      "elementName": "bulk-upload-dashboard",
      "weight": -10,
      "meta": {
        "label": "Bulk Upload",
        "pathname": "bulk-upload-dashboard"
      },
      "conditions": [
        {
          "alias": "Umb.Condition.SectionAlias",
          "match": "BulkUpload.Section"
        }
      ]
    }
  ]
}
```

**Key Points**:
- Umbraco 17 uses declarative JSON manifests instead of C# registration
- Web Components referenced via `element` and `elementName`
- Conditions control when dashboards appear

## How Multi-Targeting Works

### Build Process

When you build a multi-targeted project, MSBuild builds **both** frameworks sequentially:

```bash
cd src/BulkUpload.Core
dotnet build

# Output:
# Building for net8.0...
# Building for net10.0...
```

**Result**:
```
BulkUpload.Core/bin/Debug/
├── net8.0/
│   ├── BulkUpload.Core.dll      # net8.0 version
│   └── BulkUpload.Core.pdb
└── net10.0/
    ├── BulkUpload.Core.dll      # net10.0 version
    └── BulkUpload.Core.pdb
```

### NuGet Package Structure

When you pack the project, both frameworks are included in a single `.nupkg`:

```bash
cd src/BulkUpload
dotnet pack -c Release
```

**Package contents** (`Umbraco.Community.BulkUpload.2.0.0.nupkg`):
```
lib/
├── net8.0/
│   ├── BulkUpload.dll
│   ├── BulkUpload.Core.dll
│   └── ... (dependencies)
└── net10.0/
    ├── BulkUpload.dll
    ├── BulkUpload.Core.dll
    └── ... (dependencies)

staticwebassets/
└── ... (wwwroot files)
```

### Framework Selection

NuGet automatically selects the correct framework based on the consuming project:

**Umbraco 13 project** (`<TargetFramework>net8.0</TargetFramework>`):
- NuGet installs `lib/net8.0/BulkUpload.dll`
- Static assets served from `App_Plugins`

**Umbraco 17 project** (`<TargetFramework>net10.0</TargetFramework>`):
- NuGet installs `lib/net10.0/BulkUpload.dll`
- Static assets served from `/App_Plugins/BulkUpload`

### Dependency Resolution

NuGet resolves dependencies based on framework:

**net8.0** (Umbraco 13):
- `Umbraco.Cms.Web.Website` 13.13.0
- `Umbraco.Cms.Web.BackOffice` 13.13.0

**net10.0** (Umbraco 17):
- `Umbraco.Cms.Api.Common` 17.1.0
- `Umbraco.Cms.Web.Website` 17.1.0

## Development Workflows

### Building the Solution

```bash
# Build entire solution (both frameworks)
cd src
dotnet build BulkUpload.sln

# Build specific project
dotnet build BulkUpload.Core/BulkUpload.Core.csproj

# Build for specific framework only
dotnet build -f net8.0
dotnet build -f net10.0

# Build in Release mode
dotnet build -c Release
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific framework
dotnet test -f net8.0
dotnet test -f net10.0
```

### Running Test Sites

#### Umbraco 13 Test Site

```bash
cd src/BulkUpload.TestSite13
dotnet run

# Navigate to: https://localhost:xxxxx
```

#### Umbraco 17 Test Site

```bash
cd src/BulkUpload.TestSite17
dotnet run

# Navigate to: https://localhost:xxxxx
```

### Frontend Development

#### Umbraco 13 (AngularJS)
- Files: `BulkUpload/wwwroot/BulkUpload/`
- Edit directly, no build required
- Refresh browser to see changes

#### Umbraco 17 (Lit + Vite)

**Development mode** (watch mode):
```bash
cd src/BulkUpload/ClientV17
npm run dev
```

**Production build**:
```bash
cd src/BulkUpload/ClientV17
npm run build
```

**Note**: Frontend build happens automatically when building for net10.0:
```bash
cd src/BulkUpload
dotnet build -f net10.0
# Triggers: npm install (if needed) + npm run build
```

### Creating NuGet Packages

```bash
# Pack for both frameworks
cd src/BulkUpload
dotnet pack -c Release

# Output: bin/Release/Umbraco.Community.BulkUpload.{version}.nupkg
```

**Package includes**:
- `lib/net8.0/` - Umbraco 13 assemblies
- `lib/net10.0/` - Umbraco 17 assemblies
- `staticwebassets/` - Frontend files (both versions)
- `README_nuget.md` - Package documentation
- `icon.png` - Package icon

## Best Practices

### DO ✅

1. **Use conditional compilation for framework-specific code**
   ```csharp
   #if NET8_0
       // Umbraco 13 specific code
   #elif NET10_0
       // Umbraco 17 specific code
   #endif
   ```

2. **Keep most code framework-agnostic**
   - Business logic should work across versions
   - Only integration code needs framework-specific implementations

3. **Test both frameworks**
   ```bash
   dotnet test -f net8.0
   dotnet test -f net10.0
   ```

4. **Use conditional ItemGroups for dependencies**
   ```xml
   <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
     <PackageReference Include="Umbraco.Cms.Web.Website" Version="13.13.0" />
   </ItemGroup>
   ```

5. **Build both frameworks before releasing**
   ```bash
   dotnet build -c Release
   dotnet test -c Release
   dotnet pack -c Release
   ```

6. **Keep frontend builds separate**
   - V13: Static AngularJS files (no build)
   - V17: Modern build pipeline (Vite)

7. **Use descriptive MSBuild messages**
   ```xml
   <Message Text="[BulkUpload V17] Building frontend..." Importance="high" />
   ```

### DON'T ❌

1. **Don't put version-specific code outside `#if` blocks**
   ```csharp
   // ❌ BAD - Will break on other framework
   builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();

   // ✅ GOOD - Conditional compilation
   #if NET8_0
   builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
   #endif
   ```

2. **Don't use wrong SDK for project type**
   ```xml
   <!-- ❌ BAD - Can't serve static assets -->
   <Project Sdk="Microsoft.NET.Sdk">

   <!-- ✅ GOOD - RCL with static assets -->
   <Project Sdk="Microsoft.NET.Sdk.Razor">
   ```

3. **Don't hardcode framework versions in shared code**
   ```csharp
   // ❌ BAD
   if (UmbracoVersion == "13.0.0") { ... }

   // ✅ GOOD - Use conditional compilation
   #if NET8_0
   // Umbraco 13 logic
   #endif
   ```

4. **Don't forget to clean output directories**
   ```xml
   <!-- ✅ GOOD - Clean before building V17 -->
   <Delete Files="wwwroot\bulkupload.js;wwwroot\bulkupload.js.map" />
   ```

5. **Don't include ClientV13 in build**
   ```xml
   <!-- ✅ GOOD - Exclude from MSBuild -->
   <ItemGroup>
     <Compile Remove="ClientV13\**" />
   </ItemGroup>
   ```

6. **Don't delete V13 files when building V17**
   ```typescript
   // ✅ GOOD in vite.config.ts
   emptyOutDir: false  // Don't delete BulkUpload/ folder
   ```

7. **Don't build V17 frontend for net8.0**
   ```xml
   <!-- ✅ GOOD - Only build for net10.0 -->
   <Target Name="BuildFrontendV17" ...
           Condition="'$(TargetFramework)' == 'net10.0'">
   ```

## Advantages of Multi-Targeting

### 1. Single Codebase
- **No cherry-picking**: Changes apply to both versions automatically
- **Unified development**: One branch, one PR, one review
- **Easier maintenance**: Fix once, works everywhere

### 2. Single Package
- **Simpler distribution**: One NuGet package for all Umbraco versions
- **Automatic selection**: NuGet chooses the right version
- **Unified versioning**: One version number, consistent across platforms

### 3. Reduced Complexity
- **No branch management**: No need for `release/v13.x`, `release/v17.x` branches
- **No merge conflicts**: No cherry-picking means no merge conflicts
- **Clearer history**: Linear git history, easier to track changes

### 4. Better Testing
- **Test both versions together**: Ensures compatibility
- **Shared test infrastructure**: Tests run against both frameworks
- **Catch version-specific bugs early**: Compilation errors if code breaks one version

### 5. Faster Development
- **One PR**: Instead of separate PRs for each version
- **One build**: CI builds both versions in one run
- **One release**: Single release process for both versions

## Challenges and Solutions

### Challenge 1: Framework-Specific APIs

**Problem**: Umbraco 13 and 17 have different APIs for some operations.

**Solution**: Use conditional compilation or abstraction layers.

```csharp
public interface IContentCreator
{
    IContent CreateContent(string name, string contentTypeAlias);
}

#if NET8_0
public class Umbraco13ContentCreator : IContentCreator { ... }
#elif NET10_0
public class Umbraco17ContentCreator : IContentCreator { ... }
#endif
```

### Challenge 2: Different Frontend Frameworks

**Problem**: Umbraco 13 uses AngularJS, Umbraco 17 uses Web Components.

**Solution**: Separate frontend builds, shared backend API.

- Backend API controllers work for both versions
- Frontend files separated in `wwwroot/`
- Build process isolated per framework

### Challenge 3: Static Asset Paths

**Problem**: Different Umbraco versions expect assets at different paths.

**Solution**: Conditional `StaticWebAssetBasePath` in `.csproj`.

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <StaticWebAssetBasePath>App_Plugins</StaticWebAssetBasePath>
</PropertyGroup>

<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <StaticWebAssetBasePath>/App_Plugins/BulkUpload</StaticWebAssetBasePath>
</PropertyGroup>
```

### Challenge 4: Build Performance

**Problem**: Building both frameworks doubles build time.

**Solution**: Build specific framework during development.

```bash
# Development: Build only the framework you're testing
dotnet build -f net8.0
dotnet run --project BulkUpload.TestSite13

# CI/Release: Build both frameworks
dotnet build -c Release
```

### Challenge 5: Debugging

**Problem**: Which framework is being debugged?

**Solution**: Use conditional breakpoints and test sites.

- Use `BulkUpload.TestSite13` to debug Umbraco 13 (net8.0)
- Use `BulkUpload.TestSite17` to debug Umbraco 17 (net10.0)
- Set conditional breakpoints: `#if NET8_0`

## Version Management

### Synchronizing Versions

All projects should use the same version number:

| Project | Version | Notes |
|---------|---------|-------|
| `BulkUpload.Core` | 2.1.0 | Core library version |
| `BulkUpload` | 2.0.0 | Main package version |
| `ClientV17/package.json` | 2.0.0 | Frontend package version |
| `wwwroot/umbraco-package.json` | 2.0.0 | Umbraco manifest version |

**Update all versions together** when releasing:

```bash
# Update .csproj files
# Update package.json files
# Update umbraco-package.json
# Update CHANGELOG.md
```

### Semantic Versioning

- **MAJOR** (2.0.0): Breaking changes, new Umbraco version support
- **MINOR** (2.1.0): New features, backwards-compatible
- **PATCH** (2.0.1): Bug fixes, backwards-compatible

**Note**: No longer tied to Umbraco versions (e.g., 1.x = Umbraco 13, 2.x = Umbraco 17).
Multi-targeting allows v2.x to support **both** Umbraco 13 and 17.

## Migration from Multi-Branch Strategy

### What Changed

| Aspect | Multi-Branch (v1.x) | Multi-Targeting (v2.0+) |
|--------|---------------------|-------------------------|
| **Branches** | `release/v13.x`, `release/v17.x` | Single `main` branch |
| **Packages** | Separate packages per version | Single package with both versions |
| **Workflow** | Cherry-pick changes | Direct commit to main |
| **Testing** | Test one version at a time | Test both versions together |
| **Releases** | Separate releases (v1.x, v2.x) | Unified releases (v2.x) |

### Migration Steps

If you have local branches from the old strategy:

```bash
# 1. Backup any local work
git stash
git branch backup-old-work

# 2. Switch to new main branch
git checkout main
git pull origin main

# 3. Verify multi-targeting
dotnet build -c Release
dotnet test

# 4. Delete old branches (optional)
git branch -D release/v13.x
git branch -D release/v17.x
```

## Troubleshooting

### Build Issues

#### Error: "The type or namespace name 'X' does not exist"

**Cause**: Framework-specific type not available in current framework.

**Solution**: Use conditional compilation.

```csharp
#if NET8_0
using Umbraco.Cms.Core.Sections;  // Only available in Umbraco 13
#endif
```

#### Error: "Static web asset not found"

**Cause**: `StaticWebAssetBasePath` configuration issue.

**Solution**: Check conditional `PropertyGroup` in `.csproj`.

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <StaticWebAssetBasePath>App_Plugins</StaticWebAssetBasePath>
</PropertyGroup>
```

#### Error: "npm command not found"

**Cause**: Node.js not installed or not in PATH.

**Solution**:
1. Install Node.js 20+ from https://nodejs.org
2. Ensure `npm` is in PATH
3. Restart terminal/IDE

#### Error: "Frontend build failed"

**Cause**: npm dependencies missing or build script error.

**Solution**:
```bash
cd src/BulkUpload/ClientV17
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Runtime Issues

#### Dashboard not appearing in Umbraco 13

**Check**:
1. Section registered in `BulkUploadComposer.cs`?
2. `#if NET8_0` condition present?
3. Restart Umbraco application

#### Dashboard not appearing in Umbraco 17

**Check**:
1. `umbraco-package.json` present in `wwwroot/`?
2. `bulkupload.js` bundle built correctly?
3. Check browser console for loading errors

#### API endpoints returning 404

**Check**:
1. Controllers in `BulkUpload.Core` project?
2. Controllers decorated with `[ApiController]` and `[Route]`?
3. Project referenced correctly: `BulkUpload` → `BulkUpload.Core`

## Future Considerations

### Adding Support for Umbraco 18

When Umbraco 18 is released:

```xml
<!-- Update TargetFrameworks to include net11.0 -->
<TargetFrameworks>net8.0;net10.0;net11.0</TargetFrameworks>

<!-- Add Umbraco 18 dependencies -->
<ItemGroup Condition="'$(TargetFramework)' == 'net11.0'">
  <PackageReference Include="Umbraco.Cms.Api.Common" Version="18.x.x" />
  <PackageReference Include="Umbraco.Cms.Web.Website" Version="18.x.x" />
</ItemGroup>
```

No new branches needed - just add another target framework!

### Dropping Support for Older Versions

To drop Umbraco 13 support:

```xml
<!-- Remove net8.0 from TargetFrameworks -->
<TargetFrameworks>net10.0;net11.0</TargetFrameworks>

<!-- Remove net8.0 conditional code -->
#if NET8_0  // ← Delete these blocks
...
#endif
```

### Breaking Changes

If you need to make breaking API changes:

- **Minor/Patch**: Use `#if` blocks to maintain compatibility
- **Major**: Breaking changes OK, bump to v3.0.0

## Summary

Multi-targeting provides:

✅ **Single codebase** - No branch management, no cherry-picking
✅ **Single package** - One NuGet package for all versions
✅ **Automatic framework selection** - NuGet picks the right version
✅ **Faster development** - One PR, one build, one release
✅ **Better testing** - Test both versions simultaneously
✅ **Future-proof** - Easy to add support for new Umbraco versions

The multi-targeting architecture significantly simplifies development and maintenance while providing a better experience for both developers and users.

---

**Questions or Issues?**

- Review this document for common scenarios
- Check the [Troubleshooting](#troubleshooting) section
- Open an issue on GitHub for additional help
