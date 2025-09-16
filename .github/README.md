# Bulk Upload

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.BulkUpload?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.BulkUpload?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
[![GitHub license](https://img.shields.io/github/license/ClerksWell-Ltd/BulkUpload?color=8AB803)](../LICENSE)

# BulkUpload for Umbraco

BulkUpload is an Umbraco package that enables content editors and site administrators to import large volumes of content into Umbraco using CSV files. Designed for efficiency and flexibility, BulkUpload streamlines the process of creating and updating content nodes, making it ideal for migrations, bulk updates, or onboarding new data.

It currently just works with Umbraco 13, but we are looking at releasing it for Umbraco 16/17 soon.

## Features

- **CSV Import:** Upload and process CSV files to create or update Umbraco content nodes.
- **Custom Mapping:** Supports mapping CSV columns to Umbraco content properties, including complex types.
- **Content Type Support:** Import data for different content types by specifying aliases and parent nodes.
- **Extensible Resolvers:** Includes a set of resolvers for handling various property types (e.g., text, dates, media, block lists).
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

## Documentation

See the [docs/README_nuget.md](../docs/README_nuget.md) for more details and usage instructions.

## License

MIT © ClerksWell Ltd
