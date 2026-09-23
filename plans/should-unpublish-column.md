# Add a `bulkUploadShouldUnpublish` reserved column to BulkUpload

Repo: `D:\Code\GitHub\BulkUpload` (Umbraco.Community.BulkUpload, currently 2.1.0)
Target release: **2.2.0** · Branch: **`fix/should-unpublish-column`** off `main`

---

## Context

Importing a case report into PMCPA through the document importer takes the live page down. The
importer deliberately writes `bulkUploadShouldPublish=false` so imported changes land as a draft for
review (`ClerksWell.DocumentImporter/Csv/CsvImportSafety.cs:42`), but BulkUpload reads "do not
publish" as "actively unpublish":

```csharp
// src/BulkUpload/Services/ImportUtilityService.cs:434-442
else
{
    var published = contentItem.Published;
    _contentService.Save(contentItem);
    if (published)
    {
        _contentService.Unpublish(contentItem);   // takes the live page down
    }
}
```

On a create this branch is a no-op (`Published` is already false), so it only bites updates of live
pages — and there is no way to ask for an unpublish explicitly, nor to update a published page
without taking it down.

Fixing that one branch is not enough on its own, because the three flags are currently tangled:
`bulkUploadShouldPublish` doubles as a row filter, `bulkUploadShouldUpdate=false` silently discards a
requested publish, and unpublishing is only reachable as a side effect. This release separates them
into three independent instructions and adds the missing one.

In Umbraco 13 and 17 alike, `IContentService.Save()` on a published node saves a draft and leaves the
published version serving, so simply not calling `Unpublish` gives the wanted behaviour.

---

## The model

Three columns, three separate jobs:

| Column | Governs |
|---|---|
| `bulkUploadShouldUpdate` | **Whether data is written** — name, parent, and property values |
| `bulkUploadShouldPublish` | **Publishing**, and nothing else |
| `bulkUploadShouldUnpublish` | **Unpublishing**, and nothing else. **New.** |

Rules:

- `shouldUpdate` is the **only** data gate. For a row targeting existing content, no truthy
  `shouldUpdate` means the node's data is not touched, whatever else the row says.
- `shouldPublish` and `shouldUnpublish` act **independently of `shouldUpdate`**, so a row can publish
  or unpublish existing content without changing it.
- When both `shouldPublish` and `shouldUnpublish` are truthy, **unpublish wins**; log a warning
  naming the row.
- Absent or falsy on any of the three means the same thing: don't do that job. Truthy parsing is
  unchanged and shared across all three — `true`, `yes`, `1`, after `Trim().ToLowerInvariant()`.
- A row that would do none of the three jobs is a no-op and is **skipped silently**, exactly as
  skipped rows behave today: no `ContentImportResult`, absent from `TotalCount`, the API response and
  the results CSV.

### Truth table — this is the table to put in the docs, verbatim and without any comparison to older versions

**Existing content** — the row carries a `bulkUploadContentGuid` that resolves to a node.

| `bulkUploadShouldUpdate` | `bulkUploadShouldPublish` | `bulkUploadShouldUnpublish` | Data | Publish state |
|---|---|---|---|---|
| true | false | false | Updated | Unchanged |
| true | true | false | Updated | Published |
| true | false | true | Updated | Unpublished |
| true | true | true | Updated | Unpublished |
| false | false | false | Unchanged | Unchanged (row skipped) |
| false | true | false | Unchanged | Published |
| false | false | true | Unchanged | Unpublished |
| false | true | true | Unchanged | Unpublished |

**New content** — the row has no `bulkUploadContentGuid`.

| `bulkUploadShouldPublish` | `bulkUploadShouldUnpublish` | Result |
|---|---|---|
| false | false | Created, saved as a draft |
| true | false | Created and published |
| false | true | Created, saved as a draft |
| true | true | Created, saved as a draft |

In both tables **false means the column is absent or holds any non-truthy value** — they are treated
identically. Say that in a line under each table rather than adding a third state to the columns.

