# Multi-Version Branching and Release Strategy

## Overview

This document outlines the branching and release strategy for BulkUpload to support multiple Umbraco versions while maximizing code sharing.

## Current State

- **Version 1.0.0** supports Umbraco 13
- Planning to support Umbraco 17 as v2.x
- Most business logic is version-agnostic
- Version-specific code is primarily in dependencies and APIs

## Branch Strategy

### Main Branches

```
main (or master)
├── release/v13.x    # For Umbraco 13 (current)
└── release/v17.x    # For Umbraco 17 (upcoming)
```

### Branch Purposes

| Branch | Purpose | Umbraco Version | Package Version Pattern |
|--------|---------|-----------------|------------------------|
| `main` | Development, latest features | N/A | Development only |
| `release/v13.x` | Production releases for Umbraco 13 | 13.x | 1.x.x |
| `release/v17.x` | Production releases for Umbraco 17 | 17.x | 2.x.x |

### Key Principles

1. **`main` branch is the source of truth** - All new features developed here first
2. **Release branches are long-lived** - They exist for the lifetime of that Umbraco version support
3. **Version-specific changes go directly to release branches** - Only when they cannot be generalized
4. **Regular forward-porting** - Changes from older release branches should be forward-ported to main

## Versioning Strategy

### Semantic Versioning with Umbraco Mapping

Use semantic versioning (MAJOR.MINOR.PATCH) aligned with Umbraco version support:

```
Umbraco 13 → BulkUpload 1.x.x
Umbraco 17 → BulkUpload 2.x.x
```

### Version Bumping Rules

- **MAJOR**: New Umbraco version support (breaking changes)
- **MINOR**: New features, enhancements
- **PATCH**: Bug fixes, security patches

### Example Timeline

```
1.0.0  - Initial release (Umbraco 13)
1.1.0  - Add new resolver feature (Umbraco 13)
1.1.1  - Fix CSV parsing bug (Umbraco 13)
2.0.0  - Initial release (Umbraco 17)
2.0.1  - Fix CSV parsing bug (Umbraco 17) - backported from 1.1.1
```

## Workflow Scenarios

### Scenario 1: New Feature Development

**Goal**: Add a new feature that works across all Umbraco versions

```bash
# 1. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/new-csv-validation

# 2. Develop and test the feature
# ... make changes ...

# 3. Create PR to main
git push origin feature/new-csv-validation
# Open PR → main

# 4. After merge to main, cherry-pick to release branches
git checkout release/v13.x
git cherry-pick <commit-hash>
git push origin release/v13.x

git checkout release/v17.x
git cherry-pick <commit-hash>
git push origin release/v17.x

# 5. Release new versions
# 1.2.0, 2.1.0 (depending on current version)
```

### Scenario 2: Bug Fix

**Goal**: Fix a bug reported in a specific version

```bash
# 1. Fix in the affected release branch
git checkout release/v13.x
git checkout -b bugfix/csv-parsing-issue

# 2. Make the fix and test
# ... make changes ...

# 3. Create PR to release branch
git push origin bugfix/csv-parsing-issue
# Open PR → release/v13.x

# 4. After merge, cherry-pick to other branches
git checkout main
git cherry-pick <commit-hash>

git checkout release/v17.x
git cherry-pick <commit-hash>

# 5. Release patch version (e.g., 1.1.2)
```

### Scenario 3: Version-Specific Change

**Goal**: Make a change that only applies to one Umbraco version

```bash
# 1. Create branch from specific release branch
git checkout release/v17.x
git checkout -b fix/umbraco17-specific-api

# 2. Make version-specific changes
# ... make changes ...

# 3. Create PR to that release branch only
git push origin fix/umbraco17-specific-api
# Open PR → release/v17.x

# 4. Release new version for that branch only (e.g., 2.0.1)
```

### Scenario 4: Creating Support for New Umbraco Version

**Goal**: Add support for Umbraco 17

```bash
# 1. Create new release branch from main
git checkout main
git pull origin main
git checkout -b release/v17.x

# 2. Update dependencies in .csproj
# - Update Umbraco.Cms.Web.Website to 17.x.x
# - Update Umbraco.Cms.Web.BackOffice to 17.x.x
# - Update TargetFramework if needed

# 3. Update version and metadata
# - Update <Version> to 2.0.0
# - Update README/docs to mention Umbraco 17 support

# 4. Test thoroughly with Umbraco 17

# 5. Push branch and create initial release
git push -u origin release/v17.x

# 6. Tag and release 2.0.0
git tag v2.0.0
git push origin v2.0.0
```

## Release Process

### Preparation

1. **Ensure all tests pass**
   ```bash
   dotnet test
   ```

2. **Update version in .csproj**
   ```xml
   <Version>1.2.0</Version>
   ```

