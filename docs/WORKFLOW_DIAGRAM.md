# Workflow Diagrams

## Branch Structure

```
┌─────────────────────────────────────────────────────────────┐
│                           main                               │
│                  (development branch)                        │
│                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │  feature/   │  │  feature/   │  │  feature/   │        │
│  │   new-api   │  │  validation │  │   export    │        │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘        │
│         └────────────────┴────────────────┘                │
└─────────────────┬─────────────────┬─────────────────┬──────┘
                  │                 │                 │
                  │ cherry-pick     │ cherry-pick     │ cherry-pick
                  │                 │                 │
         ┌────────▼────────┐ ┌──────▼──────┐ ┌───────▼──────┐
         │  release/v13.x  │ │release/v16.x│ │release/v17.x │
         │                 │ │             │ │              │
         │  Umbraco 13     │ │ Umbraco 16  │ │ Umbraco 17   │
         │  Package v1.x.x │ │Package v2.x.x│ │Package v3.x.x│
         └────────┬────────┘ └──────┬──────┘ └───────┬──────┘
                  │                 │                 │
            ┌─────▼─────┐     ┌─────▼─────┐   ┌──────▼─────┐
            │   v1.0.0  │     │   v2.0.0  │   │   v3.0.0   │
            │   v1.1.0  │     │   v2.1.0  │   │   v3.1.0   │
            │   v1.2.0  │     │   v2.2.0  │   │   v3.2.0   │
            └───────────┘     └───────────┘   └────────────┘
```

## Feature Development Flow

```
Developer          main            release/v13.x    release/v16.x
    │                │                    │                │
    │ 1. Create      │                    │                │
    │   feature      │                    │                │
    │   branch       │                    │                │
    ├───────────────►│                    │                │
    │                │                    │                │
    │ 2. Develop     │                    │                │
    │   & commit     │                    │                │
    │◄───────────────┤                    │                │
    │                │                    │                │
    │ 3. Create PR   │                    │                │
    ├───────────────►│                    │                │
    │                │                    │                │
    │                │ 4. Merge to main   │                │
    │                │                    │                │
    │                │                    │                │
    │                │ 5. Cherry-pick     │                │
    │                ├───────────────────►│                │
    │                │                    │                │
    │                │ 6. Cherry-pick     │                │
    │                ├────────────────────┼───────────────►│
    │                │                    │                │
    │                │                    │ 7. Tag & Release
    │                │                    ├───► v1.x.x     │
    │                │                    │                │
    │                │                    │                ├───► v2.x.x
    │                │                    │                │
```

## Bug Fix Flow (Specific Version)

```
Developer      release/v13.x        main        release/v16.x
    │                │                │                │
    │ 1. Create      │                │                │
    │   bugfix       │                │                │
    │   branch       │                │                │
    ├───────────────►│                │                │
    │                │                │                │
    │ 2. Fix bug     │                │                │
    │◄───────────────┤                │                │
    │                │                │                │
    │ 3. PR to       │                │                │
    │   release      │                │                │
    ├───────────────►│                │                │
    │                │                │                │
    │                │ 4. Merge       │                │
    │                │                │                │
    │                │                │                │
    │                │ 5. Forward-    │                │
    │                │    port to     │                │
    │                │    main        │                │
    │                ├───────────────►│                │
    │                │                │                │
    │                │                │ 6. Cherry-pick │
    │                │                │    to v16.x    │
    │                │                ├───────────────►│
    │                │                │                │
    │                │ 7. Release     │                │
    │                ├───► v1.x.y     │                ├───► v2.x.y
    │                │                │                │
```

## New Umbraco Version Support Flow

```
Developer          main          release/v16.x       NuGet
    │                │                  │              │
    │ 1. Branch      │                  │              │
    │   from main    │                  │              │
    ├───────────────►├─────────────────►│              │
    │                │                  │              │
    │ 2. Update      │                  │              │
    │   dependencies │                  │              │
    │   to Umbraco   │                  │              │
    │   16.x         │                  │              │
    │◄───────────────┼──────────────────┤              │
    │                │                  │              │
    │ 3. Update      │                  │              │
    │   version to   │                  │              │
    │   2.0.0        │                  │              │
    │◄───────────────┼──────────────────┤              │
    │                │                  │              │
    │ 4. Test with   │                  │              │
    │   Umbraco 16   │                  │              │
    │                │                  │              │
    │                │                  │              │
    │ 5. Commit &    │                  │              │
    │   push branch  │                  │              │
    ├───────────────►│                  │              │
    │                │                  │              │
    │                │                  │              │
    │ 6. Tag v2.0.0  │                  │              │
    │                ├─────────────────►│              │
    │                │                  │              │
    │                │                  │ 7. Build &   │
    │                │                  │    publish   │
    │                │                  ├─────────────►│
    │                │                  │              │
```

## Version Timeline