### The one asymmetry, which the docs must state plainly

For a **new content** row, `bulkUploadShouldUpdate` present with a falsy value **skips the row
entirely** — nothing is created. This is the existing "don't process this row" marker and is kept.
It is the only place where an absent column and a falsy column differ: absent means "create normally",
falsy means "skip me". Document it as its own short note directly beneath the new-content table, not
as an extra column.

### Consequences to carry into the changelog (not into the docs tables)

1. Updating existing published content without `bulkUploadShouldPublish=true` no longer unpublishes
   it. The changes save as a draft and the live version keeps serving.
2. `bulkUploadShouldUpdate` is now the only thing that writes data. A row with a
   `bulkUploadContentGuid` and `bulkUploadShouldPublish=true` but no truthy `bulkUploadShouldUpdate`
   now publishes **without** applying its property values; previously it applied them.
3. `bulkUploadShouldUpdate=false` on a row with a GUID no longer discards a requested publish. It
   blocks the data write only.
4. New content rows with `bulkUploadShouldPublish=false` are now created as drafts. Previously the
   whole row was skipped and nothing was created.
5. New column `bulkUploadShouldUnpublish`.

Point 2 is the sharpest edge. Mitigate it with a warning log on any row that has a
`bulkUploadContentGuid` and property columns but no truthy `bulkUploadShouldUpdate`:
"properties present but bulkUploadShouldUpdate is not true — data not written".

---

## Implementation

### Save, publish and unpublish

Replace `ImportUtilityService.cs:423-442` with, in effect:

```csharp
var isExisting = importObject.BulkUploadContentGuid.HasValue;
var writeData  = !isExisting || importObject.BulkUploadShouldUpdate;
var unpublish  = importObject.BulkUploadShouldUnpublish;
var publish    = !unpublish && importObject.BulkUploadShouldPublish;

if (publish)
{
#if NET8_0
    _contentService.SaveAndPublish(contentItem);      // saves as part of publishing
#else
    if (writeData) _contentService.Save(contentItem);
    _contentService.Publish(contentItem, Array.Empty<string>());
#endif
}
else
{
    if (writeData) _contentService.Save(contentItem);
    if (unpublish && contentItem.Published) _contentService.Unpublish(contentItem);
}
```

- `Unpublish(IContent, string culture = "*", int userId = -1)` has an identical signature in Umbraco
  13 and 17, so the unpublish path needs **no `#if` split** — only the pre-existing publish path does.
- On net8.0 `SaveAndPublish` is the only way to publish, so a publish-without-data-change row saves an
  unmodified item before publishing. Harmless; note it in a code comment.
- Unpublishing a node that is not published is skipped with a debug log, not an error.
- `Publish` and `Unpublish` return a `PublishResult` that is currently discarded. Capture it: on
  `!Success`, log a warning and put `PublishResult.Result` into `BulkUploadInfoMessage`. **Do not**
  flip `BulkUploadSuccess` — the save did succeed, and changing that would alter existing publish
  behaviour beyond this release's scope.

### Gating the data writes

`shouldUpdate` currently is never read inside `ImportSingleItem`; the decision is made upstream by
skipping rows. It must now gate every data write for an existing node:

- the rename at lines 268-272
- the parent move at lines 275-314 (`bulkUploadParentGuid` moving a node is a data change)
- the property `SetValue` loop at lines 389-396
- the deferred-resolver loop at lines 399-421

Create rows always write data. Revisit the `changesApplied` / "No properties were updated" message at
lines 471-476 so it does not fire misleadingly on a deliberate publish-only row.

### Code touch-list