3. **Update CHANGELOG.md** (create if doesn't exist)
   ```markdown
   ## [1.2.0] - 2025-12-20
   ### Added
   - New CSV validation feature

   ### Fixed
   - CSV parsing bug with special characters
   ```

4. **Commit version bump**
   ```bash
   git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
   git commit -m "chore: bump version to 1.2.0"
   ```

### Creating the Release

1. **Build the package**
   ```bash
   cd src/BulkUpload
   dotnet build -c Release
   ```

2. **Create and push tag**
   ```bash
   git tag v1.2.0
   git push origin release/v13.x --tags
   ```

3. **Publish to NuGet**
   ```bash
   dotnet pack -c Release
   dotnet nuget push bin/Release/Umbraco.Community.BulkUpload.1.2.0.nupkg -s https://api.nuget.org/v3/index.json -k YOUR_API_KEY
   ```

4. **Create GitHub Release**
   - Go to GitHub → Releases → New Release
   - Select the tag (v1.2.0)
   - Title: "v1.2.0 - Umbraco 13"
   - Description: Copy from CHANGELOG
   - Attach .nupkg file

## Code Sharing Strategies

### Strategy 1: Maximize Shared Code

Keep version-specific code isolated:

```
src/BulkUpload/
├── Resolvers/           # Shared across all versions
├── Services/            # Shared across all versions
├── Models/              # Shared across all versions
├── Extensions/          # May have version-specific implementations
└── Compatibility/       # Version-specific adapters (if needed)
```

### Strategy 2: Use Conditional Compilation (If Needed)

For minor version differences within the same codebase:

```csharp
#if UMBRACO13
    // Umbraco 13 specific code
#else
    // Umbraco 17 specific code
#endif
```

Define in .csproj:
```xml
<PropertyGroup Condition="'$(UmbracoVersion)' == '13'">
    <DefineConstants>$(DefineConstants);UMBRACO13</DefineConstants>
</PropertyGroup>
```

**Note**: Only use if differences are minimal. Otherwise, maintain separate branches.

### Strategy 3: Abstraction Layers

Create interfaces for Umbraco-specific operations:

```csharp
public interface IUmbracoContentService
{
    IContent CreateContent(string name, IContent parent, string contentTypeAlias);
    void SaveAndPublish(IContent content);
}

// Version-specific implementations
public class Umbraco13ContentService : IUmbracoContentService { }
public class Umbraco17ContentService : IUmbracoContentService { }
```

## Maintenance Strategy

### Active Support Matrix

| Umbraco Version | BulkUpload Version | Support Status | End of Life |
|----------------|-------------------|----------------|-------------|
| 13.x | 1.x.x | Active | TBD |
| 17.x | 2.x.x | Planned | TBD |

### Support Levels

- **Active**: New features, bug fixes, security patches
- **Maintenance**: Bug fixes and security patches only
- **End of Life**: No further updates

### Bug Fix Priority

1. **Security vulnerabilities**: Fix in all supported versions immediately
2. **Critical bugs**: Fix in all active versions within 1 week
3. **Minor bugs**: Fix in main, consider backporting based on severity
4. **Enhancements**: Develop in main, selectively backport

## Automation Recommendations

### GitHub Actions Workflows

Create separate workflows for each version:

```yaml
# .github/workflows/build-v13.yml
name: Build and Test - Umbraco 13
on:
  push:
    branches: [ release/v13.x ]
  pull_request:
    branches: [ release/v13.x ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### Release Automation

Consider using tools like:
- **Semantic Release**: Automate version bumping and changelog generation
- **GitHub Actions**: Automate NuGet publishing on tag creation
- **Dependabot**: Automate dependency updates per branch

## Migration Path

### Current State → New Strategy

1. **Week 1: Setup**
   - Create `release/v13.x` from current main
   - Tag current version as v1.0.0
   - Update documentation

2. **Week 2-3: Umbraco 17 Support**
   - Branch `release/v17.x` from main
   - Update dependencies
   - Test and release v2.0.0

3. **Week 4: Process Documentation**
   - Document any version-specific quirks
   - Create runbooks for releases
   - Train team on new workflow

## Best Practices

### DO ✅

- Always develop new features in `main` first
- Keep release branches stable and production-ready
- Write comprehensive commit messages
- Tag all releases
- Maintain a CHANGELOG
- Test across all supported versions before release
- Document version-specific limitations

### DON'T ❌

- Don't develop features directly in release branches
- Don't merge release branches into each other
- Don't skip testing when cherry-picking
- Don't forget to forward-port bug fixes
- Don't let branches diverge significantly
- Don't ignore breaking changes in dependencies

## Tools and Resources

- **Git Flow**: Consider using git-flow extensions
- **Conventional Commits**: https://www.conventionalcommits.org/
- **Semantic Versioning**: https://semver.org/
- **Keep a Changelog**: https://keepachangelog.com/

## Communication

### Internal Team

- Document all version-specific changes in PR descriptions
- Use labels: `v13`, `v17`, `needs-backport`
- Regular sync meetings to discuss multi-version changes

### External Users

- Clear documentation on which version to install
- Installation instructions per Umbraco version
- Migration guides between versions

## Summary

This strategy provides:
- **Clarity**: Clear branch structure for each Umbraco version
- **Flexibility**: Support multiple versions simultaneously
- **Maintainability**: Systematic approach to backporting and forward-porting
- **Quality**: Consistent testing and release process
- **Scalability**: Easy to add support for future Umbraco versions

---

**Questions or Suggestions?**

Open an issue or discuss with the team to evolve this strategy as needed.
