# Multi-Version Branching and Release Strategy

## Overview

This document outlines the branching and release strategy for BulkUpload to support multiple Umbraco versions while maximizing code sharing.

## Current State

- **Version 1.0.0** supports Umbraco 13
- Planning to support Umbraco 17 as v2.x
- Most business logic is version-agnostic
- Version-specific code is primarily in dependencies and APIs

## Branch Strategy

### Main Branches

```
main (or master)
├── release/v13.x    # For Umbraco 13 (current)
└── release/v17.x    # For Umbraco 17 (upcoming)
```

### Branch Purposes

| Branch | Purpose | Umbraco Version | Package Version Pattern |
|--------|---------|-----------------|------------------------|
| `main` | Development, latest features | N/A | Development only |
| `release/v13.x` | Production releases for Umbraco 13 | 13.x | 1.x.x |
| `release/v17.x` | Production releases for Umbraco 17 | 17.x | 2.x.x |

### Key Principles

1. **`main` branch is the source of truth** - All new features developed here first
2. **Release branches are long-lived** - They exist for the lifetime of that Umbraco version support
3. **Version-specific changes go directly to release branches** - Only when they cannot be generalized
4. **Regular forward-porting** - Changes from older release branches should be forward-ported to main

### Branch Protection Rules

**⚠️ CRITICAL: Never commit directly to protected branches**

The following branches should be protected and require pull requests:
- `main`
- `release/v13.x`
- `release/v17.x`

**Always follow this workflow:**
1. ✅ Create a feature/bugfix branch FROM the target branch
2. ✅ Make your changes in the feature/bugfix branch
3. ✅ Push your feature/bugfix branch
4. ✅ Create a Pull Request to the target branch
5. ❌ NEVER commit directly to `main`, `release/v13.x`, or `release/v17.x`

**Example - Correct workflow:**
```bash
# ✅ CORRECT - Create branch from release branch
git checkout release/v13.x
git pull origin release/v13.x
git checkout -b fix/csv-parsing-bug

# Make changes, commit, push
git add .
git commit -m "fix: handle empty CSV columns correctly"
git push origin fix/csv-parsing-bug

# Create PR: fix/csv-parsing-bug → release/v13.x
```

**Example - Incorrect workflow:**
```bash
# ❌ WRONG - Working directly on release branch
git checkout release/v13.x
git add .
git commit -m "fix: some fix"  # ❌ DON'T DO THIS!
git push origin release/v13.x  # ❌ DON'T DO THIS!
```

## Versioning Strategy

### Semantic Versioning with Umbraco Mapping

Use semantic versioning (MAJOR.MINOR.PATCH) aligned with Umbraco version support:

```
Umbraco 13 → BulkUpload 1.x.x
Umbraco 17 → BulkUpload 2.x.x
```

### Version Bumping Rules

- **MAJOR**: New Umbraco version support (breaking changes)
- **MINOR**: New features, enhancements
- **PATCH**: Bug fixes, security patches

### Example Timeline

```
1.0.0  - Initial release (Umbraco 13)
1.1.0  - Add new resolver feature (Umbraco 13)
1.1.1  - Fix CSV parsing bug (Umbraco 13)
2.0.0  - Initial release (Umbraco 17)
2.0.1  - Fix CSV parsing bug (Umbraco 17) - backported from 1.1.1
```

## Workflow Scenarios

### Scenario 1: New Feature Development

**Goal**: Add a new feature that works across all Umbraco versions

```bash
# 1. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/new-csv-validation

# 2. Develop and test the feature
# ... make changes ...

# 3. Create PR to main
git push origin feature/new-csv-validation
# Open PR → main

# 4. After merge to main, cherry-pick to release branches
git checkout release/v13.x
git cherry-pick <commit-hash>
git push origin release/v13.x

git checkout release/v17.x
git cherry-pick <commit-hash>
git push origin release/v17.x

# 5. Release new versions
# 1.2.0, 2.1.0 (depending on current version)
```

### Scenario 2: Bug Fix

