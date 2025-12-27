# Bulk Upload Media - Sample CSV Files

This directory contains sample CSV files demonstrating the different ways to use the bulk media upload functionality.

## Sample Files

### 1. bulk-upload-mixed-sources.csv
Demonstrates importing media from multiple sources in a single upload:
- Files from the ZIP archive (using `fileName` column)
- Files from local/network paths (using `mediaSource|pathToStream`)
- Files from URLs (using `mediaSource|urlToStream`)

**Usage:**
1. Create a ZIP file containing:
   - This CSV file
   - Any files referenced in the `fileName` column (logo.png, product.jpg)
2. Upload the ZIP through the Bulk Upload Media interface
3. Files will be imported from all three sources

### 2. bulk-upload-with-folder-paths.csv
Demonstrates using flexible parent folder specification to automatically organize media:
- Uses `parent` column with folder paths like `/Products/Bikes/`
- Automatically creates folder structures if they don't exist
- Media items are organized into the specified folders

**Usage:**
1. Create a ZIP file containing only this CSV file (no media files needed)
2. Ensure the file paths in the CSV point to actual files on your server
3. Upload the ZIP through the Bulk Upload Media interface
4. Media will be imported from the file system and organized into folders

### 3. test-page-multiblock-with-urls.csv
Demonstrates the enhanced MultiBlockListResolver that creates images directly from URLs:
- Image blocks with URLs instead of GUIDs
- Carousel blocks with multiple image URLs
- Icon link blocks with icon URLs
- No need to pre-create media items
- Automatic media type detection and caching

**Features Demonstrated:**
- `image::https://example.com/photo.jpg|Caption` - Creates image from URL
- `carousel::https://example.com/1.jpg,https://example.com/2.jpg` - Multiple images from URLs
- `iconlink::https://example.com/icon.png|https://link.com|Link Name` - Icon from URL
- Mixed with richtext, video, and code blocks
- All images are downloaded and created automatically

**Usage:**
1. Create a ZIP file containing only this CSV file
2. Upload through the Bulk Upload interface (content import, not media import)
3. Images will be automatically downloaded from URLs and media items created
4. The page content will reference the newly created media items

**Benefits:**
- Simplified CSV files - no GUID management needed
- Supports URLs, file paths, and GUIDs (backward compatible)
- Built-in caching prevents duplicate media creation
- Automatic media type detection from file extension

## CSV Column Reference

### Required Columns

- **fileName**: Name of file in ZIP archive (optional if using external source)
- **parent**: Parent folder specification - supports three formats:
  - Integer ID: `1150`
  - GUID: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`
  - Path: `/Products/Images/` (auto-creates folders if they don't exist)
  - **Note**: Legacy `parentId` column is still supported for backward compatibility

### Optional Columns

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

### Example 1: Simple ZIP Upload with Folder Paths
```csv
fileName,parent,name
logo.png,/Brand/Logos/,Company Logo
banner.jpg,/Marketing/Banners/,Homepage Banner
```

### Example 2: Import from Network Share with Auto-Created Folders
```csv
mediaSource|pathToStream,parent,name
\\\\nas.company.local\\assets\\logo.png,/Brand/Logos/,Company Logo
\\\\nas.company.local\\assets\\banner.jpg,/Marketing/Banners/,Homepage Banner
```

### Example 3: Import from CDN with Integer Parent ID
```csv
mediaSource|urlToStream,parent,name
https://cdn.example.com/images/logo.png,1150,Company Logo
https://cdn.example.com/images/banner.jpg,1150,Homepage Banner
```

### Example 4: Mixed Sources with Properties
```csv
fileName,mediaSource|pathToStream,mediaSource|urlToStream,parent,name,altText|text,tags|stringArray
local.jpg,,,/Gallery/Featured/,Local Image,From ZIP,"featured,homepage"
,C:/Assets/network.jpg,,/Products/Gallery/,Network Image,From network share,"products,gallery"
,,https://example.com/cdn.jpg,/Stock/External/,CDN Image,From CDN,"external,stock"
```

### Example 5: Organize with GUID Parent Reference
```csv
mediaSource|pathToStream,parent,name,altText|text
C:/Assets/Headers/tech-post.jpg,a1b2c3d4-e5f6-7890-abcd-ef1234567890,Tech Blog Header,Technology article header
C:/Assets/Headers/news-post.jpg,a1b2c3d4-e5f6-7890-abcd-ef1234567890,News Blog Header,News article header
```

## Workflow Example: Media from URLs → Content Import

This demonstrates a real-world workflow where you import media from URLs first, then use those media items in content.

### Files Involved

1. **features-media-from-urls.csv** - Media import with image URLs
2. **features-content-workflow.csv** - Content CSV with placeholder GUIDs

### Step-by-Step Workflow

#### Step 1: Import Media from URLs

1. Create a ZIP file containing **only** `features-media-from-urls.csv` (no media files needed since we're using URLs)
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

Edit `features-content-workflow.csv` and replace the placeholders:

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

Want to test immediately? The sample uses **via.placeholder.com** which provides reliable, simple placeholder images without query parameters.

Other placeholder image services you can use:
- **via.placeholder.com** - Simple, reliable placeholders (used in the sample)
- **placeholder.com** - Customizable placeholders
- **placehold.co** - Simple placeholder generator

The sample URLs will download real placeholder images during import!

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
