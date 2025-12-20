# Workflow Diagrams

This document uses [Mermaid](https://mermaid.js.org/) diagrams for better visualization. These diagrams render automatically on GitHub, GitLab, and many markdown viewers.

## Branch Structure

```mermaid
gitGraph
    commit id: "Initial commit"
    branch release/v13.x
    branch release/v17.x

    checkout main
    commit id: "feat: new feature"
    branch feature/new-api
    commit id: "work on API"
    commit id: "complete API"
    checkout main
    merge feature/new-api tag: "merged"

    checkout release/v13.x
    cherry-pick id: "feat: new feature" tag: "v1.1.0"
    commit id: "v13 adjustments"

    checkout release/v17.x
    cherry-pick id: "feat: new feature" tag: "v2.1.0"
    commit id: "v17 adjustments"

    checkout main
    commit id: "more work"
```

**Key Points:**
- `main` is the source of truth for new development
- Feature branches merge into `main`
- Changes are cherry-picked to `release/v13.x` and `release/v17.x`
- Each release branch maintains its own version numbers

## Feature Development Flow

```mermaid
sequenceDiagram
    actor Developer
    participant main
    participant release/v13.x
    participant release/v17.x
    participant NuGet

    Developer->>main: 1. Create feature/new-csv-validation branch
    Developer->>Developer: 2. Develop & commit changes
    Developer->>main: 3. Create PR (feature → main)
    main->>main: 4. Merge PR

    Note over main,release/v17.x: After merge, cherry-pick to release branches

    main->>release/v13.x: 5. Cherry-pick commit
    release/v13.x->>release/v13.x: Test & verify
    release/v13.x->>NuGet: 6. Release v1.x.x

    main->>release/v17.x: 7. Cherry-pick commit
    release/v17.x->>release/v17.x: Test & verify
    release/v17.x->>NuGet: 8. Release v2.x.x
```

**Workflow:**
1. Create feature branch from `main`
2. Develop and commit changes
3. Create PR targeting `main`
4. After PR is merged, cherry-pick to release branches
5. Test cherry-picked changes in each version
6. Release new versions

## Bug Fix Flow (Version-Specific)

```mermaid
sequenceDiagram
    actor Developer
    participant release/v13.x
    participant main
    participant release/v17.x
    participant NuGet

    Developer->>release/v13.x: 1. Create bugfix/csv-parsing branch
    Developer->>Developer: 2. Fix bug & commit
    Developer->>release/v13.x: 3. Create PR (bugfix → release/v13.x)
    release/v13.x->>release/v13.x: 4. Merge PR

    Note over release/v13.x,release/v17.x: Forward-port fix to other branches

    release/v13.x->>main: 5. Cherry-pick to main
    main->>main: Test & verify

    release/v13.x->>release/v17.x: 6. Cherry-pick to v17.x
    release/v17.x->>release/v17.x: Test & verify

    release/v13.x->>NuGet: 7. Release v1.x.y
    release/v17.x->>NuGet: 8. Release v2.x.y (if applicable)
```

**Workflow:**
1. Create bugfix branch from affected release branch
2. Fix the bug
3. Create PR targeting the release branch
4. After merge, cherry-pick to `main` and other release branches
5. Release patch version

## New Umbraco Version Support Flow

```mermaid
sequenceDiagram
    actor Developer
    participant main
    participant release/v17.x
    participant NuGet

    Developer->>main: 1. Fetch latest main
    Developer->>release/v17.x: 2. Create release/v17.x branch

    Developer->>release/v17.x: 3. Update dependencies<br/>- Umbraco.Cms.Web.Website 17.x<br/>- Umbraco.Cms.Web.BackOffice 17.x

    Developer->>release/v17.x: 4. Update version to 2.0.0

    Developer->>release/v17.x: 5. Update docs

    Developer->>Developer: 6. Test with Umbraco 17

    Developer->>release/v17.x: 7. Commit & push branch

    Developer->>release/v17.x: 8. Create tag v2.0.0

    release/v17.x->>NuGet: 9. Build & publish package

    NuGet->>NuGet: Package v2.0.0 available
```

**Workflow:**
1. Branch `release/v17.x` from `main`
2. Update all Umbraco dependencies to version 17
3. Update package version to 2.0.0
4. Update documentation
5. Thoroughly test with Umbraco 17
6. Push branch and create initial release

## Version Timeline

```mermaid
gantt
    title BulkUpload Multi-Version Release Timeline
    dateFormat YYYY-MM-DD
    section Umbraco 13 (v1.x.x)
    v1.0.0 Initial Release    :milestone, m1, 2025-01-15, 0d
    v1.1.0 New Features        :milestone, m2, 2025-04-15, 0d
    v1.2.0 More Features       :milestone, m3, 2025-07-15, 0d
    v1.3.0 Advanced Features   :milestone, m4, 2025-10-15, 0d
    v1.3.1 Bug Fixes           :milestone, m5, 2026-01-15, 0d

    section Umbraco 17 (v2.x.x)
    v2.0.0 Initial Release    :milestone, m6, 2025-04-15, 0d
    v2.1.0 Parity with v1.1   :milestone, m7, 2025-07-15, 0d
    v2.2.0 Advanced Features  :milestone, m8, 2025-10-15, 0d
    v2.2.1 Bug Fixes          :milestone, m9, 2026-01-15, 0d
```

**Release Strategy:**
- **v1.x.x**: Umbraco 13 support (ongoing)
- **v2.x.x**: Umbraco 17 support (starts Q2 2025)
- Features developed in `main` are cherry-picked to both versions
- Each version releases independently based on need

## Cherry-Pick Workflow

```mermaid
gitGraph
    commit id: "A"
    commit id: "B"

    branch release/v13.x
    cherry-pick id: "A"
    cherry-pick id: "B"
    commit id: "C (fix)" tag: "Fix merged here"

    checkout main
    cherry-pick id: "C (fix)" tag: "C' (cherry-picked)"

    branch release/v17.x
    cherry-pick id: "C (fix)" tag: "C'' (cherry-picked)"

    checkout main
    commit id: "D"
```

**Understanding Cherry-Pick:**
- Same fix, different commit hashes
- Commit `C` in `release/v13.x` becomes:
  - Commit `C'` in `main`
  - Commit `C''` in `release/v17.x`
- All contain the same logical changes

## Release Decision Tree

```mermaid
flowchart TD
    Start([Need to make a change?]) --> ChangeType{What type of change?}

    ChangeType -->|New Feature| DevMain[Develop in main branch]
    ChangeType -->|Bug Fix| WhichBranch{Which branch affected?}
    ChangeType -->|Version-Specific| DevRelease[Develop in release/vXX.x]

    DevMain --> MergeMain[Create PR to main]
    MergeMain --> Cherry1[Cherry-pick to release branches]

    WhichBranch -->|v13.x| DevV13[Develop in release/v13.x]
    WhichBranch -->|v17.x| DevV17[Develop in release/v17.x]
    WhichBranch -->|main| DevMainBug[Develop in main]

    DevV13 --> MergeV13[Create PR to release/v13.x]
    DevV17 --> MergeV17[Create PR to release/v17.x]
    DevMainBug --> MergeMainBug[Create PR to main]

    MergeV13 --> ForwardPort1[Cherry-pick to main & v17.x]
    MergeV17 --> ForwardPort2[Cherry-pick to main & v13.x]
    MergeMainBug --> Cherry2[Cherry-pick to release branches]

    DevRelease --> MergeRelease[Create PR to release/vXX.x only]

    Cherry1 --> Version[Bump version]
    ForwardPort1 --> Version
    ForwardPort2 --> Version
    Cherry2 --> Version
    MergeRelease --> Version

    Version --> Changelog[Update CHANGELOG]
    Changelog --> Tag[Create tag]
    Tag --> Publish[Publish to NuGet]
    Publish --> Done([Complete])

    style Start fill:#e1f5ff
    style Done fill:#c8e6c9
    style DevMain fill:#fff9c4
    style DevRelease fill:#fff9c4
    style Publish fill:#ffccbc
```

**Decision Guide:**
1. **New Feature** → Develop in `main`, cherry-pick to releases
2. **Bug Fix** → Fix in affected branch, forward-port to others
3. **Version-Specific** → Develop in specific release branch only

## Conflict Resolution Flow

```mermaid
flowchart TD
    Start([git cherry-pick commit]) --> HasConflict{Conflicts?}

    HasConflict -->|No| Success[Success!]
    Success --> Push[git push]
    Push --> End([Complete])

    HasConflict -->|Yes| OpenFiles[Open conflicted files in editor]
    OpenFiles --> FindMarkers[Look for conflict markers:<br/><<<<<<< =======  >>>>>>>]
    FindMarkers --> ResolveConflicts[Manually resolve conflicts<br/>Keep appropriate changes for this branch]
    ResolveConflicts --> StageFiles[git add resolved-files]
    StageFiles --> ContinueOrAbort{Continue or Abort?}

    ContinueOrAbort -->|Continue| Continue[git cherry-pick --continue]
    ContinueOrAbort -->|Abort| Abort[git cherry-pick --abort]

    Continue --> Test[Review & Test<br/>dotnet build<br/>dotnet test]
    Test --> TestPass{Tests Pass?}

    TestPass -->|Yes| Push
    TestPass -->|No| Amend[Make adjustments<br/>git add files<br/>git commit --amend --no-edit]
    Amend --> Test

    Abort --> Manual[Create manual port<br/>git checkout -b fix/manual-port<br/>Manually apply changes]
    Manual --> End

    style Start fill:#e1f5ff
    style End fill:#c8e6c9
    style HasConflict fill:#fff9c4
    style TestPass fill:#fff9c4
    style Success fill:#c8e6c9
    style Abort fill:#ffccbc
```

**Conflict Resolution Steps:**
1. Run `git cherry-pick <commit-hash>`
2. If conflicts occur:
   - Open files and look for `<<<<<<<`, `=======`, `>>>>>>>` markers
   - Resolve conflicts manually
   - Stage files with `git add`
   - Continue with `git cherry-pick --continue`
3. Test the changes
4. If tests pass, push
5. If you can't resolve, abort and manually port the changes

## Complete Development Workflow

```mermaid
flowchart LR
    subgraph Development
        Dev[Developer] --> Feature[Create Feature Branch<br/>from main or release/vXX.x]
        Feature --> Code[Write Code]
        Code --> Commit[Commit Changes<br/>Use Conventional Commits]
        Commit --> Push[Push Branch]
    end

    subgraph Review
        Push --> PR[Create Pull Request]
        PR --> Review[Code Review]
        Review --> Approve{Approved?}
        Approve -->|No| Code
        Approve -->|Yes| Merge[Merge PR]
    end

    subgraph Distribution
        Merge --> Cherry[Cherry-pick to<br/>other branches if needed]
        Cherry --> Test[Test in each version]
        Test --> Release[Release new version]
        Release --> NuGet[(NuGet Package)]
    end

    style Dev fill:#e1f5ff
    style Feature fill:#fff9c4
    style Merge fill:#c8e6c9
    style NuGet fill:#ffccbc
```

**Complete Workflow:**
1. **Development**: Create branch, write code, commit, push
2. **Review**: Open PR, get approval, merge
3. **Distribution**: Cherry-pick as needed, test, release

---

## Viewing These Diagrams

These Mermaid diagrams render automatically on:
- ✅ GitHub
- ✅ GitLab
- ✅ Visual Studio Code (with Mermaid extension)
- ✅ Many markdown preview tools

If your viewer doesn't support Mermaid, you can:
- View on GitHub: https://github.com/ClerksWell-Ltd/BulkUpload
- Use the [Mermaid Live Editor](https://mermaid.live/)
- Install a Mermaid preview extension for your editor
