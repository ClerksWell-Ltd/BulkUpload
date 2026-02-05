# Umbraco 17 Migration Strategy for Bulk Upload

## Overview

This document outlines a refactoring strategy that will work in **both Umbraco 13 and Umbraco 17**, allowing us to incrementally prepare for v17 while maintaining full v13 compatibility.

## Current Architecture (v13)

```
App_Plugins/BulkUpload/
├── bulkUpload.Controller.js          # AngularJS controller (v13 only)
├── bulkUploadImportApiService.js     # AngularJS service (v13 only)
├── bulkUploadDashboard.html          # Template with UUI components ✅
└── bulkUploadDashboard.css           # Styles ✅
```

**Challenges:**
- ❌ AngularJS controller tightly couples business logic to framework
- ❌ AngularJS services can't be reused in v17
- ❌ Utility functions locked inside AngularJS scope
- ✅ UUI components are already web components (good!)
- ✅ CSS is framework-agnostic (good!)

## Target Architecture (v17)

```
App_Plugins/BulkUpload/
├── manifest.json                     # Extension manifest
├── dist/                             # Vite build output
│   └── bulk-upload-dashboard.js      # Compiled Lit component
├── src/
│   ├── dashboard.element.ts          # Main Lit element
│   ├── services/
│   │   └── bulk-upload-client.ts     # API client
│   └── utils/
│       ├── file-utils.ts             # File helpers
│       └── result-utils.ts           # Result helpers
└── styles.css
```

---

## 🚀 Incremental Refactoring Strategy

### Phase 1: Extract Pure JavaScript Utilities (DO NOW ✅)

**Goal:** Create framework-agnostic utility modules that work in both versions.

#### Step 1.1: Create Utility Modules

**File: `/utils/fileUtils.js`**
```javascript
/**
 * Framework-agnostic file utilities
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit)
 */

export const formatFileSize = (bytes) => {
  if (!bytes) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
};

export const getFileExtension = (filename) => {
  return filename.slice((filename.lastIndexOf('.') - 1 >>> 0) + 2);
};

export const isValidFileType = (file, acceptedTypes) => {
  if (!acceptedTypes || acceptedTypes.length === 0) return true;
  const ext = getFileExtension(file.name).toLowerCase();
  return acceptedTypes.some(type => {
    if (type.startsWith('.')) {
      return type.toLowerCase() === `.${ext}`;
    }
    return type.toLowerCase() === ext;
  });
};
```

**File: `/utils/resultUtils.js`**
```javascript
/**
 * Framework-agnostic result processing utilities
 */

export const getFailedResults = (results) => {
  if (!Array.isArray(results)) return [];
  return results.filter(result => result.success === false);
};

export const getSuccessResults = (results) => {
  if (!Array.isArray(results)) return [];
  return results.filter(result => result.success === true);
};

export const calculateResultStats = (results) => {
  if (!Array.isArray(results)) {
    return { total: 0, success: 0, failed: 0 };
  }

  return {
    total: results.length,
    success: results.filter(r => r.success).length,
    failed: results.filter(r => !r.success).length
  };
};

export const exportResultsToCsv = (results) => {
  // CSV export logic (framework-agnostic)
  const blob = new Blob([results], { type: 'text/csv' });
  return blob;
};

export const downloadBlob = (blob, filename) => {
  const link = document.createElement('a');
  link.href = window.URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  window.URL.revokeObjectURL(link.href);
};
```

#### Step 1.2: Update AngularJS Controller to Use Utilities

**Benefits:**
- ✅ Immediate code quality improvement
- ✅ Easier testing (pure functions)
- ✅ Utilities can be directly imported in v17 Lit components
- ✅ No breaking changes for v13

**Migration:**
```javascript
// Old (v13)
$scope.formatFileSize = function (bytes) { /* ... */ };

// New (works in both)
import { formatFileSize } from './utils/fileUtils.js';
$scope.formatFileSize = formatFileSize;
```

---

### Phase 2: Create Standalone API Client (DO NOW ✅)

**Goal:** Abstract API calls from AngularJS `$http` to work with both AngularJS and native `fetch`.

