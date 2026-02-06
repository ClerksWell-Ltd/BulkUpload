# Documentation Index

Complete documentation for the BulkUpload package, organized by audience and purpose.

## Quick Links

- [Main README](../../README.md) - Package overview, installation, and quick start
- [Package README](.github/README.md) - Detailed usage guide (legacy location)
- [Changelog](../../CHANGELOG.md) - Version history and release notes
- [Contributing Guidelines](../CONTRIBUTING.md) - How to contribute to the project

---

## For Users

Documentation for content editors and site administrators using BulkUpload.

### Getting Started
- **[Main README](../../README.md)** - Installation, quick start, and sample data
- **[Package README](../README.md)** - Detailed usage instructions and resolver guide

### User Guides
- **[Media Import Guide](user-guides/media-import-guide.md)** - Comprehensive guide for bulk media imports
  - Single and multi-CSV support
  - Media deduplication
  - Import from ZIP, file paths, or URLs
  - Results export and tracking

- **[Legacy Hierarchy Mapping](user-guides/LEGACY_HIERARCHY_MAPPING.md)** - Preserve parent-child relationships from legacy CMS systems
  - Using `bulkUploadLegacyId` and `bulkUploadLegacyParentId`
  - Cross-file hierarchy support
  - Migration from other CMS platforms

- **[Content Picker Legacy IDs](user-guides/CONTENT_PICKER_LEGACY_IDS.md)** - Handle legacy content picker references
  - Mapping legacy IDs to Umbraco content
  - Content picker resolver usage

### Sample Files
- **[Sample Directory](../../samples/)** - Example CSV files and templates
  - Content import samples
  - Media import samples
  - URL-based media samples
  - Multi-CSV examples
  - Block list examples
  - Legacy hierarchy examples

---

## For Developers

Documentation for developers using BulkUpload in their projects.

