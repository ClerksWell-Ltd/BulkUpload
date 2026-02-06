# Workflow Diagrams

This document uses [Mermaid](https://mermaid.js.org/) diagrams for better visualization. These diagrams render automatically on GitHub, GitLab, and many markdown viewers.

## Branch Structure

```mermaid
gitGraph
    commit id: "Initial commit"
    commit id: "v2.0.0 - Multi-targeting"

    branch feature/new-validator
    commit id: "Add validation logic"
    commit id: "Add tests"
    checkout main
    merge feature/new-validator tag: "v2.1.0"

    branch bugfix/csv-parsing
    commit id: "Fix CSV parser"
    checkout main
    merge bugfix/csv-parsing tag: "v2.1.1"

    commit id: "More features"
```

**Key Points:**
- `main` is the only long-lived branch
- Feature branches are created from `main` and merged back
- Releases are tagged directly on `main`
- Each release contains both net8.0 (Umbraco 13) and net10.0 (Umbraco 17)

## Feature Development Flow

```mermaid
sequenceDiagram
    actor Developer
    participant main
    participant NuGet

    Developer->>main: 1. Create feature/new-csv-validation branch
    Developer->>Developer: 2. Develop & commit changes
    Developer->>Developer: 3. Test with both net8.0 and net10.0
    Developer->>main: 4. Create PR (feature → main)
    main->>main: 5. Merge PR

    Note over main,NuGet: Release when ready

    Developer->>main: 6. Update version & CHANGELOG
    Developer->>main: 7. Create GitHub Release (tag v2.2.0)
    main->>main: 8. GitHub Actions build & test
    main->>NuGet: 9. Publish package (net8.0 + net10.0)
```

**Workflow:**
1. Create feature branch from `main`
2. Develop and commit changes
3. Test with both Umbraco 13 and 17
4. Create PR targeting `main`
5. After PR is merged, feature is ready for next release
6. When ready to release, update version and create GitHub Release
7. Automated workflow publishes to NuGet

## Bug Fix Flow

```mermaid
sequenceDiagram
    actor Developer
    participant main
    participant NuGet

    Developer->>main: 1. Create bugfix/csv-parsing branch
    Developer->>Developer: 2. Fix bug & commit
    Developer->>Developer: 3. Test with both frameworks
    Developer->>main: 4. Create PR (bugfix → main)
    main->>main: 5. Merge PR

    Note over main,NuGet: Hotfix release

    Developer->>main: 6. Update version (patch bump)
    Developer->>main: 7. Create GitHub Release (tag v2.1.2)
    main->>main: 8. GitHub Actions build & test
    main->>NuGet: 9. Publish hotfix (net8.0 + net10.0)
```

**Workflow:**
1. Create bugfix branch from `main`
2. Fix the bug
3. Test with both frameworks
4. Create PR targeting `main`
5. After merge, create hotfix release if urgent
6. Bump patch version (e.g., 2.1.1 → 2.1.2)
7. Create GitHub Release to trigger automated publishing

## Release Process

```mermaid
flowchart TD
    Start([Ready to Release]) --> UpdateVersion[Update version in .csproj]
    UpdateVersion --> UpdateChangelog[Update CHANGELOG.md]
    UpdateChangelog --> Commit[Commit: chore: prepare release v2.2.0]
    Commit --> Push[Push to main]
    Push --> CreateRelease[Create GitHub Release]

    CreateRelease --> ReleaseWorkflow{GitHub Actions}
    ReleaseWorkflow --> ValidateBranch[Validate: main branch]
    ValidateBranch --> SetupDotNet[Setup .NET 8 + 10]
    SetupDotNet --> SetupNode[Setup Node.js for V17 frontend]
    SetupNode --> BuildFrontend[Build V17 frontend]
    BuildFrontend --> RestoreDeps[Restore dependencies]
    RestoreDeps --> Build[Build for net8.0 and net10.0]

    Build --> BuildSuccess{Build OK?}
    BuildSuccess -->|No| BuildFail[❌ Fail workflow]
    BuildSuccess -->|Yes| RunTests[Run tests]

    RunTests --> TestSuccess{Tests OK?}
    TestSuccess -->|No| TestFail[❌ Fail workflow]
    TestSuccess -->|Yes| Pack[Pack NuGet package]

    Pack --> Publish[Publish to NuGet.org]
    Publish --> UploadArtifact[Upload package artifact]
    UploadArtifact --> Done([✅ Release Complete])

    style Start fill:#e1f5ff
    style Done fill:#c8e6c9
    style BuildFail fill:#ffccbc
    style TestFail fill:#ffccbc
```

**Automated Release Steps:**
1. Prepare: Update version and changelog locally
2. Commit and push to `main`
3. Create GitHub Release with tag
4. GitHub Actions automatically:
   - Validates release is from `main`
   - Builds for both frameworks
   - Runs all tests
   - Creates NuGet package
   - Publishes to NuGet.org

## Version Timeline

```mermaid
gantt
    title BulkUpload Release Timeline
    dateFormat YYYY-MM-DD
    section Multi-Targeting Era
    v2.0.0 Initial MT Release    :milestone, m1, 2025-01-15, 0d
    v2.1.0 New Features           :milestone, m2, 2025-04-15, 0d
    v2.1.1 Bug Fix                :milestone, m3, 2025-05-01, 0d
    v2.2.0 More Features          :milestone, m4, 2025-07-15, 0d
    v2.3.0 Advanced Features      :milestone, m5, 2025-10-15, 0d
```

**Release Strategy:**
- All releases from v2.0.0+ support both Umbraco 13 and 17
- Single version number for both frameworks
- Regular feature releases (minor version bumps)
- Hotfix releases as needed (patch version bumps)

## Framework-Specific Code Handling

```mermaid
flowchart TD
    Start([Writing Code]) --> NeedsFrameworkSpecific{Needs different<br/>code for U13 vs U17?}

    NeedsFrameworkSpecific -->|No| SharedCode[Write shared code<br/>Works for both frameworks]
    NeedsFrameworkSpecific -->|Yes| UseConditional[Use conditional compilation]

    UseConditional --> AddDirective["Add #if NET8_0 / #elif NET10_0"]
    AddDirective --> WriteU13["Write Umbraco 13 code<br/>(NET8_0 block)"]
    WriteU13 --> WriteU17["Write Umbraco 17 code<br/>(NET10_0 block)"]

    SharedCode --> Test[Test with both frameworks]
    WriteU17 --> Test

    Test --> BuildTest[dotnet build & dotnet test]
    BuildTest --> TestBoth{Both frameworks OK?}

    TestBoth -->|No| FixIssues[Fix issues]
    FixIssues --> Test
    TestBoth -->|Yes| Done([✅ Code Ready])

    style Start fill:#e1f5ff
    style Done fill:#c8e6c9
    style NeedsFrameworkSpecific fill:#fff9c4
```

**Best Practice:** Minimize framework-specific code. Most business logic should work for both Umbraco versions.

## Decision Tree: What Branch Do I Use?

```mermaid
flowchart TD
    Start([Need to make a change?]) --> ChangeType{What type of change?}

    ChangeType -->|New Feature| CreateFeature[Create feature/my-feature from main]
    ChangeType -->|Bug Fix| CreateBugfix[Create bugfix/issue-name from main]
    ChangeType -->|Hotfix| CreateHotfix[Create hotfix/critical-bug from main]
    ChangeType -->|Documentation| CreateDocs[Create docs/update-readme from main]

    CreateFeature --> Develop[Develop & test]
    CreateBugfix --> Develop
    CreateHotfix --> Develop
    CreateDocs --> Develop

    Develop --> CreatePR[Create PR to main]
    CreatePR --> Review{Approved?}

    Review -->|No| FixReview[Address feedback]
    FixReview --> CreatePR

    Review -->|Yes| Merge[Merge to main]

    Merge --> NeedRelease{Need release now?}
    NeedRelease -->|No| WaitRelease[Wait for next release cycle]
    NeedRelease -->|Yes| Release[Create GitHub Release]

    WaitRelease --> Done([Complete])
    Release --> Done

    style Start fill:#e1f5ff
    style Done fill:#c8e6c9
    style ChangeType fill:#fff9c4
    style Review fill:#fff9c4
    style NeedRelease fill:#fff9c4
```

**Simple Rule:** All work happens in feature branches created from `main` and merged back to `main`.

## Complete Development Workflow

```mermaid
flowchart LR
    subgraph Development
        Dev[Developer] --> Feature[Create Branch<br/>from main]
        Feature --> Code[Write Code]
        Code --> LocalTest[Local Test<br/>net8.0 + net10.0]
        LocalTest --> Commit[Commit Changes<br/>Use Conventional Commits]
        Commit --> Push[Push Branch]
    end

    subgraph Review
        Push --> PR[Create Pull Request<br/>to main]
        PR --> CI[CI: Build & Test]
        CI --> CodeReview[Code Review]
        CodeReview --> Approve{Approved?}
        Approve -->|No| Code
        Approve -->|Yes| Merge[Merge PR]
    end

    subgraph Release
        Merge --> ReadyToRelease{Ready for<br/>release?}
        ReadyToRelease -->|Not yet| WaitForMore[Wait for more changes]
        ReadyToRelease -->|Yes| PrepRelease[Update version<br/>& CHANGELOG]
        PrepRelease --> CreateRelease[Create GitHub Release]
        CreateRelease --> AutomatedCI[Automated:<br/>Build, Test, Publish]
        AutomatedCI --> NuGet[(NuGet Package<br/>net8.0 + net10.0)]
    end

    style Dev fill:#e1f5ff
    style Feature fill:#fff9c4
    style Merge fill:#c8e6c9
    style NuGet fill:#ffccbc
```

**Complete Workflow:**
1. **Development**: Create branch, write code, test locally, commit, push
2. **Review**: Open PR, pass CI, get approval, merge to main
3. **Release**: When ready, update version, create GitHub Release, automated publishing

## Multi-Targeting Build Flow

```mermaid
flowchart TD
    Start([dotnet build]) --> ParseProject[Parse .csproj<br/>TargetFrameworks: net8.0;net10.0]

    ParseProject --> BuildNet8[Build for net8.0]
    ParseProject --> BuildNet10[Build for net10.0]

    BuildNet8 --> RestoreNet8[Restore Umbraco 13 packages]
    BuildNet10 --> RestoreNet10[Restore Umbraco 17 packages]

    RestoreNet8 --> CompileNet8[Compile with NET8_0 defined]
    RestoreNet10 --> CompileNet10[Compile with NET10_0 defined]

    CompileNet8 --> BuildFrontendV13[Copy ClientV13 to wwwroot]
    CompileNet10 --> BuildFrontendV17[Build ClientV17 with Vite]

    BuildFrontendV13 --> OutputNet8[Output: bin/net8.0/BulkUpload.dll]
    BuildFrontendV17 --> OutputNet10[Output: bin/net10.0/BulkUpload.dll]

    OutputNet8 --> Pack[dotnet pack]
    OutputNet10 --> Pack

    Pack --> Package[(NuGet Package<br/>lib/net8.0/BulkUpload.dll<br/>lib/net10.0/BulkUpload.dll)]

    style Start fill:#e1f5ff
    style Package fill:#c8e6c9
```

**Build Process:**
- Single `dotnet build` command builds for both frameworks
- Each framework gets its own dependencies and frontend assets
- Final NuGet package contains both framework versions

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
