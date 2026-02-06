# Branching and Development Strategy

## Overview

Starting with v2.0.0, BulkUpload uses a **simplified single-branch strategy** with .NET multi-targeting to support both Umbraco 13 and Umbraco 17 from a single codebase.

## Current Architecture (v2.0.0+)

### Single Branch, Multi-Targeting

```
main
 └── Builds for both net8.0 (Umbraco 13) and net10.0 (Umbraco 17)
```

**Key Benefits:**
- **One codebase** - All changes apply to both Umbraco versions simultaneously
- **Single NuGet package** - Contains both net8.0 and net10.0 targets
- **Automatic version selection** - NuGet picks the correct framework based on the consuming project
- **No cherry-picking** - No need to port changes between branches
- **Simplified releases** - One release process for both versions

### Branch Purpose

| Branch | Purpose | Protected |
|--------|---------|-----------|
| `main` | Production-ready code, all development happens here | Yes ✓ |

### Branch Protection Rules

**⚠️ CRITICAL: Never commit directly to `main`**

The `main` branch should be protected and require pull requests:
1. ✅ Create a feature/bugfix branch FROM `main`
2. ✅ Make your changes in the feature/bugfix branch
3. ✅ Push your feature/bugfix branch
4. ✅ Create a Pull Request to `main`
5. ❌ NEVER commit directly to `main`

**Example - Correct workflow:**
```bash
# ✅ CORRECT - Create branch from main
git checkout main
git pull origin main
git checkout -b feature/new-csv-validator

# Make changes, commit, push
git add .
git commit -m "feat: add new CSV validation"
git push origin feature/new-csv-validator

# Create PR: feature/new-csv-validator → main
```

**Example - Incorrect workflow:**
```bash
# ❌ WRONG - Working directly on main
git checkout main
git add .
git commit -m "feat: some feature"  # ❌ DON'T DO THIS!
git push origin main  # ❌ DON'T DO THIS!
```

## Versioning Strategy

### Semantic Versioning

Use semantic versioning (MAJOR.MINOR.PATCH) with multi-targeting:

```
v2.0.0  - Initial multi-targeting release (supports both Umbraco 13 and 17)
v2.1.0  - New feature (available for both versions)
v2.1.1  - Bug fix (available for both versions)
```

### Version Bumping Rules

- **MAJOR**: Breaking changes or major architectural updates
- **MINOR**: New features, enhancements
- **PATCH**: Bug fixes, security patches

**Note:** Since v2.0.0+, each release supports BOTH Umbraco 13 and Umbraco 17. There's no need for separate version numbers per Umbraco version.

## Workflow Scenarios

### Scenario 1: New Feature Development

**Goal**: Add a new feature

```bash
# 1. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/new-csv-validation

# 2. Develop and test the feature
# ... make changes ...
# Test with both net8.0 and net10.0:
dotnet build
dotnet test

# 3. Commit and push
git add .
git commit -m "feat: add new CSV validation"
git push origin feature/new-csv-validation

# 4. Create PR to main on GitHub
# After merge, feature is available for both Umbraco 13 and 17
```

### Scenario 2: Bug Fix

**Goal**: Fix a bug

```bash
# 1. Create bugfix branch from main
git checkout main
git pull origin main
git checkout -b bugfix/csv-parsing-issue

# 2. Make the fix and test
# ... make changes ...
dotnet build
dotnet test

# 3. Commit and push
git add .
git commit -m "fix: handle empty CSV columns correctly"
git push origin bugfix/csv-parsing-issue

# 4. Create PR to main on GitHub
# After merge, fix is available for both Umbraco 13 and 17
```

### Scenario 3: Framework-Specific Code

**Goal**: Handle differences between Umbraco 13 and 17

When you need different code for each version, use conditional compilation:

```csharp
#if NET8_0
    // Umbraco 13 specific code
    builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#elif NET10_0
    // Umbraco 17 specific code
    // Use umbraco-package.json for section registration
#endif
```

