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

function Write-AsciiTable {
    <#
    .SYNOPSIS
        Renders a simple ASCII table with borders, similar to benchmarking tools.
    .PARAMETER Rows
        Enumerable of objects that share the same property names as the headers.
    .PARAMETER Headers
        Ordered list of headers (strings) that map to property names in each row.
    .PARAMETER AlignRight
        Property names to right-align (e.g., versions).
    .PARAMETER OutputFile
        Optional file path to write the table to (in addition to console output).
    #>
    param(
        [Parameter(Mandatory)]
        [System.Collections.IEnumerable]$Rows,
        [Parameter(Mandatory)]
        [string[]]$Headers,
        [string[]]$AlignRight = @(),
        [string]$OutputFile
    )

    $rowsArray = @($Rows)
    $outputLines = @()

    if ($rowsArray.Count -eq 0) {
        $msg = "(no rows)"
        Write-Host $msg -ForegroundColor DarkGray
        if ($OutputFile) {
            $msg | Out-File -FilePath $OutputFile -Encoding UTF8
        }
        return
    }

    $columns = foreach ($h in $Headers) {
        [pscustomobject]@{
            Name  = $h
            Width = [math]::Max($h.Length, 1)
            Align = $(if ($AlignRight -contains $h) { 'Right' } else { 'Left' })
        }
    }

    foreach ($row in $rowsArray) {
        foreach ($col in $columns) {
            $val = $row | Select-Object -ExpandProperty $col.Name
            if ($val.Length -gt $col.Width) { $col.Width = $val.Length }
        }
    }

    function Border($columns) {
        $parts = @('+')
        foreach ($c in $columns) {
            $parts += ('{0}' -f ('-' * ($c.Width + 2)))
            $parts += '+'
        }
        return ($parts -join '')
    }

    function Cell($text, $width, $align) {
        $text = [string]$text
        $w = [int]$width
        if ($align -eq 'Right') {
            return ((' {0,'  + $w + '} ') -f $text)
        } else {
            return ((' {0,-' + $w + '} ') -f $text)
        }
    }

    $top = Border $columns

    # Build header row with pipes between each column
    $headerParts = @('|')
    foreach ($c in $columns) {
        $headerParts += (Cell $c.Name $c.Width 'Left')
        $headerParts += '|'
    }
    $header = ($headerParts -join '')

    $sep = Border $columns

    $outputLines += $top
    $outputLines += $header
    $outputLines += $sep

    $lastFile = ''
    foreach ($row in $rowsArray) {
        $file = $row.'File'
        if ($lastFile -ne '' -and $file -ne $lastFile) {
            $outputLines += $sep
        }
        $lastFile = $file

        $lineParts = @('|')
        foreach ($c in $columns) {
            $val = $row | Select-Object -ExpandProperty $c.Name
            $lineParts += (Cell $val $c.Width $c.Align)
            $lineParts += '|'
        }
        $outputLines += ($lineParts -join '')
    }

    $outputLines += $top

    # Write to console
    foreach ($line in $outputLines) {
        Write-Host $line
    }

    # Write to file if specified
    if ($OutputFile) {
        $outputLines | Out-File -FilePath $OutputFile -Encoding UTF8
    }

    # Return the lines for further use
    return $outputLines
}

function Get-LatestNuGetVersion {
    param(
        [string]$PackageId,
        [string]$CurrentVersion,
        [hashtable]$Cache,
        [switch]$IncludePrerelease
    )

    # Extract major version from current version
    $currentMajor = $null
    if ($CurrentVersion -match '^(\d+)\.') {
        $currentMajor = [int]$matches[1]
    }

    $cacheKey = "$($PackageId.ToLower())_v$currentMajor"
    if ($Cache.ContainsKey($cacheKey)) {
        return $Cache[$cacheKey]
    }

    try {
        Write-Log "Querying NuGet for package: $PackageId (current: $CurrentVersion, target major: $currentMajor)" -Color Cyan

        # Query NuGet.org API
        $key = $PackageId.ToLower()
        $url = "https://api.nuget.org/v3-flatcontainer/$key/index.json"
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 10 -ErrorAction Stop

        $versions = @($response.versions | ForEach-Object { [string]$_ })

        if ($versions.Count -eq 0) {
            Write-Log "No versions found for $PackageId" -Level WARN -Color Yellow
            $Cache[$cacheKey] = $null
            return $null
        }

        # Filter out nightly builds
        $versions = @($versions | Where-Object { $_ -notmatch '-build' })

        # Filter to same major version if we could extract it
        if ($null -ne $currentMajor) {
            $versions = @($versions | Where-Object {
                if ($_ -match '^(\d+)\.') {
                    [int]$matches[1] -eq $currentMajor
                } else {
                    $false
                }
            })

            if ($versions.Count -eq 0) {
                Write-Log "No versions found in major version $currentMajor for $PackageId" -Level WARN -Color Yellow
                $Cache[$cacheKey] = $null
                return $null
            }

            Write-Log "Found $($versions.Count) version(s) in major version $currentMajor" -Color Cyan
        }

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

        Write-Log "Latest version for ${PackageId} (major $currentMajor): $latest" -Color Green
        $Cache[$cacheKey] = $latest
        return $latest
    }
    catch {
        Write-Log "Failed to query NuGet for ${PackageId}: $($_.Exception.Message)" -Level ERROR -Color Red
        $Cache[$cacheKey] = $null
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

        $latestVersion = Get-LatestNuGetVersion -PackageId $packageId -CurrentVersion $currentVersion -Cache $VersionCache -IncludePrerelease:$IncludePrerelease

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

    # Write to GitHub Actions summary if available
    if ($env:GITHUB_STEP_SUMMARY) {
        "## 📦 NuGet Package Update Summary`n" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
        "✅ All packages are already up to date" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
    }
}
else {
    Write-Log "Updated $($allChanges.Count) package(s):" -Color Yellow

    # Use ASCII table for better formatting
    $tableLines = Write-AsciiTable `
        -Rows $allChanges `
        -Headers @('File', 'Package', 'OldVersion', 'NewVersion') `
        -AlignRight @('OldVersion', 'NewVersion')

    # Save summary to file for GitHub Actions
    $summaryPath = Join-Path $RootPath "package-update-summary.txt"
    $tableLines | Out-File -FilePath $summaryPath -Encoding UTF8
    Write-Log "Summary saved to: $summaryPath" -Color Cyan

    # Write to GitHub Actions summary if available
    if ($env:GITHUB_STEP_SUMMARY) {
        "## 📦 NuGet Package Update Summary`n" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
        "Updated **$($allChanges.Count)** package(s):`n" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
        "``````" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
        $tableLines | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
        "``````" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 -Append
    }
}

Write-Log "Package update complete" -Color Green