**File: `/services/BulkUploadApiClient.js`**
```javascript
/**
 * Standalone API client that works in both v13 and v17
 * Supports both AngularJS HTTP and native fetch
 */

export class BulkUploadApiClient {
  constructor(httpAdapter) {
    this.http = httpAdapter || new FetchHttpAdapter();
  }

  async importContent(file) {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post('/Umbraco/backoffice/Api/BulkUpload/ImportAll', formData);
  }

  async importMedia(file) {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post('/Umbraco/backoffice/Api/MediaImport/ImportMedia', formData);
  }

  async exportContentResults(results) {
    return this.http.post('/Umbraco/backoffice/Api/BulkUpload/ExportResults', results, {
      responseType: 'text'
    });
  }

  async exportMediaResults(results) {
    return this.http.post('/Umbraco/backoffice/Api/MediaImport/ExportResults', results, {
      responseType: 'text'
    });
  }
}

// Native Fetch Adapter (for v17)
export class FetchHttpAdapter {
  async post(url, data, options = {}) {
    const response = await fetch(url, {
      method: 'POST',
      body: data instanceof FormData ? data : JSON.stringify(data),
      headers: data instanceof FormData ? {} : { 'Content-Type': 'application/json' },
      ...options
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const responseData = options.responseType === 'text'
      ? await response.text()
      : await response.json();

    return { data: responseData, status: response.status };
  }
}

// AngularJS HTTP Adapter (for v13)
export class AngularHttpAdapter {
  constructor($http, Upload) {
    this.$http = $http;
    this.Upload = Upload;
  }

  async post(url, data, options = {}) {
    if (data instanceof FormData || data instanceof File) {
      // Use ng-file-upload for file uploads
      return this.Upload.upload({
        url,
        file: data,
        ...options
      }).then(response => response);
    } else {
      // Use $http for regular posts
      return this.$http.post(url, data, options).then(response => response);
    }
  }
}
```

**Benefits:**
- ✅ Same API client code works in both versions
- ✅ Just swap the HTTP adapter (AngularJS → Fetch)
- ✅ Easier to test (mock the adapter)
- ✅ Clean separation of concerns

---

### Phase 3: Extract Business Logic Service (DO NOW ✅)

**File: `/services/BulkUploadService.js`**
```javascript
/**
 * Core business logic service
 * Framework-agnostic, works in both v13 and v17
 */

import { calculateResultStats } from '../utils/resultUtils.js';
import { formatFileSize } from '../utils/fileUtils.js';

export class BulkUploadService {
  constructor(apiClient, notificationHandler) {
    this.apiClient = apiClient;
    this.notify = notificationHandler;
    this.state = this.createInitialState();
  }

  createInitialState() {
    return {
      activeTab: 'content',
      content: {
        loading: false,
        file: null,
        results: null
      },
      media: {
        loading: false,
        file: null,
        results: null
      }
    };
  }

  setActiveTab(tab) {
    this.state.activeTab = tab;
  }

  setContentFile(file) {
    this.state.content.file = file;
  }

  setMediaFile(file) {
    this.state.media.file = file;
  }

  clearContentFile() {
    this.state.content.file = null;
  }

  clearMediaFile() {
    this.state.media.file = null;
  }

  async importContent() {
    const { file } = this.state.content;
    if (!file) return;

    this.state.content.loading = true;
    this.state.content.results = null;

    try {
      const response = await this.apiClient.importContent(file);

      this.state.content.results = response.data;
      this.state.content.file = null;

      const stats = calculateResultStats(response.data.results);
      const message = `${stats.success} of ${stats.total} content items imported successfully.`;

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Content Import Complete',
        message: stats.failed > 0 ? `${message} ${stats.failed} failed.` : message
      });

      return response.data;
    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Import Failed',
        message: error.message || 'An error occurred during content import.'
      });
      throw error;
    } finally {
      this.state.content.loading = false;
    }
  }

  async importMedia() {
    const { file } = this.state.media;
    if (!file) return;

    this.state.media.loading = true;
    this.state.media.results = null;

    try {
      const response = await this.apiClient.importMedia(file);

      this.state.media.results = response.data;
      this.state.media.file = null;

      const stats = calculateResultStats(response.data.results);
      const message = `${stats.success} of ${stats.total} media items imported successfully.`;

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Media Import Complete',
        message: stats.failed > 0 ? `${message} ${stats.failed} failed.` : message
      });

      return response.data;
    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Import Failed',
        message: error.message || 'An error occurred during media import.'
      });
      throw error;
    } finally {
      this.state.media.loading = false;
    }
  }

  async exportContentResults() {
    // Implementation
  }

  async exportMediaResults() {
    // Implementation
  }
}
```

**Benefits:**
- ✅ All business logic in one place
- ✅ State management separated from UI framework
- ✅ Can be used directly in v17 Lit components
- ✅ Easier to test

---

### Phase 4: Refactor AngularJS Controller as Thin Wrapper (DO NOW ✅)

