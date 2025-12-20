# Changelog

All notable changes to BulkUpload will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Mapping

- **v1.x.x** - Umbraco 13 support
- **v2.x.x** - Umbraco 16 support (planned)
- **v3.x.x** - Umbraco 17 support (planned)

## [Unreleased]

### Added
- Multi-version branching strategy documentation

## [1.0.0] - 2025-XX-XX

### Added
- Initial release
- CSV Import functionality for Umbraco 13
- Custom mapping support for CSV columns to Umbraco properties
- Extensible resolver system
- Built-in resolvers:
  - DateTime resolver
  - Text resolver
  - Media resolver
  - Block list resolver
- Error handling and logging
- Bulk Upload section in Umbraco backoffice
- Sample CSV file for testing

### Documentation
- README with installation and usage instructions
- Resolver creation guide
- Sample CSV file

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
## [2.1.0] - 2025-12-20 (Umbraco 16)

### Added
- New CSV validation feature (both versions)

### Fixed
- [v13 only] Fixed compatibility issue with Umbraco 13.5
- [v16 only] Fixed API changes in Umbraco 16.2
```
