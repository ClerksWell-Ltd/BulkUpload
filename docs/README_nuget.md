# Bulk Upload

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.BulkUpload?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.BulkUpload?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
[![GitHub license](https://img.shields.io/github/license/ClerksWell-Ltd/BulkUpload?color=8AB803)](../LICENSE)

# BulkUpload for Umbraco

BulkUpload is an Umbraco package that enables content editors and site administrators to import large volumes of content into Umbraco using CSV files. Designed for efficiency and flexibility, BulkUpload streamlines the process of creating and updating content nodes, making it ideal for migrations, bulk updates, or onboarding new data.

It currently just works with Umbraco 13, but we are looking at releasing it for Umbraco 16/17 soon.

## Features

- **CSV Import:** Upload and process CSV files to create or update Umbraco content nodes.
- **Multi-CSV Support:** Import multiple CSV files in a single ZIP upload with automatic deduplication and cross-file hierarchy management.
- **Custom Mapping:** Supports mapping CSV columns to Umbraco content properties, including complex types.
- **Content Type Support:** Import data for different content types by specifying aliases and parent nodes.
- **Legacy Hierarchy Mapping:** Preserve parent-child relationships from legacy CMS systems across multiple CSV files.
- **Media Import:** Bulk import media files with automatic deduplication across all CSV files.
- **Extensible Resolvers:** Includes a set of resolvers for handling various property types (e.g., text, dates, media, block lists).
- **Error Handling & Logging:** Provides feedback and logging for import operations to help diagnose issues.
- **Separate Results Export:** When importing multiple CSVs, results are exported as separate files for easy tracking.

## How It Works

1. Prepare one or more CSV files with columns matching your Umbraco content type properties.
2. Package your CSV file(s) in a ZIP file (optionally with media files for media imports).
3. Use the BulkUpload interface to upload your ZIP file.
4. The package processes all CSVs together, handling media deduplication and hierarchy sorting across all files.
5. Data is mapped to content properties, and nodes are created or updated in the correct dependency order.
6. Download results as a single CSV (one file) or ZIP (multiple files) with detailed import status.

## Installation

Install via NuGet:

```ps1
dotnet add package Umbraco.Community.BulkUpload
```

Or download from the [Umbraco Marketplace](https://marketplace.umbraco.com/package/bulkupload).

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

- You can <a href="https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/docs/bulk-upload-sample.csv?raw=true" download>download this sample CSV file</a>

- In the Bulk Upload section, click on the upload button, choose your CSV file and then click on the import button.

- It will attempt to import the content for you.

- If it is successful you will see a green success message and can go to the content tree and find your new content.

- If it errors, it will show you a red message. You will be able to see the error details in the Log Viewer

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
