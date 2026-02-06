# Changelog

All notable changes to BulkUpload will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Mapping

- **v1.x.x** - Umbraco 13 only (single-target architecture)
- **v2.x.x** - Umbraco 13 & 17 (multi-targeting architecture)

## [Unreleased]

### Documentation
- Comprehensive documentation overhaul
- New top-level README.md with quick start guide
- Completed CONTRIBUTING.md with development setup
- Added troubleshooting guide
- Mermaid diagrams for import processes
- Updated all documentation to reflect multi-targeting

## [2.0.0] - 2025-01-XX

### 🎉 Major Release: Multi-Targeting Architecture

Version 2.0.0 represents a significant architectural change, moving from a multi-branch strategy to a unified multi-targeting approach.

### Added
- **Multi-targeting support** - Single package now supports both Umbraco 13 (net8.0) and Umbraco 17 (net10.0)
- **Dual frontend architecture** - AngularJS for V13, Lit web components for V17
- **Umbraco 17 support** - Full compatibility with Umbraco 17.x
- Automatic framework selection based on consuming project's target framework
- Framework-specific conditional compilation for version-specific code
- Separate test sites for Umbraco 13 and Umbraco 17
- V17 frontend built with Vite and TypeScript

### Changed
- **Breaking:** Package architecture moved from multi-branch to multi-targeting
- **Breaking:** Single NuGet package ID for both versions (Umbraco.Community.BulkUpload)
- Package version numbering unified across Umbraco versions
- Build process enhanced to support dual frontend builds
- Static web assets handled differently for net8.0 vs net10.0

### Documentation
- Multi-targeting architecture guide
- Multi-targeting quick start guide
- Updated branching strategy documentation (legacy reference)
- Updated release process for multi-targeting
- Enhanced workflow diagrams
- Framework-specific development guidelines

### Migration from v1.x

If you're upgrading from v1.x to v2.0.0:
- Simply update the package version - NuGet will automatically install the correct framework-specific version
- No code changes required in your Umbraco project
- All existing features and APIs remain compatible

## [1.0.0] - 2024-11-XX

### Added
- Initial release
- **CSV Import** - Bulk content import from CSV files
- **Media Import** - Import media from ZIP files with CSV metadata
- **Multi-CSV Support** - Import multiple CSV files in a single ZIP
- **Media Deduplication** - Automatically deduplicate media across CSV files
- **Legacy Hierarchy Mapping** - Preserve parent-child relationships using `bulkUploadLegacyId` and `bulkUploadLegacyParentId`
- **Custom Resolvers** - Extensible resolver system for transforming CSV values
- **Built-in Resolvers:**
  - `dateTime` - ISO 8601 date formatting
  - `boolean` - Boolean value conversion
  - `text` - Plain text (default)
  - `stringArray` - Comma-separated values to array
  - `mediaIdToMediaUdi` - Media ID to UDI conversion
  - `guidToMediaUdi` - GUID to Media UDI conversion
  - `zipFileToMedia` - Create media from ZIP files
  - `urlToMedia` - Download media from URLs
  - `pathToMedia` - Import media from file paths
  - `sampleBlockListContent` - Sample block list resolver
  - `objectToJson` - JSON object conversion
- **Content Type Support** - Import for any Umbraco content type
- **Block List Support** - Complex content types including Block Lists
- **Export Results** - Download CSV with imported content/media IDs
- **Multi-CSV Results** - Separate result files per source CSV in ZIP
- **Flexible Media Sources** - Support for ZIP files, URLs, and file system paths
- **Flexible Parent Specification** - Integer IDs, GUIDs, or folder paths
- **Auto-folder Creation** - Automatically create media folder hierarchies
- **Error Handling** - Comprehensive error reporting and logging
- **Bulk Upload Section** - Dedicated Umbraco backoffice section
- **AngularJS Dashboard** - User-friendly upload interface

### Documentation
- Comprehensive README with installation and usage
- Media import guide with examples
- Legacy hierarchy mapping guide
- Content picker legacy IDs guide
- Resolver creation guide
- Sample CSV files (content, media, URLs)
- Multi-CSV examples
- Branching strategy documentation
- Release process guide

---

## Changelog Guidelines

### Categories
- `Added` - New features
- `Changed` - Changes in existing functionality
- `Deprecated` - Soon-to-be removed features
- `Removed` - Removed features
- `Fixed` - Bug fixes
- `Security` - Security fixes

### Version-Specific Entries

When releasing for multiple Umbraco versions, use this format:

```markdown
## [1.2.0] - 2025-12-20 (Umbraco 13)
## [2.1.0] - 2025-12-20 (Umbraco 17)

### Added
- New CSV validation feature (both versions)

### Fixed
- [v13 only] Fixed compatibility issue with Umbraco 13.5
- [v17 only] Fixed API changes in Umbraco 17.2
```
