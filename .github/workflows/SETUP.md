# GitHub Actions Workflow Setup

## Update NuGet Packages Workflow

This guide helps you configure the automated NuGet package update workflow.

## Required Repository Settings

### Enable PR Creation by GitHub Actions

The workflow needs permission to create pull requests. Follow these steps:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Actions** → **General**
3. Scroll down to **Workflow permissions**
4. Select **"Read and write permissions"**
5. **Enable** the checkbox: **"Allow GitHub Actions to create and approve pull requests"**
6. Click **Save**

### Visual Guide

```
Repository Settings
  └── Actions
      └── General
          └── Workflow permissions
              ├── ✓ Read and write permissions
              └── ✓ Allow GitHub Actions to create and approve pull requests
```

## Error: "GitHub Actions is not permitted to create or approve pull requests"

If you see this error, it means the repository setting above is not enabled.

**Quick Fix:**
1. Enable the setting as described above
2. Re-run the failed workflow from the Actions tab

## Alternative: Using a Personal Access Token (PAT)

If you prefer not to use the default `GITHUB_TOKEN`, you can use a Personal Access Token:

### 1. Create a PAT

1. Go to GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token (classic)**
3. Give it a name: `NuGet Update Workflow`
4. Select scopes:
   - ✓ `repo` (Full control of private repositories)
   - ✓ `workflow` (Update GitHub Action workflows)
5. Click **Generate token**
6. **Copy the token** (you won't see it again!)

### 2. Add PAT to Repository Secrets

1. Go to your repository → **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Name: `PAT_TOKEN`
4. Value: Paste the token you copied
5. Click **Add secret**

### 3. Update the Workflow

Edit `.github/workflows/update-nuget-packages.yml`:

Change:
```yaml
token: ${{ secrets.GITHUB_TOKEN }}
```

To:
```yaml
token: ${{ secrets.PAT_TOKEN }}
```

## Testing the Workflow

### Manual Test Run

1. Go to **Actions** tab
2. Select **Update NuGet Packages** workflow
3. Click **Run workflow**
4. Select options:
   - **Include Prerelease**: `false` (for stable versions only)
   - **Dry Run**: `true` (to test without creating a PR)
5. Click **Run workflow**

### Check the Results

1. Wait for the workflow to complete
2. Check the workflow logs to see:
   - Which packages were found
   - What versions are available
   - Whether build and tests passed

### First Real Run

Once you've tested with dry run:

1. Run workflow again with:
   - **Include Prerelease**: `false`
   - **Dry Run**: `false`
2. The workflow will create a PR if updates are found
3. Review the PR and merge if everything looks good

## Troubleshooting

### No PR Created

**Possible reasons:**
- No package updates available (all packages are already up to date)
- Build or tests failed (check workflow logs)
- Dry run mode is enabled

### Build Failures

If the build fails after updating packages:
- Check the workflow logs for error details
- The PR will show what packages were updated
- You may need to fix compatibility issues manually

### Permission Errors

**Error:** "Resource not accessible by integration"
- Solution: Enable "Read and write permissions" in workflow settings

**Error:** "GitHub Actions is not permitted to create or approve pull requests"
- Solution: Enable the checkbox as described in Required Repository Settings

## How It Works

1. **Scheduled Run**: Runs daily at 9:00 AM UTC
2. **Package Discovery**: Finds all `.csproj` files
3. **Version Check**: For each package:
   - Extracts current major version (e.g., `13` from `13.5.0`)
   - Queries NuGet for latest version in same major (e.g., latest `13.x.x`)
   - Skips if already up to date
4. **Update**: Updates package versions in `.csproj` files
5. **Validation**: Runs `dotnet restore`, `dotnet build`, and `dotnet test`
6. **PR Creation**: If all checks pass, creates a PR with changes

## Customization

### Change Schedule

Edit the cron expression in the workflow file:

```yaml
schedule:
  - cron: '0 9 * * *'  # Daily at 9:00 AM UTC
```

Common schedules:
- `'0 9 * * *'` - Daily at 9:00 AM UTC
- `'0 9 * * 1'` - Weekly on Mondays at 9:00 AM UTC
- `'0 9 1 * *'` - Monthly on the 1st at 9:00 AM UTC

### Include Prerelease Versions

Edit the environment variable:

```yaml
env:
  SCHEDULED_INCLUDE_PRERELEASE: 'true'  # Change to 'true' for prerelease
```

## Support

For issues with the workflow:
1. Check the workflow logs in the Actions tab
2. Review this setup guide
3. Check the main README in `.github/workflows/scripts/README.md`
