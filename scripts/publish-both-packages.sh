#!/bin/bash
# Script to build and publish both V13 and V17 packages to NuGet
# Usage: ./scripts/publish-both-packages.sh [version] [--dry-run]
#
# Examples:
#   ./scripts/publish-both-packages.sh 2.0.0              # Publish v2.0.0
#   ./scripts/publish-both-packages.sh 2.0.0 --dry-run    # Test without publishing

set -e  # Exit on error

VERSION=${1:-}
DRY_RUN=false

if [[ "$2" == "--dry-run" ]]; then
  DRY_RUN=true
fi

if [ -z "$VERSION" ]; then
  echo "❌ Error: Version is required"
  echo "Usage: $0 <version> [--dry-run]"
  echo "Example: $0 2.0.0"
  exit 1
fi

# Remove 'v' prefix if present
VERSION="${VERSION#v}"

echo "========================================="
echo "📦 Publishing BulkUpload Packages"
echo "========================================="
echo "Version: $VERSION"
echo "Dry Run: $DRY_RUN"
echo ""

# Update version in all csproj files
echo "📝 Updating version numbers..."
sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" src/BulkUpload/BulkUpload.csproj
sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" src/BulkUpload.V17/BulkUpload.V17.csproj
sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" src/BulkUpload.Core/BulkUpload.Core.csproj
echo "✅ Version updated to $VERSION"
echo ""

# Restore dependencies
echo "📥 Restoring dependencies..."
dotnet restore src/BulkUpload.sln --disable-parallel
echo "✅ Dependencies restored"
echo ""

# Build V17 frontend
echo "🔨 Building V17 frontend..."
cd src/BulkUpload.V17/Client
npm ci --silent
npm run build
cd ../../..
echo "✅ Frontend built"
echo ""

# Build solution
echo "🔨 Building solution..."
dotnet build src/BulkUpload.sln --configuration Release --no-restore -p:SkipPreBuild=true
if [ $? -ne 0 ]; then
  echo "❌ Build failed"
  exit 1
fi
echo "✅ Build succeeded"
echo ""

# Run tests
echo "🧪 Running tests..."
dotnet test src/BulkUpload.sln --configuration Release --no-build --verbosity quiet
if [ $? -ne 0 ]; then
  echo "❌ Tests failed"
  exit 1
fi
echo "✅ Tests passed"
echo ""

# Create output directory
mkdir -p ./artifacts/v13
mkdir -p ./artifacts/v17

# Pack V13
echo "📦 Packing V13 package..."
dotnet pack src/BulkUpload/BulkUpload.csproj --configuration Release --no-build --output ./artifacts/v13
v13_package=$(ls ./artifacts/v13/*.nupkg | head -1)
v13_name=$(basename "$v13_package")
v13_size=$(du -h "$v13_package" | cut -f1)
echo "✅ Created: $v13_name ($v13_size)"
echo ""

# Pack V17
echo "📦 Packing V17 package..."
dotnet pack src/BulkUpload.V17/BulkUpload.V17.csproj --configuration Release --no-build --output ./artifacts/v17
v17_package=$(ls ./artifacts/v17/*.nupkg | head -1)
v17_name=$(basename "$v17_package")
v17_size=$(du -h "$v17_package" | cut -f1)
echo "✅ Created: $v17_name ($v17_size)"
echo ""

# Publish packages
if [ "$DRY_RUN" = false ]; then
  echo "🚀 Publishing to NuGet..."
  echo ""

  # Check for NuGet API key
  if [ -z "$NUGET_API_KEY" ]; then
    echo "⚠️  Warning: NUGET_API_KEY environment variable not set"
    echo "Please enter your NuGet API key:"
    read -s NUGET_API_KEY
    echo ""
  fi

  # Publish V13
  echo "Publishing V13 package..."
  dotnet nuget push ./artifacts/v13/*.nupkg \
    --api-key "$NUGET_API_KEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

  if [ $? -eq 0 ]; then
    echo "✅ V13 published successfully"
  else
    echo "⚠️  V13 publish had issues (may already exist)"
  fi
  echo ""

  # Publish V17
  echo "Publishing V17 package..."
  dotnet nuget push ./artifacts/v17/*.nupkg \
    --api-key "$NUGET_API_KEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

  if [ $? -eq 0 ]; then
    echo "✅ V17 published successfully"
  else
    echo "⚠️  V17 publish had issues (may already exist)"
  fi
  echo ""
else
  echo "🔍 DRY RUN - Skipping NuGet publish"
  echo "Packages created in ./artifacts/"
  echo ""
fi

# Summary
echo "========================================="
echo "✨ Summary"
echo "========================================="
echo "Version: $VERSION"
echo ""
echo "V13 Package:"
echo "  - Name: $v13_name"
echo "  - Size: $v13_size"
echo "  - Path: ./artifacts/v13/"
echo ""
echo "V17 Package:"
echo "  - Name: $v17_name"
echo "  - Size: $v17_size"
echo "  - Path: ./artifacts/v17/"
echo ""

if [ "$DRY_RUN" = false ]; then
  echo "📦 Packages published to NuGet.org"
  echo ""
  echo "Links:"
  echo "  - https://www.nuget.org/packages/Umbraco.Community.BulkUpload/$VERSION"
  echo "  - https://www.nuget.org/packages/Umbraco.Community.BulkUpload.V17/$VERSION"
else
  echo "💡 To publish, run without --dry-run flag"
fi
echo ""
echo "🎉 Done!"