**Goal**: Fix a bug reported in a specific version

```bash
# 1. Fix in the affected release branch
git checkout release/v13.x
git checkout -b bugfix/csv-parsing-issue

# 2. Make the fix and test
# ... make changes ...

# 3. Create PR to release branch
git push origin bugfix/csv-parsing-issue
# Open PR → release/v13.x

# 4. After merge, cherry-pick to other branches
git checkout main
git cherry-pick <commit-hash>

git checkout release/v17.x
git cherry-pick <commit-hash>

# 5. Release patch version (e.g., 1.1.2)
```

### Scenario 3: Version-Specific Change

**Goal**: Make a change that only applies to one Umbraco version

```bash
# 1. Create branch from specific release branch
git checkout release/v17.x
git checkout -b fix/umbraco17-specific-api

# 2. Make version-specific changes
# ... make changes ...

# 3. Create PR to that release branch only
git push origin fix/umbraco17-specific-api
# Open PR → release/v17.x

# 4. Release new version for that branch only (e.g., 2.0.1)
```

### Scenario 4: Creating Support for New Umbraco Version

**Goal**: Add support for Umbraco 17

```bash
# 1. Create new release branch from main
git checkout main
git pull origin main
git checkout -b release/v17.x

# 2. Update dependencies in .csproj
# - Update Umbraco.Cms.Web.Website to 17.x.x
# - Update Umbraco.Cms.Web.BackOffice to 17.x.x
# - Update TargetFramework if needed

# 3. Update version and metadata
# - Update <Version> to 2.0.0
# - Update README/docs to mention Umbraco 17 support

# 4. Test thoroughly with Umbraco 17

# 5. Push branch and create initial release
git push -u origin release/v17.x

# 6. Tag and release 2.0.0
git tag v2.0.0
git push origin v2.0.0
```

## Cherry-Picking Guide

Cherry-picking is the process of applying commits from one branch to another. This is essential for our multi-version strategy.

### When to Cherry-Pick

1. **After merging to main** - Cherry-pick to `release/v13.x` and `release/v17.x`
2. **After merging to a release branch** - Cherry-pick to `main` and other release branches
3. **Forward-porting** - When a fix in an older release (v13) needs to go to newer releases (v17) and main

### Understanding Cherry-Pick

Cherry-picking creates a **new commit** with the same changes but a **different commit hash**:

```
Original commit in main:
  commit abc123 "fix: handle empty columns"

After cherry-pick to release/v13.x:
  commit def456 "fix: handle empty columns"

Same content, different hash!
```

### Step-by-Step Cherry-Pick Process

#### Step 1: Identify the Commit Hash

After your PR is merged, find the commit hash from GitHub or command line:

```bash
# View recent commits in the target branch
git log main --oneline -10

# Example output:
# abc1234 fix: handle empty columns
# def5678 feat: add new resolver
# ghi9012 docs: update README
```

Copy the hash of the commit you want to cherry-pick (e.g., `abc1234`).

#### Step 2: Switch to Target Branch

```bash
# Switch to the branch where you want to apply the change
git checkout release/v13.x
git pull origin release/v13.x
```

#### Step 3: Cherry-Pick the Commit

```bash
# Apply the commit
git cherry-pick abc1234

# If successful, you'll see:
# [release/v13.x def4567] fix: handle empty columns
# 1 file changed, 5 insertions(+), 2 deletions(-)
```

#### Step 4: Handle Conflicts (If They Occur)

If there are conflicts, Git will pause and tell you:

```bash
# Conflict message:
# error: could not apply abc1234... fix: handle empty columns
# hint: after resolving the conflicts, mark them with
# hint: "git add <file>", then run "git cherry-pick --continue"
```

**Resolve conflicts:**

```bash
# 1. Open conflicted files in your editor
#    Look for conflict markers: <<<<<<<, =======, >>>>>>>

# 2. Manually resolve the conflicts
#    Keep the changes that make sense for this branch

# 3. Stage the resolved files
git add <resolved-files>

# 4. Continue the cherry-pick
git cherry-pick --continue

# OR, if you want to abort:
git cherry-pick --abort
```