```
Timeline    Umbraco 13          Umbraco 16          Umbraco 17
            (v1.x.x)            (v2.x.x)            (v3.x.x)
───────────────────────────────────────────────────────────────

2025-Q1     v1.0.0 ●────┐
            Initial      │
            Release      │
                        │
2025-Q2     v1.1.0 ●    │      v2.0.0 ●────┐
            New         │      Initial      │
            Features    │      Release      │
                        │                   │
2025-Q3     v1.2.0 ●    │      v2.1.0 ●     │      v3.0.0 ●
            More        │      Parity       │      Initial
            Features    │      with v1.1    │      Release
                        │                   │
2025-Q4     v1.3.0 ●    │      v2.2.0 ●     │      v3.1.0 ●
            Advanced    │      Advanced     │      Parity
            Features    │      Features     │      with v2.1
                        │                   │
2026-Q1     v1.3.1 ●    │      v2.2.1 ●     │      v3.1.1 ●
            Bug Fixes   │      Bug Fixes    │      Bug Fixes
                        │                   │
───────────────────────────────────────────────────────────────

Legend:
  ●  Release
  ─  Active development
```

## Cherry-Pick Workflow

```
Scenario: Bug fix in release/v13.x needs to go to other branches

Step 1: Fix merged to release/v13.x
┌─────────────────┐
│ release/v13.x   │
│   ●─────●───●   │  ← Commits A, B, C (C is the fix)
└─────────────────┘

Step 2: Cherry-pick to main
┌─────────────────┐
│ main            │
│   ●─────●───●'  │  ← Commit C' (cherry-picked from C)
└─────────────────┘
        ▲
        │ git cherry-pick C
        │

Step 3: Cherry-pick to release/v16.x
┌─────────────────┐
│ release/v16.x   │
│   ●─────●───●'' │  ← Commit C'' (cherry-picked from C)
└─────────────────┘
        ▲
        │ git cherry-pick C
        │

Step 4: Cherry-pick to release/v17.x
┌─────────────────┐
│ release/v17.x   │
│   ●─────●───●'''│  ← Commit C''' (cherry-picked from C)
└─────────────────┘
        ▲
        │ git cherry-pick C
        │

Note: C, C', C'', C''' contain the same logical change
      but may have different commit hashes
```

## Release Decision Tree

```
                    ┌─────────────────┐
                    │  Need to make   │
                    │   a change?     │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ What type of    │
                    │    change?      │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
┌───────▼──────┐    ┌────────▼────────┐   ┌──────▼───────┐
│ New Feature  │    │    Bug Fix      │   │ Version-     │
│              │    │                 │   │ Specific     │
└───────┬──────┘    └────────┬────────┘   └──────┬───────┘
        │                    │                    │
        │                    │                    │
┌───────▼──────────────┐    │            ┌───────▼────────┐
│ Develop in main      │    │            │ Develop in     │
│                      │    │            │ release/vXX.x  │
└───────┬──────────────┘    │            └───────┬────────┘
        │                   │                    │
┌───────▼──────────────┐    │            ┌───────▼────────┐
│ Merge to main        │    │            │ Merge to       │
│                      │    │            │ release/vXX.x  │
└───────┬──────────────┘    │            └───────┬────────┘
        │                   │                    │
        │           ┌───────▼────────┐          │
        │           │ Which branch?  │          │
        │           └───────┬────────┘          │
        │                   │                   │
        │      ┌────────────┼────────┐         │
        │      │            │        │         │
        │  ┌───▼───┐   ┌────▼───┐ ┌─▼──┐     │
        │  │ v13.x │   │  v16.x │ │main│     │
        │  └───┬───┘   └────┬───┘ └─┬──┘     │
        │      │            │       │         │
        └──────┼────────────┼───────┘         │
               │            │                 │
       ┌───────▼────────────▼─────────────────▼────┐
       │ Cherry-pick to other branches if needed   │
       └───────────────────┬───────────────────────┘
                           │
                  ┌────────▼────────┐
                  │ Bump version    │
                  │ Update CHANGELOG│
                  └────────┬────────┘
                           │
                  ┌────────▼────────┐
                  │ Create tag      │
                  └────────┬────────┘
                           │
                  ┌────────▼────────┐
                  │ Publish to      │
                  │ NuGet           │
                  └─────────────────┘
```

## Conflict Resolution Flow

```
During Cherry-Pick:

git cherry-pick <commit>
        │
        ▼
   ┌─────────┐
   │Conflicts?│
   └────┬────┘
        │
    Yes │ No
        │  │
        │  └──────────► Success! ──► git push
        │
        ▼
  ┌──────────────┐
  │ Resolve in   │
  │   editor     │
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │  git add     │
  │  <files>     │
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │git cherry-   │
  │pick          │
  │--continue    │
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │  Review &    │
  │  Test        │
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │  git push    │
  └──────────────┘
```