| File | Change |
|---|---|
| `src/BulkUpload/Constants/ReservedColumns.cs` | Add `BulkUploadShouldUnpublish = "bulkUploadShouldUnpublish"` **and add it to the `All` set** — `All` is what stops the column being mapped onto a document property at `ImportUtilityService.cs:185`. Correct XML doc comment. |
| `src/BulkUpload/Models/ImportObject.cs` | Add `BulkUploadShouldUnpublish` + `BulkUploadShouldUnpublishColumnExisted`. Extend `CanImport` (lines 114-115) to `(guid && (shouldUpdate \|\| shouldPublish \|\| shouldUnpublish)) \|\| (name && docType)`, otherwise an unpublish-only CSV with blank name and docTypeAlias is silently dropped. While here: the XML comments on `BulkUploadShouldPublish` (lines 62-83) are copy-pasted from the update pair and describe the wrong thing; `ShouldPublish` (line 67) is dead with zero references — mark it `[Obsolete]` rather than removing it, since it is public and this is a minor release. |
| `src/BulkUpload/Models/ContentImportResult.cs` | Add the same two properties. |
| `src/BulkUpload/Services/ImportUtilityService.cs` | New parse block in `CreateImportObject` cloning the tolerant `shouldPublishKey` scan at lines 99-114 — `Keys.FirstOrDefault(k => k.Split('\|')[0].Equals(..., OrdinalIgnoreCase))`, **not** the exact-key `TryGetValue` the GUID columns use, so `bulkUploadShouldUnpublish\|text` and odd casing both work. Assign in the initializer at 155-168. Rewrite the save block and add the data gating as above. Echo the two new flags at **all six** result-construction sites: lines 249, 289, 335, 372, 479, 497. |
| `src/BulkUpload/Services/IImportUtilityService.cs` | `ImportSingleItem` gains an optional `bool unpublish = false` after `publish`, keeping the signature source-compatible for external callers. |
| `src/BulkUpload/Services/BulkImportService.cs` | Replace both existing skip rules (lines 173-188) with the single no-op rule: skip a row with a GUID when none of the three flags is truthy; skip a create row when the `shouldUpdate` column is present and falsy. Pass the new flag at line 211. Fix the skip summary log at line 198, which currently always claims `bulkUploadShouldUpdate was false`. The file-level mode detection at 146-157 is logging only — update its wording. |
| `src/BulkUpload/Controllers/ImportController.cs` | Results CSV: `hadShouldUnpublishColumn` detection near line 354, header near 377, row value near 408, mirroring `hadShouldPublishColumn`. The `StartsWith("bulkUpload")` filter at line 341 already keeps the new column out of the echoed original columns. Update the Swagger XML docs at lines 69-107 and 244-261. |

### Client-side CSV mode detection — three copies, all must change

`hasContentUpdateHeaders` accepts a CSV only when it has `bulkuploadcontentguid` **and**
(`bulkuploadshouldupdate` or `bulkuploadshouldpublish`). Add `bulkuploadshouldunpublish` to that
disjunction, or an unpublish-only CSV is rejected or misrouted to the media pipeline before it reaches
the server.

- `src/BulkUpload/ClientV17/src/utils/file.utils.ts:170-174`
- `src/BulkUpload/ClientV13/BulkUpload/utils/fileUtils.js:155-159`
- `src/BulkUpload/wwwroot/BulkUpload/utils/fileUtils.js` — **tracked generated copy** of the V13 file.
  The build regenerates it, but it is in git, so commit it too.

### In-product dashboard docs — two sources plus one generated copy

Add a `req-item` card for the new column beside the existing `bulkUploadShouldUpdate` one:

- `src/BulkUpload/ClientV13/BulkUpload/bulkUploadDashboard.html` (~lines 303-400)
- `src/BulkUpload/wwwroot/BulkUpload/bulkUploadDashboard.html` (tracked generated copy)
- `src/BulkUpload/ClientV17/src/components/bulk-upload-dashboard.element.ts` (~lines 486-580)

---

## Documentation

The truth tables above go in **verbatim and clean** — the two tables, the "false means absent or
non-truthy" line, and the new-content `shouldUpdate` note. No "previously", no "changed in 2.2.0", no
comparison column. Version history belongs in the changelog alone.

