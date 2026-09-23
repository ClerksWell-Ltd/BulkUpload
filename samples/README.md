# Bulk Upload - Sample CSV Files

This directory contains sample CSV files demonstrating the different ways to use the bulk upload functionality, including both create and update modes for content and media.

## Create Mode vs Update Mode

BulkUpload supports two distinct modes of operation:

### Create Mode (Default)
Creates new content or media items. Requires:
- **Content**: `parent`, `docTypeAlias`, `name`
- **Media**: `fileName` (for ZIP uploads) or `mediaSource|urlToStream` / `mediaSource|pathToStream` (for external sources)

### Update Mode
Updates existing content or media items. Requires:
- **Content**: `bulkUploadContentGuid` plus at least one of `bulkUploadShouldUpdate`, `bulkUploadShouldPublish` or `bulkUploadShouldUnpublish`
- **Media**: `bulkUploadShouldUpdate=true`, `bulkUploadMediaGuid`, `parent`, `name`

**Key Points:**
- Update mode uses the GUID to locate the specific item to update
- `bulkUploadShouldUpdate=true` is what writes the row's name, parent and property values to existing content
- `bulkUploadShouldPublish` and `bulkUploadShouldUnpublish` change the publish state of content, with or without a data update
- Missing columns won't be modified (partial updates are supported)

