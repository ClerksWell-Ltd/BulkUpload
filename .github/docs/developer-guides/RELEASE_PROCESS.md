# Release Process Guide

This guide covers the complete release process for BulkUpload, including automated and manual steps.

## Overview

**v2.0.0+ Multi-Targeting Architecture:**
BulkUpload uses GitHub Actions to automate the release process. Starting with v2.0.0, the package uses .NET multi-targeting to support both Umbraco 13 (net8.0) and Umbraco 17 (net10.0) in a **single NuGet package** published from the `main` branch.

## Automated Workflows

### 1. Release to NuGet (`release.yml`)

**Trigger:** When you publish a GitHub Release

**What it does:**
- ✅ Verifies the release is from `main` branch
- ✅ Sets up both .NET 8 and .NET 10 SDKs
- ✅ Builds Umbraco 17 frontend (npm)
- ✅ Restores dependencies
- ✅ Builds the project in Release mode (both net8.0 and net10.0)
- ✅ Runs all tests
- ✅ Packs the NuGet package (includes both frameworks)
- ✅ Publishes to NuGet.org
- ✅ Uploads package as GitHub artifact

**Branch restriction:** Only works for releases created from `main` branch (v2.0.0+)

### 2. Post-Release Automation (`post-release.yml`)

**Trigger:** Automatically after a GitHub Release is published

**What it does:**
- ✅ Updates CHANGELOG.md with release version and date
- ✅ Bumps version in .csproj to next patch version
- ✅ Creates a PR with these changes for review

**Output:** A pull request labeled `post-release` that you need to review and merge.

## Complete Release Checklist

### Prerequisites

- [ ] All features/fixes for the release are merged
- [ ] All tests are passing
- [ ] CHANGELOG.md "Unreleased" section has all changes documented
- [ ] You have `NUGET_API_KEY` configured in GitHub repository secrets

### Step 1: Prepare the Main Branch (v2.0.0+)

```bash
# Ensure you're on main with latest changes
git checkout main
git pull origin main

# Verify everything builds and tests pass (both frameworks)
dotnet build src/BulkUpload.sln --configuration Release
dotnet test
```

### Step 2: Update Version and CHANGELOG (Manual)

**Update version in BOTH .csproj files (must match):**

```xml
<!-- src/BulkUpload/BulkUpload.csproj -->
<Version>2.1.0</Version>  <!-- Update this -->

<!-- src/BulkUpload.Core/BulkUpload.Core.csproj -->
<Version>2.1.0</Version>  <!-- Update this to match -->
```

**Update CHANGELOG.md:**

Ensure the "Unreleased" section has all changes, following the format:

```markdown
## [Unreleased]

### Added
- New feature X
- New feature Y

### Fixed
- Bug fix Z

### Changed
- Updated dependency A
```

**Commit these changes:**

```bash
git add src/BulkUpload/BulkUpload.csproj src/BulkUpload.Core/BulkUpload.Core.csproj CHANGELOG.md
git commit -m "chore: prepare release v2.1.0"
git push origin main
```

### Step 3: Create GitHub Release

1. Go to GitHub → **Releases** → **Draft a new release**
2. Click **"Choose a tag"** → Type new tag (e.g., `v2.1.0`) → **"Create new tag on publish"**
3. **Target:** Select `main` branch
4. **Release title:** `v2.1.0` (multi-targeted for Umbraco 13 & 17)
5. **Description:** Copy the changes from CHANGELOG.md for this version
6. **Optional:** Check "Set as latest release"
7. Click **"Publish release"**

### Step 4: Automated Workflows Run

**What happens automatically:**

1. **Release Workflow (`release.yml`)** triggers:
   - Builds and tests the project
   - Publishes to NuGet.org
   - Takes ~2-5 minutes

2. **Post-Release Workflow (`post-release.yml`)** triggers:
   - Updates CHANGELOG.md
   - Bumps version to next patch (e.g., 1.2.0 → 1.2.1)
   - Creates a PR

**Monitor the workflows:**
- Go to GitHub → **Actions** tab
- Verify both workflows complete successfully
- If the NuGet publish fails, check the logs and your `NUGET_API_KEY` secret

### Step 5: Review and Merge Post-Release PR

1. Go to **Pull Requests** tab
2. Find the automated PR: `"chore: post-release cleanup for v1.2.0"`
3. Review the changes:
   - CHANGELOG.md updated correctly?
   - Version bumped correctly?
4. **Merge the PR**

Your release branch is now ready for the next development cycle!

### Step 6: Cherry-Pick to Other Branches (Manual)

**Important:** If changes need to be shared across Umbraco versions, cherry-pick them manually.

#### Scenario A: Bug Fix Released in v13, Needs to Go to v17 and Main

```bash
# 1. Identify the commit hash from the release branch
git log release/v13.x --oneline -5
# Example output: abc1234 fix: handle empty CSV columns

# 2. Cherry-pick to main
git checkout main
git pull origin main
git cherry-pick abc1234
# Resolve conflicts if needed
dotnet test
git push origin main

# 3. Cherry-pick to release/v17.x
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick abc1234
# Resolve conflicts if needed
dotnet test
git push origin release/v17.x
```

#### Scenario B: Feature Released in Main, Needs to Go to Release Branches

```bash
# 1. Identify the commit hash from main
git log main --oneline -10
# Example: def5678 feat: add new CSV validator

# 2. Cherry-pick to release/v13.x
git checkout release/v13.x
git pull origin release/v13.x
git cherry-pick def5678
dotnet test
git push origin release/v13.x

# 3. Cherry-pick to release/v17.x
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick def5678
dotnet test
git push origin release/v17.x

# 4. Create new releases for both branches (repeat Steps 2-5)
```

