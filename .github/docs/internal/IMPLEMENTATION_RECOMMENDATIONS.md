# Implementation Recommendations for Bulk Upload Improvements

## Executive Summary

The new `PathToMediaResolver` and `UrlToMediaResolver` provide powerful capabilities that can significantly enhance the bulk-upload-media functionality. However, there's an architectural consideration: these resolvers currently perform the **complete upload workflow** (download/read → upload to Umbraco → return UDI), while the bulk upload needs **file streams** to handle uploads itself.

## Current State Analysis

### How Bulk Upload Works Today

**Controller Flow** (`MediaImportController.cs:29-178`):
```
1. Receive ZIP file
2. Extract to temp directory
3. Parse CSV rows
4. For each row:
   - Create MediaImportObject (requires fileName)
   - Find file in ZIP by fileName
   - Open file stream from ZIP
   - Pass to ImportSingleMediaItem()
```

**Service Flow** (`MediaImportService.cs:123-238`):
```
ImportSingleMediaItem(MediaImportObject, Stream):
1. Validate object (requires fileName + parentId)
2. Determine media type from file extension
3. Check for existing media by name
4. Upload file using MediaFileManager
5. Set properties from Properties dictionary
6. Save media item
```

### How Resolvers Work Today

**PathToMediaResolver** (`Resolvers/PathToMediaResolver.cs`):
```
Input:  C:/Assets/photo.jpg|1234
Process:
1. Read file from filesystem
2. Create media item in parent 1234
3. Upload file to Umbraco
4. Return media UDI (e.g., "umb://media/abc123...")
```

**UrlToMediaResolver** (`Resolvers/UrlToMediaResolver.cs`):
```
Input:  https://cdn.example.com/photo.jpg|1234
Process:
1. Download file from URL
2. Create media item in parent 1234
3. Upload file to Umbraco
4. Return media UDI
```

### The Core Problem

The resolvers **create media items** themselves, but bulk upload also wants to **create media items**. This creates a conflict:

- **If we use resolvers directly**: They'll create media in one location, return UDI, and bulk upload tries to create again
- **If we extract streams**: We need to refactor resolver logic into reusable components

## Recommended Approaches

### Option 1: Create Stream-Only Resolver Variants (Recommended)

Create new resolvers that only fetch files and return streams, without creating media.

#### New Files to Create:

**`Resolvers/PathToStreamResolver.cs`**:
```csharp
public class PathToStreamResolver : IResolver
{
    public string Alias => "pathToStream";

    public object? Resolve(object? value)
    {
        // Extract file path from value
        // Validate file exists
        // Return FileInfo or file path string
        // Let caller handle the stream
    }
}
```

**`Resolvers/UrlToStreamResolver.cs`**:
```csharp
public class UrlToStreamResolver : IResolver
{
    public string Alias => "urlToStream";

    public object? Resolve(object? value)
    {
        // Extract URL from value
        // Validate URL format
        // Return URL string
        // Let caller handle the download
    }
}
```

#### Modify MediaImportObject:

```csharp
public class MediaImportObject
{
    public required string FileName { get; set; }
    public string? Name { get; set; }
    public int ParentId { get; set; }
    public string? MediaTypeAlias { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    // NEW: Support for external sources
    public MediaSource? ExternalSource { get; set; }

    public bool CanImport =>
        (!string.IsNullOrWhiteSpace(FileName) || ExternalSource != null)
        && ParentId > 0;

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FileName;
}

public class MediaSource
{
    public MediaSourceType Type { get; set; }
    public required string Value { get; set; }  // File path or URL
}

public enum MediaSourceType
{
    ZipFile,
    FilePath,
    Url
}
```

#### Modify Controller Logic:

```csharp
// In MediaImportController.ImportMedia() around line 108-128

// Check for external source first
Stream? fileStream = null;
string actualFileName = importObject.FileName;

if (importObject.ExternalSource != null)
{
    switch (importObject.ExternalSource.Type)
    {
        case MediaSourceType.FilePath:
            var filePath = importObject.ExternalSource.Value;
            if (!File.Exists(filePath))
            {
                results.Add(new MediaImportResult
                {
                    FileName = filePath,
                    Success = false,
                    ErrorMessage = $"File not found: {filePath}"
                });
                continue;
            }
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            actualFileName = Path.GetFileName(filePath);
            break;

        case MediaSourceType.Url:
            var url = importObject.ExternalSource.Value;
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                fileStream = memoryStream;

                // Extract filename from URL
                actualFileName = Path.GetFileName(new Uri(url).LocalPath);
                if (string.IsNullOrWhiteSpace(Path.GetExtension(actualFileName)))
                {
                    actualFileName += ".jpg"; // Default extension
                }
            }
            catch (Exception ex)
            {
                results.Add(new MediaImportResult
                {
                    FileName = url,
                    Success = false,
                    ErrorMessage = $"Failed to download from URL: {ex.Message}"
                });
                continue;
            }
            break;

        case MediaSourceType.ZipFile:
            // Fall through to existing logic
            break;
    }
}

// If no external source, use existing ZIP file logic
if (fileStream == null)
{
    var mediaFiles = Directory.GetFiles(tempDirectory, importObject.FileName, SearchOption.AllDirectories);
    if (mediaFiles.Length == 0)
    {
        results.Add(new MediaImportResult
        {
            FileName = importObject.FileName,
            Success = false,
            ErrorMessage = $"File not found in ZIP archive: {importObject.FileName}"
        });
        continue;
    }
    fileStream = new FileStream(mediaFiles[0], FileMode.Open, FileAccess.Read);
}

// Update fileName if it came from external source
if (!string.IsNullOrWhiteSpace(actualFileName))
{
    importObject.FileName = actualFileName;
}

// Import the media item
var result = _mediaImportService.ImportSingleMediaItem(importObject, fileStream);
results.Add(result);

// Clean up stream
fileStream?.Dispose();
```