See [Publish state](../.github/docs/user-guides/UPDATE_MODE_GUIDE.md#publish-state) in the update mode guide for how the three columns combine.

## Sample Files

### content-upload-basic.csv
Creates a single content item at the root and publishes it:
- `name` and `docTypeAlias` identify the new item
- `bulkUploadShouldPublish=true` publishes it; with `false` (or without the column) it would be saved as a draft
- `title` and `subtitle` are set as property values

Replace `content` with a document type alias from your site before uploading.

### content-unpublish-basic.csv
Unpublishes existing content without changing its data:
- `bulkUploadContentGuid` identifies each item
- `bulkUploadShouldUnpublish=true` unpublishes it
- There is no `bulkUploadShouldUpdate` column, so no data is written; `name` is only there to make the file readable

Replace the sample GUIDs with content GUIDs from your Umbraco instance (the results CSV of an earlier import includes them).

### media-upload-from-zip-file.zip
A media import: a CSV (`parent`, `name`, `fileName`) together with the image files it references. Upload it through the media import.

### media-only.zip
A ZIP of media files in folders, with no CSV.

### multi-csv-with-legacy-content-pickers.zip
A multi-CSV content import that builds a small blog (home, article list, articles, authors and categories) across several CSV files:
- `bulkUploadLegacyId` and `bulkUploadLegacyParentId` build the hierarchy across files
- `legacyContentPicker` and `legacyContentPickers` link articles to authors and categories
- `zipFileToMedia` creates author images from files in the ZIP
- `bulkUploadShouldPublish=TRUE` publishes every item

## CSV Column Reference

### Create Mode - Required Columns

**Content CSV:**
- **parent**: Parent content ID, GUID, or path (e.g., `1100`, `a1b2c3d4-...`, `/News/2024/`)
- **docTypeAlias**: Content type alias (e.g., `articlePage`, `productPage`)
- **name**: Content item name

**Media CSV:**
- **fileName**: Name of file in ZIP archive (optional if using external source)
- **parent**: Parent folder specification - supports three formats:
  - Integer ID: `1150`
  - GUID: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`
  - Path: `/Products/Images/` (auto-creates folders if they don't exist)
  - **Note**: Legacy `parentId` column is still supported for backward compatibility

### Update Mode - Required Columns

**Content CSV:**
- **bulkUploadContentGuid**: GUID of the content item to update
- **bulkUploadShouldUpdate**: `true` to write the row's name, parent and property values to the item
- **bulkUploadShouldPublish**: `true` to publish the item
- **bulkUploadShouldUnpublish**: `true` to unpublish the item (wins over `bulkUploadShouldPublish`)

At least one of the three flags must be `true`, or the row is skipped. `true`, `yes` and `1` all count as true.

**Media CSV:**
- **bulkUploadShouldUpdate**: Must be set to `true` to enable update mode
- **bulkUploadMediaGuid**: GUID of the media item to update
- **parent**: Parent folder ID, GUID, or path (used to verify correct item)
- **name**: Name to match existing item (used to verify correct item)

### Optional Columns

**Content CSV:**
- **bulkUploadShouldPublish**: `true` to publish a new item; otherwise it is saved as a draft

**Media CSV:**
- **name**: Display name for the media item (defaults to fileName)
- **mediaTypeAlias**: Umbraco media type (auto-detected from extension if not provided)

### External Source Columns

- **mediaSource|pathToStream**: Import from local/network file path
  - Example: `C:/Assets/image.jpg`
  - Example with folder: `C:/Assets/image.jpg|/Gallery/Photos/`
  - Supports absolute paths, relative paths, and UNC network paths

- **mediaSource|urlToStream**: Download and import from URL
  - Example: `https://cdn.example.com/image.jpg`
  - Example with folder: `https://cdn.example.com/image.jpg|/Downloads/`
  - Only HTTP and HTTPS protocols are supported

### Property Columns

Any additional columns can be used to set properties on media items using resolvers:

- **propertyName|text**: Plain text value
- **propertyName|stringArray**: Comma-separated array
- **propertyName|dateTime**: Date/time value
- **propertyName|objectToJson**: JSON object

## Security Notes

### File Path Security
- Access to system directories is blocked for security
- Blocked paths include: `/windows/system32`, `/etc/`, `/var/`, etc.
- Ensure your application has read permissions for the specified paths

### URL Security
- Private IP addresses and localhost are blocked to prevent SSRF attacks
- Only HTTP and HTTPS protocols are supported
- Downloads timeout after 30 seconds
- Consider configuring allowed domains for production use

## Examples

### Update Mode Examples

#### Example 1: Update Content Properties
```csv
bulkUploadShouldUpdate,bulkUploadContentGuid,parent,name,title,description|text
true,a1b2c3d4-e5f6-7890-abcd-ef1234567890,1100,My Article,Updated Title,New description text
```

#### Example 2: Unpublish Content Without Changing It
```csv
bulkUploadContentGuid,bulkUploadShouldUnpublish,name
a1b2c3d4-e5f6-7890-abcd-ef1234567890,true,My Article
```
No `bulkUploadShouldUpdate` column, so the item's data is left alone and only its publish state changes. This is [content-unpublish-basic.csv](content-unpublish-basic.csv).

#### Example 3: Update Content and Keep the Published Version Serving
```csv
bulkUploadShouldUpdate,bulkUploadContentGuid,title
true,a1b2c3d4-e5f6-7890-abcd-ef1234567890,Draft title for review
```
No publish column, so the change is saved as a draft and the page stays published with its current content until someone publishes the draft.

#### Example 4: Update Media Properties
```csv
bulkUploadShouldUpdate,bulkUploadMediaGuid,parent,name,altText|text,tags|stringArray
true,d4e5f6a7-b8c9-0123-def0-123456789abc,1150,Logo,New alt text,"tag1,tag2,tag3"
```

### Create Mode Examples

#### Example 5: Simple ZIP Upload with Folder Paths
```csv
fileName,parent,name
logo.png,/Brand/Logos/,Company Logo
banner.jpg,/Marketing/Banners/,Homepage Banner
```

#### Example 6: Import from Network Share with Auto-Created Folders
```csv
mediaSource|pathToStream,parent,name
\\\\nas.company.local\\assets\\logo.png,/Brand/Logos/,Company Logo
\\\\nas.company.local\\assets\\banner.jpg,/Marketing/Banners/,Homepage Banner
```

#### Example 7: Import from CDN with Integer Parent ID
```csv
mediaSource|urlToStream,parent,name
https://cdn.example.com/images/logo.png,1150,Company Logo
https://cdn.example.com/images/banner.jpg,1150,Homepage Banner
```

#### Example 8: Mixed Sources with Properties
```csv
fileName,mediaSource|pathToStream,mediaSource|urlToStream,parent,name,altText|text,tags|stringArray
local.jpg,,,/Gallery/Featured/,Local Image,From ZIP,"featured,homepage"
,C:/Assets/network.jpg,,/Products/Gallery/,Network Image,From network share,"products,gallery"
,,https://example.com/cdn.jpg,/Stock/External/,CDN Image,From CDN,"external,stock"
```

#### Example 9: Organize with GUID Parent Reference
```csv
mediaSource|pathToStream,parent,name,altText|text
C:/Assets/Headers/tech-post.jpg,a1b2c3d4-e5f6-7890-abcd-ef1234567890,Tech Blog Header,Technology article header
C:/Assets/Headers/news-post.jpg,a1b2c3d4-e5f6-7890-abcd-ef1234567890,News Blog Header,News article header
```

## Workflow Example: Media from URLs → Content Import

This demonstrates a real-world workflow where you import media from URLs first, then use those media items in content.

### Files Involved

You write two CSVs for this workflow:

1. **A media CSV** - uses `mediaSource|urlToStream` to import images from URLs
2. **A content CSV** - references those images by GUID, using placeholders until the media exists

### Step-by-Step Workflow

#### Step 1: Import Media from URLs

1. Create a ZIP file containing **only** your media CSV (no media files needed since we're using URLs)
2. Go to Bulk Upload dashboard → **Media Import** tab
3. Upload the ZIP file
4. Wait for import to complete
5. Download the results CSV

#### Step 2: Extract Media GUIDs

Open the results CSV and note the `bulkUploadMediaGuid` values:

```csv
bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,bulkUploadMediaUdi,...
feature-hero.jpg,true,abc123-...,umb://media/abc123...,...
image-row-example.jpg,true,def456-...,umb://media/def456...,...
carousel-image-1.jpg,true,ghi789-...,umb://media/ghi789...,...
...
```

#### Step 3: Update Content CSV with Real GUIDs

Edit your content CSV and replace the placeholders:

- `REPLACE-WITH-FEATURE-HERO-GUID` → `abc123-...` (from feature-hero.jpg)
- `REPLACE-WITH-IMAGE-ROW-EXAMPLE-GUID` → `def456-...` (from image-row-example.jpg)
- `REPLACE-WITH-CAROUSEL-1-GUID` → `ghi789-...` (from carousel-image-1.jpg)
- And so on for all carousel images...

#### Step 4: Import Content

1. Create a ZIP file containing your updated content CSV
2. Go to Bulk Upload dashboard → **Bulk Upload** tab
3. Upload the ZIP file
4. Your content will be created with all the imported images properly linked!

### Why This Approach?

This two-step process is useful when:
- Media files are hosted on external CDNs or URLs
- You want to organize media into folders using path syntax (`/Features/Images/`)
- You need the media GUIDs to reference in content imports
- You're migrating from another CMS and have media URLs

### Quick Test

Want to test immediately? Point your media CSV at a placeholder image service:
- **via.placeholder.com** - Simple, reliable placeholders
- **placeholder.com** - Customizable placeholders
- **placehold.co** - Simple placeholder generator

The URLs will download real placeholder images during import.

## Tips

1. **Empty ZIP Files**: When using only external sources (paths or URLs), you can upload a ZIP containing just the CSV file.

2. **Flexible Parent Specification**: The `parent` column accepts three formats:
   - **Integer ID**: `1150` - Direct media folder ID
   - **GUID**: `a1b2c3d4-e5f6-7890-abcd-ef1234567890` - Folder GUID
   - **Path**: `/Gallery/Photos/` - Auto-creates folder structure if it doesn't exist

3. **Error Handling**: Check the import results CSV for detailed error messages if any imports fail.

4. **Performance**: Downloading from URLs may take longer than local files. Consider the timeout settings for large files.

5. **Backward Compatibility**:
   - Existing CSV files using `parentId` column still work
   - Files with only `fileName` continue to work without modification
   - Mix old and new formats if needed