### Step 7: Verify NuGet Publication

1. Go to [NuGet.org](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
2. Verify the new version appears (may take 5-10 minutes to index)
3. Check that the package details are correct
4. Test installation: `dotnet add package Umbraco.Community.BulkUpload --version 1.2.0`

## Release Types

### Patch Release (1.2.3 → 1.2.4)

**When:** Bug fixes, security patches

**Process:** Follow all steps above

**Cherry-pick strategy:** Usually cherry-pick to all active release branches

### Minor Release (1.2.0 → 1.3.0)

**When:** New features, enhancements

**Process:** Follow all steps above

**Cherry-pick strategy:** Consider impact - may only release on one version

### Major Release (1.x.x → 2.0.0)

**When:** New Umbraco version support, breaking changes

**Process:**
1. Follow all steps above
2. Create new `release/vXX.x` branch if needed (see [Creating New Release Branch](#creating-new-release-branch))
3. Update documentation to reference new version

## Creating New Release Branch

When adding support for a new Umbraco version (e.g., Umbraco 17):

### Step 1: Create Release Branch

```bash
# 1. Start from main
git checkout main
git pull origin main

# 2. Create new release branch
git checkout -b release/v17.x

# 3. Update dependencies in .csproj
# Update Umbraco.Cms.Web.Website to 17.x.x
# Update Umbraco.Cms.Web.BackOffice to 17.x.x
# Update TargetFramework if needed

# 4. Update version to 2.0.0
# <Version>2.0.0</Version>

# 5. Update CHANGELOG.md
# Add new section for 2.0.0

# 6. Test thoroughly with Umbraco 17
dotnet build
dotnet test

# 7. Push branch
git push -u origin release/v17.x
```

### Step 2: Create Initial Release

Follow the standard release process (Steps 1-7 above) to create v2.0.0

### Step 3: Update Documentation

- [ ] Update README.md with Umbraco 17 installation instructions
- [ ] Update CHANGELOG.md version mapping
- [ ] Update package compatibility matrix

## Troubleshooting

### Release Workflow Fails: "Not from release branch"

**Problem:** Tried to create release from wrong branch (e.g., `main`)

**Solution:**
1. Delete the draft release
2. Create a new release from the correct `release/*` branch

### NuGet Push Fails: "Package already exists"

**Problem:** Version already published to NuGet

**Solution:**
1. Bump version in .csproj
2. Commit and push
3. Delete the GitHub release
4. Create a new release with the new version tag

### Cherry-Pick Conflicts

**Problem:** Cherry-pick fails due to conflicts

**Solution:**
```bash
# Option 1: Resolve conflicts manually
git cherry-pick abc1234
# Fix conflicts in your editor
git add <resolved-files>
git cherry-pick --continue

# Option 2: Abort and manually port the fix
git cherry-pick --abort
git checkout -b fix/manual-port
# Manually recreate the fix
git commit -m "fix: (ported from v13) description"
```

### Post-Release PR Not Created

**Problem:** Post-release workflow didn't create a PR

**Solution:**
1. Check GitHub Actions logs for errors
2. Manually create the PR:
   ```bash
   git checkout release/v13.x
   # Update CHANGELOG.md and .csproj manually
   git checkout -b chore/post-release-v1.2.0
   git commit -am "chore: post-release cleanup"
   git push origin chore/post-release-v1.2.0
   # Create PR on GitHub
   ```

## Quick Reference

### Versioning Rules

| Change Type | Version Bump | Example |
|------------|--------------|---------|
| New Umbraco version | MAJOR | 1.x.x → 2.0.0 |
| New feature | MINOR | 1.2.x → 1.3.0 |
| Bug fix | PATCH | 1.2.3 → 1.2.4 |
| Security fix | PATCH | 1.2.3 → 1.2.4 |

### Version Mapping

| BulkUpload Version | Umbraco Version | Branch |
|-------------------|----------------|---------|
| 1.x.x | 13.x | `release/v13.x` |
| 2.x.x | 17.x | `release/v17.x` |

### Required GitHub Secrets

| Secret Name | Purpose | Where to Get |
|------------|---------|--------------|
| `NUGET_API_KEY` | Publish to NuGet.org | https://www.nuget.org/account/apikeys |

## Best Practices

### Before Release

- ✅ Run all tests locally
- ✅ Test the package in a real Umbraco project
- ✅ Review all changes in the "Unreleased" section of CHANGELOG
- ✅ Ensure commit messages follow Conventional Commits format
- ✅ Update documentation if needed

### During Release

- ✅ Use semantic versioning correctly
- ✅ Write clear, detailed release notes
- ✅ Tag releases with Umbraco version in title
- ✅ Wait for automated workflows to complete before proceeding

### After Release

- ✅ Merge the post-release PR promptly
- ✅ Cherry-pick important changes to other branches
- ✅ Verify package appears on NuGet.org
- ✅ Test installation from NuGet
- ✅ Announce the release (if applicable)

## Support Matrix

Maintain active support for:

| Support Level | Description | Actions |
|--------------|-------------|---------|
| **Active** | New features, bug fixes, security patches | Full release process |
| **Maintenance** | Bug fixes and security patches only | Selective releases |
| **End of Life** | No further updates | No releases |

Check [BRANCHING_STRATEGY.md](./BRANCHING_STRATEGY.md#maintenance-strategy) for current support status.

---

## Questions or Issues?

- See [BRANCHING_STRATEGY.md](./BRANCHING_STRATEGY.md) for detailed branching workflow
- Check [CONTRIBUTING.md](../.github/CONTRIBUTING.md) for contribution guidelines
- Open an issue on GitHub for questions
