# Contributing to BulkUpload

Thank you for your interest in contributing to BulkUpload! We welcome contributions from the community.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Architecture Overview](#architecture-overview)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Coding Standards](#coding-standards)
- [Submitting Changes](#submitting-changes)
- [Release Process](#release-process)

## Code of Conduct

Please be respectful and constructive in all interactions. We're here to build something great together.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Node.js 18+ and npm (for Umbraco 17 frontend development)
- Git
- An IDE (Visual Studio 2022, VS Code, or Rider)
- Basic knowledge of Umbraco CMS

### Repository Structure

```
BulkUpload/
├── src/
│   ├── BulkUpload/                    # Main package (multi-targeted: net8.0, net10.0)
│   │   ├── Controllers/               # API controllers
│   │   ├── Services/                  # Business logic services
│   │   ├── Resolvers/                 # CSV value resolvers
│   │   ├── Models/                    # Data models
│   │   ├── ClientV13/                 # Umbraco 13 frontend (AngularJS)
│   │   ├── ClientV17/                 # Umbraco 17 frontend (Lit + TypeScript)
│   │   └── wwwroot/                   # Static web assets
│   ├── BulkUpload.Tests/              # Unit tests
│   ├── BulkUpload.TestSite13/         # Umbraco 13 test site
│   └── BulkUpload.TestSite17/         # Umbraco 17 test site
├── docs/                              # Documentation
├── samples/                           # Sample CSV files
└── .github/                           # GitHub configuration and docs
```

## Development Setup

### 1. Clone the Repository

```bash
git clone https://github.com/ClerksWell-Ltd/BulkUpload.git
cd BulkUpload
```

### 2. Restore Dependencies

```bash
# Restore .NET packages
dotnet restore

# For Umbraco 17 frontend development
cd src/BulkUpload/ClientV17
npm install
cd ../../..
```

### 3. Build the Solution

```bash
# Build for both Umbraco 13 and 17
dotnet build

# Build for specific framework
dotnet build -f net8.0    # Umbraco 13
dotnet build -f net10.0   # Umbraco 17
```

### 4. Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### 5. Test with Local Sites

#### Option A: Use Existing Test Sites

The repository includes pre-configured test sites:

**Umbraco 13 Test Site:**
```bash
cd src/BulkUpload.TestSite13
dotnet run
```

**Umbraco 17 Test Site:**
```bash
cd src/BulkUpload.TestSite17
dotnet run
```

**Default Credentials:**
- URL: `https://localhost:44362/umbraco` (check console for actual port)
- Email: `admin@example.com`
- Password: `1234567890`

**Note:** The test site databases are included in the repository (SQLite). If you encounter issues, delete the `umbraco.db` file and restart to trigger a fresh install.

#### Option B: Create a New Test Site

```bash
# Install Umbraco templates
dotnet new install Umbraco.Templates::13.10.0 --force

# Create a new site
dotnet new umbraco -n "MyTestSite" \
  --friendly-name "Administrator" \
  --email "admin@example.com" \
  --password "1234567890" \
  --development-database-type SQLite

cd MyTestSite

# Reference the local BulkUpload project
dotnet add reference ../BulkUpload/src/BulkUpload/BulkUpload.csproj

# Run the site
dotnet run
```

Then:
1. Navigate to the backoffice
2. Go to **Users** → **Administrator Group**
3. Add **Bulk Upload** section permission
4. Refresh the page to see the Bulk Upload section

## Architecture Overview

BulkUpload v2.0.0+ uses **multi-targeting** to support both Umbraco 13 and 17 from a single codebase.

### Multi-Targeting

- **Single Package:** One NuGet package contains both Umbraco 13 and 17 support
- **Target Frameworks:** `net8.0` (Umbraco 13) and `net10.0` (Umbraco 17)
- **Automatic Selection:** NuGet automatically installs the correct version based on the consuming project's target framework
- **Shared Code:** Most business logic is shared between both versions
- **Framework-Specific Code:** Conditional compilation (`#if NET8_0`, `#if NET10.0`) for version-specific features

### Key Components

1. **Controllers** - API endpoints for content and media import
2. **Services** - Core business logic (MediaImportService, HierarchyResolver, etc.)
3. **Resolvers** - Extensible system for transforming CSV values to Umbraco property values
4. **Models** - Data transfer objects and view models
5. **Frontend (V13)** - AngularJS dashboard in `ClientV13/`
6. **Frontend (V17)** - Lit web components with Vite bundling in `ClientV17/`

For detailed architecture documentation, see [Multi-Targeting Architecture](../docs/MULTI_TARGETING.md).

## Development Workflow

### Creating a Feature

```bash
# 1. Create a feature branch from main
git checkout main
git pull origin main
git checkout -b feature/my-feature

# 2. Make your changes
# - Edit code in src/BulkUpload/
# - Add tests in src/BulkUpload.Tests/
# - Update documentation if needed

# 3. Test your changes
dotnet build
dotnet test

# Test with Umbraco 13
cd src/BulkUpload.TestSite13
dotnet run

# Test with Umbraco 17 (in a new terminal)
cd src/BulkUpload.TestSite17
dotnet run

# 4. Commit your changes
git add .
git commit -m "feat: add my feature"

# 5. Push and create a pull request
git push origin feature/my-feature
```

### Framework-Specific Code

When you need different code for Umbraco 13 vs 17, use conditional compilation:

```csharp
#if NET8_0
// Umbraco 13 specific code
using Umbraco.Cms.Core.Sections;
#elif NET10.0
// Umbraco 17 specific code
using Umbraco.Cms.Core.Features;
#endif

public class MyService
{
    public void DoSomething()
    {
#if NET8_0
        // Umbraco 13 implementation
        var result = LegacyMethod();
#else
        // Umbraco 17 implementation
        var result = await NewAsyncMethod();
#endif
    }
}
```

### Frontend Development

**Umbraco 13 (AngularJS):**
- Files located in `ClientV13/BulkUpload/`
- Copied to `wwwroot/BulkUpload/` during build
- No build step required (direct file copy)

**Umbraco 17 (Lit + TypeScript):**
```bash
cd src/BulkUpload/ClientV17

# Install dependencies
npm install

# Development mode (watch for changes)
npm run dev

# Production build
npm run build
```

The V17 build outputs to `wwwroot/bulkupload.js` and is automatically included in the net10.0 build.

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test src/BulkUpload.Tests/BulkUpload.Tests.csproj

# With coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

### Writing Tests

- Place unit tests in `src/BulkUpload.Tests/`
- Use xUnit as the testing framework
- Mock dependencies using Moq or NSubstitute
- Test both net8.0 and net10.0 paths when using conditional compilation

Example:

```csharp
public class DateTimeResolverTests
{
    [Fact]
    public void Resolve_ValidDate_ReturnsIso8601()
    {
        // Arrange
        var resolver = new DateTimeResolver();
        var input = "2024-01-15";

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("2024-01-15T00:00:00.0000000", result);
    }
}
```

### Manual Testing

1. Build the package: `dotnet build`
2. Run a test site (TestSite13 or TestSite17)
3. Upload sample CSV files from `samples/` directory
4. Verify imports succeed and results CSV is correct
5. Check Umbraco log viewer for any errors

## Coding Standards

### General Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods focused and small
- Add XML documentation comments for public APIs
- Use nullable reference types (`#nullable enable`)

### Naming Conventions

- **Classes/Interfaces:** PascalCase (`MediaImportService`, `IResolver`)
- **Methods:** PascalCase (`ProcessImport`, `ResolveValue`)
- **Properties:** PascalCase (`FileName`, `ParentId`)
- **Local variables:** camelCase (`csvData`, `importResult`)
- **Constants:** PascalCase (`MaxFileSize`, `DefaultParentId`)

### Code Organization

- One class per file
- Place interfaces in separate files
- Group related classes in folders
- Order members: fields, constructors, properties, methods
- Keep files under 500 lines when possible

### Commit Messages

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

## Submitting Changes

### Pull Request Process

1. **Fork the repository** (for external contributors)
2. **Create a feature branch** from `main`
3. **Make your changes** with clear, atomic commits
4. **Write/update tests** to cover your changes
5. **Update documentation** if you've changed APIs or added features
6. **Run tests** to ensure everything passes
7. **Push your branch** and create a pull request
8. **Describe your changes** in the PR description
9. **Link related issues** using "Fixes #123" or "Closes #456"
10. **Wait for review** and address any feedback

### Pull Request Checklist

- [ ] Code builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Tested manually with both Umbraco 13 and 17 (if applicable)
- [ ] Documentation updated (if needed)
- [ ] CHANGELOG.md updated (for significant changes)
- [ ] No merge conflicts with `main`
- [ ] Commit messages follow Conventional Commits format
- [ ] Code follows project coding standards

### Review Process

- Maintainers will review your PR within a few days
- Address any requested changes
- Once approved, maintainers will merge your PR
- Your contribution will be included in the next release

## Release Process

Releases are managed by maintainers. The process is automated via GitHub Actions:

1. Update version in `src/BulkUpload/BulkUpload.csproj`
2. Update `CHANGELOG.md` with release notes
3. Commit changes: `git commit -m "chore: prepare release v2.1.0"`
4. Create a GitHub Release with tag `v2.1.0`
5. GitHub Actions automatically builds and publishes to NuGet

For detailed release instructions, see [Release Process](../docs/RELEASE_PROCESS.md).

## Need Help?

- **Questions?** Open a [Discussion](https://github.com/ClerksWell-Ltd/BulkUpload/discussions)
- **Found a bug?** Open an [Issue](https://github.com/ClerksWell-Ltd/BulkUpload/issues)
- **Documentation:** See the [docs](../docs/) folder
- **Examples:** Check the [samples](../samples/) folder

## License

By contributing to BulkUpload, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to BulkUpload! 🎉
