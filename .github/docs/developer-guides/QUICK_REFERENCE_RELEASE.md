# Quick Reference: Release Process

Quick command reference for releasing from the `main` branch with multi-targeting support.

## Creating a Release

### 1. Prepare Release

```bash
# Checkout main branch
git checkout main
git pull origin main

# Update version in .csproj (manually edit)
# src/BulkUpload/BulkUpload.csproj: <Version>2.2.0</Version>

# Update CHANGELOG.md (manually edit)
# Add release notes under new version heading

# Commit changes
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v2.2.0"
git push origin main
```

### 2. Create GitHub Release

1. Go to: https://github.com/ClerksWell-Ltd/BulkUpload/releases/new
2. **Tag:** `v2.2.0` (create new tag)
3. **Target:** `main`
4. **Title:** `v2.2.0`
5. **Description:** Copy from CHANGELOG.md
6. Click **"Publish release"**

### 3. Automated (No Action Needed)

The release workflow automatically:
- ✅ Validates release is from `main` branch
- ✅ Builds for both net8.0 (Umbraco 13) and net10.0 (Umbraco 17)
- ✅ Runs all tests
- ✅ Creates NuGet package with both frameworks
- ✅ Publishes to NuGet.org
- ✅ Uploads package artifact to GitHub

### 4. Verify Release

```bash
# Check NuGet.org for new version
# https://www.nuget.org/packages/Umbraco.Community.BulkUpload

# Verify package contains both frameworks
dotnet nuget locals http-cache --clear
dotnet add package Umbraco.Community.BulkUpload --version 2.2.0
```

## Version Numbering

### Semantic Versioning: MAJOR.MINOR.PATCH

| Change Type | Version Example | Description |
|-------------|----------------|-------------|
| Breaking change | 3.0.0 | Major architectural changes |
| New feature | 2.2.0 | Add new functionality |
| Bug fix | 2.1.1 | Fix existing functionality |
| Security fix | 2.1.2 | Security patches |

**Note:** Each release supports BOTH Umbraco 13 (net8.0) and Umbraco 17 (net10.0).

## Testing Before Release

### Build and Test Locally

```bash
# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore

# Build for both frameworks
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release

# Create package locally (optional - to test packaging)
dotnet pack src/BulkUpload/BulkUpload.csproj --configuration Release --output ./artifacts
```

### Verify Package Contents

```bash
# Extract and inspect package
cd ./artifacts
unzip -l Umbraco.Community.BulkUpload.2.2.0.nupkg

# Check for:
# - lib/net8.0/BulkUpload.dll
# - lib/net10.0/BulkUpload.dll
# - contentFiles or staticwebassets for frontend assets
```

## Hotfix Process

For urgent fixes that need to be released quickly:

```bash
# 1. Create hotfix branch
git checkout main
git pull origin main
git checkout -b hotfix/critical-bug

# 2. Fix the bug
# ... make changes ...

# 3. Test thoroughly
dotnet build
dotnet test

# 4. Commit and push
git add .
git commit -m "fix: resolve critical bug in CSV parser"
git push origin hotfix/critical-bug

# 5. Create PR to main
# After PR is approved and merged...

# 6. Follow normal release process above
# Update version (e.g., 2.1.1 → 2.1.2 for patch)
# Create GitHub Release
```

## Rollback a Release

If you need to unpublish or rollback a release:

### Unpublish from NuGet

```bash
# NuGet doesn't allow deletion, but you can unlist
# Go to: https://www.nuget.org/packages/Umbraco.Community.BulkUpload/manage
# Click "Unlist" for the problematic version
# This hides it from search but allows existing users to continue using it
```

### Create Corrective Release

```bash
# 1. Fix the issue
git checkout main
git pull origin main
git checkout -b fix/release-issue

# 2. Make corrections
# ... fix code ...

# 3. Test thoroughly
dotnet build
dotnet test

# 4. Commit and merge to main
git add .
git commit -m "fix: correct issue in v2.2.0"
# Create PR, get approved, merge

# 5. Release new patch version v2.2.1
# Follow normal release process
```

## Prerelease Versions

For beta or RC releases:

```bash
# 1. Update version with prerelease suffix
# src/BulkUpload/BulkUpload.csproj: <Version>2.3.0-beta.1</Version>

# 2. Commit and push
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare prerelease v2.3.0-beta.1"
git push origin main

# 3. Create GitHub Release
# Tag: v2.3.0-beta.1
# Target: main
# ✓ Check "This is a pre-release"
# Publish

# Note: Prerelease versions won't appear in NuGet search by default
# Users must explicitly opt-in: dotnet add package BulkUpload --version 2.3.0-beta.1
```

## Release Checklist

### Before Release

- [ ] All features/fixes merged to `main`
- [ ] Code builds successfully (`dotnet build -c Release`)
- [ ] All tests pass (`dotnet test -c Release`)
- [ ] Tested manually with Umbraco 13
- [ ] Tested manually with Umbraco 17
- [ ] Version bumped in src/BulkUpload/BulkUpload.csproj
- [ ] CHANGELOG.md updated with release notes
- [ ] All changes committed and pushed to `main`
- [ ] No pending PRs that should be included

### During Release

- [ ] GitHub Release created from `main` branch
- [ ] Correct tag version (e.g., v2.2.0)
- [ ] Release notes added
- [ ] Published (not saved as draft)

### After Release

- [ ] Verify on NuGet.org: https://www.nuget.org/packages/Umbraco.Community.BulkUpload
- [ ] Test installing package: `dotnet add package Umbraco.Community.BulkUpload --version 2.2.0`
- [ ] Update marketplace listing (if needed)
- [ ] Announce release (if significant)
- [ ] Close milestone (if using GitHub milestones)

## Troubleshooting

### Release Workflow Failed

Check GitHub Actions:
```
https://github.com/ClerksWell-Ltd/BulkUpload/actions/workflows/release.yml
```

Common issues:
- **Build failed:** Check build logs for errors
- **Tests failed:** Check test logs, fix failing tests
- **NuGet push failed:**
  - Check if version already exists on NuGet
  - Verify NUGET_API_KEY secret is set
  - Check NuGet.org status

### Wrong Version Published

1. Unlist the incorrect version on NuGet.org
2. Fix the version locally
3. Create new release with correct version

### Missing Framework in Package

Verify .csproj has:
```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Check build output for both frameworks:
```bash
dotnet build -c Release
# Should see:
# BulkUpload -> ...bin/Release/net8.0/BulkUpload.dll
# BulkUpload -> ...bin/Release/net10.0/BulkUpload.dll
```

## Version History

| Version | Release Date | Umbraco Support | Notes |
|---------|-------------|-----------------|-------|
| 2.0.0+ | 2025-01-15 | 13 & 17 | Multi-targeting architecture |
| 1.x.x | Before 2025 | 13 only | Legacy single-target releases |

## Useful Links

- **NuGet Package:** https://www.nuget.org/packages/Umbraco.Community.BulkUpload
- **GitHub Releases:** https://github.com/ClerksWell-Ltd/BulkUpload/releases
- **GitHub Actions:** https://github.com/ClerksWell-Ltd/BulkUpload/actions
- **Release Workflow:** `.github/workflows/release.yml`

## GitHub Secrets Required

| Secret | Purpose | Where to Get |
|--------|---------|--------------|
| `NUGET_API_KEY` | Publish to NuGet.org | https://www.nuget.org/account/apikeys |

Configure at: https://github.com/ClerksWell-Ltd/BulkUpload/settings/secrets/actions
