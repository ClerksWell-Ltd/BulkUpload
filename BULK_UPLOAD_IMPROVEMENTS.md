# Bulk Upload Media Improvements with PathToMediaResolver and UrlToMediaResolver

## Current Architecture Analysis

### Existing Bulk Upload Flow
**File**: `src/BulkUpload/Controllers/MediaImportController.cs`

Currently, the bulk media upload works as follows:
1. User uploads a ZIP file containing:
   - One CSV file with media metadata
   - Media files referenced in the CSV
2. System extracts ZIP to temporary directory
3. Parses CSV rows into `MediaImportObject` instances
4. For each row, finds the file inside the ZIP using `fileName`
5. Uploads the file stream to Umbraco using `MediaFileManager`

### Key Limitation
**All media files must be packaged inside the ZIP file**. This creates several issues:
- Large ZIP files for bulk imports
- Manual file collection is tedious
- Can't directly import from network shares
- Can't import from URLs/CDNs
- Duplicate storage (original + ZIP copy)

## New Capabilities with Resolvers

### PathToMediaResolver
**Alias**: `pathToMedia`
**Purpose**: Import media from local/network file paths

Features:
- Supports absolute paths (`C:/Images/photo.jpg`)
- Supports relative paths (`./images/photo.jpg`)
- Supports UNC network paths (`\\server\share\images\photo.jpg`)
- Auto-creates folder structures
- Flexible parent folder specification (ID, GUID, or path)

### UrlToMediaResolver
**Alias**: `urlToMedia`
**Purpose**: Download and import media from URLs

Features:
- Downloads from any HTTP/HTTPS URL
- Handles query parameters and URL encoding
- Auto-detects file extension and media type
- Flexible parent folder specification
- 30-second timeout for downloads

## Proposed Improvements

### 1. **Hybrid Media Source Support**

Allow CSV to specify media sources in multiple ways:

#### Option A: File in ZIP (Current Behavior)
```csv
fileName,parentId,name
hero.jpg,1150,Hero Image
```
The system looks for `hero.jpg` inside the ZIP.

#### Option B: External File Path (New)
```csv
fileName,mediaSource|pathToMedia,parentId,name
,C:/Projects/Assets/hero.jpg,1150,Hero Image
```
Or with pipe syntax in the value:
```csv
fileName,mediaSource|pathToMedia:/Gallery/,parentId,name
,C:/Projects/Assets/hero.jpg,,Hero Image
```
The system reads from the file system and creates media in `/Gallery/`.

#### Option C: Download from URL (New)
```csv
fileName,mediaSource|urlToMedia,parentId,name
,https://cdn.example.com/images/hero.jpg,1150,Hero Image
```

#### Option D: Mixed Sources (New)
```csv
fileName,mediaSource|pathToMedia,parentId,name
logo.png,,,1150,Company Logo
,C:/Assets/hero.jpg,1150,Hero Image
,https://cdn.example.com/banner.jpg,1150,Banner
```
- `logo.png` → from ZIP
- `C:/Assets/hero.jpg` → from file system
- `https://cdn.example.com/banner.jpg` → from URL

### 2. **Implementation Changes**

#### Modify `MediaImportService.CreateMediaImportObject()`

**Current Logic** (`MediaImportService.cs:42-74`):
- Always requires `fileName` property
- Processes other columns through resolver system
- Doesn't consider resolvers for media source

**Proposed Logic**:
1. Check if `fileName` is provided → use ZIP file (existing behavior)
2. If `fileName` is empty, check for `mediaSource` property:
   - If `mediaSource` uses `pathToMedia` or `urlToMedia` resolver → extract result
   - The resolver result should be the file path/URL, not the final media UDI
3. Store the source type and value in `MediaImportObject` for later processing

#### Add New Properties to `MediaImportObject`

**Current Model** (`Models/MediaImportObject.cs`):
```csharp
public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }
    public int ParentId { get; set; }
    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public bool CanImport => !string.IsNullOrWhiteSpace(FileName) && ParentId > 0;
}
```

**Proposed Model**:
```csharp
public class MediaImportObject
{
    public required string FileName { get; set; }  // Optional if MediaSource is set
    public string? Name { get; set; }
    public int ParentId { get; set; }
    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    // NEW: Support for external media sources
    public MediaSourceType SourceType { get; set; } = MediaSourceType.ZipFile;
    public string? MediaSource { get; set; }  // File path or URL

    public bool CanImport =>
        (!string.IsNullOrWhiteSpace(FileName) || !string.IsNullOrWhiteSpace(MediaSource))
        && ParentId > 0;
}

public enum MediaSourceType
{
    ZipFile,      // File from uploaded ZIP (current behavior)
    FilePath,     // File from local/network path
    Url           // File from URL download
}
```

#### Modify `ImportSingleMediaItem()` Method

**Current Signature** (`MediaImportService.cs:76`):
```csharp
public MediaImportResult ImportSingleMediaItem(MediaImportObject mediaObject, Stream fileStream)
```

**Proposed Changes**:
1. Make `fileStream` optional (nullable)
2. If `mediaObject.SourceType == FilePath`:
   - Use `PathToMediaResolver` to read file and get stream
   - Or directly read file using `File.OpenRead()`
3. If `mediaObject.SourceType == Url`:
   - Use `UrlToMediaResolver` to download and get stream
   - Or directly download using `HttpClient`
4. If `mediaObject.SourceType == ZipFile`:
   - Use provided `fileStream` (existing behavior)

