# Script to build and publish both V13 and V17 packages to NuGet
# Usage: .\scripts\publish-both-packages.ps1 -Version "2.0.0" [-DryRun]
#
# Examples:
#   .\scripts\publish-both-packages.ps1 -Version "2.0.0"              # Publish v2.0.0
#   .\scripts\publish-both-packages.ps1 -Version "2.0.0" -DryRun      # Test without publishing

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Remove 'v' prefix if present
$Version = $Version.TrimStart('v')

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "📦 Publishing BulkUpload Packages" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Dry Run: $DryRun"
Write-Host ""

# Update version in all csproj files
Write-Host "📝 Updating version numbers..." -ForegroundColor Yellow
(Get-Content "src/BulkUpload/BulkUpload.csproj") -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content "src/BulkUpload/BulkUpload.csproj"
(Get-Content "src/BulkUpload.V17/BulkUpload.V17.csproj") -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content "src/BulkUpload.V17/BulkUpload.V17.csproj"
(Get-Content "src/BulkUpload.Core/BulkUpload.Core.csproj") -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content "src/BulkUpload.Core/BulkUpload.Core.csproj"
Write-Host "✅ Version updated to $Version" -ForegroundColor Green
Write-Host ""

# Restore dependencies
Write-Host "📥 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore src/BulkUpload.sln --disable-parallel
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "✅ Dependencies restored" -ForegroundColor Green
Write-Host ""

# Build V17 frontend
Write-Host "🔨 Building V17 frontend..." -ForegroundColor Yellow
Push-Location "src/BulkUpload.V17/Client"
npm ci --silent
npm run build
Pop-Location
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "✅ Frontend built" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build src/BulkUpload.sln --configuration Release --no-restore -p:SkipPreBuild=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "✅ Build succeeded" -ForegroundColor Green
Write-Host ""

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Yellow
dotnet test src/BulkUpload.sln --configuration Release --no-build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "✅ Tests passed" -ForegroundColor Green
Write-Host ""

# Create output directories
New-Item -ItemType Directory -Force -Path "./artifacts/v13" | Out-Null
New-Item -ItemType Directory -Force -Path "./artifacts/v17" | Out-Null

# Pack V13
Write-Host "📦 Packing V13 package..." -ForegroundColor Yellow
dotnet pack src/BulkUpload/BulkUpload.csproj --configuration Release --no-build --output ./artifacts/v13
$v13Package = Get-ChildItem "./artifacts/v13/*.nupkg" | Select-Object -First 1
$v13Name = $v13Package.Name
$v13Size = "{0:N2} KB" -f ($v13Package.Length / 1KB)
Write-Host "✅ Created: $v13Name ($v13Size)" -ForegroundColor Green
Write-Host ""

# Pack V17
Write-Host "📦 Packing V17 package..." -ForegroundColor Yellow
dotnet pack src/BulkUpload.V17/BulkUpload.V17.csproj --configuration Release --no-build --output ./artifacts/v17
$v17Package = Get-ChildItem "./artifacts/v17/*.nupkg" | Select-Object -First 1
$v17Name = $v17Package.Name
$v17Size = "{0:N2} KB" -f ($v17Package.Length / 1KB)
Write-Host "✅ Created: $v17Name ($v17Size)" -ForegroundColor Green
Write-Host ""

# Publish packages
if (-not $DryRun) {
    Write-Host "🚀 Publishing to NuGet..." -ForegroundColor Yellow
    Write-Host ""

    # Check for NuGet API key
    $apiKey = $env:NUGET_API_KEY
    if (-not $apiKey) {
        Write-Host "⚠️  Warning: NUGET_API_KEY environment variable not set" -ForegroundColor Yellow
        $apiKey = Read-Host -Prompt "Please enter your NuGet API key" -AsSecureString
        $apiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
    }

    # Publish V13
    Write-Host "Publishing V13 package..." -ForegroundColor Yellow
    dotnet nuget push ./artifacts/v13/*.nupkg `
        --api-key $apiKey `
        --source https://api.nuget.org/v3/index.json `
        --skip-duplicate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ V13 published successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  V13 publish had issues (may already exist)" -ForegroundColor Yellow
    }
    Write-Host ""

    # Publish V17
    Write-Host "Publishing V17 package..." -ForegroundColor Yellow
    dotnet nuget push ./artifacts/v17/*.nupkg `
        --api-key $apiKey `
        --source https://api.nuget.org/v3/index.json `
        --skip-duplicate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ V17 published successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  V17 publish had issues (may already exist)" -ForegroundColor Yellow
    }
    Write-Host ""
} else {
    Write-Host "🔍 DRY RUN - Skipping NuGet publish" -ForegroundColor Cyan
    Write-Host "Packages created in ./artifacts/"
    Write-Host ""
}

# Summary
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "✨ Summary" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host ""
Write-Host "V13 Package:"
Write-Host "  - Name: $v13Name"
Write-Host "  - Size: $v13Size"
Write-Host "  - Path: ./artifacts/v13/"
Write-Host ""
Write-Host "V17 Package:"
Write-Host "  - Name: $v17Name"
Write-Host "  - Size: $v17Size"
Write-Host "  - Path: ./artifacts/v17/"
Write-Host ""

if (-not $DryRun) {
    Write-Host "📦 Packages published to NuGet.org" -ForegroundColor Green
    Write-Host ""
    Write-Host "Links:"
    Write-Host "  - https://www.nuget.org/packages/Umbraco.Community.BulkUpload/$Version"
    Write-Host "  - https://www.nuget.org/packages/Umbraco.Community.BulkUpload.V17/$Version"
} else {
    Write-Host "💡 To publish, run without -DryRun flag" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "🎉 Done!" -ForegroundColor Green