#### Modify Service Logic:

```csharp
// In MediaImportService.CreateMediaImportObject() around line 94-120

// After processing special columns (fileName, parentId, etc.)
MediaSource? externalSource = null;

foreach (var property in dynamicProperties)
{
    var columnDetails = property.Key.Split('|');
    var columnName = columnDetails.First();
    string? aliasValue = null;
    if (columnDetails.Length > 1)
    {
        aliasValue = columnDetails.Last();
    }

    // Skip special columns
    if (new string[] { "fileName", "name", "parentId", "mediaTypeAlias" }.Contains(columnName))
        continue;

    // Check for external source resolvers
    if (aliasValue == "pathToStream")
    {
        var pathValue = property.Value?.ToString();
        if (!string.IsNullOrWhiteSpace(pathValue))
        {
            externalSource = new MediaSource
            {
                Type = MediaSourceType.FilePath,
                Value = pathValue
            };
            continue;  // Don't add to properties
        }
    }
    else if (aliasValue == "urlToStream")
    {
        var urlValue = property.Value?.ToString();
        if (!string.IsNullOrWhiteSpace(urlValue))
        {
            externalSource = new MediaSource
            {
                Type = MediaSourceType.Url,
                Value = urlValue
            };
            continue;  // Don't add to properties
        }
    }

    // Process normal properties
    var resolverAlias = aliasValue ?? "text";
    var resolver = _resolverFactory.GetByAlias(resolverAlias);
    object? propertyValue = null;
    if (resolver != null)
    {
        propertyValue = resolver.Resolve(property.Value);
    }
    propertiesToCreate.Add(columnName, propertyValue);
}

importObject.Properties = propertiesToCreate;
importObject.ExternalSource = externalSource;
```

#### CSV Examples:

**ZIP File Only (Existing)**:
```csv
fileName,parentId,name
hero.jpg,1150,Hero Image
logo.png,1150,Company Logo
```

**File Path Source**:
```csv
fileName,mediaSource|pathToStream,parentId,name
,C:/Assets/hero.jpg,1150,Hero Image
,\\nas\share\images\logo.png,1150,Company Logo
```

**URL Source**:
```csv
fileName,mediaSource|urlToStream,parentId,name
,https://cdn.example.com/hero.jpg,1150,Hero Image
,https://images.unsplash.com/photo-123.jpg,1150,Stock Photo
```

**Mixed Sources**:
```csv
fileName,mediaSource|pathToStream,mediaSource|urlToStream,parentId,name
hero.jpg,,,1150,Hero (from ZIP)
,C:/Assets/banner.jpg,,1150,Banner (from path)
,,https://cdn.example.com/logo.jpg,1150,Logo (from URL)
```

---

### Option 2: Refactor Existing Resolvers with Modes

Add a "mode" parameter to existing resolvers to control behavior.

#### Challenges:
- Resolvers are stateless and don't have context
- Can't return different types (Stream vs UDI string)
- Would break existing functionality

**Not recommended** due to complexity and breaking changes.

---

### Option 3: Bypass Resolvers, Implement Directly in Controller

Extract file source detection logic directly into the controller without using resolvers.

#### Advantages:
- Simpler implementation
- No new resolver classes needed
- Full control over stream handling

#### Disadvantages:
- Less extensible
- Doesn't leverage resolver pattern
- Duplicates logic

#### Implementation:

Simply detect columns named `filePath` and `fileUrl` directly in the controller:

```csharp
// Check for special source columns
string? filePath = null;
string? fileUrl = null;

if (dynamicProperties.TryGetValue("filePath", out object? filePathValue))
{
    filePath = filePathValue?.ToString();
}

if (dynamicProperties.TryGetValue("fileUrl", out object? fileUrlValue))
{
    fileUrl = fileUrlValue?.ToString();
}

// Determine source priority: fileName > filePath > fileUrl
if (!string.IsNullOrWhiteSpace(importObject.FileName))
{
    // Use ZIP file (existing logic)
}
else if (!string.IsNullOrWhiteSpace(filePath))
{
    // Read from file path
    fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
}
else if (!string.IsNullOrWhiteSpace(fileUrl))
{
    // Download from URL
    // ... download logic
}
```

