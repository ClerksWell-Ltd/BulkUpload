# Multi-Targeting Quick Start

A quick reference guide for working with BulkUpload's multi-targeting architecture.

## TL;DR

Starting with v2.0.0, BulkUpload supports **both Umbraco 13 and 17** from a **single codebase** in a **single NuGet package**.

- One branch (`main`)
- One package (`Umbraco.Community.BulkUpload`)
- Targets both `net8.0` (Umbraco 13) and `net10.0` (Umbraco 17)
- NuGet automatically picks the right version

## Key Changes from v1.x

| Aspect | v1.x (Old) | v2.0+ (New) |
|--------|-----------|------------|
| **Branches** | `release/v13.x`, `release/v17.x` | Single `main` branch |
| **Packages** | Separate per version | Single package |
| **Workflow** | Cherry-pick between branches | Direct commit to main |
| **Version** | v1.x (Umbraco 13), v2.x (Umbraco 17) | v2.x (both versions) |

## Essential Commands

### Building

```bash
# Build both frameworks
dotnet build

# Build specific framework
dotnet build -f net8.0    # Umbraco 13
dotnet build -f net10.0   # Umbraco 17

# Release build
dotnet build -c Release
```

### Testing

```bash
# Test both frameworks
dotnet test

# Test specific framework
dotnet test -f net8.0
dotnet test -f net10.0
```

### Running Test Sites

```bash
# Umbraco 13
cd src/BulkUpload.TestSite13
dotnet run

# Umbraco 17
cd src/BulkUpload.TestSite17
dotnet run
```

### Frontend Development

```bash
# V13 (AngularJS) - No build needed
# Edit files directly in: BulkUpload/wwwroot/BulkUpload/

# V17 (Lit + Vite) - Watch mode
cd src/BulkUpload/ClientV17
npm run dev

# V17 - Production build
cd src/BulkUpload/ClientV17
npm run build
```

### Creating Packages

```bash
cd src/BulkUpload
dotnet pack -c Release

# Output: bin/Release/Umbraco.Community.BulkUpload.{version}.nupkg
# Contains: lib/net8.0/ and lib/net10.0/
```

## Framework-Specific Code

Use conditional compilation for version-specific code:

```csharp
#if NET8_0
    // Umbraco 13 specific code
    builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#elif NET10_0
    // Umbraco 17 specific code
    // (Uses umbraco-package.json instead)
#endif
```

## Project Structure

```
BulkUpload.Core/              # Core business logic
├── TargetFrameworks: net8.0;net10.0
└── Conditional dependencies per framework

BulkUpload/                   # Umbraco integration (RCL)
├── TargetFrameworks: net8.0;net10.0
├── wwwroot/
│   ├── BulkUpload/          # V13 AngularJS (static)
│   ├── bulkupload.js        # V17 bundle (built)
│   └── umbraco-package.json # V17 manifest
├── ClientV13/               # V13 source (not built)
└── ClientV17/               # V17 source (Vite build)
```

## Common Patterns

### Adding Framework-Specific Dependencies

```xml
<!-- net8.0 only -->
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <PackageReference Include="Umbraco.Cms.Web.Website" Version="13.13.0" />
</ItemGroup>

<!-- net10.0 only -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <PackageReference Include="Umbraco.Cms.Api.Common" Version="17.1.0" />
</ItemGroup>
```

### Conditional Build Targets

```xml
<!-- Only run for net10.0 -->
<Target Name="BuildFrontendV17" BeforeTargets="BeforeBuild"
        Condition="'$(TargetFramework)' == 'net10.0'">
  <Exec Command="npm run build" WorkingDirectory="ClientV17" />
</Target>
```

### Conditional Using Directives

```csharp
#if NET8_0
using Umbraco.Cms.Core.Sections;
using BulkUpload.Sections;
#endif
```

## Development Workflow

```bash
# 1. Create feature branch
git checkout main
git pull origin main
git checkout -b feature/my-feature

# 2. Make changes
# (Changes apply to both net8.0 and net10.0)

# 3. Build and test both frameworks
dotnet build
dotnet test

# 4. Test in both versions
cd src/BulkUpload.TestSite13 && dotnet run  # Test Umbraco 13
cd src/BulkUpload.TestSite17 && dotnet run  # Test Umbraco 17

# 5. Create PR to main
git push origin feature/my-feature
# Open PR on GitHub
```

## Release Workflow

```bash
# 1. Update versions
# - src/BulkUpload.Core/BulkUpload.Core.csproj
# - src/BulkUpload/BulkUpload.csproj
# - src/BulkUpload/ClientV17/package.json
# - src/BulkUpload/wwwroot/umbraco-package.json

# 2. Update CHANGELOG.md
# - Move [Unreleased] items to [2.1.0] section

# 3. Commit
git add .
git commit -m "chore: prepare release v2.1.0"

# 4. Create GitHub Release
# - Tag: v2.1.0
# - Target: main
# - Automated workflow publishes to NuGet
```

## Troubleshooting

### Build Errors

**"Type or namespace name 'X' does not exist"**
- Add framework-specific `using` with `#if NET8_0`

**"Static web asset not found"**
- Check `StaticWebAssetBasePath` in `.csproj`

**"npm command not found"**
- Install Node.js 20+
- Ensure `npm` is in PATH

### Runtime Issues

**Dashboard not appearing in Umbraco 13**
- Check `#if NET8_0` in `BulkUploadComposer.cs`
- Verify section registration

**Dashboard not appearing in Umbraco 17**
- Check `wwwroot/umbraco-package.json` exists
- Verify `bulkupload.js` built correctly
- Check browser console for errors

**API endpoints returning 404**
- Verify controllers in `BulkUpload.Core` project
- Check `[ApiController]` and `[Route]` attributes

## Advantages

✅ **Single codebase** - No branch management
✅ **No cherry-picking** - Changes apply to both versions
✅ **Single package** - One NuGet package for all versions
✅ **Automatic selection** - NuGet picks the right version
✅ **Better testing** - Test both versions simultaneously
✅ **Faster development** - One PR, one build, one release

## Resources

- **[Full Documentation](./MULTI_TARGETING.md)** - Complete multi-targeting guide
- **[CLAUDE.md](../.claude/CLAUDE.md)** - Developer quick reference
- **[Quick Reference](./QUICK_REFERENCE.md)** - Command cheat sheet
- **[Branching Strategy](./BRANCHING_STRATEGY.md)** - Legacy multi-branch docs (pre-v2.0.0)

---

**Questions?** See [MULTI_TARGETING.md](./MULTI_TARGETING.md) for detailed documentation.
