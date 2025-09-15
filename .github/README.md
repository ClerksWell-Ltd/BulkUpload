# Bulk Upload

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.BulkUpload?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.BulkUpload?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
[![GitHub license](https://img.shields.io/github/license/prjseal/BulkUpload?color=8AB803)](../LICENSE)

# BulkUpload for Umbraco

BulkUpload is an Umbraco package that enables content editors and site administrators to import large volumes of content into Umbraco using CSV files. Designed for efficiency and flexibility, BulkUpload streamlines the process of creating and updating content nodes, making it ideal for migrations, bulk updates, or onboarding new data.

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

<img alt="..." src="https://github.com/prjseal/BulkUpload/blob/develop/docs/screenshots/screenshot.png">
-->

Or download from the [Umbraco Marketplace](https://marketplace.umbraco.com/package/bulkupload).

## Documentation

See the [docs/README_nuget.md](../docs/README_nuget.md) for more details and usage instructions.

## License

MIT © Paul Seal
