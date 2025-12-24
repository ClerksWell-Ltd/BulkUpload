# Bulk Upload

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.BulkUpload?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.BulkUpload?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
[![GitHub license](https://img.shields.io/github/license/ClerksWell-Ltd/BulkUpload?color=8AB803)](../LICENSE)

# BulkUpload for Umbraco

BulkUpload is an Umbraco package that enables content editors and site administrators to import large volumes of content and media into Umbraco using CSV files. Designed for efficiency and flexibility, BulkUpload streamlines the process of creating and updating content nodes and media items, making it ideal for migrations, bulk updates, or onboarding new data.

It currently just works with Umbraco 13, but we are looking at releasing it for Umbraco 16/17 soon.

## Features

- **Content Import:** Upload CSV files or ZIP files (CSV + media) to create or update Umbraco content nodes with embedded images.
- **Multi-CSV Support:** Import multiple CSV files in a single ZIP upload with automatic media deduplication and cross-file hierarchy management.
- **Media Import:** Upload ZIP files containing CSV metadata and media files (images, documents, videos) for bulk media creation, or CSV-only for URL-based media.
- **Legacy Hierarchy Mapping:** Preserve parent-child relationships from legacy CMS systems across multiple CSV files using `bulkUploadLegacyId` and `bulkUploadLegacyParentId` columns.
- **Custom Mapping:** Supports mapping CSV columns to Umbraco content properties, including complex types.
- **Content Type Support:** Import data for different content types by specifying aliases and parent nodes.
- **Extensible Resolvers:** Includes a set of resolvers for handling various property types (e.g., text, dates, media, block lists, ZIP files).
- **Export Results:** Download CSV files containing IDs of imported content and media items for tracking and subsequent imports. Multi-CSV imports generate separate result files per source CSV.
- **Error Handling & Logging:** Provides feedback and logging for import operations to help diagnose issues.

## How It Works

1. Prepare one or more CSV files with columns matching your Umbraco content type properties.
2. Package your CSV file(s) in a ZIP file (optionally with media files).
3. Use the BulkUpload interface to upload your ZIP file.
4. The package processes all CSVs together, handling:
   - **Media deduplication** - Same media file referenced in multiple CSVs is created only once
   - **Hierarchy sorting** - Parent-child relationships work across CSV files using topological sort
   - **Legacy ID mapping** - Cross-file parent references using `bulkUploadLegacyParentId`
5. Data is mapped to content properties, and nodes are created or updated in the correct dependency order.
6. Download results as a single CSV (one file) or ZIP with separate CSVs per source file.

## Installation

Install via NuGet:

```ps1
dotnet add package Umbraco.Community.BulkUpload
```

Or download from the [Umbraco Marketplace](https://marketplace.umbraco.com/package/umbraco.community.bulkupload).

This script will install the package with Clean Starter Kit so you can test it straight away

```ps1
# Ensure we have the version specific Umbraco templates
dotnet new install Umbraco.Templates::13.10.0 --force

# Create solution/project
dotnet new sln --name "MySolution"
dotnet new umbraco --force -n "MyProject"  --friendly-name "Administrator" --email "admin@example.com" --password "1234567890" --development-database-type SQLite
dotnet sln add "MyProject"


#Add starter kit
dotnet add "MyProject" package clean --version 4.2.2

#Add BulkUpload package
dotnet add "MyProject" package Umbraco.Community.BulkUpload

#Add Packages
#Ignored Clean as it was added as a starter kit

dotnet run --project "MyProject"
#Running
```

In the umbraco backoffice, go to the users section and add the Bulk Upload section to the relevant group e.g. Administrators and refresh the page. You will see a new section called Bulk Upload.

## Using the tool

### Content Import

The package supports bulk importing content with optional media files:
- **CSV file** (content only) - Upload just the CSV when using URLs or existing media references
- **ZIP file** (new) - Upload a ZIP containing CSV + media files for content with embedded images/files

Sample files:
- <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-sample.csv?raw=true" download>CSV-only sample</a>

**How it works:**

1. **Prepare your content CSV** with required columns: `parentId`, `docTypeAlias`, `name`
2. **For content with media:**
   - Add media references using resolvers: `heroImage|zipFileToMedia`, `heroImage|urlToMedia`, or `heroImage|pathToMedia`
   - Use `zipFileToMedia` for media files packaged in the ZIP
   - Use `urlToMedia` for images from URLs
   - Use `pathToMedia` for images from file system paths
3. **Upload:**
   - **CSV only:** For content without media or using URL/path media references
   - **ZIP file:** Package CSV + media files together for content with embedded images
4. **View results:** Success message shows counts, download results CSV for imported content IDs

**Example CSV with ZIP media:**
```csv
parentId,docTypeAlias,name,heroImage|zipFileToMedia
1100,productPage,Red Widget,hero-red-widget.jpg
1100,productPage,Blue Widget,hero-blue-widget.jpg
```

Create a ZIP with this CSV and the media files (`hero-red-widget.jpg`, `hero-blue-widget.jpg`) and upload it via the Content Import tab.
- If it is successful you will see a green success message and can go to the content tree and find your new content.
- If it errors, it will show you a red message. You will be able to see the error details in the Log Viewer

### Media Import

The package supports bulk importing media files (images, documents, videos, etc.) from multiple sources:
- **ZIP file with single CSV** - Upload a ZIP containing one CSV metadata file and media files
- **ZIP file with multiple CSVs** - Upload multiple CSV files in one ZIP for organized imports with automatic deduplication
- **CSV file only** - Upload just a CSV when all media comes from external sources
- **File paths** - Import from local or network file system paths
- **URLs** - Download directly from HTTP/HTTPS URLs

**Multi-CSV Benefits:**
- Organize imports by category, department, or source system
- Automatic deduplication: same media file referenced in multiple CSVs is created only once
- Separate result files per source CSV for easy tracking

Sample files:
- <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-media-sample.csv?raw=true" download>Traditional media CSV (for use in ZIP)</a>
- <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-media-url-sample.csv?raw=true" download>CSV-only sample (URL-based media)</a>

**How it works:**

1. **Choose your upload method:**
   - **Option A (ZIP):** Bundle CSV + media files together
   - **Option B (CSV only):** Upload just CSV when using URLs or file paths

2. **Create a CSV file** with the following columns:
   - `fileName` - The exact filename of the media file in the ZIP (required for ZIP uploads, optional for URL/path imports)
   - `mediaSource|pathToStream` - Import from local/network file path (optional)
   - `mediaSource|urlToStream` - Download from URL (optional)
   - `parent` - Parent folder specification supporting:
     - Integer ID: `1150`
     - GUID: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`
     - Path: `/Products/Images/` (auto-creates folders)
     - Legacy `parentId` column still supported
   - Optional columns: `name`, `mediaTypeAlias`, plus any custom media properties (e.g., `altText`, `caption`)

3. **Upload:**
   - **For ZIP uploads:** Create a ZIP file containing the CSV and media files, then upload via the **Media Import** tab
   - **For CSV-only uploads:** Simply upload the CSV file directly (all media must use `mediaSource|urlToStream` or `mediaSource|pathToStream`)

4. **Download results** - After import, you can download a CSV with IDs of all created media items

**CSV Examples:**

**Traditional ZIP approach with auto-created folders:**
```csv
fileName,parent,name,altText,caption
product-hero.jpg,/Products/Images/,Product Hero Image,Red widget product,Main product photo
user-manual.pdf,/Docs/Manuals/,User Manual V2,,Product documentation
```

**Import from file paths with folder paths:**
```csv
mediaSource|pathToStream,parent,name,altText
C:/Assets/Images/product-hero.jpg,/Products/Images/,Product Hero Image,Red widget product
\\nas\share\media\user-manual.pdf,/Docs/Manuals/,User Manual V2,User guide
```

**Import from URLs with integer ID:**
```csv
mediaSource|urlToStream,parent,name,altText
https://cdn.example.com/images/hero.jpg,1150,Product Hero Image,Red widget product
https://example.com/docs/manual.pdf,1150,User Manual V2,User guide
```

**Mixed sources with flexible parent formats:**
```csv
fileName,mediaSource|pathToStream,mediaSource|urlToStream,parent,name
logo.png,,,/Brand/Logos/,Company Logo
,C:/Assets/banner.jpg,,1150,Homepage Banner
,,https://cdn.example.com/hero.jpg,/Marketing/Headers/,Hero Image
```

For more examples and detailed instructions, see the [samples directory](../samples/).

**Using imported media in content:**

After media import, download the results CSV which contains `mediaId`, `mediaGuid`, and `mediaUdi` for each imported item. You can then use these IDs in your content import CSV:

```csv
parentId,docTypeAlias,name,heroImage|mediaIdToMediaUdi
1100,productPage,Red Widget,2001
```

For detailed instructions, see the [Media Import Guide](../docs/media-import-guide.md).

## Resolvers

This package is basically an engine which you can create resolvers for and use them to convert the values from the CSV file into values that will be stored in Umbraco.

Here is the DateTime resolver:

```cs
namespace Umbraco.Community.BulkUpload.Resolvers;

public class DateTimeResolver : IResolver
{
    public string Alias() => "dateTime";

    public object Resolve(object value)
    {
        if (value is not string str || !DateTime.TryParse(str, out var dateTime))
            return string.Empty;

        // Format as ISO 8601 (e.g., "2025-09-12T14:30:00")
        return dateTime.ToString("o");
    }
}
```

In our [example CSV file](https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-sample.csv) there is a column called `articleDate` and it uses the `dateTime` resolver. The way we tell it to use it is by making the column header look like this:

`articleDate|dateTime`

### Creating your own Resolvers

Create a class in your project and inherit from the IResolver interface

```cs
using Umbraco.Community.BulkUpload.Resolvers;

public class ExampleResolver : IResolver
{
    public string Alias() => "example";

    public object Resolve(object value)
    {
        throw new NotImplementedException();
    }
}
```

Then implement the resolve method. You could make it do whatever you want. Get it to pull content from Umbraco, get it to generate content from an AI, anything really and then return it to be stored in Umbraco.

### Registering your resolver

If you want to use the resolver you created, you will need to create a composer and register it in the services like this:

```cs
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.BulkUpload.Resolvers;
internal class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // if you are just converting values and not depending on any services, use AddSingleton
        builder.Services.AddSingleton<IResolver, ExampleResolver>();

        // if you need to inject services into your resolver, use AddTransient
        builder.Services.AddTransient<IResolver, ExampleResolver>();
    }
}
```

### Using your resolver

In your CSV file add a column e.g. `title` and get it to use the `example` resolver like this:

`title|example`

## License

MIT &copy; ClerksWell Ltd

This project is licensed under the [MIT License](https://opensource.org/license/mit) which means you can basically do what you want with it. Also be aware of the third party dependencies below though

## Third-Party Dependencies

This project uses the following third-party libraries:

### CsvHelper

**Package:** CsvHelper
**Author:** Josh Close
**License:** Dual licensed under [Apache License 2.0](https://opensource.org/license/apache-2-0) or
[Microsoft Public License (MS-PL)](https://opensource.org/license/ms-pl-html)

CsvHelper is used as a NuGet dependency and is not modified in this project. Please refer to its license terms if you redistribute or bundle it with your software. The licenses are fairly permissive and should allow for commercial use and redistribution, but as always double check yourself.