| File | Change |
|---|---|
| `.github/docs/user-guides/UPDATE_MODE_GUIDE.md` | The primary home for this. Add the new column to the field tables (lines 26-27, 78-79) and add a **"Publish state"** section holding both tables. Correct line 146 ("Saves and publishes (for content)") and line 129, which frames `bulkUploadShouldUpdate` as the sole mode switch. |
| `README.md` | The "Update Mode" section (lines 135-157) documents `bulkUploadShouldUpdate` but never mentions `bulkUploadShouldPublish` at all. Add a short "Publish state" subsection with the existing-content table and a link to the full guide. |
| `samples/README.md` | Column reference (lines 111-166) plus a worked unpublish example. The file also links several sample CSVs deleted in `776a6f7` — fix or drop those dead links while here. |
| `samples/` | Add `content-unpublish-basic.csv`. Check `content-upload-basic.csv`, whose header is `bulkUploadShouldPublish,name,docTypeAlias,…` with no update column: under the new rules its falsy rows create drafts rather than being skipped, so confirm it still demonstrates what it claims. |
| `CHANGELOG.md` | Move `[Unreleased]` into `## [2.2.0] - <date>`. `Added`: the new column. `Changed`: the five consequences listed above, in the existing bold-lead-in prose style — the 2.0.0 "New reserved columns" entry is the precedent. |
| `.github/docs/user-guides/LEGACY_HIERARCHY_MAPPING.md` | "Reserved CSV Columns" list at lines 11-18. Lines 189-221 are a recipe for adding a reserved column — check the new one satisfies it. |
| `.github/docs/process-diagrams.md` | The Mermaid import flowchart (~line 230) has no branch for the skip or publish-state rules. Add one; it is where a reader goes looking for this. |
| `.github/docs/troubleshooting.md` | Lines 387 and 560-564 cover these columns. Add "my page was unpublished by an import" pointing at the new rules. |
| `.claude/CLAUDE.md` | "Reserved CSV Columns" list at lines 445-451 and "Content Update Flow" at 407-411. Untracked, but it is the best prose list in the repo and the next agent reads it. Already drifted: line 402 names an `ImportUtilityService.CreateNode()` that does not exist — the method is `ImportSingleItem`. Fix that too. |
| `swagger.json` (repo root) | Regenerate if the XML doc changes alter it. |

`.github/README.md` and `.github/docs/README_nuget.md` name no columns at all; leave them apart from a
one-line feature mention in the nuget readme if it reads naturally.

---

## Tests — `src/BulkUpload.Tests` (xUnit 2.9.3 + Moq, net8.0 only)

There are currently **no tests for `ImportUtilityService` or `BulkImportService` at all**. All six of
`ImportUtilityService`'s constructor dependencies are mockable interfaces, so no new infrastructure is
needed. Match the `// Arrange / // Act / // Assert` style and `Method_Expected_WhenCondition` naming
from `Models/ImportObjectTests.cs`.

- **`Services/ImportUtilityServiceTests.cs`** (new)
  - `CreateImportObject` parsing for the new column: `true`/`yes`/`1`, casing and whitespace, falsy
    values, column absent, and the `bulkUploadShouldUnpublish|text` resolver-suffix form. Pass a plain
    `Dictionary<string, object>` as the record, as `MediaImportServiceTests` does.
  - `ImportSingleItem` against a `Mock<IContentService>`, asserting via `Verify`. **Write the first
    one first and watch it fail** — it is the regression test for the reported bug:
    - published node, `shouldUpdate=true`, no publish or unpublish → `Save` once, `Unpublish` **never**
    - published node + unpublish → `Save`, then `Unpublish`
    - unpublished node + unpublish → `Save`, `Unpublish` never
    - both flags truthy → `Unpublish`, publish **never**
    - publish only → the publish path, `Unpublish` never
    - existing node, `shouldUpdate` falsy, `shouldPublish=true` → publish happens, **no `SetValue`**
      on the content and no rename or move
  - One test per row of both truth tables is the right granularity here.
- **`Services/BulkImportServiceTests.cs`** (new) — the no-op skip rule and the create-row
  `shouldUpdate=false` skip, asserting which rows reach `IImportUtilityService.ImportSingleItem`.
