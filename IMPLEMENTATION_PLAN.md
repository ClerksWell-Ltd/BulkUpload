# Unified File Upload Implementation Plan

## Status: 95% Complete

### ✅ Completed

#### Angular (ClientV13) - 100% Complete
- ✅ CSV detection utilities in `fileUtils.js`
- ✅ Unified HTML template in `bulkUploadDashboard.html` (removed tabs)
- ✅ Unified controller in `bulkUpload.Controller.js`
- ✅ Unified service in `BulkUploadService.js` with `importUnified()` method
- ✅ Updated API client with `exportMediaPreprocessingResults()`
- ✅ Updated CSS with detection badge styles
- ✅ Added JSZip CDN to manifest

#### Lit (ClientV17) - 95% Complete
- ✅ CSV detection utilities in `file.utils.ts`
- ✅ Unified service in `bulk-upload.service.ts` with `importUnified()` method
- ✅ Updated API client with `exportMediaPreprocessingResults()` and interfaces
- ✅ Added jszip to `package.json`

### 🔄 Remaining

#### Lit Component (`bulk-upload-dashboard.element.ts`) - 5% Remaining

The component needs these specific changes to match the Angular unified structure:

**1. Update Imports:**
```typescript
import { analyzeUploadFile } from '../utils/file.utils';
```

**2. Update Handler Methods:**

Replace:
```typescript
private handleContentFileChange(e: Event): void {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (file) {
    this.service.setContentFile(file, input);
  }
}

private handleMediaFileChange(e: Event): void {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (file) {
    this.service.setMediaFile(file, input);
  }
}
```

With:
```typescript
private async handleFileChange(e: Event): Promise<void> {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (file) {
    try {
      const detection = await analyzeUploadFile(file);
      this.service.setFile(file, input, detection);
    } catch (error) {
      console.error('Error analyzing file:', error);
      this.service.setFile(file, input, null);
    }
  }
}
```

Replace:
```typescript
private triggerFileInput(type: 'content' | 'media'): void {
  const inputId = type === 'content' ? 'content-file-input' : 'media-file-input';
  const input = this.shadowRoot?.getElementById(inputId) as HTMLInputElement;
  if (input) {
    input.click();
  }
}
```

With:
```typescript
private triggerFileInput(): void {
  const input = this.shadowRoot?.getElementById('unified-file-input') as HTMLInputElement;
  if (input) {
    input.click();
  }
}
```

Replace:
```typescript
private async handleContentImport(): Promise<void> {
  await this.service.importContent();
  // ...
}

private async handleMediaImport(): Promise<void> {
  await this.service.importMedia();
  // ...
}
```

With:
```typescript
private async handleImport(): Promise<void> {
  await this.service.importUnified();
  setTimeout(() => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, 100);
}
```

Add:
```typescript
private async handleMediaPreprocessingExport(): Promise<void> {
  const response = await this.service.exportMediaPreprocessingResults();
  if (response) {
    await downloadResponseFile(response, 'media-preprocessing-results.csv');
  }
}

private countSuccessfulMedia(): number {
  return this.dashboardState.results.mediaPreprocessing?.filter(r => r.success).length ?? 0;
}

private countFailedMedia(): number {
  return this.dashboardState.results.mediaPreprocessing?.filter(r => !r.success).length ?? 0;
}
```

**3. Update render() Method:**

Replace the entire render method to match the Angular HTML structure:

