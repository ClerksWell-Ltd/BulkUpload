# Release Process Guide

This guide covers the complete release process for BulkUpload from the `main` branch with multi-targeting support.

## Overview

**v2.0.0+ Multi-Targeting Architecture:**
BulkUpload uses GitHub Actions to automate releases. Starting with v2.0.0, the package uses .NET multi-targeting to support both Umbraco 13 (net8.0) and Umbraco 17 (net10.0) in a **single NuGet package** published from the `main` branch.

**Key Points:**
- All releases are created from `main` branch
- Each release supports BOTH Umbraco 13 and 17
- Fully automated through GitHub Actions
- No cherry-picking or branch synchronization needed

## Automated Workflow

### Release to NuGet (`release.yml`)

**Trigger:** When you publish a GitHub Release

**What it does:**
- ✅ Validates release is from `main` branch
- ✅ Sets up .NET 8 and .NET 10 SDKs
- ✅ Builds Umbraco 17 frontend with Vite
- ✅ Restores dependencies
- ✅ Builds for both net8.0 and net10.0
- ✅ Runs all tests
- ✅ Packs NuGet package with both frameworks
- ✅ Publishes to NuGet.org
- ✅ Uploads package artifact to GitHub

**Branch restriction:** Must be created from `main` branch

## Complete Release Checklist

### Prerequisites

- [ ] All features/fixes are merged to `main`
- [ ] All tests are passing
- [ ] CHANGELOG.md has all changes documented
- [ ] `NUGET_API_KEY` is configured in GitHub repository secrets

### Step 1: Prepare Main Branch

```bash
# Ensure you're on main with latest changes
git checkout main
git pull origin main

# Verify everything builds and tests pass
dotnet build src/BulkUpload.sln --configuration Release
dotnet test

# Test both frameworks specifically
dotnet build -f net8.0
dotnet build -f net10.0
```

### Step 2: Update Version and CHANGELOG

**Update version in .csproj:**

```xml
<!-- src/BulkUpload/BulkUpload.csproj -->
<Version>2.2.0</Version>  <!-- Update this -->
```

**Update CHANGELOG.md:**

Move items from "Unreleased" to new version section:

```markdown
## [Unreleased]

### Added

### Fixed

### Changed

## [2.2.0] - 2025-02-06

### Added
- New CSV validation feature
- Support for additional file types

### Fixed
- Bug in media import
- CSV parsing edge case
```

**Commit changes:**

```bash
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v2.2.0"
git push origin main
```

### Step 3: Create GitHub Release

1. Go to GitHub → **Releases** → **Draft a new release**
2. Click **"Choose a tag"** → Type new tag (e.g., `v2.2.0`) → **"Create new tag on publish"**
3. **Target:** Select `main` branch
4. **Release title:** `v2.2.0`
5. **Description:** Copy the changes from CHANGELOG.md for this version
6. **Optional:** Check "Set as latest release"
7. Click **"Publish release"**

### Step 4: Monitor Automated Workflow

**GitHub Actions runs automatically:**

1. Go to GitHub → **Actions** tab
2. Find the "Release to NuGet" workflow run
3. Monitor the steps:
   - ✅ Build (both net8.0 and net10.0)
   - ✅ Test
   - ✅ Pack
   - ✅ Publish to NuGet
4. Workflow takes ~3-5 minutes
5. If anything fails, check the logs and fix the issue

**Common issues:**
- **Build failed:** Check build logs for errors
- **Tests failed:** Fix failing tests and create new release
- **NuGet push failed:** Verify `NUGET_API_KEY` secret is set correctly

### Step 5: Verify NuGet Publication

