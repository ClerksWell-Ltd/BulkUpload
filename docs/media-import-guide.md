# Media Import Guide

The Bulk Upload package supports importing media files (images, documents, videos, etc.) in bulk using a ZIP file containing CSV metadata files and the actual media files.

## Multi-CSV Support

You can include **multiple CSV files** in a single ZIP upload. This enables you to:
- Organize imports by category, department, or source system
- Process large batches in logical groups
- Maintain separate metadata files while ensuring media deduplication across all files

When multiple CSV files are included, the system:
1. **Processes all CSV files together** - All media references from all CSVs are gathered first
2. **Deduplicates media across all files** - If `image.jpg` appears in multiple CSVs, it's created only once
3. **Creates media items** - All unique media items are created before any content processing
4. **Exports results per CSV** - Each source CSV gets its own results file in a ZIP

## How It Works

1. **Prepare your media files** - Collect all images, PDFs, videos, or other files you want to import
2. **Create one or more CSV files** - Define metadata for each media item (see format below)
3. **Create a ZIP file** - Package the CSV(s) and all media files together
4. **Upload via dashboard** - Use the "Media Import" tab in the Bulk Upload dashboard
5. **Download results** - After import, download results (single CSV or ZIP with multiple CSVs)

## CSV Format

### Required Columns

| Column | Description | Example |
|--------|-------------|---------|
| `fileName` | The exact filename of the media file in the ZIP | `product-image.jpg` |
| `parentId` | The ID of the parent media folder in Umbraco | `1150` |

### Optional Columns

| Column | Description | Example |
|--------|-------------|---------|
| `name` | Display name in Umbraco (defaults to fileName if empty) | `Product Hero Image` |
| `mediaTypeAlias` | Umbraco media type (auto-detected from extension if empty) | `Image`, `File`, `Video` |
| `altText` | Alt text for images | `Red widget product` |
| `caption` | Caption for images | `Main product photo` |
| `bulkUploadLegacyId` | Legacy CMS identifier for tracking/reference (not persisted as a media property) | `old-cms-123` |

### Additional Property Columns

You can include any custom media property by adding columns with the property alias. Use the pipe syntax to specify a resolver:

```csv
fileName,parentId,name,tags|stringArray,publishDate|dateTime
image1.jpg,1150,Hero Image,"marketing,featured","2024-01-15"
```

### Auto-Detection of Media Types

If you don't specify `mediaTypeAlias`, the system automatically detects the media type based on file extension:

- **Image**: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.svg`
- **Video**: `.mp4`, `.avi`, `.mov`, `.wmv`, `.webm`
- **Audio**: `.mp3`, `.wav`, `.ogg`, `.wma`
- **File**: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt` (and any other extension)

## Sample CSV

```csv
fileName,parentId,name,mediaTypeAlias,altText,caption,bulkUploadLegacyId
product-hero.jpg,1150,Product Hero Image,Image,Red widget product photo,Main product showcase image,old-123
product-thumbnail.jpg,1150,Product Thumbnail,Image,Red widget thumbnail,Product thumbnail view,old-124
banner-homepage.png,1150,Homepage Banner,Image,Welcome to our site,Hero section banner,
user-manual.pdf,1151,User Manual V2,File,,Product documentation,old-125
brochure.pdf,1151,Marketing Brochure,File,,Sales material,
logo.svg,1150,Company Logo,Image,Company logo,Corporate branding,old-126
```

See `docs/bulk-upload-media-sample.csv` for a complete example.

## Creating the ZIP File

### Single CSV Import

Your ZIP file structure should look like this:

```
media-import.zip
├── media-data.csv              # CSV with metadata (any name ending in .csv)
├── product-hero.jpg            # Media files referenced in CSV
├── product-thumbnail.jpg
├── banner-homepage.png
├── user-manual.pdf
├── brochure.pdf
└── logo.svg
```

### Multiple CSV Import

When using multiple CSV files, organize your ZIP like this:

