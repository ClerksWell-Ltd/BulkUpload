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