**New controller becomes just a bridge:**

```javascript
import { BulkUploadService } from './services/BulkUploadService.js';
import { BulkUploadApiClient, AngularHttpAdapter } from './services/BulkUploadApiClient.js';
import { formatFileSize, getFileExtension } from './utils/fileUtils.js';
import { getFailedResults } from './utils/resultUtils.js';

angular
  .module("umbraco")
  .controller("bulkUploadController", function ($scope, $http, Upload, notificationsService) {

    // Create HTTP adapter for AngularJS
    const httpAdapter = new AngularHttpAdapter($http, Upload);
    const apiClient = new BulkUploadApiClient(httpAdapter);

    // Create notification handler
    const notificationHandler = (notification) => {
      notificationsService.add(notification);
    };

    // Create service instance
    const service = new BulkUploadService(apiClient, notificationHandler);

    // Bind service state to scope (for AngularJS data binding)
    $scope.state = service.state;

    // Expose utility functions
    $scope.formatFileSize = formatFileSize;
    $scope.getFailedResults = getFailedResults;

    // Expose service methods
    $scope.setActiveTab = (tab) => {
      service.setActiveTab(tab);
      $scope.$apply();
    };

    $scope.onFileSelected = (file, evt) => {
      service.setContentFile(file);
      $scope.$apply();
    };

    $scope.onUploadClicked = async () => {
      await service.importContent();
      $scope.$apply();
    };

    // ... other methods
  });
```

**Benefits:**
- ✅ Controller is now just ~50 lines (was 266!)
- ✅ 90% of code is reusable in v17
- ✅ Still works perfectly in v13
- ✅ Much easier to test

---

### Phase 5: Create Package Structure for v17 (LATER)

**When migrating to v17, create:**

```
package.json           # Define as ES module
tsconfig.json          # TypeScript config
vite.config.ts         # Vite bundler config
umbraco-package.json   # Extension manifest
```

**The Lit component will look like:**

```typescript
import { LitElement, html, css } from 'lit';
import { BulkUploadService } from './services/BulkUploadService.js';
import { BulkUploadApiClient } from './services/BulkUploadApiClient.js';

export class BulkUploadDashboard extends LitElement {
  private service: BulkUploadService;

  constructor() {
    super();
    const apiClient = new BulkUploadApiClient(); // Uses fetch adapter
    const notificationHandler = (n) => this.dispatchEvent(new CustomEvent('notification', { detail: n }));
    this.service = new BulkUploadService(apiClient, notificationHandler);
  }

  render() {
    return html`
      <uui-box>
        <uui-tab-group>
          <uui-tab
            label="Content Import"
            ?active=${this.service.state.activeTab === 'content'}
            @click=${() => this.service.setActiveTab('content')}>
            Content Import
          </uui-tab>
          <!-- ... -->
        </uui-tab-group>

        ${this.service.state.activeTab === 'content' ? this.renderContentTab() : this.renderMediaTab()}
      </uui-box>
    `;
  }

  renderContentTab() {
    // Same UI structure, but in Lit template syntax
  }
}

customElements.define('bulk-upload-dashboard', BulkUploadDashboard);
```

---

## Summary: What to Do NOW

### ✅ Immediate Actions (Work in Both v13 & v17)

1. **Extract utilities** → `utils/fileUtils.js`, `utils/resultUtils.js`
2. **Create API client** → `services/BulkUploadApiClient.js` with adapters
3. **Create service** → `services/BulkUploadService.js` with business logic
4. **Refactor controller** → Thin wrapper around service
5. **Add tests** → Test the pure JavaScript modules

### Benefits

| Benefit | v13 | v17 |
|---------|-----|-----|
| Code reuse | ✅ Immediate | ✅ Direct import |
| Testability | ✅ Improved | ✅ Same tests |
| Maintainability | ✅ Better | ✅ Better |
| Migration effort | 🟢 Low | 🟢 Already done |

### Migration Effort Estimate

- **Phase 1-4 (NOW)**: ~2-3 days
- **Phase 5 (v17)**: ~1 day (just UI rewrite)

**Total code reuse when migrating to v17: ~90%**

---

## Conclusion

By refactoring now to extract framework-agnostic modules, we:

1. ✅ Improve code quality immediately in v13
2. ✅ Make code more testable
3. ✅ Prepare 90% of the codebase for v17
4. ✅ Reduce v17 migration effort by ~80%
5. ✅ Create a pattern for other Umbraco plugins

The key insight: **Business logic doesn't need to know about AngularJS or Lit. Keep it pure JavaScript.**