#### Step 5: Test the Changes

```bash
# Build and test to ensure the cherry-pick works correctly
dotnet build
dotnet test
```

#### Step 6: Push the Changes

```bash
# Push the cherry-picked commit
git push origin release/v13.x
```

### Cherry-Picking Multiple Commits

If you have multiple commits to cherry-pick:

```bash
# Option 1: Cherry-pick commits one by one
git cherry-pick abc1234
git cherry-pick def5678
git cherry-pick ghi9012

# Option 2: Cherry-pick a range of commits
git cherry-pick abc1234..ghi9012

# Option 3: Cherry-pick multiple specific commits
git cherry-pick abc1234 def5678 ghi9012
```

### Complete Cherry-Pick Workflow Example

**Scenario:** You fixed a bug in `release/v13.x` and need to apply it to `main` and `release/v17.x`.

```bash
# 1. Your PR fix/csv-bug → release/v13.x was merged
#    Commit hash: abc1234

# 2. Cherry-pick to main
git checkout main
git pull origin main
git cherry-pick abc1234
# Resolve conflicts if needed
dotnet test
git push origin main

# 3. Cherry-pick to release/v17.x
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick abc1234
# Resolve conflicts if needed
dotnet test
git push origin release/v17.x

# Done! The fix is now in all three branches
```

### Writing Cherry-Pick Friendly Commits

To make cherry-picking easier, follow these guidelines:

#### 1. Keep Commits Atomic and Focused

**✅ Good - Single purpose commit:**
```bash
git commit -m "fix: handle empty CSV columns correctly"
```

**❌ Bad - Multiple unrelated changes:**
```bash
git commit -m "fix: CSV parsing and update docs and refactor validator"
```

#### 2. Avoid Mixing Concerns

**✅ Good - Separate commits:**
```bash
git commit -m "refactor: extract CSV validation logic"
git commit -m "fix: handle empty columns in validator"
git commit -m "test: add tests for empty column handling"
```

**❌ Bad - Mixed concerns:**
```bash
git commit -m "fix bug, add tests, refactor code, update docs"
```

#### 3. Use Self-Contained Commits

Each commit should:
- Build successfully
- Pass all tests
- Not depend on uncommitted changes
- Work independently

**✅ Good - Self-contained:**
```bash
# Commit 1: Interface change + all implementations
git add Services/IResolverService.cs
git add Resolvers/*.cs
git commit -m "feat: add async support to IResolverService"
```

**❌ Bad - Broken state:**
```bash
# Commit 1: Only interface change
git add Services/IResolverService.cs
git commit -m "feat: add async to interface"

# Commit 2: Implementations (build is broken between commits!)
git add Resolvers/*.cs
git commit -m "feat: implement async methods"
```

#### 4. Write Clear Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

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
- `chore`: Maintenance

**✅ Good examples:**
```bash
git commit -m "fix: handle null values in DateTimeResolver"

git commit -m "feat(resolver): add ImageResolver for media uploads

Implements new ImageResolver that:
- Handles base64 encoded images
- Supports URLs to download images
- Creates media items in Umbraco media library

Closes #123"

git commit -m "refactor: simplify CSV parsing logic"
```

**❌ Bad examples:**
```bash
git commit -m "fix stuff"
git commit -m "updates"
git commit -m "WIP"
git commit -m "fixed bug from yesterday"
```

#### 5. Avoid Version-Specific Code in Shared Commits

If a commit will be cherry-picked to multiple branches, avoid version-specific code:

**✅ Good - Generic code:**
```csharp
public object Resolve(object value)
{
    if (value is not string str || string.IsNullOrEmpty(str))
        return string.Empty;

    return str.Trim();
}
```

**❌ Bad - Version-specific hardcoded:**
```csharp
public object Resolve(object value)
{
    // This will break when cherry-picked to v17
    if (UmbracoVersion == "13.0.0")  // ❌ Don't do this
    {
        // v13 specific logic
    }
}
```

