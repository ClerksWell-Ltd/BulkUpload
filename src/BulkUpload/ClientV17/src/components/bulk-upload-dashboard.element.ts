/**
 * Bulk Upload Dashboard Component - Unified Version
 * Lit-based component for Umbraco 17
 * Single unified upload field for all content and media scenarios
 */

import { LitElement, html, css, nothing } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { BulkUploadApiClient } from '../api/bulk-upload-api';
import { BulkUploadService, type BulkUploadState, type Notification } from '../services/bulk-upload.service';
import { formatFileSize, analyzeUploadFile } from '../utils/file.utils';
import { downloadResponseFile } from '../utils/result.utils';
import type { ImportResultResponse, MediaPreprocessingResult } from '../api/bulk-upload-api';

@customElement('bulk-upload-dashboard')
export class BulkUploadDashboardElement extends LitElement {
  @state() private dashboardState: BulkUploadState;
  private service: BulkUploadService;

  constructor() {
    super();

    const apiClient = new BulkUploadApiClient();
    this.service = new BulkUploadService(
      apiClient,
      this.handleNotification.bind(this),
      this.handleStateChange.bind(this)
    );

    this.dashboardState = this.service.getState();
  }

  private handleNotification(notification: Notification): void {
    // Dispatch custom event for Umbraco notification system
    this.dispatchEvent(new CustomEvent('notification', {
      detail: { notification },
      bubbles: true,
      composed: true
    }));
  }

  private handleStateChange(state: BulkUploadState): void {
    this.dashboardState = { ...state };
  }

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

  private triggerFileInput(): void {
    const input = this.shadowRoot?.getElementById('unified-file-input') as HTMLInputElement;
    if (input) {
      input.click();
    }
  }