```typescript
render() {
  const { loading, file, detection, results } = this.dashboardState;

  return html`
    <div class="bulk-upload-dashboard">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1>Bulk Upload</h1>
          <p>Import content and media into your Umbraco site in bulk via CSV or ZIP files.</p>
          <div class="header-badge">
            <span class="dot"></span>
            Ready to import
          </div>
        </div>
      </div>

      <!-- Detection Badge -->
      ${detection ? html`
        <div class="detection-badge-container" style="margin-bottom: 20px;">
          <uui-badge color="positive" look="primary">
            📦 Detected: ${detection.summary}
          </uui-badge>
        </div>
      ` : nothing}

      <!-- Results Section -->
      ${(results.content || results.media) && !loading ? html`
        <div class="results-container">
          ${results.content ? this.renderContentResults(results.content) : nothing}
          ${results.media ? this.renderMediaResults(results.media) : nothing}
          ${results.mediaPreprocessing ? this.renderMediaPreprocessingResults(results.mediaPreprocessing) : nothing}
        </div>
      ` : nothing}

      <!-- Upload Card -->
      ${this.renderUploadCard(file, loading)}

      <!-- Loading State -->
      ${loading ? this.renderLoadingState() : nothing}

      <!-- Requirements Card -->
      ${!loading ? this.renderUnifiedRequirements() : nothing}

      <!-- Footer -->
      ${this.renderFooter()}
    </div>
  `;
}
```

**4. Add New Render Methods:**

```typescript
private renderContentResults(results: any) {
  return html`
    <div class="upload-card">
      <div class="card-header">
        <div class="icon-circle green">✓</div>
        <div>
          <h2>Content Import Results</h2>
          <span class="subtitle">Summary of your content import</span>
        </div>
      </div>
      <div class="card-body">
        ${this.renderResultsSummary(results)}
        <div style="display: flex; gap: 10px;">
          <uui-button
            label="Download Results CSV"
            look="outline"
            @click=${this.handleContentExport}>
            ⬇ Download Results CSV
          </uui-button>
          <uui-button
            label="Clear Results"
            look="outline"
            @click=${() => this.service.clearContentResults()}>
            Clear Results
          </uui-button>
        </div>
      </div>
    </div>
  `;
}

private renderMediaResults(results: any) {
  return html`
    <div class="upload-card">
      <div class="card-header">
        <div class="icon-circle green">✓</div>
        <div>
          <h2>Media Import Results</h2>
          <span class="subtitle">Summary of your media import</span>
        </div>
      </div>
      <div class="card-body">
        ${this.renderResultsSummary(results)}
        <div style="display: flex; gap: 10px;">
          <uui-button
            label="Download Results CSV"
            look="outline"
            @click=${this.handleMediaExport}>
            ⬇ Download Results CSV
          </uui-button>
          <uui-button
            label="Clear Results"
            look="outline"
            @click=${() => this.service.clearMediaResults()}>
            Clear Results
          </uui-button>
        </div>
      </div>
    </div>
  `;
}

private renderMediaPreprocessingResults(results: any[]) {
  return html`
    <div class="upload-card">
      <div class="card-header">
        <div class="icon-circle green">✓</div>
        <div>
          <h2>Media Preprocessing Results</h2>
          <span class="subtitle">Media files created during content import</span>
        </div>
      </div>
      <div class="card-body">
        <div class="results-summary">
          <div class="badge badge-total">
            <strong>Total:</strong> ${results.length}
          </div>
          <div class="badge badge-success">
            <strong>✓ Success:</strong> ${this.countSuccessfulMedia()}
          </div>
          ${this.countFailedMedia() > 0 ? html`
            <div class="badge badge-failed">
              <strong>✗ Failed:</strong> ${this.countFailedMedia()}
            </div>
          ` : nothing}
        </div>
        <div style="display: flex; gap: 10px;">
          <uui-button
            label="Download Media Results"
            look="outline"
            @click=${this.handleMediaPreprocessingExport}>
            ⬇ Download Media Results
          </uui-button>
        </div>
      </div>
    </div>
  `;
}

private renderResultsSummary(results: any) {
  return html`
    <div class="results-summary">
      <div class="badge badge-total">
        <strong>Total:</strong> ${results.totalCount}
      </div>
      <div class="badge badge-success">
        <strong>✓ Success:</strong> ${results.successCount}
      </div>
      ${results.failureCount > 0 ? html`
        <div class="badge badge-failed">
          <strong>✗ Failed:</strong> ${results.failureCount}
        </div>
      ` : nothing}
    </div>
  `;
}

private renderUploadCard(file: File | null, loading: boolean) {
  return html`
    <div class="upload-card">
      <div class="card-header">
        <div class="icon-circle green">▲</div>
        <div>
          <h2>Upload File</h2>
          <span class="subtitle">Drag & drop or browse for your import file</span>
        </div>
      </div>
      <div class="card-body">
        ${!file ? this.renderDropZone() : this.renderFilePreview(file, loading)}

        <input
          type="file"
          id="unified-file-input"
          accept=".csv,.zip"
          ?disabled=${loading}
          @change=${this.handleFileChange}
          style="display: none;" />

        <div class="btn-container">
          <uui-button
            label="Import"
            look="primary"
            color="positive"
            ?disabled=${!file || loading}
            @click=${this.handleImport}
            style="--uui-button-padding: 12px 28px;">
            ${loading ? 'Processing...' : '▲ Import'}
          </uui-button>
        </div>
      </div>
    </div>
  `;
}

private renderDropZone() {
  return html`
    <div class="drop-zone" @click=${this.triggerFileInput}>
      <div class="upload-icon">▲</div>
      <h3>Drag files here or <em>browse</em></h3>
      <p>Select a CSV or ZIP file to start your import</p>
      <div class="file-types">
        <span class="file-chip">.csv</span>
        <span class="file-chip">.zip</span>
      </div>
    </div>
  `;
}

private renderFilePreview(file: File, loading: boolean) {
  return html`
    <div class="file-preview">
      <div class="file-icon">📄</div>
      <div class="file-details">
        <div class="file-name">${file.name}</div>
        <div class="file-size">${formatFileSize(file.size)}</div>
      </div>
      ${!loading ? html`
        <uui-button
          label="Clear"
          look="outline"
          @click=${() => this.service.clearFile()}>
          Clear
        </uui-button>
      ` : nothing}
    </div>
  `;
}

private renderLoadingState() {
  return html`
    <div class="loading-state">
      <uui-loader-bar style="color: #006eff"></uui-loader-bar>
      <p>Importing, please wait...</p>
    </div>
  `;
}

private renderUnifiedRequirements() {
  return html`
    <div class="requirements-card">
      <div class="card-header">
        <div class="icon-circle blue">ℹ</div>
        <div>
          <h2>Requirements & Help</h2>
          <span class="subtitle">Everything you need to format your import file correctly</span>
        </div>
      </div>
      <div class="card-body">
        <!-- Copy the requirements sections from Angular HTML -->
        <!-- Include: Supported Upload Types, Content CSV Requirements, Media CSV Requirements, Additional Features -->
      </div>
    </div>
  `;
}
```

