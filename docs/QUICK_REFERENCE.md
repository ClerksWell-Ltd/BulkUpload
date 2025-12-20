# Quick Reference - Multi-Version Strategy

## Branch Overview

| Branch | Umbraco | Package Version | Purpose |
|--------|---------|----------------|---------|
| `main` | N/A | Development | Latest features |
| `release/v13.x` | 13.x | 1.x.x | Umbraco 13 releases |
| `release/v17.x` | 17.x | 2.x.x | Umbraco 17 releases |

## ⚠️ Critical Rules

**NEVER commit directly to these branches:**
- ❌ `main`
- ❌ `release/v13.x`
- ❌ `release/v17.x`

**ALWAYS:**
- ✅ Create a feature/bugfix branch
- ✅ Make changes in your branch
- ✅ Create a Pull Request to merge

## Common Commands

### Feature Development (Cross-Version)

```bash
# 1. Create feature from main
git checkout main
git pull origin main
git checkout -b feature/my-feature

# 2. Develop, commit, push
git add .
git commit -m "feat: add my feature"
git push origin feature/my-feature

# 3. Create PR to main
# After merge, cherry-pick to release branches:

git checkout release/v13.x
git pull origin release/v13.x
git cherry-pick <commit-hash>
git push origin release/v13.x

git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick <commit-hash>
git push origin release/v17.x
```

### Bug Fix (Specific Version)

```bash
# 1. Create fix from affected branch
git checkout release/v13.x
git pull origin release/v13.x
git checkout -b bugfix/issue-description

# 2. Fix, commit, push
git add .
git commit -m "fix: resolve issue with CSV parsing"
git push origin bugfix/issue-description

# 3. Create PR to release/v13.x
# After merge, cherry-pick to other branches if applicable

git checkout main
git cherry-pick <commit-hash>
git push origin main

git checkout release/v17.x
git cherry-pick <commit-hash>
git push origin release/v17.x
```

### Release New Version

**📖 Full instructions:** See [RELEASE_PROCESS.md](./RELEASE_PROCESS.md)

**Quick summary:**

```bash
# 1. Prepare release branch
git checkout release/v13.x
git pull origin release/v13.x

# 2. Update version in .csproj and CHANGELOG.md
# Edit src/BulkUpload/BulkUpload.csproj: <Version>1.2.0</Version>
# Edit CHANGELOG.md: Add changes under version heading

# 3. Commit and push
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: prepare release v1.2.0"
git push origin release/v13.x

# 4. Create GitHub Release (GitHub UI)
# - Tag: v1.2.0 (create new)
# - Target: release/v13.x
# - Title: v1.2.0 - Umbraco 13
# - Publish release

# 5. Automated workflows handle:
# ✅ Building and testing
# ✅ Publishing to NuGet
# ✅ Creating post-release PR (merge it when ready)
```

### Add Support for New Umbraco Version

```bash
# 1. Create release branch from main
git checkout main
git pull origin main
git checkout -b release/v17.x

# 2. Update .csproj dependencies
# Edit src/BulkUpload/BulkUpload.csproj:
# - Umbraco.Cms.Web.Website → 17.0.0
# - Umbraco.Cms.Web.BackOffice → 17.0.0
# - Version → 2.0.0

# 3. Test with Umbraco 17

# 4. Update documentation
# Update README.md to mention Umbraco 17 support

# 5. Push and tag
git add .
git commit -m "feat: add Umbraco 17 support"
git push -u origin release/v17.x
git tag v2.0.0
git push origin v2.0.0
```

## Version Numbering

### Pattern: MAJOR.MINOR.PATCH

| Change Type | Umbraco 13 | Umbraco 17 |
|-------------|-----------|-----------|
| New Umbraco version | N/A | 2.0.0 |
| New feature | 1.1.0 | 2.1.0 |
| Bug fix | 1.0.1 | 2.0.1 |
| Security fix | 1.0.2 | 2.0.2 |

## Git Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add new CSV validation
fix: resolve parsing error with special characters
docs: update README with new examples
chore: bump version to 1.2.0
refactor: simplify resolver registration
test: add unit tests for DateTimeResolver
perf: optimize CSV processing
```

### Scopes (optional)

```
feat(resolver): add new ImageResolver
fix(csv): handle empty columns correctly
docs(readme): add troubleshooting section
```

## Cherry-Picking Guide

### Step-by-Step Cherry-Pick

```bash
# 1. Find the commit hash
git log main --oneline -10
# Copy the commit hash (e.g., abc1234)

# 2. Switch to target branch
git checkout release/v13.x
git pull origin release/v13.x

# 3. Cherry-pick the commit
git cherry-pick abc1234

# 4. Test the changes
dotnet build
dotnet test

# 5. Push the cherry-picked commit
git push origin release/v13.x
```

### Find commits to cherry-pick

```bash
# See commits in main not in release branch
git log release/v13.x..main --oneline

