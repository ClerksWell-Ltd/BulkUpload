# Troubleshooting Guide

Common issues and solutions for BulkUpload.

## Table of Contents

- [Installation Issues](#installation-issues)
- [Upload Issues](#upload-issues)
- [CSV Parsing Issues](#csv-parsing-issues)
- [Media Import Issues](#media-import-issues)
- [Content Import Issues](#content-import-issues)
- [Resolver Issues](#resolver-issues)
- [Permission Issues](#permission-issues)
- [Performance Issues](#performance-issues)
- [Multi-CSV Issues](#multi-csv-issues)
- [Debugging Tips](#debugging-tips)

---

## Installation Issues

### Package Not Appearing in Backoffice

**Problem:** Installed the package but don't see the "Bulk Upload" section

**Solutions:**

1. **Grant section access:**
   - Go to **Users** section
   - Select user group (e.g., Administrators)
   - Check **Bulk Upload** in allowed sections
   - Click **Save**
   - **Refresh the browser** (Ctrl+F5)

2. **Verify installation:**
   ```bash
   dotnet list package | grep BulkUpload
   ```
   Should show: `Umbraco.Community.BulkUpload`

3. **Check target framework compatibility:**
   - Umbraco 13 requires `net8.0`
   - Umbraco 17 requires `net10.0`

4. **Restart the application:**
   ```bash
   dotnet clean
   dotnet build
   dotnet run
   ```

### Wrong Version Installed

**Problem:** Package installed but getting compatibility errors

**Solutions:**

1. **Check your Umbraco version:**
   ```bash
   dotnet list package | grep Umbraco.Cms
   ```

2. **Verify correct framework:**
   - Umbraco 13 → Package uses `net8.0` assembly
   - Umbraco 17 → Package uses `net10.0` assembly

3. **Reinstall if needed:**
   ```bash
   dotnet remove package Umbraco.Community.BulkUpload
   dotnet add package Umbraco.Community.BulkUpload
   ```

---

## Upload Issues

### File Upload Fails

**Problem:** "Failed to upload file" error

**Solutions:**

1. **Check file size:**
   - Default ASP.NET limit is 30MB
   - Increase in `appsettings.json`:
   ```json
   {
     "Kestrel": {
       "Limits": {
         "MaxRequestBodySize": 104857600
       }
     }
   }
   ```

2. **Check file format:**
   - Content import: `.csv` or `.zip` containing CSV
   - Media import: `.zip` containing CSV and media files, or `.csv` only

3. **Verify file permissions:**
   - Ensure web server can read uploaded files
   - Check temporary upload directory permissions

### Upload Times Out

**Problem:** Upload times out for large files

**Solutions:**

1. **Increase timeout:**
   ```json
   {
     "Kestrel": {
       "Limits": {
         "RequestHeadersTimeout": "00:05:00"
       }
     }
   }
   ```

2. **Split into smaller batches:**
   - Split large CSV into multiple smaller files
   - Import in batches

3. **Use multi-CSV approach:**
   - Organize into multiple CSVs in one ZIP
   - BulkUpload processes them together

---

## CSV Parsing Issues

### "No CSV file found in ZIP"

**Problem:** ZIP uploaded but CSV not detected

**Solutions:**

1. **Verify CSV is in ZIP root:**
   ```
   ✅ Correct:
   upload.zip
   ├── data.csv
   └── images/
       └── hero.jpg

   ❌ Incorrect:
   upload.zip
   └── folder/
       └── data.csv
   ```

2. **Check file extension:**
   - Must be `.csv` (lowercase)
   - Not `.CSV`, `.txt`, or `.xlsx`

3. **Verify file encoding:**
   - Use UTF-8 encoding
   - Avoid BOM (Byte Order Mark) if possible

### "Missing required fields"

**Problem:** Import fails with missing required fields error

**Solutions:**

1. **Content import requires:**
   - `parentId` - Parent node ID (integer or -1 for root)
   - `docTypeAlias` - Content type alias
   - `name` - Node name

2. **Media import requires:**
   - `fileName` OR `mediaSource|urlToStream` OR `mediaSource|pathToStream`
   - `parent` OR `parentId` - Parent folder (ID, GUID, or path)

3. **Example valid headers:**
   ```csv
   parentId,docTypeAlias,name,title
   1100,article,My Article,Article Title
   ```

### Column Headers Not Recognized

**Problem:** Properties not importing despite correct headers

**Solutions:**

1. **Check property alias spelling:**
   - Must exactly match Umbraco property alias
   - Case-sensitive: `publishDate` ≠ `PublishDate`

2. **Verify content type:**
   - Property must exist on the specified content type
   - Check in Umbraco Settings → Document Types

3. **Check for hidden characters:**
   - Open CSV in text editor
   - Look for extra spaces or special characters

### "Invalid column header format"

**Problem:** Resolver syntax not recognized

**Solutions:**

1. **Use correct syntax:**
   ```csv
   ✅ Correct: propertyAlias|resolverAlias
   ❌ Wrong: propertyAlias:resolverAlias
   ❌ Wrong: propertyAlias resolverAlias
   ```

2. **Verify resolver exists:**
   - Built-in: `dateTime`, `boolean`, `zipFileToMedia`, etc.
   - Custom: Ensure registered via composer

3. **Example:**
   ```csv
   publishDate|dateTime,isPublished|boolean,heroImage|zipFileToMedia
   2024-01-15,true,hero.jpg
   ```

---

## Media Import Issues

### "File not found in ZIP archive"

**Problem:** Media file referenced in CSV but not found

**Solutions:**

1. **Check exact filename match:**
   - Case-sensitive on Linux: `hero.jpg` ≠ `Hero.jpg`
   - Verify no extra spaces: `hero.jpg ` ≠ `hero.jpg`

2. **Check file location in ZIP:**
   - Files can be in subdirectories
   - Specify path in CSV: `images/hero.jpg`

3. **Verify ZIP structure:**
   ```bash
   unzip -l upload.zip
   ```

### "Media type 'Image' not found"

**Problem:** Specified media type doesn't exist

**Solutions:**

1. **Use correct media type alias:**
   - Default types: `Image`, `File`, `Video`, `Audio`
   - Must match exactly (case-sensitive)

2. **Let auto-detection work:**
   - Omit `mediaTypeAlias` column
   - Package detects based on file extension

3. **Check custom media types:**
   - Verify media type exists in Settings → Media Types
   - Use exact alias from media type

### "Cannot download from URL"

**Problem:** Media import from URL fails

**Solutions:**

1. **Verify URL is accessible:**
   ```bash
   curl -I https://example.com/image.jpg
   ```

2. **Check firewall/proxy:**
   - Server must allow outbound HTTP/HTTPS
   - Configure proxy if needed

3. **Verify URL format:**
   ```csv
   mediaSource|urlToStream
   https://example.com/image.jpg
   http://cdn.example.com/file.pdf
   ```

4. **Check SSL certificate:**
   - Ensure URL has valid SSL if using HTTPS

### "Path not accessible"

**Problem:** Media import from file path fails

**Solutions:**

1. **Verify path format:**
   - Windows: `C:\Images\hero.jpg` or `\\server\share\images\hero.jpg`
   - Linux: `/mnt/images/hero.jpg`

2. **Check permissions:**
   - Web server must have read access
   - Test with same user account as web server

3. **Use forward slashes:**
   ```csv
   mediaSource|pathToStream
   C:/Images/hero.jpg
   ```

### Parent Folder Not Found

**Problem:** Media import fails due to missing parent folder

**Solutions:**

1. **Use folder path for auto-creation:**
   ```csv
   parent,name,fileName
   /Products/Images/,Hero Image,hero.jpg
   ```

2. **Create folder first:**
   - Create in Umbraco Media section
   - Note the folder ID
   - Use ID in CSV:
   ```csv
   parentId,name,fileName
   1150,Hero Image,hero.jpg
   ```

3. **Use GUID:**
   ```csv
   parent,name,fileName
   a1b2c3d4-e5f6-7890-abcd-ef1234567890,Hero Image,hero.jpg
   ```

---

## Content Import Issues

### "Parent node not found"

**Problem:** Import fails because parent doesn't exist

**Solutions:**

1. **Verify parent ID:**
   - Check parent node exists in Umbraco
   - Right-click node → Info to see ID

2. **Use root for top-level:**
   ```csv
   parentId,docTypeAlias,name
   -1,homePage,Home
   ```

3. **Create parents first:**
   - Import parent nodes first
   - Then import children

### "Document type not found"

**Problem:** Content type alias doesn't exist

**Solutions:**

1. **Check content type alias:**
   - Go to Settings → Document Types
   - Copy exact alias (case-sensitive)

2. **Verify content type exists:**
   ```csv
   docTypeAlias
   article       ✅ Correct
   Article       ❌ Wrong (case mismatch)
   ```

### Hierarchy Import Fails

**Problem:** Parent-child relationships not working across CSVs

**Solutions:**

1. **Use legacy IDs:**
   ```csv
   bulkUploadLegacyId,bulkUploadLegacyParentId,docTypeAlias,name
   old-1,,homePage,Home
   old-2,old-1,contentPage,About
   old-3,old-2,contentPage,Team
   ```

2. **Ensure correct ordering:**
   - BulkUpload automatically sorts
   - Parents created before children

3. **Check for circular dependencies:**
   - Node cannot be its own parent
   - No circular parent chains

---

## Resolver Issues

### "Resolver not found"

**Problem:** Custom resolver not recognized

**Solutions:**

1. **Verify registration:**
   ```csharp
   public class MyComposer : IComposer
   {
       public void Compose(IUmbracoBuilder builder)
       {
           builder.Services.AddSingleton<IResolver, MyResolver>();
       }
   }
   ```

2. **Check resolver alias:**
   ```csharp
   public class MyResolver : IResolver
   {
       public string Alias() => "myResolver"; // Use this in CSV
   }
   ```

3. **Rebuild application:**
   ```bash
   dotnet clean
   dotnet build
   ```

### Resolver Returns Wrong Value

**Problem:** Resolver transforms value incorrectly

**Solutions:**

1. **Add logging:**
   ```csharp
   public object Resolve(object value)
   {
       _logger.LogInformation("Input: {Value}", value);
       var result = Transform(value);
       _logger.LogInformation("Output: {Result}", result);
       return result;
   }
   ```

2. **Check input format:**
   - Verify CSV value format matches expected
   - Test with simple values first

3. **Handle null/empty:**
   ```csharp
   if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
       return string.Empty;
   ```

---

## Permission Issues

### "Insufficient permissions"

**Problem:** Cannot create content/media

**Solutions:**

1. **Verify user permissions:**
   - User must have Create rights
   - Check in Users → User Group → Permissions

2. **Check content type permissions:**
   - User must have access to content type
   - Verify in Document Type permissions

3. **Media permissions:**
   - User must have access to Media section
   - Needs Create permission on media types

---

## Performance Issues

### Slow Import

**Problem:** Import takes a very long time

**Solutions:**

1. **Reduce batch size:**
   - Split large CSV into smaller files
   - Import in batches

2. **Optimize resolvers:**
   - Use caching for repeated lookups
   - Avoid N+1 queries

3. **Disable indexing temporarily:**
   - Rebuild indexes after import
   - Significantly faster for large imports

4. **Use database optimization:**
   - Ensure proper indexes
   - Run database maintenance

### Memory Issues

**Problem:** Out of memory errors during import

**Solutions:**

1. **Increase memory limit:**
   - Adjust in hosting configuration
   - Increase app pool memory (IIS)

2. **Process in smaller batches:**
   - Split CSV into multiple files
   - Import one at a time

3. **Optimize media handling:**
   - Use URLs instead of ZIP for large media
   - Stream media instead of loading all at once

---

## Multi-CSV Issues

### Deduplication Not Working

**Problem:** Same media created multiple times

**Solutions:**

1. **Verify exact filename match:**
   - Case-sensitive: `logo.jpg` ≠ `Logo.jpg`
   - Extra spaces: `logo.jpg ` ≠ `logo.jpg`

2. **Check CSV structure:**
   - All CSVs must be in same ZIP
   - Cannot deduplicate across separate uploads

3. **Review results:**
   - Check GUID in results CSV
   - Same GUID = deduplicated

### Cross-File Hierarchy Not Working

**Problem:** Parent references across CSVs fail

**Solutions:**

1. **Use legacy IDs:**
   ```csv
   # CSV 1
   bulkUploadLegacyId,docTypeAlias,name
   parent-1,homePage,Home

   # CSV 2
   bulkUploadLegacyParentId,docTypeAlias,name
   parent-1,contentPage,Child
   ```

2. **Ensure both CSVs in same ZIP:**
   - Must upload together
   - Cannot reference across separate uploads

---

## Debugging Tips

### Enable Detailed Logging

Add to `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "BulkUpload": "Debug"
      }
    }
  }
}
```

### Check Umbraco Logs

1. Go to Settings → Log Viewer
2. Filter by "BulkUpload"
3. Look for error messages and stack traces

### Inspect Network Traffic

1. Open browser Developer Tools (F12)
2. Go to Network tab
3. Upload file
4. Check request/response for errors

### Validate CSV Format

Use online CSV validators:
- [CSV Lint](https://csvlint.io/)
- Ensure proper escaping of quotes and commas

### Test with Minimal CSV

Start with simplest possible CSV:

```csv
parentId,docTypeAlias,name
-1,homePage,Test
```

Add complexity incrementally to isolate issues.

### Check Results CSV

Always download and review results CSV:
- Shows which rows succeeded/failed
- Contains error messages
- Includes generated IDs

---

## Still Need Help?

If you're still experiencing issues:

1. **Search existing issues:**
   - [GitHub Issues](https://github.com/ClerksWell-Ltd/BulkUpload/issues)

2. **Create a new issue:**
   - Include Umbraco version
   - Include BulkUpload version
   - Provide sample CSV (anonymized)
   - Include error messages from log
   - Describe expected vs actual behavior

3. **Ask in discussions:**
   - [GitHub Discussions](https://github.com/ClerksWell-Ltd/BulkUpload/discussions)

4. **Check documentation:**
   - [Documentation Index](.github/docs/README.md)
   - [Media Import Guide](.github/docs/user-guides/media-import-guide.md)
   - [Custom Resolvers Guide](.github/docs/custom-resolvers-guide.md)
