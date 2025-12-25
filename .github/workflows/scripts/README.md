# NuGet Package Update Automation

This directory contains scripts used by the GitHub Actions workflow to automatically update NuGet packages.

## Scripts

### Update-NuGetPackages.ps1

PowerShell script that:
- Scans all `.csproj` files in the repository
- Queries NuGet.org API for the latest version of each package
- Updates package versions in the `.csproj` files
- Generates a summary of changes

**Parameters:**
- `-RootPath`: Repository root path (defaults to current directory)
- `-IncludePrerelease`: Include prerelease versions when checking for updates
- `-DryRun`: Display what would be updated without making changes

**Usage:**
```powershell
# Dry run to see what would be updated
./Update-NuGetPackages.ps1 -DryRun

# Update to latest stable versions
./Update-NuGetPackages.ps1

# Update including prerelease versions
./Update-NuGetPackages.ps1 -IncludePrerelease
```

## Workflow

The `update-nuget-packages.yml` workflow:
1. Runs daily at 9:00 AM UTC (can also be triggered manually)
2. Executes the update script
3. Runs `dotnet build` to ensure the project still compiles
4. Runs `dotnet test` to ensure all tests pass
5. Creates a Pull Request with the updates if successful

## Manual Trigger

You can manually trigger the workflow from the Actions tab with options:
- **Include Prerelease**: Choose whether to include prerelease versions
- **Dry Run**: Test the workflow without creating a PR