```
media-import.zip
├── marketing-images.csv        # First CSV with marketing media
├── product-images.csv          # Second CSV with product media
├── documents.csv               # Third CSV with documents
├── marketing/
│   ├── banner-homepage.png
│   └── hero-campaign.jpg
├── products/
│   ├── product-hero.jpg
│   └── product-thumbnail.jpg
└── docs/
    ├── user-manual.pdf
    └── brochure.pdf
```

**Important Notes:**
- The ZIP can contain one or more CSV files
- All files referenced in any CSV's `fileName` column must exist in the ZIP
- Files can be in subdirectories within the ZIP
- CSV filenames can be anything ending in `.csv`
- **Deduplication:** If the same filename appears in multiple CSVs, the media is created only once and reused

## Finding Parent IDs

To find the parent folder ID:

1. Go to the Media section in Umbraco
2. Right-click on the folder where you want to import media
3. Select "Info" or view the URL - the ID will be shown or visible in the browser address bar
4. Use this ID in the `parentId` column of your CSV

Alternatively, use `-1` to import to the root of the Media section.

## Import Results

After import completes, you'll see:
- **Total Count**: Number of media items across all CSVs
- **Success Count**: Number successfully imported
- **Failure Count**: Number that failed

### Single CSV Results

Click "Download Results CSV" to get a detailed report. The report includes:
- **BulkUpload result columns**: Status, IDs, and error information
- **Original CSV columns**: All columns from your uploaded CSV with their original values (including resolver syntax in headers)

Example with a simple input CSV:
```csv
fileName,parentId,name,bulkUploadLegacyId
product-hero.jpg,1150,Product Hero,old-123
product-error.jpg,1150,Product Error,old-124
```

Results in this report CSV:
```csv
bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,bulkUploadMediaUdi,bulkUploadErrorMessage,bulkUploadLegacyId,fileName,parentId,name,bulkUploadLegacyId
product-hero.jpg,true,a1b2c3d4-e5f6-7890-abcd-ef1234567890,umb://media/a1b2c3d4e5f67890abcdef1234567890,,old-123,product-hero.jpg,1150,Product Hero,old-123
product-error.jpg,false,,,File not found in ZIP archive: product-error.jpg,old-124,product-error.jpg,1150,Product Error,old-124
```

Example with custom properties and resolvers:
```csv
fileName,parentId,name,altText,tags|stringArray,bulkUploadLegacyId
hero.jpg,1150,Hero Image,Alt text here,"tag1,tag2",old-123
```

Results in:
```csv
bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,bulkUploadMediaUdi,bulkUploadErrorMessage,bulkUploadLegacyId,fileName,parentId,name,altText,tags|stringArray,bulkUploadLegacyId
hero.jpg,true,a1b2c3d4-e5f6-7890-abcd-ef1234567890,umb://media/a1b2c3d4e5f67890abcdef1234567890,,old-123,hero.jpg,1150,Hero Image,Alt text here,"tag1,tag2",old-123
```

The report preserves your original column names (including resolver syntax like `tags|stringArray`) and values, making it easy to correlate results with your source data.

### Multiple CSV Results

When you upload a ZIP with multiple CSV files, the results download as a ZIP file containing:
- **Separate CSV files** - One result file per source CSV with the naming pattern: `{source-csv-name}-import-results.csv`
- **Same format** - Each result CSV has the same structure as single CSV results
- **Source tracking** - A `SourceCsvFileName` column identifies which original CSV the row came from

**Example:** Upload `media-import.zip` containing:
- `marketing-images.csv` (10 items)
- `product-images.csv` (15 items)
- `documents.csv` (5 items)

**Download:** `media-import-import-results.zip` containing:
- `marketing-images-import-results.csv` (10 rows)
- `product-images-import-results.csv` (15 rows)
- `documents-import-results.csv` (5 rows)

This separation makes it easy to:
- Review results for each source separately
- Share results with different teams or departments
- Re-import only failed items from a specific source
- Track which original CSV each media item came from

### Media Deduplication Across CSVs

**Important:** When the same media file is referenced in multiple CSV files:

