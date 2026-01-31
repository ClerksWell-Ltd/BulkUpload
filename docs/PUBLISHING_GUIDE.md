# Publishing Guide

This guide explains how to publish both the V13 and V17 packages to NuGet.

## Quick Summary

**Yes, we can publish both packages now!**

Both packages are ready:
- ✅ **V13**: `Umbraco.Community.BulkUpload.2.0.0.nupkg` (39 KB)
- ✅ **V17**: `Umbraco.Community.BulkUpload.V17.2.0.0.nupkg` (50 KB)

All builds pass, tests pass, and packages are properly configured.

---

## Publishing Options

### Option 1: Manual Publishing (Recommended for First Release)

#### Windows (PowerShell)

```powershell
# Set your NuGet API key
$env:NUGET_API_KEY = "your-api-key-here"

# Dry run (test without publishing)
.\scripts\publish-both-packages.ps1 -Version "2.0.0" -DryRun

# Actual publish
.\scripts\publish-both-packages.ps1 -Version "2.0.0"
```

#### Linux/Mac (Bash)

```bash
# Set your NuGet API key
export NUGET_API_KEY="your-api-key-here"

# Dry run (test without publishing)
./scripts/publish-both-packages.sh 2.0.0 --dry-run

# Actual publish
./scripts/publish-both-packages.sh 2.0.0
```

**What the script does:**
1. Updates version numbers in all `.csproj` files
2. Restores NuGet dependencies
3. Builds V17 frontend (npm install + build)
4. Builds entire solution (both V13 and V17)
5. Runs all tests
6. Creates both NuGet packages
7. Publishes both to NuGet.org (or skips if `--dry-run`)

---

### Option 2: GitHub Actions (Automated)

We've created a new workflow: `.github/workflows/release-both.yml`

#### Using GitHub Releases (Recommended)

1. **Commit and push all changes to `main`**
   ```bash
   git add .
   git commit -m "chore: prepare v2.0.0 release with V17 support"
   git push origin main
   ```

2. **Create a GitHub Release from `main` branch**
   - Go to: https://github.com/ClerksWell-Ltd/BulkUpload/releases/new
   - Tag: `v2.0.0`
   - Target: `main` branch
   - Release title: `v2.0.0 - Umbraco 17 Support`
   - Description: (see template below)
   - Click "Publish release"

3. **The workflow will automatically:**
   - Build both packages
   - Run tests
   - Publish to NuGet.org

**Release Description Template:**
```markdown
## 🎉 BulkUpload v2.0.0

This major release introduces **Umbraco 17 support** alongside continued Umbraco 13 support!

### ✨ What's New

- **Umbraco 17 Package**: New `Umbraco.Community.BulkUpload.V17` package with Lit-based frontend
- **Multi-targeting**: Core library now supports both net8.0 (U13) and net10.0 (U17)
- **Modern Frontend**: TypeScript + Lit + Vite for V17
- **100% Backward Compatible**: V13 users can continue using existing package

### 📦 Packages

- **Umbraco 13**: `Umbraco.Community.BulkUpload` v2.0.0
- **Umbraco 17**: `Umbraco.Community.BulkUpload.V17` v2.0.0

### 📥 Installation

**Umbraco 13:**
```bash
dotnet add package Umbraco.Community.BulkUpload --version 2.0.0
```

**Umbraco 17:**
```bash
dotnet add package Umbraco.Community.BulkUpload.V17 --version 2.0.0
```

### 🔗 Links

- [V13 on NuGet](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/2.0.0)
- [V17 on NuGet](https://www.nuget.org/packages/Umbraco.Community.BulkUpload.V17/2.0.0)
- [Documentation](https://github.com/ClerksWell-Ltd/BulkUpload)

See [CHANGELOG.md](./CHANGELOG.md) for complete details.
```

#### Using Manual Workflow Dispatch

If you want more control:

1. Go to: https://github.com/ClerksWell-Ltd/BulkUpload/actions/workflows/release-both.yml
2. Click "Run workflow"
3. Enter version (e.g., `2.0.0`)
4. Click "Run workflow"