#### 6. One Logical Change Per Commit

**✅ Good - Logical grouping:**
```bash
# Commit 1: Fix the bug
git commit -m "fix: prevent division by zero in progress calculator"

# Commit 2: Add tests for the fix
git commit -m "test: add tests for division by zero handling"

# Commit 3: Update docs
git commit -m "docs: document progress calculator edge cases"
```

Each commit can be cherry-picked independently if needed.

### Troubleshooting Cherry-Picks

#### Problem: Cherry-pick creates conflicts

**Solution:** Resolve manually or use a different strategy:

```bash
# Option 1: Resolve conflicts manually
git cherry-pick abc1234
# Fix conflicts
git add <files>
git cherry-pick --continue

# Option 2: Abort and manually recreate the fix
git cherry-pick --abort
git checkout -b fix/manual-port-from-v13
# Manually apply the changes
git commit -m "fix: (port from v13) handle empty columns"
```

#### Problem: Cherry-pick applies but breaks tests

**Solution:** The code may need version-specific adjustments:

```bash
# After cherry-pick
git cherry-pick abc1234
dotnet test  # Tests fail!

# Make necessary adjustments for this version
git add <files>
git commit --amend --no-edit  # Amend the cherry-picked commit
```

#### Problem: Accidentally cherry-picked to wrong branch

**Solution:** Reset or revert:

```bash
# If you haven't pushed yet
git reset --hard HEAD~1

# If you've already pushed
git revert <cherry-picked-commit-hash>
git push
```

### Best Practices Summary

1. ✅ **Always create feature branches** - Never work directly on main or release branches
2. ✅ **Keep commits atomic** - One logical change per commit
3. ✅ **Write clear commit messages** - Use Conventional Commits format
4. ✅ **Test before pushing** - Ensure cherry-picks don't break tests
5. ✅ **Make self-contained commits** - Each commit should build and pass tests
6. ✅ **Avoid version-specific code** - Keep commits generic when possible
7. ✅ **Document conflicts** - If you had to resolve conflicts, note it in PR comments

## Release Process

### Preparation

1. **Ensure all tests pass**
   ```bash
   dotnet test
   ```

2. **Update version in .csproj**
   ```xml
   <Version>1.2.0</Version>
   ```

