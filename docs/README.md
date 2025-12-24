# Documentation Index

Complete documentation for the BulkUpload package, organized by audience and purpose.

## Quick Links

- [Main README](../.github/README.md) - Package overview, installation, and basic usage
- [Changelog](../CHANGELOG.md) - Version history and release notes
- [Contributing Guidelines](../.github/CONTRIBUTING.md) - How to contribute to the project

---

## For Users

Documentation for content editors and site administrators using BulkUpload.

### Getting Started
- **[Main README](../.github/README.md)** - Installation, basic usage, and resolver guide
- **[Media Import Guide](./media-import-guide.md)** - Comprehensive guide for bulk media imports
  - Single and multi-CSV support
  - Media deduplication
  - Import from ZIP, file paths, or URLs
  - Results export and tracking

### Sample Files
- **[Sample Directory](../samples/)** - Example CSV files and templates
  - Content import samples
  - Media import samples
  - URL-based media samples

### Advanced Features
- **[Legacy Hierarchy Mapping](./LEGACY_HIERARCHY_MAPPING.md)** - Preserve parent-child relationships from legacy CMS systems
  - Using `bulkUploadLegacyId` and `bulkUploadLegacyParentId`
  - Cross-file hierarchy support
  - Migration from other CMS platforms

---

## For Contributors

Documentation for developers contributing to BulkUpload.

### Development Workflow

#### Quick References
- **[Quick Reference](./QUICK_REFERENCE.md)** - Essential commands for development and releases
  - Feature development workflow
  - Bug fix workflow
  - Cherry-pick guide
  - Version numbering
  - Commit message format

- **[Quick Reference: Release](./QUICK_REFERENCE_RELEASE.md)** - Command cheat sheet for releases
  - Creating releases
  - Cherry-picking changes
  - Testing and troubleshooting

#### Complete Guides
- **[Branching Strategy](./BRANCHING_STRATEGY.md)** - Multi-version branching and release strategy
  - Branch structure and purposes
  - Version-specific workflows
  - Cherry-picking guide
  - Code sharing strategies
  - Maintenance strategy

- **[Release Process](./RELEASE_PROCESS.md)** - Complete release process guide
  - Automated workflows
  - Manual steps
  - Release checklist
  - Troubleshooting

- **[Workflow Diagrams](./WORKFLOW_DIAGRAM.md)** - Visual workflow guides
  - Branch structure diagrams
  - Feature development flow
  - Bug fix flow
  - Cherry-pick workflow
  - Conflict resolution flow

### Contributing
- **[Contributing Guidelines](../.github/CONTRIBUTING.md)** - How to contribute to the project

---

## For Maintainers

Internal documentation for package maintainers and planning.

### Implementation Planning
- **[Implementation Recommendations](../IMPLEMENTATION_RECOMMENDATIONS.md)** - Technical recommendations for bulk upload improvements
  - Stream-based resolver architecture
  - Security considerations
  - Implementation checklist

- **[Bulk Upload Improvements](../BULK_UPLOAD_IMPROVEMENTS.md)** - Analysis of PathToMediaResolver and UrlToMediaResolver integration
  - Current architecture analysis
  - Proposed improvements
  - Use cases and examples

### Package Information
- **[NuGet README](./README_nuget.md)** - Package description for NuGet.org listing

---

## Documentation by Topic

### Content Import
- [Main README - Using the tool](../.github/README.md#using-the-tool)
- [Main README - Resolvers](../.github/README.md#resolvers)
- [Legacy Hierarchy Mapping](./LEGACY_HIERARCHY_MAPPING.md)

### Media Import
- [Media Import Guide](./media-import-guide.md)
- [Main README - Media Import](../.github/README.md#media-import)

### Development & Release
- [Branching Strategy](./BRANCHING_STRATEGY.md)
- [Release Process](./RELEASE_PROCESS.md)
- [Quick Reference](./QUICK_REFERENCE.md)
- [Quick Reference: Release](./QUICK_REFERENCE_RELEASE.md)
- [Workflow Diagrams](./WORKFLOW_DIAGRAM.md)

### Version History
- [Changelog](../CHANGELOG.md)

---

## GitHub Workflows

Automated workflows in `.github/workflows/`:

- **[release.yml](../.github/workflows/release.md)** - Automated release to NuGet
- **[post-release.yml](../.github/workflows/post-release.md)** - Post-release automation
- **[build.yml](../.github/workflows/build.md)** - Build and test automation

---

## External Resources

### Standards & Conventions
- [Conventional Commits](https://www.conventionalcommits.org/) - Commit message format
- [Semantic Versioning](https://semver.org/) - Version numbering
- [Keep a Changelog](https://keepachangelog.com/) - Changelog format

### Package Links
- [NuGet Package](https://www.nuget.org/packages/Umbraco.Community.BulkUpload)
- [GitHub Repository](https://github.com/ClerksWell-Ltd/BulkUpload)
- [GitHub Releases](https://github.com/ClerksWell-Ltd/BulkUpload/releases)
- [Umbraco Marketplace](https://marketplace.umbraco.com/package/bulkupload)

---

## Need Help?

- **Users**: Check the [Main README](../.github/README.md) and [Media Import Guide](./media-import-guide.md)
- **Contributors**: Start with [Quick Reference](./QUICK_REFERENCE.md) and [Branching Strategy](./BRANCHING_STRATEGY.md)
- **Issues**: Report at [GitHub Issues](https://github.com/ClerksWell-Ltd/BulkUpload/issues)
