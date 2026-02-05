# BulkUpload Process Diagrams

Visual guides to understand how BulkUpload processes imports.

## Table of Contents

- [Content Import Process](#content-import-process)
- [Media Import Process](#media-import-process)
- [Multi-CSV Import Process](#multi-csv-import-process)
- [Hierarchy Resolution Process](#hierarchy-resolution-process)
- [Media Deduplication Process](#media-deduplication-process)

---

## Content Import Process

### Single CSV Content Import

```mermaid
flowchart TD
    Start([User uploads CSV or ZIP]) --> ValidateFile{Valid file?}
    ValidateFile -->|No| Error1[Show error message]
    ValidateFile -->|Yes| ExtractCSV[Extract CSV from ZIP<br/>or use CSV directly]

    ExtractCSV --> ParseCSV[Parse CSV using CsvHelper]
    ParseCSV --> ValidateHeaders{Required headers<br/>present?}
    ValidateHeaders -->|No| Error2[Show missing headers error]
    ValidateHeaders -->|Yes| ProcessRows[Process each row]

    ProcessRows --> CreateImportObject[Create ImportObject from row]
    CreateImportObject --> ApplyResolvers[Apply resolvers to<br/>transform values]

    ApplyResolvers --> MediaCheck{Contains media<br/>resolvers?}
    MediaCheck -->|Yes| PreprocessMedia[Preprocess media<br/>from ZIP/URLs/paths]
    MediaCheck -->|No| SkipMedia[Skip media preprocessing]
    PreprocessMedia --> BuildHierarchy[Build hierarchy]
    SkipMedia --> BuildHierarchy

    BuildHierarchy --> SortTopological[Sort by parent-child<br/>dependencies]
    SortTopological --> CreateContent[Create content nodes<br/>in correct order]

    CreateContent --> SetProperties[Set property values]
    SetProperties --> Publish{Should publish?}
    Publish -->|Yes| PublishNode[Publish content node]
    Publish -->|No| SaveDraft[Save as draft]

    PublishNode --> NextRow{More rows?}
    SaveDraft --> NextRow
    NextRow -->|Yes| ProcessRows
    NextRow -->|No| GenerateResults[Generate results CSV]

    GenerateResults --> Success([Download results])
    Error1 --> End([End])
    Error2 --> End
```

### CSV with ZIP Media Import

```mermaid
flowchart TD
    Start([User uploads ZIP file]) --> ExtractZIP[Extract ZIP contents]
    ExtractZIP --> FindCSV{CSV file found?}
    FindCSV -->|No| Error1[Error: No CSV in ZIP]
    FindCSV -->|Yes| ParseCSV[Parse CSV]

    ParseCSV --> ProcessRows[For each row]
    ProcessRows --> CheckMediaResolver{Has media<br/>resolver?}

    CheckMediaResolver -->|zipFileToMedia| FindInZIP[Find media file in ZIP]
    CheckMediaResolver -->|urlToMedia| DownloadURL[Download from URL]
    CheckMediaResolver -->|pathToMedia| LoadFromPath[Load from file path]
    CheckMediaResolver -->|No| SkipMedia[Skip media]

    FindInZIP --> CreateMedia[Create media item in Umbraco]
    DownloadURL --> CreateMedia
    LoadFromPath --> CreateMedia
    CreateMedia --> CacheMediaGuid[Cache media GUID]

    CacheMediaGuid --> CreateContent[Create content node]
    SkipMedia --> CreateContent
    CreateContent --> LinkMedia[Link media to content property]

    LinkMedia --> NextRow{More rows?}
    NextRow -->|Yes| ProcessRows
    NextRow -->|No| GenerateResults[Generate results CSV]
    GenerateResults --> Success([Download results])

    Error1 --> End([End])
```

---

## Media Import Process

### Single CSV Media Import

```mermaid
flowchart TD
    Start([User uploads ZIP/CSV]) --> FileType{File type?}
    FileType -->|ZIP| ExtractZIP[Extract ZIP]
    FileType -->|CSV Only| ParseCSV[Parse CSV directly]

    ExtractZIP --> FindCSV[Find CSV in ZIP]
    FindCSV --> ParseCSV

    ParseCSV --> ProcessRow[For each row in CSV]
    ProcessRow --> CheckSource{Media source?}

    CheckSource -->|fileName| FindInZIP[Find file in ZIP]
    CheckSource -->|urlToStream| DownloadURL[Download from URL]
    CheckSource -->|pathToStream| LoadPath[Load from file path]

    FindInZIP --> ValidateFile{File exists?}
    DownloadURL --> ValidateFile
    LoadPath --> ValidateFile

    ValidateFile -->|No| LogError[Log error for this row]
    ValidateFile -->|Yes| DetectMediaType[Auto-detect media type<br/>if not specified]

    DetectMediaType --> CreateFolder{Parent folder<br/>is path?}
    CreateFolder -->|Yes| AutoCreateFolder[Auto-create folder<br/>hierarchy]
    CreateFolder -->|No| UseParentID[Use parent ID/GUID]

    AutoCreateFolder --> CreateMediaItem[Create media item]
    UseParentID --> CreateMediaItem
    CreateMediaItem --> SetProperties[Set media properties<br/>name, altText, etc.]

    SetProperties --> SaveMedia[Save to Umbraco]
    SaveMedia --> RecordResult[Record result with GUID/UDI]
    LogError --> RecordResult

    RecordResult --> NextRow{More rows?}
    NextRow -->|Yes| ProcessRow
    NextRow -->|No| GenerateResults[Generate results CSV]

    GenerateResults --> Success([Download results])
```

### Media Type Auto-Detection

```mermaid
flowchart TD
    Start([File extension]) --> CheckExtension{Extension?}

    CheckExtension -->|.jpg, .jpeg, .png<br/>.gif, .webp, .svg| Image[Media Type: Image]
    CheckExtension -->|.mp4, .avi, .mov<br/>.webm| Video[Media Type: Video]
    CheckExtension -->|.mp3, .wav, .ogg<br/>.wma| Audio[Media Type: Audio]
    CheckExtension -->|.pdf, .doc, .docx<br/>.xls, .xlsx, etc.| File[Media Type: File]
    CheckExtension -->|Other| DefaultFile[Media Type: File<br/>default]

    Image --> Return([Return media type])
    Video --> Return
    Audio --> Return
    File --> Return
    DefaultFile --> Return
```

---

## Multi-CSV Import Process

### Multi-CSV with Deduplication

```mermaid
flowchart TD
    Start([User uploads ZIP<br/>with multiple CSVs]) --> ExtractZIP[Extract ZIP contents]
    ExtractZIP --> FindCSVs[Find all CSV files]
    FindCSVs --> GatherPhase[GATHER PHASE]

    GatherPhase --> ParseAllCSVs[Parse all CSVs]
    ParseAllCSVs --> CollectRecords[Collect all records<br/>with source tracking]
    CollectRecords --> MediaPreprocess[MEDIA PREPROCESSING]

    MediaPreprocess --> ExtractMediaRefs[Extract all media<br/>references from all CSVs]
    ExtractMediaRefs --> DedupeMedia{Same filename in<br/>multiple CSVs?}

    DedupeMedia -->|Yes| CreateOnce[Create media item<br/>only once]
    DedupeMedia -->|No| CreateNormal[Create media item]

    CreateOnce --> CacheGlobal[Cache GUID globally<br/>for all CSVs]
    CreateNormal --> CacheGlobal
    CacheGlobal --> AllMediaDone{All media<br/>processed?}

    AllMediaDone -->|No| ExtractMediaRefs
    AllMediaDone -->|Yes| HierarchyPhase[HIERARCHY RESOLUTION]

    HierarchyPhase --> BuildGlobalHierarchy[Build hierarchy across<br/>all CSV files]
    BuildGlobalHierarchy --> TopologicalSort[Topological sort<br/>parents before children]
    TopologicalSort --> ContentCreation[CONTENT CREATION]

    ContentCreation --> CreateInOrder[Create content in<br/>dependency order]
    CreateInOrder --> LinkCrossFile[Link cross-file<br/>parent references]
    LinkCrossFile --> ResultsPhase[RESULTS EXPORT]

    ResultsPhase --> SeparateResults[Create separate<br/>result CSV per source]
    SeparateResults --> PackageZIP[Package all results<br/>in ZIP file]
    PackageZIP --> Success([Download results ZIP])
```

### Media Deduplication Logic

```mermaid
flowchart TD
    Start([Process media reference]) --> Normalize[Normalize filename<br/>case-insensitive]
    Normalize --> CheckCache{Already in<br/>cache?}

    CheckCache -->|Yes| ReuseGUID[Reuse existing GUID]
    CheckCache -->|No| CreateNew[Create new media item]

    CreateNew --> SaveMedia[Save to Umbraco]
    SaveMedia --> AddToCache[Add to global cache]
    AddToCache --> ReturnGUID[Return GUID]

    ReuseGUID --> MarkDedupe[Mark as deduplicated]
    MarkDedupe --> ReturnGUID
    ReturnGUID --> RecordInResults[Record in result CSV<br/>for this source]

    RecordInResults --> End([Continue processing])
```

---

## Hierarchy Resolution Process

### Legacy Hierarchy Mapping

```mermaid
flowchart TD
    Start([Import with legacy IDs]) --> ParseCSV[Parse CSV]
    ParseCSV --> CheckLegacyID{Has<br/>bulkUploadLegacyId?}

    CheckLegacyID -->|Yes| StoreLegacyID[Store legacy ID<br/>for this row]
    CheckLegacyID -->|No| NoLegacyID[No legacy tracking]

    StoreLegacyID --> CheckParentID{Has<br/>bulkUploadLegacyParentId?}
    NoLegacyID --> CheckNormalParent{Has parentId?}

    CheckParentID -->|Yes| LookupParent[Lookup parent by<br/>legacy ID in cache]
    CheckParentID -->|No| UseNormalParent[Use standard parentId]
    CheckNormalParent -->|Yes| UseNormalParent
    CheckNormalParent -->|No| UseRoot[Use root -1]

    LookupParent --> ParentFound{Parent found<br/>in cache?}
    ParentFound -->|Yes| UseParentGUID[Use parent's Umbraco ID]
    ParentFound -->|No| DeferCreation[Defer creation<br/>mark as dependency]

    UseParentGUID --> CreateNode[Create content node]
    UseNormalParent --> CreateNode
    UseRoot --> CreateNode

    CreateNode --> CacheNewNode[Cache new node's GUID<br/>with legacy ID]
    CacheNewNode --> UpdateWaiting{Nodes waiting<br/>for this parent?}

    UpdateWaiting -->|Yes| CreateWaiting[Create waiting nodes<br/>now that parent exists]
    UpdateWaiting -->|No| NextRow{More rows?}

    CreateWaiting --> NextRow
    DeferCreation --> NextRow
    NextRow -->|Yes| ParseCSV
    NextRow -->|No| Success([All nodes created])
```

### Topological Sort for Dependencies

```mermaid
flowchart TD
    Start([List of import objects]) --> BuildGraph[Build dependency graph]
    BuildGraph --> IdentifyRoots[Identify root nodes<br/>parentId = -1]

    IdentifyRoots --> InitQueue[Initialize processing queue<br/>with root nodes]
    InitQueue --> ProcessQueue{Queue empty?}

    ProcessQueue -->|No| DequeueNode[Dequeue next node]
    ProcessQueue -->|Yes| CheckRemaining{Unprocessed<br/>nodes remain?}

    DequeueNode --> CreateNode[Create this node]
    CreateNode --> MarkComplete[Mark node as complete]
    MarkComplete --> FindChildren[Find child nodes<br/>waiting for this parent]

    FindChildren --> EnqueueChildren[Add children to queue]
    EnqueueChildren --> ProcessQueue

    CheckRemaining -->|No| Success([All nodes sorted<br/>and created])
    CheckRemaining -->|Yes| CircularDep[Circular dependency<br/>detected]
    CircularDep --> Error([Error: Cannot resolve])
```

---

## Media Deduplication Process

### Cross-CSV Media Deduplication

```mermaid
flowchart TD
    Start([Multiple CSV files]) --> InitCache[Initialize global<br/>media cache]
    InitCache --> CSV1[Process CSV 1]

    CSV1 --> Row1[Row references<br/>logo.jpg]
    Row1 --> Check1{logo.jpg<br/>in cache?}
    Check1 -->|No| Create1[Create media item]
    Create1 --> Cache1[Add to cache:<br/>logo.jpg → GUID-ABC]
    Cache1 --> Result1[Result: GUID-ABC]

    Result1 --> CSV2[Process CSV 2]
    CSV2 --> Row2[Row references<br/>logo.jpg]
    Row2 --> Check2{logo.jpg<br/>in cache?}
    Check2 -->|Yes| Reuse1[Reuse cached GUID-ABC]
    Reuse1 --> Result2[Result: GUID-ABC<br/>same as CSV 1]

    Result2 --> CSV3[Process CSV 3]
    CSV3 --> Row3[Row references<br/>LOGO.JPG<br/>case-insensitive]
    Row3 --> Normalize[Normalize to<br/>logo.jpg]
    Normalize --> Check3{logo.jpg<br/>in cache?}
    Check3 -->|Yes| Reuse2[Reuse cached GUID-ABC]
    Reuse2 --> Result3[Result: GUID-ABC<br/>same as CSV 1 & 2]

    Result3 --> Summary[Summary]
    Summary --> Count[1 media item created<br/>3 rows reference it<br/>2 deduplications saved]
```

---

## Resolver Pipeline

### Value Transformation Process

```mermaid
flowchart TD
    Start([CSV column value]) --> ParseHeader[Parse column header<br/>property|resolver]
    ParseHeader --> HasResolver{Resolver<br/>specified?}

    HasResolver -->|No| DefaultResolver[Use default text resolver]
    HasResolver -->|Yes| FindResolver[Find resolver by alias]

    FindResolver --> ResolverFound{Resolver<br/>registered?}
    ResolverFound -->|No| Error[Error: Resolver not found]
    ResolverFound -->|Yes| CallResolver[Call resolver.Resolve value]

    DefaultResolver --> Transform[Transform value]
    CallResolver --> Transform

    Transform --> Examples{Example<br/>transformations}
    Examples -->|dateTime| ISO8601[Convert to ISO 8601<br/>2024-01-15T00:00:00]
    Examples -->|boolean| TrueFalse[Convert to true/false]
    Examples -->|stringArray| SplitArray[Split by comma<br/>to array]
    Examples -->|zipFileToMedia| CreateMedia[Create media from ZIP<br/>return UDI]
    Examples -->|guidToMediaUdi| ConvertUDI[Convert GUID to<br/>umb://media/guid]

    ISO8601 --> SetProperty[Set property value]
    TrueFalse --> SetProperty
    SplitArray --> SetProperty
    CreateMedia --> SetProperty
    ConvertUDI --> SetProperty

    SetProperty --> Success([Property set])
    Error --> End([Skip this property])
```

---

## Error Handling Flow

### Import Error Handling

```mermaid
flowchart TD
    Start([Import row]) --> TryProcess{Try process}
    TryProcess -->|Success| RecordSuccess[Record success<br/>with GUID/UDI]
    TryProcess -->|Error| CatchError[Catch exception]

    CatchError --> LogError[Log error message]
    LogError --> RecordFailure[Record failure in results<br/>with error message]

    RecordSuccess --> ContinueSuccess{More rows?}
    RecordFailure --> ContinueFail{More rows?}

    ContinueSuccess -->|Yes| NextRow[Next row]
    ContinueFail -->|Yes| NextRow
    ContinueSuccess -->|No| GenerateResults[Generate results]
    ContinueFail -->|No| GenerateResults

    NextRow --> TryProcess

    GenerateResults --> AddStats[Add summary stats:<br/>Total, Success, Failed]
    AddStats --> ReturnResults([Return results CSV/ZIP])
```

---

## Notes

- All diagrams use the Mermaid.js syntax for rendering in GitHub and most markdown viewers
- These processes are simplified for clarity; actual implementation includes additional validation and error handling
- For detailed code-level documentation, see the source code in `src/BulkUpload/Services/`