### Custom Resolvers
- **[Main README - Resolvers](../README.md#resolvers)** - Creating custom resolvers
- **[Custom Resolvers Guide](custom-resolvers-guide.md)** - Detailed guide for extending BulkUpload

### Troubleshooting
- **[Troubleshooting Guide](troubleshooting.md)** - Common issues and solutions

---

## For Contributors

Documentation for developers contributing to BulkUpload.

### Getting Started
- **[Contributing Guidelines](../CONTRIBUTING.md)** - Development setup and workflow
- **[Architecture Overview](../CONTRIBUTING.md#architecture-overview)** - High-level architecture

### Architecture & Development

#### Current Architecture (v2.0.0+)
- **[Branching Strategy](developer-guides/BRANCHING_STRATEGY.md)** - **Main-branch workflow with multi-targeting**
  - Single branch, single codebase
  - Multi-targeting configuration (net8.0 and net10.0)
  - Conditional compilation for framework-specific code
  - Development workflows
  - Best practices

- **[Multi-Targeting Quick Start](developer-guides/MULTI_TARGETING_QUICK_START.md)** - **Quick reference** for multi-targeting
  - Essential build and test commands
  - Framework-specific code patterns
  - Common workflows
  - Troubleshooting
  - Branch structure and purposes
  - Version-specific workflows
  - Cherry-picking guide
  - Code sharing strategies
  - **Note**: As of v2.0.0, BulkUpload uses multi-targeting instead of multi-branch strategy

#### Quick References
- **[Quick Reference](developer-guides/QUICK_REFERENCE.md)** - Essential commands for development and releases
  - Feature development workflow
  - Bug fix workflow
  - Cherry-pick guide (legacy)
  - Version numbering
  - Commit message format

- **[Quick Reference: Release](developer-guides/QUICK_REFERENCE_RELEASE.md)** - Command cheat sheet for releases
  - Creating releases
  - Testing and troubleshooting

#### Complete Guides
- **[Release Process](developer-guides/RELEASE_PROCESS.md)** - Complete release process guide
  - Automated workflows
  - Manual steps
  - Release checklist
  - Troubleshooting

- **[Workflow Diagrams](developer-guides/WORKFLOW_DIAGRAM.md)** - Visual workflow guides
  - Branch structure diagrams
  - Feature development flow
  - Bug fix flow
  - Cherry-pick workflow
  - Conflict resolution flow

---

## For Maintainers

Internal documentation for package maintainers and planning.

### Internal Documentation
- **[Bulk Upload Improvements](internal/BULK_UPLOAD_IMPROVEMENTS.md)** - Analysis of PathToMediaResolver and UrlToMediaResolver integration
- **[Implementation Recommendations](internal/IMPLEMENTATION_RECOMMENDATIONS.md)** - Technical recommendations for improvements
- **[Core Package Implementation](internal/CORE_PACKAGE_IMPLEMENTATION.md)** - Core package implementation details
- **[Redesign Implementation](internal/REDESIGN_IMPLEMENTATION.md)** - UI redesign implementation
- **[UI Improvements V2](internal/UI_IMPROVEMENTS_V2.md)** - UI improvements documentation
- **[Umbraco 17 Compatibility Review](internal/UMBRACO_17_COMPATIBILITY_REVIEW.md)** - Compatibility analysis
- **[Umbraco 17 Migration Strategy](internal/UMBRACO_17_MIGRATION_STRATEGY.md)** - Migration plan for Umbraco 17 support

---

## Documentation by Topic

### Installation & Setup
- [Main README - Installation](../../README.md#installation)
- [Main README - Quick Test Setup](../../README.md#quick-test-setup)
- [Contributing - Development Setup](../CONTRIBUTING.md#development-setup)

### Content Import
- [Package README - Content Import](../README.md#content-import)
- [Legacy Hierarchy Mapping](user-guides/LEGACY_HIERARCHY_MAPPING.md)
- [Content Picker Legacy IDs](user-guides/CONTENT_PICKER_LEGACY_IDS.md)

### Media Import
- [Media Import Guide](user-guides/media-import-guide.md)
- [Package README - Media Import](../README.md#media-import)

### Custom Resolvers
- [Package README - Resolvers](../README.md#resolvers)
- [Custom Resolvers Guide](custom-resolvers-guide.md)

### Development & Release
- [Branching Strategy](developer-guides/BRANCHING_STRATEGY.md) - **Main-branch workflow with multi-targeting (v2.0.0+)**
- [Multi-Targeting Quick Start](developer-guides/MULTI_TARGETING_QUICK_START.md) - **Quick reference for multi-targeting**
- [Release Process](developer-guides/RELEASE_PROCESS.md) - Releasing from main branch
- [Workflow Diagrams](developer-guides/WORKFLOW_DIAGRAM.md) - Visual workflow guides

### Troubleshooting
- [Troubleshooting Guide](troubleshooting.md)
- [Package README - Error Handling](../README.md#error-handling)

### Version History
- [Changelog](../../CHANGELOG.md)

---

## GitHub Workflows

Automated workflows in `.github/workflows/`:

- **Build Workflow** - Build and test automation for PRs
- **Release Workflow** - Automated release to NuGet
- **Post-Release Workflow** - Post-release automation
- **Update NuGet Packages** - Dependency management
- **CodeQL Analysis** - Security scanning

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
- [Umbraco Marketplace](https://marketplace.umbraco.com/package/umbraco.community.bulkupload)

---

## Need Help?

- **Users**: Check the [Main README](../../README.md) and [Media Import Guide](user-guides/media-import-guide.md)
- **Developers**: See [Custom Resolvers Guide](custom-resolvers-guide.md) and [Troubleshooting](troubleshooting.md)
- **Contributors**: Start with [Contributing Guidelines](../CONTRIBUTING.md) and [Multi-Targeting Quick Start](developer-guides/MULTI_TARGETING_QUICK_START.md)
- **Issues**: Report at [GitHub Issues](https://github.com/ClerksWell-Ltd/BulkUpload/issues)
- **Discussions**: Ask questions at [GitHub Discussions](https://github.com/ClerksWell-Ltd/BulkUpload/discussions)