# See commits in one release not in another
git log release/v13.x..release/v17.x --oneline
```

### Cherry-pick multiple commits

```bash
# One by one
git cherry-pick abc1234
git cherry-pick def5678
git cherry-pick ghi9012

# Or all at once
git cherry-pick abc1234 def5678 ghi9012

# Or a range
git cherry-pick abc1234..ghi9012
```

### Cherry-pick with conflicts

```bash
git cherry-pick <commit-hash>

# If conflicts occur:
# 1. Open conflicted files (look for <<<<<<< markers)
# 2. Resolve conflicts manually
# 3. Stage resolved files
git add <resolved-files>

# 4. Continue cherry-pick
git cherry-pick --continue

# OR abort if needed
git cherry-pick --abort
```

### Complete Example

```bash
# Scenario: Fix in v13 needs to go to main and v17

# 1. PR merged to release/v13.x (commit abc1234)

# 2. Cherry-pick to main
git checkout main
git pull origin main
git cherry-pick abc1234
dotnet test
git push origin main

# 3. Cherry-pick to release/v17.x
git checkout release/v17.x
git pull origin release/v17.x
git cherry-pick abc1234
dotnet test
git push origin release/v17.x
```

## Writing Good Commits for Cherry-Picking

### ✅ DO: Keep commits atomic

```bash
# Good - single purpose
git commit -m "fix: handle empty CSV columns"

# Bad - multiple purposes
git commit -m "fix CSV, update docs, refactor code"
```

### ✅ DO: Make self-contained commits

Each commit should:
- Build successfully
- Pass all tests
- Work independently

```bash
# Good - complete change
git add IResolverService.cs Resolvers/*.cs
git commit -m "feat: add async support to resolvers"

# Bad - broken in between
git add IResolverService.cs
git commit -m "add async to interface"  # Breaks build!
git add Resolvers/*.cs
git commit -m "implement async"  # Now it works
```

### ✅ DO: Use clear commit messages

Follow Conventional Commits:

```bash
git commit -m "feat: add new feature"
git commit -m "fix: resolve bug"
git commit -m "docs: update README"
git commit -m "refactor: simplify logic"
git commit -m "test: add unit tests"
```

### ❌ DON'T: Mix multiple changes

```bash
# Bad - hard to cherry-pick selectively
git commit -m "fix bug, add feature, update docs"

# Good - separate commits
git commit -m "fix: resolve CSV parsing issue"
git commit -m "feat: add validation"
git commit -m "docs: update README"
```

### ❌ DON'T: Use version-specific code

```bash
# Bad - breaks when cherry-picked
if (umbracoVersion == "13.0.0") { }

# Good - version-agnostic
if (config.EnableFeature) { }
```

## Testing Checklist

Before releasing:

- [ ] All unit tests pass (`dotnet test`)
- [ ] Manual testing with target Umbraco version
- [ ] CSV import works correctly
- [ ] All resolvers function as expected
- [ ] No errors in Umbraco log
- [ ] Documentation is up to date
- [ ] CHANGELOG.md is updated
- [ ] Version number is bumped in .csproj

## Troubleshooting

### Wrong branch merged?

```bash
# Revert the merge commit
git revert -m 1 <merge-commit-hash>
```

### Need to undo a push?

```bash
# Create a new commit that undoes changes
git revert <commit-hash>
git push origin <branch-name>
```

### Forgot to cherry-pick?

```bash
# Find the commit
git log --all --grep="search term"
# Cherry-pick it
git cherry-pick <commit-hash>
```

## GitHub Labels

Use these labels on PRs and issues:

- `v13` - Affects Umbraco 13 version
- `v17` - Affects Umbraco 17 version
- `needs-backport` - Should be cherry-picked to release branches
- `breaking-change` - Breaking API change
- `bug` - Bug fix
- `enhancement` - New feature
- `documentation` - Documentation update

## Release Checklist

- [ ] Code reviewed and approved
- [ ] Tests passing
- [ ] Version bumped in .csproj
- [ ] CHANGELOG.md updated
- [ ] Committed and pushed
- [ ] Tagged (e.g., v1.2.0)
- [ ] Built package (`dotnet pack`)
- [ ] Published to NuGet
- [ ] GitHub Release created
- [ ] Release notes published
- [ ] Documentation updated (if needed)
- [ ] Team notified

## Resources

### Documentation
- [Release Process Guide](./RELEASE_PROCESS.md) - Detailed release instructions
- [Release Quick Reference](./QUICK_REFERENCE_RELEASE.md) - Command cheat sheet
- [Branching Strategy](./BRANCHING_STRATEGY.md) - Full strategy document
- [Workflow Diagrams](./WORKFLOW_DIAGRAM.md) - Visual workflow guides

### External Resources
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)

### Automated Workflows
- **Release to NuGet:** `.github/workflows/release.yml`
- **Post-Release:** `.github/workflows/post-release.yml`