1. Go to [NuGet.org](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
2. Verify the new version appears (may take 5-10 minutes to index)
3. Check that both frameworks are included:
   - Dependencies → .NETStandard 8.0
   - Dependencies → .NETStandard 10.0
4. Test installation:
   ```bash
   dotnet nuget locals http-cache --clear
   dotnet add package Umbraco.Community.BulkUpload --version 2.2.0
   ```

### Step 6: Complete

That's it! The release is complete. The package is now available on NuGet with support for both Umbraco 13 and 17.

## Release Types

### Patch Release (2.1.0 → 2.1.1)

**When:** Bug fixes, security patches

**Process:** Follow all steps above, bump PATCH version

**Example:** `v2.1.1`

### Minor Release (2.1.0 → 2.2.0)

**When:** New features, enhancements

**Process:** Follow all steps above, bump MINOR version

**Example:** `v2.2.0`

### Major Release (2.x.x → 3.0.0)

**When:** Breaking changes, major architectural updates

**Process:**
1. Follow all steps above, bump MAJOR version
2. Update documentation to highlight breaking changes
3. Consider pre-release versions first (3.0.0-beta.1)

**Example:** `v3.0.0`

## Pre-release Versions

For beta or release candidate versions:

```bash
# Update to prerelease version
# src/BulkUpload/BulkUpload.csproj: <Version>2.3.0-beta.1</Version>

# Commit and push
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare prerelease v2.3.0-beta.1"
git push origin main

# Create GitHub Release
# Tag: v2.3.0-beta.1
# Target: main
# ✓ Check "This is a pre-release"
# Publish
```

**Note:** Prerelease packages won't show in NuGet search by default. Users must explicitly reference the version.

## Hotfix Process

For urgent fixes that need immediate release:

```bash
# 1. Create hotfix branch
git checkout main
git pull origin main
git checkout -b hotfix/critical-security-fix

# 2. Fix the issue
# ... make changes ...

# 3. Test thoroughly
dotnet build
dotnet test

# 4. Create PR and get it merged
git add .
git commit -m "fix: critical security vulnerability in CSV parser"
git push origin hotfix/critical-security-fix
# Create PR → Get approved → Merge

# 5. After merge, follow normal release process
# Bump patch version (e.g., 2.1.1 → 2.1.2)
# Create GitHub Release
```

## Troubleshooting

### Release Workflow Failed

**Check GitHub Actions:**
- Go to Actions → Find failed workflow
- Review error logs
- Common fixes:
  - Build errors: Fix code and create new release
  - Test failures: Fix tests and create new release
  - NuGet push failed: Check `NUGET_API_KEY` secret

### Wrong Version Published

**Cannot delete from NuGet, but can unlist:**
1. Go to https://www.nuget.org/packages/Umbraco.Community.BulkUpload/manage
2. Find the incorrect version
3. Click "Unlist"
4. Publish corrected version with higher version number

### Build Fails for One Framework

**Check conditional compilation:**
```bash
# Build each framework separately to identify issue
dotnet build -f net8.0
dotnet build -f net10.0

# Review conditional compilation blocks
# Ensure #if NET8_0 and #if NET10.0 blocks are correct
```

### Missing Frontend Assets in Package

**Verify frontend build:**
```bash
# For V17 frontend
cd src/BulkUpload/ClientV17
npm install
npm run build

# Check wwwroot/ is generated
ls src/BulkUpload/wwwroot/
```

## Version Numbering

### Semantic Versioning: MAJOR.MINOR.PATCH

| Change Type | Version Bump | Example |
|------------|--------------|---------|
| Breaking change | MAJOR | 2.x.x → 3.0.0 |
| New feature | MINOR | 2.1.x → 2.2.0 |
| Bug fix | PATCH | 2.1.0 → 2.1.1 |
| Security fix | PATCH | 2.1.1 → 2.1.2 |

**Note:** All versions from v2.0.0+ support both Umbraco 13 and 17.

## Required GitHub Secrets

| Secret Name | Purpose | Where to Get |
|------------|---------|--------------|
| `NUGET_API_KEY` | Publish to NuGet.org | https://www.nuget.org/account/apikeys |

**To configure:**
1. Go to GitHub repository settings
2. Navigate to Secrets and variables → Actions
3. Add `NUGET_API_KEY` with your NuGet API key
4. Scope: Select "Expiration: 365 days" and "Push packages" permission

## Best Practices

### Before Release

- ✅ Run all tests locally (`dotnet test`)
- ✅ Test package in real Umbraco 13 and 17 projects
- ✅ Review CHANGELOG.md for completeness
- ✅ Ensure commit messages follow Conventional Commits
- ✅ Update documentation if APIs changed

### During Release

- ✅ Use correct semantic versioning
- ✅ Write clear, detailed release notes
- ✅ Wait for automated workflow to complete
- ✅ Don't create multiple releases simultaneously

### After Release

- ✅ Verify package on NuGet.org
- ✅ Test installation: `dotnet add package`
- ✅ Check package includes both frameworks
- ✅ Announce release if significant
- ✅ Close related GitHub milestones

## Quick Reference Commands

```bash
# Prepare release
git checkout main && git pull origin main
dotnet build -c Release && dotnet test

# Update version and CHANGELOG.md (manual edit)
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v2.2.0"
git push origin main

# Create GitHub Release (via GitHub UI)
# - Tag: v2.2.0
# - Target: main
# - Publish

# Verify release
# Check GitHub Actions
# Check NuGet.org after 5-10 minutes
```

## Support and Documentation

- **Branching Strategy:** [BRANCHING_STRATEGY.md](./BRANCHING_STRATEGY.md)
- **Quick Reference:** [QUICK_REFERENCE_RELEASE.md](./QUICK_REFERENCE_RELEASE.md)
- **Contributing:** [CONTRIBUTING.md](../.github/CONTRIBUTING.md)
- **Multi-Targeting:** [MULTI_TARGETING_QUICK_START.md](./MULTI_TARGETING_QUICK_START.md)

## Questions or Issues?

- Check documentation in `docs/` folder
- Review GitHub Actions logs
- Open an issue on GitHub
- Join Umbraco Discord #package-development

---

**Simplified with multi-targeting!** One branch, one package, both Umbraco versions.
