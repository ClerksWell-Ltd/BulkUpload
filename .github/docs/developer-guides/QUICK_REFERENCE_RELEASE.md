# Quick Reference: Release & Branch Operations

Quick command reference for common release and branching operations.

## Creating a Release

### 1. Prepare Release Branch
```bash
# Checkout release branch
git checkout release/v13.x  # or release/v17.x
git pull origin release/v13.x

# Update version in .csproj (manually edit)
# Update CHANGELOG.md (manually edit)

# Commit changes
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v1.2.0"
git push origin release/v13.x
```

### 2. Create GitHub Release
1. Go to: https://github.com/ClerksWell-Ltd/BulkUpload/releases/new
2. **Tag:** `v1.2.0` (create new tag)
3. **Target:** `release/v13.x`
4. **Title:** `v1.2.0 - Umbraco 13`
5. **Description:** Copy from CHANGELOG.md
6. Click **"Publish release"**

### 3. Automated (No Action Needed)
- ✅ Builds and tests
- ✅ Publishes to NuGet
- ✅ Creates post-release PR

### 4. Merge Post-Release PR
```bash
# Review the automated PR, then merge via GitHub UI
```

## Cherry-Picking Changes

### Bug Fix: v13 → v17 + main
```bash
# 1. Find commit hash
git log release/v13.x --oneline -5
# Copy commit hash (e.g., abc1234)

# 2. Cherry-pick to main
git checkout main
git pull origin main
git cherry-pick abc1234
dotnet test
git push origin main

# 3. Cherry-pick to v17
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick abc1234
dotnet test
git push origin release/v17.x
```

### Feature: main → v13 + v17
```bash
# 1. Find commit hash from main
git log main --oneline -10
# Copy commit hash (e.g., def5678)

# 2. Cherry-pick to v13
git checkout release/v13.x
git pull origin release/v13.x
git cherry-pick def5678
dotnet test
git push origin release/v13.x

# 3. Cherry-pick to v17
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick def5678
dotnet test
git push origin release/v17.x
```

### Resolve Cherry-Pick Conflicts
```bash
# If cherry-pick has conflicts:
git cherry-pick abc1234
# Fix conflicts in editor
git add <resolved-files>
git cherry-pick --continue
dotnet test
git push

# Or abort if needed:
git cherry-pick --abort
```

## Creating New Release Branch

### For New Umbraco Version (e.g., v17)
```bash
# 1. Create from main
git checkout main
git pull origin main
git checkout -b release/v17.x

# 2. Update .csproj (manually):
#    - Umbraco.Cms.Web.Website → 17.x.x
#    - Umbraco.Cms.Web.BackOffice → 17.x.x
#    - <Version>2.0.0</Version>

# 3. Update CHANGELOG.md (add v2.0.0 section)

# 4. Test
dotnet restore
dotnet build --configuration Release
dotnet test

# 5. Push branch
git add .
git commit -m "feat: add Umbraco 17 support (v2.0.0)"
git push -u origin release/v17.x

# 6. Create initial release (follow "Creating a Release" above)
```

## Common Workflows

### Feature Development
```bash
# Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/new-feature

# Make changes, commit
git add .
git commit -m "feat: add new feature"
git push origin feature/new-feature

# Create PR to main on GitHub
```

### Bug Fix for Specific Version
```bash
# Create bugfix branch from release branch
git checkout release/v13.x
git pull origin release/v13.x
git checkout -b bugfix/fix-csv-parsing

# Make changes, commit
git add .
git commit -m "fix: handle empty CSV columns"
git push origin bugfix/fix-csv-parsing

# Create PR to release/v13.x on GitHub
```

## Checking Status

### See Recent Commits
```bash
# On current branch
git log --oneline -10

# On specific branch
git log release/v13.x --oneline -10

# With diffs
git log -p -2
```

### Check Differences Between Branches
```bash
# See commits in v13 not in main
git log main..release/v13.x --oneline

# See file changes
git diff main..release/v13.x

# See commits since branch diverged
git log main...release/v13.x --oneline
```

### See What's in Current Release
```bash
# View current version
cat src/BulkUpload/BulkUpload.csproj | grep "<Version>"

# View latest tag
git describe --tags --abbrev=0

# View all tags
git tag -l
```

## Testing

### Run All Tests
```bash
dotnet test
```

### Build Release Package Locally
```bash
# Build in release mode
dotnet build src/BulkUpload/BulkUpload.csproj --configuration Release

# Pack NuGet package
dotnet pack src/BulkUpload/BulkUpload.csproj --configuration Release --output ./artifacts

# Package will be in: ./artifacts/Umbraco.Community.BulkUpload.{version}.nupkg
```

### Test Package Locally
```bash
# Install from local file
dotnet add package Umbraco.Community.BulkUpload --source ./artifacts
```

## Troubleshooting

### Undo Last Commit (Not Pushed)
```bash
git reset --soft HEAD~1  # Keep changes staged
# or
git reset --hard HEAD~1  # Discard changes
```

### Undo Last Commit (Already Pushed)
```bash
git revert HEAD
git push
```

### See What Changed in a Commit
```bash
git show abc1234
```

### Find Which Branch Contains a Commit
```bash
git branch -a --contains abc1234
```

## Version Mapping

| BulkUpload | Umbraco | Branch | Status |
|-----------|---------|--------|--------|
| 1.x.x | 13.x | `release/v13.x` | Active |
| 2.x.x | 17.x | `release/v17.x` | Planned |

## Useful Links

- **Full Release Process:** [RELEASE_PROCESS.md](./RELEASE_PROCESS.md)
- **Branching Strategy:** [BRANCHING_STRATEGY.md](./BRANCHING_STRATEGY.md)
- **Workflow Diagrams:** [WORKFLOW_DIAGRAM.md](./WORKFLOW_DIAGRAM.md)
- **NuGet Package:** https://www.nuget.org/packages/Umbraco.Community.BulkUpload
- **GitHub Releases:** https://github.com/ClerksWell-Ltd/BulkUpload/releases
- **GitHub Actions:** https://github.com/ClerksWell-Ltd/BulkUpload/actions

## GitHub Secrets Required

| Secret | Purpose | Where to Get |
|--------|---------|--------------|
| `NUGET_API_KEY` | Publish to NuGet.org | https://www.nuget.org/account/apikeys |

Configure at: https://github.com/ClerksWell-Ltd/BulkUpload/settings/secrets/actions
