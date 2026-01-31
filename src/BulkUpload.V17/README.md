# BulkUpload.V17 - Umbraco 17 Package

This is the Umbraco 17 version of the BulkUpload package, featuring a modern Lit-based frontend and the same powerful backend as the Umbraco 13 version.

## Package Structure

```
BulkUpload.V17/
├── Client/                              # TypeScript/Lit frontend source
│   ├── src/
│   │   ├── components/                  # Lit web components
│   │   │   └── bulk-upload-dashboard.element.ts
│   │   ├── services/                    # Business logic services
│   │   │   └── bulk-upload.service.ts
│   │   ├── api/                         # API client
│   │   │   └── bulk-upload-api.ts
│   │   ├── utils/                       # Utility functions
│   │   │   ├── file.utils.ts
│   │   │   └── result.utils.ts
│   │   ├── manifests.ts                 # Extension registration
│   │   └── index.ts                     # Entry point
│   ├── package.json                     # NPM dependencies
│   ├── vite.config.ts                   # Vite bundler config
│   └── tsconfig.json                    # TypeScript config
│
├── App_Plugins/BulkUpload/              # Built frontend assets
│   ├── dist/
│   │   └── bundle.js                    # Compiled frontend (built by Vite)
│   ├── lang/
│   │   └── en.xml                       # Localization
│   └── umbraco-package.json             # Umbraco 17 manifest
│
├── BulkUploadComposer.cs                # DI registration
└── BulkUpload.V17.csproj                # Project file

Dependencies:
└── BulkUpload.Core (net10.0)            # Shared business logic
```

## Key Differences from V13

### Frontend

1. **Framework**: Lit web components instead of AngularJS
2. **Build System**: Vite instead of no build system
3. **Language**: TypeScript instead of JavaScript
4. **Module System**: ES modules instead of IIFE/global variables

### Backend

1. **Packages**: Uses `Umbraco.Cms` + `Umbraco.Cms.Api.Management` instead of separate `Web.Website` and `Web.BackOffice` packages
2. **Section Registration**: Sections defined in `umbraco-package.json` instead of C# `ISection` interface
3. **No ManifestFilter**: Umbraco 17 auto-discovers `umbraco-package.json`

## Build Process

### Frontend Build

```bash
cd Client
npm install
npm run build
```

This generates `App_Plugins/BulkUpload/dist/bundle.js`.

### Backend Build

```bash
dotnet build BulkUpload.V17.csproj -c Release
```

The frontend build is automatically triggered before the .NET build via the `BuildFrontend` MSBuild target.

### Package Creation

```bash
dotnet pack BulkUpload.V17.csproj -c Release
```

Output: `bin/Release/Umbraco.Community.BulkUpload.V17.2.0.0.nupkg`

## Development Workflow

### Frontend Development

```bash
cd Client
npm run dev
```

This runs Vite in watch mode, automatically rebuilding on file changes.

### Backend Development

Use the standard .NET development workflow:

```bash
dotnet build
dotnet run --project ../BulkUpload.TestSite17  # If test site exists
```

## Architecture

### Shared Backend Logic

BulkUpload.V17 references `BulkUpload.Core` (targeting `net10.0`), which provides:
- API controllers (with conditional compilation for v13/v17)
- Services (ImportUtilityService, MediaImportService, etc.)
- Resolvers (CSV column transformers)
- Models and constants

### Frontend Architecture

The frontend is built using a **service-oriented architecture**:

1. **Components** (`bulk-upload-dashboard.element.ts`): UI presentation using Lit
2. **Services** (`bulk-upload.service.ts`): Business logic and state management
3. **API Client** (`bulk-upload-api.ts`): HTTP communication with backend
4. **Utilities**: Reusable functions for file handling and result processing

This separation makes the code testable and maintainable.

## Extension Registration

Extensions (sections, dashboards) are registered in two places:

1. **JavaScript** (`Client/src/manifests.ts`): Programmatic registration with dynamic imports
2. **JSON** (`App_Plugins/BulkUpload/umbraco-package.json`): Declarative registration for static discovery

Both methods work, but the JSON approach is preferred for better IDE support and static analysis.

## Dependencies

### NPM Dependencies

- `@umbraco-cms/backoffice` - Umbraco 17 backoffice framework
- `lit` - Web components framework
- `vite` - Frontend build tool
- `typescript` - Type checking and compilation

### NuGet Dependencies

- `Umbraco.Cms` 17.1.0 - Core Umbraco functionality
- `Umbraco.Cms.Api.Management` 17.1.0 - Management API
- `BulkUpload.Core` 2.0.0 - Shared business logic

## Migrating from V13

If you're upgrading from BulkUpload v13:

1. Uninstall `Umbraco.Community.BulkUpload`
2. Install `Umbraco.Community.BulkUpload.V17`
3. No code changes required - all resolvers and services work the same way

The backend API is 100% compatible, so any custom resolvers or code that interacts with the service layer will continue to work.

## Future Enhancements

Potential areas for improvement:

- [ ] Add unit tests for Lit components
- [ ] Implement notification context integration with Umbraco 17's notification system
- [ ] Add localization support beyond English
- [ ] Create Storybook documentation for components
- [ ] Add E2E tests with Playwright

## License

MIT License - see LICENSE file in repository root.