1. **Created once** - The media item is created only the first time it's encountered
2. **Same GUID for all** - All CSVs referencing that file receive the same `bulkUploadMediaGuid` and `bulkUploadMediaUdi`
3. **No duplicates** - This prevents duplicate media items in your Media library

**Example:** Both `marketing-images.csv` and `product-images.csv` reference `logo.jpg`:

```csv
# marketing-images-import-results.csv
bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,SourceCsvFileName
logo.jpg,true,abc123-def456-...,marketing-images.csv

# product-images-import-results.csv
bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,SourceCsvFileName
logo.jpg,true,abc123-def456-...,product-images.csv
```

Both rows show `bulkUploadSuccess=true` and the **same GUID**, confirming the media was created once and reused.

## Using Imported Media IDs

The results CSV provides two identifiers for each imported media item:

1. **bulkUploadMediaGuid** - GUID (e.g., `a1b2c3d4-e5f6-7890-abcd-ef1234567890`)
2. **bulkUploadMediaUdi** - UDI format (e.g., `umb://media/a1b2c3d4e5f67890abcdef1234567890`)

You can use these in a subsequent content import CSV by referencing the GUID:

```csv
parentId,docTypeAlias,name,heroImage|guidToMediaUdi
1100,productPage,Red Widget,a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

## Error Handling

Common errors and solutions:

| Error | Solution |
|-------|----------|
| "No CSV file found in ZIP archive" | Ensure your ZIP contains a `.csv` file |
| "File not found in ZIP archive: filename.jpg" | Check that the filename in CSV exactly matches the file in ZIP (case-sensitive) |
| "Media type 'CustomType' not found" | Verify the mediaTypeAlias exists in your Umbraco installation |
| "Invalid import object: Missing required fields" | Ensure each row has both `fileName` and `parentId` |

## Tips

1. **Test with a small batch first** - Import 5-10 items to verify your CSV format is correct
2. **Use consistent naming** - Keep filenames simple and avoid special characters
3. **Check file sizes** - Very large files may timeout; consider splitting into multiple ZIPs
4. **Backup first** - Always backup your media folder before bulk operations
5. **Use the results CSV** - Keep the results CSV for reference when linking media to content

## Advanced: Custom Properties

If your media type has custom properties, you can set them using the resolver syntax:

```csv
fileName,parentId,photographer,dateTaken|dateTime,tags|stringArray
photo1.jpg,1150,John Smith,"2024-03-15","landscape,sunset,beach"
photo2.jpg,1150,Jane Doe,"2024-03-16","portrait,studio"
```

Available resolvers:
- `text` - Plain text (default)
- `boolean` - True/false values
- `dateTime` - Dates
- `stringArray` - Comma-separated values converted to array
- `objectToJson` - Complex JSON objects
- And more (see main documentation)

## Technical Details: Processing Order

Understanding the processing order is important when working with multiple CSV files:

### 1. Gather Phase
- **All CSV files** are read into memory
- Records from all CSVs are collected with source tracking
- No media or content is created yet

### 2. Media Preprocessing Phase
- **All media references** from all CSVs are extracted together
- Deduplication happens using a case-insensitive dictionary
- Each unique media reference is created **only once**
- Media GUIDs are cached for later use

### 3. Content Creation Phase (if applicable)
- Import objects are created from all CSV records
- Hierarchy is validated and sorted across all CSVs
- Content items are created in dependency order (parents before children)

This "gather-first" approach ensures:
- **No duplicate media** across CSV files
- **Correct parent-child relationships** even when split across files
- **Efficient processing** with a single pass through all data
- **Global consistency** in hierarchy and media references

### Media Cache Behavior

The media item cache (`MediaItemCache`):
- Uses case-insensitive comparison for file paths and URLs
- Is cleared at the start of each import to ensure fresh state
- Persists across all CSVs within a single import operation
- Maps original references to Umbraco media GUIDs

**Example:**
```
CSV1: image.jpg → First encounter → Creates media → Cache: "image.jpg" → Guid(ABC)
CSV2: image.jpg → Found in cache → Reuses Guid(ABC) → No creation
CSV3: IMAGE.JPG → Case-insensitive match → Reuses Guid(ABC) → No creation
```
