# Quick Reference - Multi-Version Strategy

## Branch Overview

| Branch | Umbraco | Package Version | Purpose |
|--------|---------|----------------|---------|
| `main` | N/A | Development | Latest features |
| `release/v13.x` | 13.x | 1.x.x | Umbraco 13 releases |
| `release/v16.x` | 16.x | 2.x.x | Umbraco 16 releases |
| `release/v17.x` | 17.x | 3.x.x | Umbraco 17 releases |

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

git checkout release/v16.x
git pull origin release/v16.x
git cherry-pick <commit-hash>
git push origin release/v16.x
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

git checkout release/v16.x
git cherry-pick <commit-hash>
git push origin release/v16.x
```

### Release New Version

```bash
# 1. Checkout release branch
git checkout release/v13.x
git pull origin release/v13.x

# 2. Update version in .csproj
# Edit src/BulkUpload/BulkUpload.csproj
# Change <Version>1.1.0</Version> to <Version>1.2.0</Version>

# 3. Update CHANGELOG.md
# Add release notes

# 4. Commit version bump
git add src/BulkUpload/BulkUpload.csproj CHANGELOG.md
git commit -m "chore: bump version to 1.2.0"
git push origin release/v13.x

# 5. Create and push tag
git tag v1.2.0
git push origin v1.2.0

# 6. Build and publish
cd src/BulkUpload
dotnet pack -c Release
dotnet nuget push bin/Release/Umbraco.Community.BulkUpload.1.2.0.nupkg \
  -s https://api.nuget.org/v3/index.json \
  -k YOUR_API_KEY

# 7. Create GitHub Release
# Go to GitHub → Releases → New Release
```

### Add Support for New Umbraco Version

```bash
# 1. Create release branch from main
git checkout main
git pull origin main
git checkout -b release/v16.x

# 2. Update .csproj dependencies
# Edit src/BulkUpload/BulkUpload.csproj:
# - Umbraco.Cms.Web.Website → 16.0.0
# - Umbraco.Cms.Web.BackOffice → 16.0.0
# - Version → 2.0.0

# 3. Test with Umbraco 16

# 4. Update documentation
# Update README.md to mention Umbraco 16 support

# 5. Push and tag
git add .
git commit -m "feat: add Umbraco 16 support"
git push -u origin release/v16.x
git tag v2.0.0
git push origin v2.0.0
```

## Version Numbering

### Pattern: MAJOR.MINOR.PATCH

| Change Type | Umbraco 13 | Umbraco 16 | Umbraco 17 |
|-------------|-----------|-----------|-----------|
| New Umbraco version | N/A | 2.0.0 | 3.0.0 |
| New feature | 1.1.0 | 2.1.0 | 3.1.0 |
| Bug fix | 1.0.1 | 2.0.1 | 3.0.1 |
| Security fix | 1.0.2 | 2.0.2 | 3.0.2 |

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

### Find commits to cherry-pick

```bash
# See commits in main not in release branch
git log release/v13.x..main --oneline

# See commits in one release not in another
git log release/v13.x..release/v16.x --oneline
```

### Cherry-pick single commit

```bash
git checkout release/v13.x
git cherry-pick <commit-hash>
```

### Cherry-pick multiple commits

```bash
git checkout release/v13.x
git cherry-pick <commit-1> <commit-2> <commit-3>
```

### Cherry-pick with conflicts

```bash
git cherry-pick <commit-hash>
# If conflicts occur:
# 1. Resolve conflicts in files
# 2. git add <resolved-files>
# 3. git cherry-pick --continue
# Or to abort: git cherry-pick --abort
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
- `v16` - Affects Umbraco 16 version
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

- [Full Strategy Document](./BRANCHING_STRATEGY.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)