**Alternative Approach**: Instead of modifying the method signature, resolve the stream **before** calling `ImportSingleMediaItem()` in the controller.

### 3. **Controller Changes**

#### Modify `MediaImportController.ImportMedia()`

**Current Logic** (`MediaImportController.cs:54-115`):
1. Extract ZIP to temp directory
2. Parse CSV
3. For each row:
   - Create `MediaImportObject`
   - Find file in ZIP by `fileName`
   - Call `ImportSingleMediaItem()` with file stream from ZIP

**Proposed Logic**:
1. Extract ZIP to temp directory (unchanged)
2. Parse CSV (unchanged)
3. For each row:
   - Create `MediaImportObject` (now may include `MediaSource`)
   - **Resolve the file stream based on source type**:
     ```csharp
     Stream? fileStream = mediaObject.SourceType switch
     {
         MediaSourceType.ZipFile => GetFileFromZip(mediaObject.FileName),
         MediaSourceType.FilePath => GetFileFromPath(mediaObject.MediaSource),
         MediaSourceType.Url => GetFileFromUrl(mediaObject.MediaSource),
         _ => null
     };
     ```
   - Call `ImportSingleMediaItem()` with resolved stream
   - Dispose stream after import

### 4. **Benefits**

1. **Reduced ZIP File Size**: Only include CSV and files not available externally
2. **Network Share Integration**: Import directly from NAS/SFTP mounts
3. **CDN/Cloud Storage**: Import from S3, Azure Blob, etc. without downloading locally first
4. **Flexibility**: Mix sources in single import (ZIP + paths + URLs)
5. **Automation**: Scripts can reference existing file paths without manual ZIP creation
6. **Large Files**: Avoid ZIP size limits by streaming from external sources
7. **Backward Compatible**: Existing CSV files with `fileName` only still work

### 5. **Example Use Cases**

#### Use Case 1: Import from Network Share
```csv
fileName,mediaSource|pathToMedia:/Products/Images/,name
,\\nas.company.local\assets\products\widget-1.jpg,Widget Product Image
,\\nas.company.local\assets\products\widget-2.jpg,Widget Detail Image
```

#### Use Case 2: Import from Mixed Sources
```csv
fileName,mediaSource|pathToMedia,mediaSource|urlToMedia,parentId,name
logo.png,,,1150,Logo (from ZIP)
,C:/Projects/Assets/banner.jpg,,1150,Banner (from local path)
,,https://cdn.example.com/hero.jpg,1150,Hero (from URL)
```

#### Use Case 3: Organize by Folder Path
```csv
mediaSource|pathToMedia,parentId,name,altText|text
C:/Assets/Products/bike.jpg|/Products/Bikes/,,Mountain Bike,High-quality mountain bike
C:/Assets/Products/helmet.jpg|/Products/Accessories/,,Helmet,Safety helmet
C:/Assets/Staff/john.jpg|/About/Team/,,John Doe,Team member photo
```
This creates folder structures automatically while importing.

## Implementation Priority

### Phase 1: Core Functionality (High Priority)
- [ ] Add `MediaSourceType` enum and properties to `MediaImportObject`
- [ ] Update `CreateMediaImportObject()` to detect and parse `mediaSource` column
- [ ] Modify `ImportSingleMediaItem()` or controller to handle multiple source types
- [ ] Add file stream resolution logic for FilePath and Url sources
- [ ] Update validation logic in `CanImport` property

### Phase 2: Error Handling & Validation (High Priority)
- [ ] Validate file paths exist before attempting import
- [ ] Validate URLs are reachable (optional pre-check)
- [ ] Handle network timeouts and file access errors gracefully
- [ ] Add detailed error messages to `MediaImportResult`
- [ ] Log resolver failures with context

### Phase 3: User Experience (Medium Priority)
- [ ] Update documentation with new CSV format examples
- [ ] Add sample CSV templates for each source type
- [ ] Provide clear error messages for common issues
- [ ] Consider UI hints in Umbraco backoffice

### Phase 4: Advanced Features (Low Priority)
- [ ] Support authentication for URLs (basic auth, bearer tokens)
- [ ] Support SFTP/FTP protocols via `PathToMediaResolver`
- [ ] Batch download optimization for multiple URLs
- [ ] Progress tracking for large file downloads
- [ ] Retry logic for failed downloads

## Technical Considerations

### Security
- **Path Traversal**: Validate file paths don't escape allowed directories
- **URL Safety**: Validate URLs are from allowed domains (configurable whitelist)
- **File Size Limits**: Enforce max file size for downloads
- **SSRF Prevention**: Block private IP ranges and localhost URLs

### Performance
- **Streaming**: Use stream-based downloads to avoid memory issues
- **Parallel Downloads**: Consider downloading multiple URLs concurrently
- **Timeout Handling**: Configurable timeouts for network operations
- **Temp File Cleanup**: Ensure downloaded files are cleaned up after import

### Backward Compatibility
- **Existing CSVs**: Continue to work with `fileName` only
- **Optional Columns**: `mediaSource` is optional, not required
- **Fallback Behavior**: If both `fileName` and `mediaSource` exist, prefer `fileName`

## Conclusion

The PathToMediaResolver and UrlToMediaResolver provide powerful capabilities that can transform the bulk-upload-media functionality from a simple ZIP-based importer into a versatile media integration tool. By allowing mixed sources (ZIP files, file paths, and URLs), users gain significant flexibility while maintaining backward compatibility with existing workflows.

The implementation is straightforward and leverages existing resolver infrastructure, making it a natural extension of the current architecture.