---

### Option 3: Individual Package Publishing

If you need to publish packages separately:

#### V13 Only

```bash
cd src/BulkUpload
dotnet pack -c Release
dotnet nuget push bin/Release/*.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

#### V17 Only

```bash
# Build frontend first
cd src/BulkUpload.V17/Client
npm ci
npm run build
cd ..

# Pack and publish
dotnet pack -c Release
dotnet nuget push bin/Release/*.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## Pre-Publishing Checklist

Before publishing, ensure:

- [ ] All changes committed and pushed to GitHub
- [ ] Version numbers updated in all `.csproj` files
- [ ] CHANGELOG.md updated with release notes
- [ ] All tests pass: `dotnet test src/BulkUpload.sln`
- [ ] Both packages build successfully
- [ ] V17 frontend builds without errors
- [ ] No untracked or uncommitted files in `src/BulkUpload.V17/App_Plugins/BulkUpload/dist/`

**Quick verification:**
```bash
# Windows
.\scripts\publish-both-packages.ps1 -Version "2.0.0" -DryRun

# Linux/Mac
./scripts/publish-both-packages.sh 2.0.0 --dry-run
```

---

## Troubleshooting

### "Package already exists" Error

This is normal if you're re-publishing the same version. NuGet.org doesn't allow replacing packages. Solutions:

1. **Increment version** (recommended): `2.0.1` instead of `2.0.0`
2. **Use --skip-duplicate flag** (already included in scripts)

### Frontend Build Fails

```bash
cd src/BulkUpload.V17/Client
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Missing NuGet API Key

Get your API key from: https://www.nuget.org/account/apikeys

Then set it:
```bash
# Windows PowerShell
$env:NUGET_API_KEY = "your-key"

# Linux/Mac
export NUGET_API_KEY="your-key"
```

### GitHub Actions Fails

Check:
1. Secrets are set: Settings → Secrets → Actions → `NUGET_API_KEY`
2. Permissions: Settings → Actions → General → Workflow permissions → "Read and write"
3. Branch protection rules don't block the workflow

---

## Post-Publishing

After successful publishing:

1. **Verify packages on NuGet.org:**
   - https://www.nuget.org/packages/Umbraco.Community.BulkUpload/
   - https://www.nuget.org/packages/Umbraco.Community.BulkUpload.V17/

2. **Tag the release:**
   ```bash
   git tag v2.0.0
   git push origin v2.0.0
   ```

3. **Update documentation:**
   - Update README.md with installation instructions
   - Announce on Umbraco forums/Discord
   - Tweet about the release

4. **Monitor:**
   - NuGet download stats
   - GitHub issues for bug reports
   - Community feedback

---

## Version Strategy

We use **semantic versioning** (SemVer):

- **Major** (2.0.0): Breaking changes, new Umbraco version support
- **Minor** (2.1.0): New features, no breaking changes
- **Patch** (2.0.1): Bug fixes only

**Both packages use the same version number** to avoid confusion.

---

## Next Steps After v2.0.0

Consider:

1. Create `release/v13.x` branch for v13-specific bug fixes
2. Create `release/v17.x` branch for v17-specific bug fixes
3. Update branching strategy documentation
4. Set up automated changelog generation
5. Add package download badges to README

---

## Support

If you encounter issues:

- Check existing issues: https://github.com/ClerksWell-Ltd/BulkUpload/issues
- Ask on Umbraco Discord: #package-development
- Contact: [your-contact-info]

---

## Quick Reference

| Command | Purpose |
|---------|---------|
| `.\scripts\publish-both-packages.ps1 -Version "2.0.0" -DryRun` | Test build (Windows) |
| `.\scripts\publish-both-packages.ps1 -Version "2.0.0"` | Publish both (Windows) |
| `./scripts/publish-both-packages.sh 2.0.0 --dry-run` | Test build (Linux/Mac) |
| `./scripts/publish-both-packages.sh 2.0.0` | Publish both (Linux/Mac) |
| GitHub Release from `main` | Automated publish via Actions |
| Manual workflow dispatch | Publish specific version via Actions |