**Best Practice:** Keep framework-specific code to a minimum. Most business logic should be shared.

## Multi-Targeting Configuration

Both main projects use `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`:

**BulkUpload.csproj:**
```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
</PropertyGroup>

<!-- Umbraco 13 dependencies (net8.0) -->
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <PackageReference Include="Umbraco.Cms.Web.Website" Version="13.x.x" />
  <PackageReference Include="Umbraco.Cms.Web.BackOffice" Version="13.x.x" />
</ItemGroup>

<!-- Umbraco 17 dependencies (net10.0) -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <PackageReference Include="Umbraco.Cms.Api.Common" Version="17.x.x" />
  <PackageReference Include="Umbraco.Cms.Core" Version="17.x.x" />
</ItemGroup>
```

## Release Process

**For detailed release instructions, see [RELEASE_PROCESS.md](./RELEASE_PROCESS.md)**

### Quick Overview

1. **Prepare:** Update version in .csproj and CHANGELOG.md
2. **Commit:** Commit changes with message: `chore: prepare release v2.1.0`
3. **Release:** Create a GitHub Release from `main` branch with tag `v2.1.0`
4. **Automated:** GitHub Actions builds and publishes to NuGet
5. **Done:** Package is available on NuGet with both net8.0 and net10.0 targets

### Automated Workflows

- **`release.yml`**: Builds, tests, and publishes to NuGet when you create a GitHub Release
- **`build.yml`**: Runs on every push and pull request to validate changes

## Commit Message Standards

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

**Format:**
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code refactoring
- `test`: Adding tests
- `docs`: Documentation
- `chore`: Maintenance (version bumps, dependency updates)
- `perf`: Performance improvements

## Testing Requirements

Before merging any PR:

- [ ] Code builds successfully for both net8.0 and net10.0 (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Tested manually with Umbraco 13 (if making changes that might affect it)
- [ ] Tested manually with Umbraco 17 (if making changes that might affect it)
- [ ] No merge conflicts with `main`

## Code Sharing Strategy

### Maximize Shared Code

Keep version-specific code isolated:

```
src/BulkUpload/
├── Resolvers/           # Shared across all versions
├── Services/            # Shared across all versions
├── Models/              # Shared across all versions
├── Controllers/         # Mostly shared, some conditional code
├── ClientV13/           # Umbraco 13 frontend (AngularJS)
└── ClientV17/           # Umbraco 17 frontend (Lit)
```

### Conditional Compilation

Use conditional compilation sparingly and only when necessary:

```csharp
#if NET8_0
    // Umbraco 13 specific code
#else
    // Umbraco 17 specific code
#endif
```

## Best Practices

### DO ✅

- Always develop features in `main`
- Keep `main` branch stable and production-ready
- Write comprehensive commit messages
- Test across both target frameworks before merging
- Maintain CHANGELOG.md
- Document framework-specific limitations
- Use conditional compilation sparingly

### DON'T ❌

- Don't commit directly to `main`
- Don't skip testing when making changes
- Don't create unnecessary framework-specific code
- Don't ignore build warnings
- Don't forget to update documentation

## Migration from Previous Strategy

If you have code or documentation referencing the old `release/v13.x` and `release/v17.x` branches:

1. Those branches are **deprecated** and no longer used
2. All development happens in `main` with multi-targeting
3. Update any documentation or scripts that reference old branches
4. Cherry-picking is no longer needed - changes apply to both versions automatically

## Summary

The multi-targeting approach provides:

- **Simplicity**: Single branch, single workflow
- **Maintainability**: Changes apply to both versions automatically
- **Quality**: Consistent testing and release process
- **Efficiency**: No cherry-picking or branch synchronization needed
- **Scalability**: Easy to add support for future Umbraco versions

---

**Questions or Suggestions?**

Open an issue or discussion to help improve this strategy.