3. **Update CHANGELOG.md** (create if doesn't exist)
   ```markdown
   ## [1.2.0] - 2025-12-20
   ### Added
   - New CSV validation feature

   ### Fixed
   - CSV parsing bug with special characters
   ```

4. **Commit version bump**
   ```bash
   git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
   git commit -m "chore: bump version to 1.2.0"
   ```

### Creating the Release

1. **Build the package**
   ```bash
   cd src/BulkUpload
   dotnet build -c Release
   ```

2. **Create and push tag**
   ```bash
   git tag v1.2.0
   git push origin release/v13.x --tags
   ```

3. **Publish to NuGet**
   ```bash
   dotnet pack -c Release
   dotnet nuget push bin/Release/Umbraco.Community.BulkUpload.1.2.0.nupkg -s https://api.nuget.org/v3/index.json -k YOUR_API_KEY
   ```

4. **Create GitHub Release**
   - Go to GitHub → Releases → New Release
   - Select the tag (v1.2.0)
   - Title: "v1.2.0 - Umbraco 13"
   - Description: Copy from CHANGELOG
   - Attach .nupkg file

## Code Sharing Strategies

### Strategy 1: Maximize Shared Code

Keep version-specific code isolated:

```
src/BulkUpload/
├── Resolvers/           # Shared across all versions
├── Services/            # Shared across all versions
├── Models/              # Shared across all versions
├── Extensions/          # May have version-specific implementations
└── Compatibility/       # Version-specific adapters (if needed)
```

### Strategy 2: Use Conditional Compilation (If Needed)

For minor version differences within the same codebase:

```csharp
#if UMBRACO13
    // Umbraco 13 specific code
#else
    // Umbraco 17 specific code
#endif
```

Define in .csproj:
```xml
<PropertyGroup Condition="'$(UmbracoVersion)' == '13'">
    <DefineConstants>$(DefineConstants);UMBRACO13</DefineConstants>
</PropertyGroup>
```

**Note**: Only use if differences are minimal. Otherwise, maintain separate branches.

### Strategy 3: Abstraction Layers

Create interfaces for Umbraco-specific operations:

```csharp
public interface IUmbracoContentService
{
    IContent CreateContent(string name, IContent parent, string contentTypeAlias);
    void SaveAndPublish(IContent content);
}

// Version-specific implementations
public class Umbraco13ContentService : IUmbracoContentService { }
public class Umbraco17ContentService : IUmbracoContentService { }
```

## Maintenance Strategy

### Active Support Matrix

| Umbraco Version | BulkUpload Version | Support Status | End of Life |
|----------------|-------------------|----------------|-------------|
| 13.x | 1.x.x | Active | TBD |
| 17.x | 2.x.x | Planned | TBD |

### Support Levels

- **Active**: New features, bug fixes, security patches
- **Maintenance**: Bug fixes and security patches only
- **End of Life**: No further updates

### Bug Fix Priority

1. **Security vulnerabilities**: Fix in all supported versions immediately
2. **Critical bugs**: Fix in all active versions within 1 week
3. **Minor bugs**: Fix in main, consider backporting based on severity
4. **Enhancements**: Develop in main, selectively backport

## Automation Recommendations

### GitHub Actions Workflows

Create separate workflows for each version:

```yaml
# .github/workflows/build-v13.yml
name: Build and Test - Umbraco 13
on:
  push:
    branches: [ release/v13.x ]
  pull_request:
    branches: [ release/v13.x ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### Release Automation

Consider using tools like:
- **Semantic Release**: Automate version bumping and changelog generation
- **GitHub Actions**: Automate NuGet publishing on tag creation
- **Dependabot**: Automate dependency updates per branch

## Migration Path

### Current State → New Strategy

1. **Week 1: Setup**
   - Create `release/v13.x` from current main
   - Tag current version as v1.0.0
   - Update documentation

2. **Week 2-3: Umbraco 17 Support**
   - Branch `release/v17.x` from main
   - Update dependencies
   - Test and release v2.0.0

3. **Week 4: Process Documentation**
   - Document any version-specific quirks
   - Create runbooks for releases
   - Train team on new workflow

## Best Practices

### DO ✅

- Always develop new features in `main` first
- Keep release branches stable and production-ready
- Write comprehensive commit messages
- Tag all releases
- Maintain a CHANGELOG
- Test across all supported versions before release
- Document version-specific limitations

### DON'T ❌

- Don't develop features directly in release branches
- Don't merge release branches into each other
- Don't skip testing when cherry-picking
- Don't forget to forward-port bug fixes
- Don't let branches diverge significantly
- Don't ignore breaking changes in dependencies

## Tools and Resources

- **Git Flow**: Consider using git-flow extensions
- **Conventional Commits**: https://www.conventionalcommits.org/
- **Semantic Versioning**: https://semver.org/
- **Keep a Changelog**: https://keepachangelog.com/

## Communication

### Internal Team

- Document all version-specific changes in PR descriptions
- Use labels: `v13`, `v17`, `needs-backport`
- Regular sync meetings to discuss multi-version changes

### External Users

- Clear documentation on which version to install
- Installation instructions per Umbraco version
- Migration guides between versions

## Summary

This strategy provides:
- **Clarity**: Clear branch structure for each Umbraco version
- **Flexibility**: Support multiple versions simultaneously
- **Maintainability**: Systematic approach to backporting and forward-porting
- **Quality**: Consistent testing and release process
- **Scalability**: Easy to add support for future Umbraco versions

---

**Questions or Suggestions?**

Open an issue or discuss with the team to evolve this strategy as needed.
