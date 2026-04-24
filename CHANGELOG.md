# Changelog

All notable changes to BulkUpload will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Mapping

- **v1.x.x** - Umbraco 13 only (single-target architecture)
- **v2.x.x** - Umbraco 13 & 17 (multi-targeting architecture)

## [Unreleased]

## [2.1.0] - 2026-04-24

### Security
- **Umbraco 17 (net10.0): require backoffice authentication on import endpoints.** In 2.0.0–2.0.6, `BulkUploadController` and `MediaImportController` inherited `ControllerBase` with no `[Authorize]` attribute and were routed at `/api/v1/content/...` and `/api/v1/media/...` — outside Umbraco's management-API path. That placement meant the endpoints were reachable without a backoffice session AND couldn't be protected by the standard Umbraco auth policies, because OpenIddict only validates tokens for paths under `/umbraco/`. The controllers are now routed via `[VersionedApiBackOfficeRoute]` under `/umbraco/management/api/v1/bulk-upload/...` and require the `SectionAccessContent` / `SectionAccessMedia` policies. Umbraco 13 (net8.0) was never affected — those builds inherit `UmbracoAuthorizedApiController`.

### Changed (Breaking — Umbraco 17 only)
- **API endpoint URLs moved under `/umbraco/management/api/v1/bulk-upload/...`.** This was required to plug the authorization gap above. If you call these endpoints directly from external scripts, CI jobs, or integrations, update your URLs:
  - `POST /api/v1/content/importall` → `POST /umbraco/management/api/v1/bulk-upload/content/importall`
  - `POST /api/v1/content/exportresults` → `POST /umbraco/management/api/v1/bulk-upload/content/exportresults`
  - `POST /api/v1/content/exportmediapreprocessingresults` → `POST /umbraco/management/api/v1/bulk-upload/content/exportmediapreprocessingresults`
  - `POST /api/v1/media/importmedia` → `POST /umbraco/management/api/v1/bulk-upload/media/importmedia`
  - `POST /api/v1/media/importmediafromzip` → `POST /umbraco/management/api/v1/bulk-upload/media/importmediafromzip`
  - `POST /api/v1/media/exportresults` → `POST /umbraco/management/api/v1/bulk-upload/media/exportresults`

  External callers must now include a valid backoffice bearer token (`Authorization: Bearer <token>`) obtained via the standard Umbraco management-API OAuth2 authorization_code + PKCE flow. The bundled backoffice dashboard has been updated automatically.

## [2.0.6] - 2026-04-22

### Added
- `pathToMediaPicker` resolver: emits Media Picker 3 array format for local or network file paths. Previously only `urlToMediaPicker` supported this shape, forcing callers to base64-encode file bytes as data URIs or host files on HTTP.
- `zipFileToMediaPicker` resolver: same as `pathToMediaPicker` but for files bundled in the upload ZIP.

### Fixed
- **Umbraco 17 media rendering**: media items created by `urlToMedia`, `urlToMediaPicker`, `pathToMedia`, `pathToMediaPicker`, `zipFileToMediaPicker`, and `multiBlockList` (image/carousel/iconlink) now store the URL-form path (`/media/abc/file.jpg`) on `umbracoFile` instead of the filesystem-relative path (`abc/file.jpg`). Previously the Umbraco 17 MediaPicker3 rendered as "no link" because the ImageCropper could not build an image URL from the stored value.
- **Umbraco 17 MediaPicker3 inside block list**: `urlToMediaPicker`, `pathToMediaPicker`, and `zipFileToMediaPicker` now emit the full picker item shape — `mediaTypeAlias`, `crops`, `focalPoint` are required for the v17 picker to render a link.
- **`objectToJson` resolver wrapping**: resolver string results are no longer auto-parsed into nested JSON arrays/objects. The v17 block list `values[].value` format stores MediaPicker3 values as escaped JSON strings, not as native arrays.

### Changed
- Shared the UDI → Media Picker 3 array conversion between the URL, path, and zip picker resolvers via a new internal `MediaUdiHelper`. No behavioural change to `urlToMediaPicker` output shape; only the extra fields now emitted.

## [2.0.0] - 2026-03-04

### 🎉 Major Release: Multi-Targeting Architecture

Version 2.0.0 represents a significant architectural change, moving from a multi-branch strategy to a unified multi-targeting approach.

### Added
- **Multi-targeting support** - Single package now supports both Umbraco 13 (net8.0) and Umbraco 17 (net10.0)
- **Dual frontend architecture** - AngularJS for V13, Lit web components for V17
- **Umbraco 17 support** - Full compatibility with Umbraco 17.x
- **Update mode for content** - Update existing content by GUID using `bulkUploadShouldUpdate` and `bulkUploadContentGuid` columns, with partial property updates and optional parent move via `bulkUploadParentGuid`
- **Update mode for media** - Update existing media by GUID using `bulkUploadShouldUpdate` and `bulkUploadMediaGuid` columns, with per-row create/update decision
- **Unified file upload for V13** - Single upload component for both CSV and ZIP files in Umbraco 13 frontend (ClientV13)
- **Swagger/OpenAPI documentation** - API documentation for V17 endpoints via Swashbuckle
- **CSV detection improvements** - Relaxed content CSV detection to require only `docTypeAlias` + `name` (parent optional), recognise update mode files
- **No-property update tracking** - Tracking and reporting when update mode rows have no property changes
- **New reserved columns** - `bulkUploadShouldUpdate`, `bulkUploadContentGuid`, `bulkUploadMediaGuid`, `bulkUploadParentGuid`, `bulkUploadSuccess`, `bulkUploadShouldPublish`
- **Update mode sample files** - `content-update-sample.csv` and `media-update-sample.csv`
- Automatic framework selection based on consuming project's target framework
- Framework-specific conditional compilation for version-specific code
- Separate test sites for Umbraco 13 and Umbraco 17
- V17 frontend built with Vite and TypeScript

### Changed
- **Breaking:** Package architecture moved from multi-branch to multi-targeting
- **Breaking:** Single NuGet package ID for both versions (Umbraco.Community.BulkUpload)
- **Breaking:** Merged `BulkUpload.Core` into `BulkUpload` - single project for all business logic
- Package version numbering unified across Umbraco versions
- Build process enhanced to support dual frontend builds
- Static web assets handled differently for net8.0 vs net10.0
- ZIP upload summary now shows total CSV count instead of breaking down by type

### Documentation
- Comprehensive documentation overhaul for v2.0.0 launch
- New top-level README.md with quick start guide
- Completed CONTRIBUTING.md with development setup
- Added troubleshooting guide
- Mermaid diagrams for import processes
- Multi-targeting architecture guide
- Multi-targeting quick start guide
- Update mode guide with examples and best practices
- Updated branching strategy documentation (legacy reference)
- Updated release process for multi-targeting
- Enhanced workflow diagrams
- Framework-specific development guidelines
- Updated all documentation to reflect multi-targeting
- Removed stale BulkUpload.Core references across all documentation
- Fixed `parentId` → `parent` across all user-facing docs and examples
- Fixed release workflow docs to reflect main-branch strategy
- Fixed broken links in Content Picker Legacy IDs guide

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