  private async handleImport(): Promise<void> {
    await this.service.importUnified();
    setTimeout(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }, 100);
  }

  private async handleContentExport(): Promise<void> {
    const response = await this.service.exportContentResults();
    if (response) {
      await downloadResponseFile(response, 'content-results.csv');
    }
  }

  private async handleMediaExport(): Promise<void> {
    const response = await this.service.exportMediaResults();
    if (response) {
      await downloadResponseFile(response, 'media-results.csv');
    }
  }

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
        ${(results.content || results.media || results.mediaPreprocessing) && !loading ? html`
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

  private renderContentResults(results: ImportResultResponse) {
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

  private renderMediaResults(results: ImportResultResponse) {
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

  private renderMediaPreprocessingResults(results: MediaPreprocessingResult[]) {
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

  private renderResultsSummary(results: ImportResultResponse) {
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
        <details class="requirements-details">
          <summary class="card-header">
            <div class="icon-circle blue">ℹ</div>
            <div>
              <h2>Requirements & Help</h2>
              <span class="subtitle">Everything you need to format your import file correctly</span>
            </div>
            <div class="toggle-icon">▼</div>
          </summary>
          <div class="card-body">
          <!-- Supported Upload Types -->
          <div class="req-section">
            <div class="req-label">Supported Upload Types</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                <strong>Media CSV:</strong> Import media items from URLs or file paths
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                <strong>Content CSV:</strong> Import content nodes
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with Media CSV:</strong> CSV for media import
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with Content CSV:</strong> CSV for content import
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with Media CSV + Content CSV:</strong> Import media first, then content
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with Media CSV + Media Files:</strong> CSV references files in ZIP
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with Content CSV + Media Files:</strong> Content references files via zipFileToMedia
              </div>
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP with All Three:</strong> Media CSV processed first, then Content CSV with media files
              </div>
            </div>
          </div>

          <!-- Content CSV Requirements -->
          <div class="req-section">
            <div class="req-label">Content CSV - Required Columns</div>
            <div class="req-grid">
              <div class="req-item">
                <code>parent</code>
                <div class="desc">Parent ID, GUID, or content path</div>
                <div class="examples">
                  <span>1050</span>
                  <span>71332aa7-…</span>
                  <span>/news/2024/</span>
                </div>
              </div>
              <div class="req-item">
                <code>docTypeAlias</code>
                <div class="desc">Content type alias</div>
                <div class="examples">
                  <span>articlePage</span>
                </div>
              </div>
              <div class="req-item">
                <code>name</code>
                <div class="desc">Content item name</div>
                <div class="examples">
                  <span>My Article</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Media CSV Requirements -->
          <div class="req-section">
            <div class="req-label">Media CSV - Required Columns (depends on import type)</div>
            <div class="req-grid">
              <div class="req-item">
                <code>fileName</code>
                <div class="desc">For ZIP uploads: path to file within ZIP</div>
                <div class="examples">
                  <span>image.jpg</span>
                  <span>photos/pic.png</span>
                </div>
              </div>
              <div class="req-item">
                <code>mediaSource|urlToStream</code>
                <div class="desc">For URL imports</div>
                <div class="examples">
                  <span>https://...</span>
                </div>
              </div>
              <div class="req-item">
                <code>mediaSource|pathToStream</code>
                <div class="desc">For file path imports</div>
                <div class="examples">
                  <span>C:\\Images\\...</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Additional Features -->
          <div class="req-section">
            <div class="req-label">Additional Features</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">🖼</div>
                Reference media in content using resolvers like <code>heroImage|zipFileToMedia</code>
              </div>
              <div class="media-tip">
                <div class="tip-icon">👥</div>
                Supports multi-CSV imports with automatic media deduplication
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                Add extra CSV columns for any property on your doc type or media type
              </div>
              <div class="media-tip">
                <div class="tip-icon">🔄</div>
                Update mode: Use <code>bulkUploadShouldUpdate</code> column to update existing items
              </div>
              <div class="media-tip">
                <div class="tip-icon">📁</div>
                <code>parent</code> column for folders (auto-creates folders for both content and media)
              </div>
            </div>
          </div>
        </details>
      </div>
    `;
  }

  private renderFooter() {
    return html`
      <footer class="plugin-footer">
        <div class="divider"></div>
        <a href="https://www.clerkswell.com" target="_blank" rel="noopener noreferrer" class="brand-link">
          Made for the Umbraco Community with
          <span class="heart">❤️</span>
          from
          <img src="/App_Plugins/BulkUpload/images/cw-logo-primary-blue.png" alt="ClerksWell" />
        </a>
      </footer>
    `;
  }

  static styles = css`
    /* Base Styles */
    :host {
      display: block;
      --umb-blue: #1b264f;
      --umb-blue-light: #2c3e6b;
      --umb-blue-hover: #243561;
      --umb-surface: #f6f7fb;
      --umb-white: #ffffff;
      --umb-border: #e0e3eb;
      --umb-text: #303033;
      --umb-text-muted: #68697a;
      --umb-accent: #2bc37b;
      --umb-accent-soft: #e6f9f0;
      --umb-accent-hover: #25a86a;
      --umb-danger: #d42054;
      --umb-warning: #f5c142;
      --umb-code-bg: #f0f1f5;
      --umb-shadow-sm: 0 1px 3px rgba(27,38,79,0.06);
      --umb-shadow-md: 0 4px 12px rgba(27,38,79,0.08);
      --umb-shadow-lg: 0 8px 32px rgba(27,38,79,0.12);
      --umb-radius: 8px;
      --umb-radius-lg: 12px;
    }

    .bulk-upload-dashboard {
      max-width: 960px;
      margin: 0 auto;
      animation: fadeUp 0.5s ease both;
      margin-top: 40px;
      margin-bottom: 14px;
    }

    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(12px); }
      to { opacity: 1; transform: translateY(0); }
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    /* Header */
    .page-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      margin-bottom: 24px;
    }

    .page-header h1 {
      font-size: 28px;
      font-weight: 900;
      color: var(--umb-blue);
      letter-spacing: -0.5px;
      margin: 0;
    }

    .page-header p {
      color: var(--umb-text-muted);
      font-size: 14px;
      margin-top: 6px;
      line-height: 1.5;
    }

    .header-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
      font-size: 12px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 20px;
      margin-top: 10px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .header-badge .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: var(--umb-accent);
    }

    /* Detection Badge */
    .detection-badge-container {
      animation: fadeIn 0.3s ease-in;
    }

    .detection-badge-container uui-badge {
      font-size: 14px;
      font-weight: 600;
    }

    /* Results Container */
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

    /* Cards */
    .upload-card,
    .requirements-card {
      background: var(--umb-white);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius-lg);
      box-shadow: var(--umb-shadow-sm);
      overflow: hidden;
      margin-bottom: 20px;
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 18px 24px;
      border-bottom: 1px solid var(--umb-border);
      background: var(--umb-surface);
    }

    .card-header h2 {
      font-size: 17px;
      font-weight: 700;
      color: var(--umb-text);
      margin: 0;
      line-height: 1.3;
    }

    .card-header .subtitle {
      font-size: 13px;
      color: var(--umb-text-muted);
      display: block;
      margin-top: 2px;
    }

    .icon-circle {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 18px;
      font-weight: 700;
      flex-shrink: 0;
    }

    .icon-circle.green {
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
    }

    .icon-circle.blue {
      background: #e6f2ff;
      color: #006eff;
    }

    .card-body {
      padding: 24px;
    }

    /* Results Summary */
    .results-summary {
      display: flex;
      gap: 10px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 8px 14px;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
    }

    .badge-total {
      background: #f0f1f5;
      color: var(--umb-text);
    }

    .badge-success {
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
    }

    .badge-failed {
      background: #ffe6ed;
      color: var(--umb-danger);
    }

    /* Drop Zone */
    .drop-zone {
      border: 2px dashed var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 40px 24px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s ease;
      background: var(--umb-surface);
    }

    .drop-zone:hover {
      border-color: var(--umb-accent);
      background: var(--umb-accent-soft);
    }

    .upload-icon {
      font-size: 40px;
      color: var(--umb-accent);
      margin-bottom: 12px;
    }

    .drop-zone h3 {
      font-size: 16px;
      font-weight: 600;
      color: var(--umb-text);
      margin: 0 0 6px 0;
    }

    .drop-zone h3 em {
      color: var(--umb-accent);
      font-style: normal;
      text-decoration: underline;
    }

    .drop-zone p {
      color: var(--umb-text-muted);
      font-size: 13px;
      margin: 0 0 16px 0;
    }

    .file-types {
      display: flex;
      gap: 8px;
      justify-content: center;
    }

    .file-chip {
      background: var(--umb-white);
      border: 1px solid var(--umb-border);
      padding: 4px 12px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 600;
      color: var(--umb-text-muted);
    }

    /* File Preview */
    .file-preview {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 16px;
      background: var(--umb-surface);
      border-radius: var(--umb-radius);
      border: 1px solid var(--umb-border);
    }

    .file-icon {
      font-size: 32px;
    }

    .file-details {
      flex: 1;
    }

    .file-name {
      font-size: 14px;
      font-weight: 600;
      color: var(--umb-text);
      margin-bottom: 4px;
    }

    .file-size {
      font-size: 12px;
      color: var(--umb-text-muted);
    }

    /* Button Container */
    .btn-container {
      margin-top: 16px;
    }

    /* Loading State */
    .loading-state {
      padding: 24px;
      text-align: center;
    }

    .loading-state p {
      margin-top: 12px;
      color: var(--umb-text-muted);
      font-size: 14px;
    }

    /* Requirements */
    .req-section {
      margin-bottom: 28px;
    }

    .req-section:last-child {
      margin-bottom: 0;
    }

    .req-label {
      font-size: 14px;
      font-weight: 700;
      color: var(--umb-text);
      margin-bottom: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .req-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 16px;
    }

    .req-item {
      background: var(--umb-surface);
      padding: 14px;
      border-radius: var(--umb-radius);
      border: 1px solid var(--umb-border);
    }

    .req-item code {
      display: inline-block;
      background: var(--umb-code-bg);
      color: var(--umb-blue);
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 13px;
      font-weight: 600;
      margin-bottom: 8px;
      font-family: 'Courier New', monospace;
    }

    .req-item .desc {
      font-size: 12px;
      color: var(--umb-text-muted);
      margin-bottom: 8px;
      line-height: 1.4;
    }

    .examples {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
    }

    .examples span {
      background: var(--umb-white);
      border: 1px solid var(--umb-border);
      padding: 3px 8px;
      border-radius: 3px;
      font-size: 11px;
      color: var(--umb-text-muted);
      font-family: 'Courier New', monospace;
    }

    .media-tips {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .media-tip {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 12px;
      background: var(--umb-surface);
      border-radius: var(--umb-radius);
      border: 1px solid var(--umb-border);
      font-size: 13px;
      color: var(--umb-text);
      line-height: 1.5;
    }

    .tip-icon {
      font-size: 18px;
      flex-shrink: 0;
    }

    .media-tip strong {
      color: var(--umb-blue);
    }

    .media-tip code {
      background: var(--umb-code-bg);
      color: var(--umb-blue);
      padding: 2px 6px;
      border-radius: 3px;
      font-size: 12px;
      font-family: 'Courier New', monospace;
    }

    /* Footer */
    .plugin-footer {
      margin-top: 20px;
      padding-top: 24px;
    }

    .divider {
      height: 1px;
      background: var(--umb-border);
      margin-bottom: 20px;
    }

    .brand-link {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      font-size: 13px;
      color: var(--umb-text-muted);
      text-decoration: none;
      transition: color 0.2s ease;
    }

    .brand-link:hover {
      color: var(--umb-text);
    }

    .brand-link img {
      height: 20px;
      margin-left: 4px;
    }

    .heart {
      color: var(--umb-danger);
      font-size: 14px;
    }

    /* Collapsible requirements */
    .requirements-details {
      all: unset;
      display: block;
    }

    .requirements-details > summary {
      cursor: pointer;
      list-style: none;
      user-select: none;
    }

    .requirements-details > summary::-webkit-details-marker {
      display: none;
    }

    .requirements-details > summary {
      position: relative;
    }

    .requirements-details > summary .toggle-icon {
      position: absolute;
      right: 28px;
      top: 50%;
      transform: translateY(-50%) rotate(-90deg);
      transition: transform 0.3s ease;
      color: var(--umb-text-muted);
      font-size: 12px;
      width: 24px;
      height: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      background: rgba(27,38,79,0.05);
    }

    .requirements-details[open] > summary .toggle-icon {
      transform: translateY(-50%) rotate(0deg);
    }

    .requirements-details > summary:hover .toggle-icon {
      background: rgba(27,38,79,0.1);
      color: var(--umb-blue);
    }
  `;
}
