# Quick Reference - Multi-Targeting Workflow

## Branch Overview

| Branch | Purpose | Target Frameworks |
|--------|---------|------------------|
| `main` | Production code, all development | net8.0 (Umbraco 13) + net10.0 (Umbraco 17) |

## ⚠️ Critical Rules

**NEVER commit directly to:**
- ❌ `main`

**ALWAYS:**
- ✅ Create a feature/bugfix branch from `main`
- ✅ Make changes in your branch
- ✅ Create a Pull Request to merge to `main`

## Common Commands

### Feature Development

```bash
# 1. Create feature from main
git checkout main
git pull origin main
git checkout -b feature/my-feature

# 2. Develop, commit, push
git add .
git commit -m "feat: add my feature"
git push origin feature/my-feature

# 3. Create PR to main on GitHub
# After merge, feature is available for both Umbraco 13 and 17
```

### Bug Fix

```bash
# 1. Create fix from main
git checkout main
git pull origin main
git checkout -b bugfix/issue-description

# 2. Fix, commit, push
git add .
git commit -m "fix: resolve issue with CSV parsing"
git push origin bugfix/issue-description

# 3. Create PR to main on GitHub
# After merge, fix is available for both Umbraco 13 and 17
```

### Release New Version

**📖 Full instructions:** See [RELEASE_PROCESS.md](./RELEASE_PROCESS.md)

**Quick summary:**

```bash
# 1. Prepare release on main
git checkout main
git pull origin main

# 2. Update version in .csproj and CHANGELOG.md
# Edit src/BulkUpload/BulkUpload.csproj: <Version>2.2.0</Version>
# Edit CHANGELOG.md: Add changes under version heading

# 3. Commit and push
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v2.2.0"
git push origin main

# 4. Create GitHub Release (GitHub UI)
# - Tag: v2.2.0 (create new)
# - Target: main
# - Title: v2.2.0
# - Publish release

# 5. Automated workflows handle:
# ✅ Building for both net8.0 and net10.0
# ✅ Running tests
# ✅ Publishing to NuGet
```

## Build and Test Commands

### Build for Both Frameworks

```bash
# Build entire solution (both net8.0 and net10.0)
dotnet build

# Build specific framework
dotnet build -f net8.0    # Umbraco 13 only
dotnet build -f net10.0   # Umbraco 17 only

# Build in Release mode
dotnet build -c Release
```

### Run Tests

```bash
# Run all tests (tests both frameworks)
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test project
dotnet test src/BulkUpload.Tests/BulkUpload.Tests.csproj
```

### Create NuGet Package Locally

```bash
# Pack NuGet package (contains both net8.0 and net10.0)
dotnet pack src/BulkUpload/BulkUpload.csproj -c Release

# Output location: src/BulkUpload/bin/Release/Umbraco.Community.BulkUpload.{version}.nupkg
```

## Frontend Development

### Umbraco 13 Frontend (AngularJS)

Files in `ClientV13/BulkUpload/` are automatically copied to `wwwroot/BulkUpload/` during build.

```bash
# No special build steps needed - just edit files in ClientV13/
# Files are copied during dotnet build
```

### Umbraco 17 Frontend (Lit + TypeScript)

```bash
cd src/BulkUpload/ClientV17

# Install dependencies (first time only)
npm install

# Development mode (watch for changes)
npm run dev

# Production build (creates wwwroot/bulkupload.js)
npm run build

# Return to root
cd ../../..
```

## Version Numbering

### Pattern: MAJOR.MINOR.PATCH

| Change Type | Example | Supports |
|-------------|---------|----------|
| New major version | v3.0.0 | Breaking changes |
| New feature | v2.1.0 | Both Umbraco 13 & 17 |
| Bug fix | v2.0.1 | Both Umbraco 13 & 17 |
| Security fix | v2.0.2 | Both Umbraco 13 & 17 |

## Git Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add new CSV validation
fix: resolve parsing error with special characters
docs: update README with new examples
chore: bump version to 2.1.0
refactor: simplify resolver registration
test: add unit tests for DateTimeResolver
perf: optimize CSV processing
```

### Scopes (optional)

```
feat(resolver): add new ImageResolver
fix(csv): handle empty columns correctly
docs(readme): add troubleshooting section
```

## Framework-Specific Code

When you need different code for Umbraco 13 vs 17:

```csharp
#if NET8_0
    // Umbraco 13 specific code
    builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#elif NET10_0
    // Umbraco 17 specific code
    // Use umbraco-package.json for registration
#endif
```

**Best Practice:** Keep framework-specific code minimal. Most logic should be shared.

## Testing Checklist

Before creating a PR:

- [ ] Code builds for both net8.0 and net10.0 (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Manual testing with Umbraco 13 (if applicable)
- [ ] Manual testing with Umbraco 17 (if applicable)
- [ ] No errors in Umbraco log
- [ ] Documentation is up to date
- [ ] CHANGELOG.md is updated
- [ ] No merge conflicts with `main`

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

### Build Issues

```bash
# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Frontend Build Issues (V17)

```bash
cd src/BulkUpload/ClientV17

# Clean node_modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Rebuild
npm run build
```

## Checking Status

### See Recent Commits

```bash
# On current branch
git log --oneline -10

# With diffs
git log -p -2
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

## Release Checklist

- [ ] Code reviewed and approved
- [ ] Tests passing
- [ ] Version bumped in .csproj
- [ ] CHANGELOG.md updated
- [ ] Committed and pushed to main
- [ ] Tagged (e.g., v2.2.0)
- [ ] GitHub Release created
- [ ] NuGet package published (automated)

## Resources

### Documentation
- [Branching Strategy](./BRANCHING_STRATEGY.md) - Development workflow
- [Multi-Targeting Guide](./MULTI_TARGETING_QUICK_START.md) - Architecture details
- [Release Process](./RELEASE_PROCESS.md) - Detailed release guide

### External Resources
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)

### Automated Workflows
- **Release to NuGet:** `.github/workflows/release.yml`
- **Build & Test:** `.github/workflows/build.yml`
