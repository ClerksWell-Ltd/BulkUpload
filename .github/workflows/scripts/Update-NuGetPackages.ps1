<#
.SYNOPSIS
    Update NuGet packages in all .csproj files to their latest versions.

.PARAMETER RootPath
    Repository root path. Defaults to current directory.

.PARAMETER IncludePrerelease
    If set, includes prerelease versions when checking for updates.

.PARAMETER DryRun
    If set, displays what would be updated without making changes.
#>
param(
    [string]$RootPath = (Get-Location).Path,
    [switch]$IncludePrerelease,
    [switch]$DryRun
)

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'INFO',
        [string]$Color = 'White'
    )
    $timestamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $Color
}

function Get-LatestNuGetVersion {
    param(
        [string]$PackageId,
        [hashtable]$Cache,
        [switch]$IncludePrerelease
    )

    $key = $PackageId.ToLower()
    if ($Cache.ContainsKey($key)) {
        return $Cache[$key]
    }

    try {
        Write-Log "Querying NuGet for package: $PackageId" -Color Cyan

        # Query NuGet.org API
        $url = "https://api.nuget.org/v3-flatcontainer/$key/index.json"
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 10 -ErrorAction Stop

        $versions = @($response.versions | ForEach-Object { [string]$_ })

        if ($versions.Count -eq 0) {
            Write-Log "No versions found for $PackageId" -Level WARN -Color Yellow
            $Cache[$key] = $null
            return $null
        }

        # Filter out nightly builds
        $versions = @($versions | Where-Object { $_ -notmatch '-build' })

        if (-not $IncludePrerelease) {
            # Get only stable versions
            $stableVersions = @($versions | Where-Object { $_ -notmatch '-' })
            if ($stableVersions.Count -gt 0) {
                $versions = $stableVersions
            }
        }

        # Sort versions and get the latest
        $sortedVersions = $versions | Sort-Object { [Version]($_ -replace '-.*$', '') } -Descending
        $latest = $sortedVersions[0]

        Write-Log "Latest version for ${PackageId}: $latest" -Color Green
        $Cache[$key] = $latest
        return $latest
    }
    catch {
        Write-Log "Failed to query NuGet for ${PackageId}: $($_.Exception.Message)" -Level ERROR -Color Red
        $Cache[$key] = $null
        return $null
    }
}

function Update-CsprojPackages {
    param(
        [string]$CsprojPath,
        [hashtable]$VersionCache,
        [switch]$IncludePrerelease,
        [switch]$DryRun
    )

    $result = @{
        Path = $CsprojPath
        Updated = $false
        Changes = @()
    }

    try {
        [xml]$xml = Get-Content -Path $CsprojPath -Raw
    }
    catch {
        Write-Log "Failed to read $CsprojPath : $($_.Exception.Message)" -Level ERROR -Color Red
        return $result
    }

    $packageReferences = $xml.SelectNodes("//PackageReference[@Version]")

    foreach ($package in $packageReferences) {
        $packageId = $package.Include
        $currentVersion = $package.Version

        if (-not $packageId -or -not $currentVersion) {
            continue
        }

        # Skip private assets that don't need updating
        if ($package.PrivateAssets -eq 'all' -or $package.PrivateAssets -eq 'All') {
            Write-Log "Skipping $packageId (PrivateAssets=all)" -Color Gray
            continue
        }

        $latestVersion = Get-LatestNuGetVersion -PackageId $packageId -Cache $VersionCache -IncludePrerelease:$IncludePrerelease

        if (-not $latestVersion) {
            continue
        }

        if ($currentVersion -eq $latestVersion) {
            Write-Log "$packageId is already at latest version: $latestVersion" -Color Gray
            continue
        }

        Write-Log "Updating $packageId : $currentVersion -> $latestVersion" -Color Yellow

        if (-not $DryRun) {
            $package.SetAttribute("Version", $latestVersion)
        }

        $result.Updated = $true
        $result.Changes += @{
            Package = $packageId
            OldVersion = $currentVersion
            NewVersion = $latestVersion
        }
    }

    if ($result.Updated -and -not $DryRun) {
        try {
            $xml.Save($CsprojPath)
            Write-Log "Saved updates to $CsprojPath" -Color Green
        }
        catch {
            Write-Log "Failed to save $CsprojPath : $($_.Exception.Message)" -Level ERROR -Color Red
        }
    }

    return $result
}

# Main execution
Write-Log "Starting NuGet package update..." -Color Cyan
Write-Log "Root Path: $RootPath" -Color Cyan
Write-Log "Include Prerelease: $IncludePrerelease" -Color Cyan
Write-Log "Dry Run: $DryRun" -Color Cyan

if (-not (Test-Path $RootPath)) {
    Write-Log "Root path not found: $RootPath" -Level ERROR -Color Red
    exit 1
}

# Find all .csproj files
Write-Log "Scanning for .csproj files..." -Color Cyan
$csprojFiles = Get-ChildItem -Path $RootPath -Filter *.csproj -Recurse -File

if ($csprojFiles.Count -eq 0) {
    Write-Log "No .csproj files found in $RootPath" -Level ERROR -Color Red
    exit 1
}

Write-Log "Found $($csprojFiles.Count) .csproj file(s)" -Color Cyan

$versionCache = @{}
$allChanges = @()

foreach ($csproj in $csprojFiles) {
    Write-Log "`nProcessing: $($csproj.Name)" -Color Cyan
    $updateResult = Update-CsprojPackages `
        -CsprojPath $csproj.FullName `
        -VersionCache $versionCache `
        -IncludePrerelease:$IncludePrerelease `
        -DryRun:$DryRun

    if ($updateResult.Updated) {
        foreach ($change in $updateResult.Changes) {
            $allChanges += [PSCustomObject]@{
                File = $csproj.Name
                Package = $change.Package
                OldVersion = $change.OldVersion
                NewVersion = $change.NewVersion
            }
        }
    }
}

# Display summary
Write-Log "`n========== UPDATE SUMMARY ==========" -Color Cyan
if ($allChanges.Count -eq 0) {
    Write-Log "No packages needed updating" -Color Green
}
else {
    Write-Log "Updated $($allChanges.Count) package(s):" -Color Yellow
    $allChanges | Format-Table -AutoSize | Out-String | Write-Host

    # Save summary to file for GitHub Actions
    $summaryPath = Join-Path $RootPath "package-update-summary.txt"
    $allChanges | Format-Table -AutoSize | Out-File -FilePath $summaryPath -Encoding UTF8
    Write-Log "Summary saved to: $summaryPath" -Color Cyan
}

Write-Log "Package update complete" -Color Green