**CSV Example**:
```csv
fileName,filePath,fileUrl,parentId,name
hero.jpg,,,1150,Hero (from ZIP)
,C:/Assets/banner.jpg,,1150,Banner (from path)
,,https://cdn.example.com/logo.jpg,1150,Logo (from URL)
```

---

## Recommendation

**Use Option 1** (Stream-Only Resolver Variants) because:

1. **Consistent with Architecture**: Leverages existing resolver pattern
2. **Extensible**: Easy to add new source types (FTP, S3, etc.)
3. **Flexible**: Supports resolver parameterization for folder paths
4. **Clean Separation**: Stream acquisition vs media creation
5. **Reusable**: Stream resolvers can be used elsewhere

## Implementation Checklist

### Phase 1: Core Functionality
- [ ] Create `MediaSource` model class
- [ ] Update `MediaImportObject` with `ExternalSource` property
- [ ] Create `PathToStreamResolver` (alias: `pathToStream`)
- [ ] Create `UrlToStreamResolver` (alias: `urlToStream`)
- [ ] Register new resolvers in `BulkUploadComposer`
- [ ] Modify `MediaImportService.CreateMediaImportObject()` to detect source resolvers
- [ ] Modify `MediaImportController.ImportMedia()` to handle external sources
- [ ] Update validation logic in `MediaImportObject.CanImport`

### Phase 2: Error Handling
- [ ] Add file existence validation for paths
- [ ] Add URL reachability validation
- [ ] Handle network timeouts gracefully
- [ ] Add detailed error messages to results
- [ ] Log all resolver failures with context

### Phase 3: Advanced Features
- [ ] Support parameterized folder paths (e.g., `url|urlToStream:/Gallery/`)
- [ ] Add HTTP timeout configuration
- [ ] Add max file size limits
- [ ] Implement retry logic for failed downloads
- [ ] Support URL authentication headers

### Phase 4: Documentation
- [ ] Update README with new CSV formats
- [ ] Add sample CSV files for each source type
- [ ] Document security considerations
- [ ] Add troubleshooting guide

## Security Considerations

### File Path Access
- **Path Traversal**: Validate paths don't escape allowed directories
- **Permissions**: Ensure application has read access to specified paths
- **UNC Paths**: Consider authentication requirements for network shares

### URL Downloads
- **SSRF Prevention**: Block localhost and private IP ranges
- **Domain Whitelist**: Consider configurable allowed domains
- **File Size Limits**: Prevent memory exhaustion from large downloads
- **Content Type Validation**: Verify downloaded content matches expected media types

### Example Security Validation:

```csharp
private bool IsAllowedFilePath(string path)
{
    // Prevent path traversal
    var fullPath = Path.GetFullPath(path);

    // Define allowed base directories (configurable)
    var allowedPaths = new[]
    {
        "C:\\AllowedAssets\\",
        "\\\\nas.company.local\\assets\\"
    };

    return allowedPaths.Any(allowed =>
        fullPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
}

private bool IsAllowedUrl(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return false;

    // Block private IP ranges
    var host = uri.Host;
    if (host == "localhost" ||
        host == "127.0.0.1" ||
        host.StartsWith("192.168.") ||
        host.StartsWith("10.") ||
        host.StartsWith("172."))
    {
        return false;
    }

    // Optional: whitelist allowed domains
    var allowedDomains = new[] { "cdn.example.com", "assets.company.com" };
    return allowedDomains.Any(domain =>
        host.Equals(domain, StringComparison.OrdinalIgnoreCase));
}
```

## Performance Considerations

### Streaming vs Buffering
- **Large Files**: Use streaming to avoid loading entire file into memory
- **URL Downloads**: Download to temp file first, then stream to Umbraco
- **Parallel Processing**: Consider concurrent downloads (with throttling)

### Cleanup
- **Temp Files**: Ensure downloaded files are deleted after processing
- **Streams**: Dispose streams properly in finally blocks
- **Background Tasks**: Consider moving large downloads to background jobs

## Example Implementation Timeline

### Week 1: Foundation
- Implement `MediaSource` model
- Create stream-only resolvers
- Update `MediaImportObject`

### Week 2: Integration
- Modify service to detect sources
- Update controller to handle sources
- Add basic error handling

### Week 3: Polish
- Add security validations
- Implement advanced features
- Write comprehensive tests

### Week 4: Documentation & Testing
- Create documentation
- End-to-end testing
- Performance optimization

## Conclusion

By creating stream-only resolver variants, we can leverage the powerful capabilities of `PathToMediaResolver` and `UrlToMediaResolver` while maintaining clean separation of concerns in the bulk upload functionality. This approach is extensible, maintainable, and provides maximum flexibility for users importing media from diverse sources.
