# Bulk Upload

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.BulkUpload?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.BulkUpload?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
[![GitHub license](https://img.shields.io/github/license/ClerksWell-Ltd/BulkUpload?color=8AB803)](../LICENSE)

# BulkUpload for Umbraco

BulkUpload is an Umbraco package that enables content editors and site administrators to import large volumes of content and media into Umbraco using CSV files. Designed for efficiency and flexibility, BulkUpload streamlines the process of creating and updating content nodes and media items, making it ideal for migrations, bulk updates, or onboarding new data.

It currently just works with Umbraco 13, but we are looking at releasing it for Umbraco 16/17 soon.

## Features

- **Content CSV Import:** Upload and process CSV files to create or update Umbraco content nodes.
- **Media ZIP Import:** Upload ZIP files containing CSV metadata and media files (images, documents, videos) for bulk media creation.
- **Custom Mapping:** Supports mapping CSV columns to Umbraco content properties, including complex types.
- **Content Type Support:** Import data for different content types by specifying aliases and parent nodes.
- **Extensible Resolvers:** Includes a set of resolvers for handling various property types (e.g., text, dates, media, block lists).
- **Export Results:** Download CSV files containing IDs of imported media items for use in subsequent content imports.
- **Error Handling & Logging:** Provides feedback and logging for import operations to help diagnose issues.

## How It Works

1. Prepare your CSV file with columns matching your Umbraco content type properties.
2. Use the BulkUpload interface to upload your CSV.
3. The package parses each row, maps data to content properties, and creates or updates nodes in Umbraco.

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

- You can <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-sample.csv?raw=true" download>download this sample CSV file</a>

- In the Bulk Upload section, click on the **Content Import** tab.

- Click on the upload button, choose your CSV file and then click on the import button.

- It will attempt to import the content for you.

- If it is successful you will see a green success message and can go to the content tree and find your new content.

- If it errors, it will show you a red message. You will be able to see the error details in the Log Viewer

### Media Import

The package now supports bulk importing media files (images, documents, videos, etc.) using a ZIP file approach.

- You can <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-media-sample.csv?raw=true" download>download this sample media CSV file</a>

**How it works:**

1. **Prepare your media files** - Collect all images, PDFs, videos, or other files you want to import
2. **Create a CSV file** with the following required columns:
   - `fileName` - The exact filename of the media file in the ZIP
   - `parentId` - The ID of the parent media folder in Umbraco
   - Optional columns: `name`, `mediaTypeAlias`, plus any custom media properties (e.g., `altText`, `caption`)
3. **Create a ZIP file** containing both the CSV and all media files
4. **Upload the ZIP** via the **Media Import** tab in the Bulk Upload dashboard
5. **Download results** - After import, you can download a CSV with IDs of all created media items

**CSV Example:**
```csv
fileName,parentId,name,altText,caption
product-hero.jpg,1150,Product Hero Image,Red widget product,Main product photo
user-manual.pdf,1151,User Manual V2,,Product documentation
```

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