- **`Models/ImportObjectTests.cs`** — extend the `CanImport` matrix (lines 284-360) with the
  guid + unpublish-only case.

The test project targets **net8.0 only**, so it exercises the `#if NET8_0` `SaveAndPublish` branch
only. Acceptable: the new gating and unpublish logic is identical on both frameworks, and only the
pre-existing publish branch differs. Multi-targeting the test project is a worthwhile follow-up but is
**out of scope**.

---

## Branch, build and verification

1. `git checkout main && git pull && git checkout -b fix/should-unpublish-column`. `main` is the only
   long-lived branch and must never be committed to directly. Conventional Commits (`feat:`, `fix:`,
   `docs:`, `test:`).
2. Bump `<Version>` to `2.2.0` in `src/BulkUpload/BulkUpload.csproj:16`. The release workflow rewrites
   this from the git tag anyway, but bumping it is the repo convention.
3. Build both frameworks and run the tests:
   ```powershell
   dotnet build src/BulkUpload.sln -p:SkipPreBuild=true
   dotnet test src/BulkUpload.Tests/BulkUpload.Tests.csproj
   ```
   A full net10.0 build runs `npm install` + `npm run build` in `src/BulkUpload/ClientV17` via the
   `BuildFrontendV17` target, so Node 20+ must be on PATH. Dropping `-p:SkipPreBuild=true` also runs
   `dotnet format --severity warn`.
4. Manual pass in **both** test sites (`src/BulkUpload.TestSite13`, `src/BulkUpload.TestSite17`), which
   reference the project directly. Walk the existing-content table: publish a node, then import a CSV
   for each of its eight rows against that node's GUID and confirm the data and publish-state columns
   both hold. The load-bearing one is `shouldUpdate=true, shouldPublish=false` — the page must **stay
   published** with the changes sitting as a draft.

### Local NuGet feed loop — undocumented today, add it to `RELEASE_PROCESS.md`

There is no `nuget.config`, no local feed folder and no pack script in the repo, and the test sites use
`ProjectReference`, so nothing currently exercises the packaged artefact.

```powershell
# pack outside the repo so nothing stray gets committed
dotnet pack src/BulkUpload/BulkUpload.csproj -c Release -o D:\Code\local-nuget
dotnet nuget add source D:\Code\local-nuget -n local-bulkupload   # user-level, one-off
```

Then in PMCPA bump `ClerksWell.DocumentImporter/ClerksWell.DocumentImporter.csproj:51` to `2.2.0`, run
`dotnet nuget locals http-cache --clear`, restore, and re-run the failing case import against a
published case page.

**Do not commit a `nuget.config` pointing at a local path** — it would break CI for everyone else. Use
the user-level source above.

### Release, after sign-off and not part of this work

Merge the PR to `main`, then GitHub → Releases → new tag `v2.2.0` targeting `main` → Publish. That
fires `.github/workflows/release.yml`, which refuses any target but `main`, rewrites `<Version>` from
the tag, runs the tests (a failure aborts the release), packs and pushes to nuget.org.

---

## Consumer follow-up (PMCPA, separate session)

Once 2.2.0 is referenced, **`CsvImportSafety` needs no change**: it already writes
`bulkUploadShouldPublish=false` + `bulkUploadShouldUpdate=true`, which is existing-content table row 1
— data updated, publish state unchanged. Exactly the wanted behaviour.

Still outstanding in PMCPA and **not** covered here: the separate defect where "update 3" updated the
page named `… (3)` instead of the third item in the list, because `CasePageImportTool.cs:450` returns
`ExistingMatches` with no stable ordinal for the user's number to bind to.

---

## Out of scope

- Multi-targeting `BulkUpload.Tests` to net10.0.
- The media pipeline (`MediaImportService`, `bulkUploadMediaGuid`) — media has no publish concept.
- Removing the dead `ImportObject.ShouldPublish` property; obsolete it, do not delete, in a minor.
- Any change to the PMCPA repo.