**5. Remove Old Methods:**
- Delete `renderContentRequirements()`
- Delete `renderMediaRequirements()`
- Delete old tab-specific render methods

**6. Update CSS in Component:**
Add to the `static styles` section:
```typescript
.detection-badge-container {
  animation: fadeIn 0.3s ease-in;
}

.results-container {
  animation: fadeIn 0.3s ease-in;
  margin-bottom: 20px;
}

.results-container > .upload-card {
  margin-bottom: 20px;
}

.results-container > .upload-card:last-child {
  margin-bottom: 0;
}
```

## Next Steps

1. Apply the above changes to `bulk-upload-dashboard.element.ts`
2. Run `npm install` in ClientV17 to install jszip
3. Run `npm run build` in ClientV17 to compile TypeScript
4. Test both Angular and Lit versions
5. Commit and push

## Testing Checklist

- [ ] Media CSV only
- [ ] Content CSV only
- [ ] ZIP with Media CSV
- [ ] ZIP with Content CSV
- [ ] ZIP with both CSVs (verify media processed first)
- [ ] ZIP with Content CSV + media files
- [ ] ZIP with Media CSV + media files
- [ ] ZIP with all three

## Key Implementation Points

1. **Media-First Processing**: Media CSV is always processed before Content CSV
2. **Client-Side Detection**: Files are analyzed before upload to show what was detected
3. **Combined Results**: Can show content, media, and media preprocessing results simultaneously
4. **Identical UIs**: Angular and Lit versions should look and behave identically
