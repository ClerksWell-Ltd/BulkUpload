/**
 * Bulk Upload Dashboard Component
 * Lit-based component for Umbraco 17
 * Redesigned UI matching V13 AngularJS version
 */

import { LitElement, html, css, nothing } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { BulkUploadApiClient } from '../api/bulk-upload-api';
import { BulkUploadService, type BulkUploadState, type Notification } from '../services/bulk-upload.service';
import { formatFileSize } from '../utils/file.utils';
import { downloadResponseFile } from '../utils/result.utils';

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

  private async handleContentFileChange(e: Event): Promise<void> {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      await this.service.setContentFileWithDetection(file, input);
    }
  }

  private handleMediaFileChange(e: Event): void {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.service.setMediaFile(file, input);
    }
  }

  private triggerFileInput(type: 'content' | 'media'): void {
    const inputId = type === 'content' ? 'content-file-input' : 'media-file-input';
    const input = this.shadowRoot?.getElementById(inputId) as HTMLInputElement;
    if (input) {
      input.click();
    }
  }

  private async handleContentImport(): Promise<void> {
    await this.service.importWithAutoDetection();
    // Scroll to top to show results
    setTimeout(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }, 100);
  }

  private async handleMediaImport(): Promise<void> {
    await this.service.importMedia();
    // Scroll to top to show results
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

  render() {
    const { activeTab, content, media } = this.dashboardState;

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

        <!-- Tab Navigation -->
        <uui-tab-group style="margin-bottom: 28px;">
          <uui-tab
            label="Content Import"
            ?active=${activeTab === 'content'}
            @click=${() => this.service.setActiveTab('content')}>
            Content Import
          </uui-tab>
          <uui-tab
            label="Media Import"
            ?active=${activeTab === 'media'}
            @click=${() => this.service.setActiveTab('media')}>
            Media Import
          </uui-tab>
        </uui-tab-group>

        <!-- Content Import Panel -->
        ${activeTab === 'content' ? html`
          <div class="tab-panel">
            ${content.results && !content.loading ? this.renderResults('content', content.results) : nothing}
            ${this.renderUploadCard('content', content)}
            ${content.loading ? this.renderLoadingState('content') : nothing}
            ${!content.loading ? this.renderContentRequirements() : nothing}
          </div>
        ` : nothing}

        <!-- Media Import Panel -->
        ${activeTab === 'media' ? html`
          <div class="tab-panel">
            ${media.results && !media.loading ? this.renderResults('media', media.results) : nothing}
            ${this.renderUploadCard('media', media)}
            ${media.loading ? this.renderLoadingState('media') : nothing}
            ${!media.loading ? this.renderMediaRequirements() : nothing}
          </div>
        ` : nothing}

        <!-- Footer -->
        ${this.renderFooter()}
      </div>
    `;
  }

  private renderUploadCard(type: 'content' | 'media', tabState: any) {
    const isContent = type === 'content';
    const fileInputId = isContent ? 'content-file-input' : 'media-file-input';
    const title = isContent ? 'Upload File' : 'Upload File';
    const subtitle = isContent
      ? 'Automatically detects content or media imports'
      : 'Drag & drop or browse for your media import file';

    return html`
      <div class="upload-card">
        <div class="card-header">
          <div class="icon-circle green">▲</div>
          <div>
            <h2>${title}</h2>
            <span class="subtitle">${subtitle}</span>
          </div>
        </div>
        <div class="card-body">
          <!-- Drop Zone -->
          ${!tabState.file ? html`
            <div class="drop-zone" @click=${() => this.triggerFileInput(type)}>
              <div class="upload-icon">▲</div>
              <h3>Drag files here or <em>browse</em></h3>
              <p>${isContent ? 'File type will be automatically detected' : 'Select a ZIP or CSV file to start your media import'}</p>
              <div class="file-types">
                <span class="file-chip">.csv</span>
                <span class="file-chip">.zip</span>
              </div>
            </div>
          ` : nothing}

          <!-- File Preview -->
          ${tabState.file && !tabState.loading ? html`
            <div class="file-preview">
              <div class="file-icon">📄</div>
              <div class="file-details">
                <div class="file-name">${tabState.file.name}</div>
                <div class="file-size">${formatFileSize(tabState.file.size)}</div>
                ${isContent && tabState.detectedImportType ? html`
                  <div class="detected-type">
                    ${tabState.detectedImportType === 'content' ? html`
                      <span class="type-badge content-badge">📝 Content Import</span>
                    ` : tabState.detectedImportType === 'media' ? html`
                      <span class="type-badge media-badge">🖼 Media Import</span>
                    ` : html`
                      <span class="type-badge unknown-badge">❓ Unknown Type</span>
                    `}
                  </div>
                ` : nothing}
              </div>
              <uui-button
                label="Clear"
                look="outline"
                @click=${() => isContent ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            </div>
          ` : nothing}

          <!-- Hidden File Input -->
          <input
            type="file"
            id=${fileInputId}
            accept=".csv,.zip"
            ?disabled=${tabState.loading}
            @change=${isContent ? this.handleContentFileChange : this.handleMediaFileChange}
          />

          <!-- Import Button -->
          <div class="btn-container">
            <uui-button
              label=${isContent ? 'Import File' : 'Import Media'}
              look="primary"
              color="positive"
              ?disabled=${!tabState.file || tabState.loading}
              @click=${isContent ? this.handleContentImport : this.handleMediaImport}
              style="--uui-button-padding: 12px 28px;">
              ${tabState.loading ? 'Processing...' : isContent ? (
                tabState.detectedImportType === 'content' ? '▲ Import Content' :
                tabState.detectedImportType === 'media' ? '▲ Import Media' :
                '▲ Import File'
              ) : '▲ Import Media'}
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }

  private renderLoadingState(type: 'content' | 'media') {
    return html`
      <div class="loading-state">
        <uui-loader-bar></uui-loader-bar>
        <p>Importing ${type}, please wait...</p>
      </div>
    `;
  }

  private renderContentRequirements() {
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
          <!-- Required CSV Columns -->
          <div class="req-section">
            <div class="req-label">Required CSV Columns</div>
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

          <!-- Media Files -->
          <div class="req-section">
            <div class="req-label">Media Files</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">▲</div>
                Upload a <strong>ZIP file</strong> to include media with your content
              </div>
              <div class="media-tip">
                <div class="tip-icon">🖼</div>
                Reference media using resolvers like <code>heroImage|zipFileToMedia</code>
              </div>
              <div class="media-tip">
                <div class="tip-icon">👥</div>
                Supports multi-CSV imports with automatic media deduplication
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                Add extra CSV columns for any property on your doc type
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  private renderMediaRequirements() {
    return html`
      <div class="requirements-card">
        <div class="card-header">
          <div class="icon-circle blue">ℹ</div>
          <div>
            <h2>Requirements & Help</h2>
            <span class="subtitle">Everything you need to format your media import file correctly</span>
          </div>
        </div>
        <div class="card-body">
          <!-- Upload Options -->
          <div class="req-section">
            <div class="req-label">Upload Options</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP file:</strong> Contains CSV and media files referenced in it
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                <strong>CSV only:</strong> For URL or file path imports
              </div>
            </div>
          </div>

          <!-- Required Columns -->
          <div class="req-section">
            <div class="req-label">Required CSV Columns (depends on import type)</div>
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

          <!-- Optional Features -->
          <div class="req-section">
            <div class="req-label">Optional Features</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">📁</div>
                <code>parent</code> column for folder ID/GUID/path (auto-creates folders)
              </div>
              <div class="media-tip">
                <div class="tip-icon">🏷</div>
                <code>name</code> and <code>mediaTypeAlias</code> (auto-detected if omitted)
              </div>
              <div class="media-tip">
                <div class="tip-icon">👥</div>
                Multi-CSV imports with automatic media deduplication
              </div>
              <div class="media-tip">
                <div class="tip-icon">✨</div>
                Custom properties like <code>altText</code>, <code>caption</code>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  private renderResults(type: 'content' | 'media', results: any) {
    const stats = {
      total: results.totalCount || 0,
      success: results.successCount || 0,
      failed: results.failureCount || 0
    };

    const title = type === 'content' ? 'Import Results' : 'Import Results';
    const subtitle = type === 'content' ? 'Summary of your content import' : 'Summary of your media import';

    return html`
      <div class="upload-card">
        <div class="card-header">
          <div class="icon-circle green">✓</div>
          <div>
            <h2>${title}</h2>
            <span class="subtitle">${subtitle}</span>
          </div>
        </div>
        <div class="card-body">
          <div class="results-summary">
            <div class="badge badge-total">
              <strong>Total:</strong> ${stats.total}
            </div>
            <div class="badge badge-success">
              <strong>✓ Success:</strong> ${stats.success}
            </div>
            ${stats.failed > 0 ? html`
              <div class="badge badge-failed">
                <strong>✗ Failed:</strong> ${stats.failed}
              </div>
            ` : nothing}
          </div>

          <div style="display: flex; gap: 10px;">
            <uui-button
              label="Download Results CSV"
              look="outline"
              @click=${type === 'content' ? this.handleContentExport : this.handleMediaExport}
              style="--uui-button-padding: 10px 20px;">
              ⬇ Download Results CSV
            </uui-button>
            <uui-button
              label="Clear Results"
              look="outline"
              @click=${() => type === 'content' ? this.service.clearContentResults() : this.service.clearMediaResults()}
              style="--uui-button-padding: 10px 20px;">
              Clear Results
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }

  private renderFooter() {
    return html`
      <footer class="plugin-footer">
        <div class="divider"></div>
        <a href="https://www.clerkswell.com" target="_blank" rel="noopener noreferrer" class="brand-link">
          Made for the Umbraco Community
          <span class="heart">❤️</span>
          from
          <img src="/App_Plugins/BulkUpload/images/cw-logo-primary-blue.png" alt="ClerksWell" />
        </a>
      </footer>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    /* Import shared variables */
    .bulk-upload-dashboard {
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

      max-width: 960px;
      margin: 24px auto 10px auto;
      padding: 20px;
      animation: fadeUp 0.5s ease both;
    }

    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(12px); }
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

    /* Tab panel */
    .tab-panel {
      animation: fadeIn 0.3s ease-in;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    /* Cards */
    .upload-card,
    .requirements-card {
      background: var(--umb-white);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius-lg);
      box-shadow: var(--umb-shadow-sm);
      overflow: hidden;
      margin-bottom: 24px;
      transition: box-shadow .25s;
    }

    .upload-card:hover,
    .requirements-card:hover {
      box-shadow: var(--umb-shadow-md);
    }

    .card-header {
      padding: 20px 28px;
      display: flex;
      align-items: center;
      gap: 12px;
      border-bottom: 1px solid var(--umb-border);
      background: linear-gradient(180deg, #fafbfe 0%, var(--umb-white) 100%);
    }

    .card-header .icon-circle {
      width: 36px;
      height: 36px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      font-size: 18px;
    }

    .card-header .icon-circle.blue {
      background: rgba(27,38,79,0.08);
      color: var(--umb-blue);
    }

    .card-header .icon-circle.green {
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
    }

    .card-header h2 {
      font-size: 15px;
      font-weight: 700;
      color: var(--umb-blue);
      margin: 0;
    }

    .card-header .subtitle {
      font-size: 12px;
      color: var(--umb-text-muted);
      display: block;
      margin-top: 2px;
    }

    .card-body {
      padding: 28px;
    }

    /* Drop zone */
    .drop-zone {
      border: 2px dashed var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 48px 32px;
      text-align: center;
      transition: border-color .25s, background .25s, transform .15s;
      cursor: pointer;
    }

    .drop-zone:hover {
      border-color: var(--umb-accent);
      background: var(--umb-accent-soft);
      transform: scale(1.005);
    }

    .drop-zone .upload-icon {
      width: 56px;
      height: 56px;
      border-radius: 16px;
      background: linear-gradient(135deg, var(--umb-accent-soft) 0%, #d4f3e4 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 16px;
      color: var(--umb-accent);
      transition: transform .3s;
      font-size: 24px;
    }

    .drop-zone:hover .upload-icon {
      transform: translateY(-4px);
    }

    .drop-zone h3 {
      font-size: 15px;
      font-weight: 700;
      color: var(--umb-text);
      margin-bottom: 6px;
    }

    .drop-zone h3 em {
      font-style: normal;
      color: var(--umb-accent);
      text-decoration: underline;
      text-underline-offset: 2px;
    }

    .drop-zone p {
      font-size: 13px;
      color: var(--umb-text-muted);
      margin: 0;
    }

    .drop-zone .file-types {
      display: flex;
      gap: 8px;
      justify-content: center;
      margin-top: 16px;
    }

    .file-chip {
      background: var(--umb-code-bg);
      color: var(--umb-text-muted);
      font-size: 11px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 6px;
      letter-spacing: 0.3px;
      text-transform: uppercase;
    }

    /* File preview */
    .file-preview {
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 16px;
      margin: 16px 0;
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .file-preview .file-icon {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      background: var(--umb-accent-soft);
      color: var(--umb-accent);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      flex-shrink: 0;
    }

    .file-preview .file-details {
      flex: 1;
    }

    .file-preview .file-name {
      font-weight: 700;
      color: var(--umb-text);
      font-size: 14px;
    }

    .file-preview .file-size {
      color: var(--umb-text-muted);
      font-size: 12px;
      margin-top: 2px;
    }

    .file-preview .detected-type {
      margin-top: 6px;
    }

    .type-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 11px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 12px;
      letter-spacing: 0.3px;
      text-transform: uppercase;
    }

    .content-badge {
      background: rgba(27,38,79,0.08);
      color: var(--umb-blue);
    }

    .media-badge {
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
    }

    .unknown-badge {
      background: rgba(245,193,66,0.15);
      color: #c49a2e;
    }

    /* Hidden file input */
    input[type="file"] {
      position: absolute;
      width: 1px;
      height: 1px;
      opacity: 0;
      pointer-events: none;
    }

    /* Buttons */
    .btn-container {
      text-align: center;
      margin-top: 24px;
    }

    uui-button[look="primary"] {
      --uui-button-background-color: var(--umb-accent);
      --uui-button-background-color-hover: var(--umb-accent-hover);
      box-shadow: 0 2px 8px rgba(43,195,123,0.25);
      transition: transform .15s, box-shadow .2s;
    }

    uui-button[look="primary"]:hover {
      transform: translateY(-1px);
      box-shadow: 0 4px 16px rgba(43,195,123,0.35);
    }

    uui-button[look="primary"]:active {
      transform: translateY(0);
    }

    /* Requirements */
    .req-section {
      margin-bottom: 24px;
    }

    .req-section:last-child {
      margin-bottom: 0;
    }

    .req-label {
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      color: var(--umb-text-muted);
      margin-bottom: 14px;
    }

    .req-grid {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr;
      gap: 12px;
    }

    @media (max-width: 768px) {
      .req-grid {
        grid-template-columns: 1fr;
      }
    }

    .req-item {
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 16px;
      transition: border-color .2s, box-shadow .2s;
    }

    .req-item:hover {
      border-color: rgba(27,38,79,0.2);
      box-shadow: var(--umb-shadow-sm);
    }

    .req-item code {
      display: inline-block;
      background: var(--umb-blue);
      color: #fff;
      font-size: 12px;
      font-weight: 700;
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
      padding: 3px 8px;
      border-radius: 5px;
      margin-bottom: 8px;
    }

    .req-item .desc {
      font-size: 13px;
      color: var(--umb-text-muted);
      line-height: 1.45;
    }

    .req-item .examples {
      margin-top: 8px;
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }

    .req-item .examples span {
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
      font-size: 11px;
      background: var(--umb-code-bg);
      color: var(--umb-text-muted);
      padding: 2px 7px;
      border-radius: 4px;
    }

    /* Media tips */
    .media-tips {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    @media (max-width: 768px) {
      .media-tips {
        grid-template-columns: 1fr;
      }
    }

    .media-tip {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 14px 16px;
      font-size: 13px;
      color: var(--umb-text-muted);
      line-height: 1.45;
    }

    .media-tip .tip-icon {
      width: 22px;
      height: 22px;
      border-radius: 6px;
      background: rgba(27,38,79,0.06);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      color: var(--umb-blue-light);
      font-size: 12px;
    }

    .media-tip code {
      background: var(--umb-code-bg);
      padding: 1px 5px;
      border-radius: 3px;
      font-size: 11px;
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
    }

    /* Results */
    .results-summary {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      margin-bottom: 1.5em;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from { opacity: 0; transform: translateX(-20px); }
      to { opacity: 1; transform: translateX(0); }
    }

    .badge {
      padding: 8px 16px;
      border-radius: 20px;
      border: 1px solid;
      font-size: 13px;
    }

    .badge-total {
      background-color: #f5f5f5;
      border-color: #ddd;
    }

    .badge-success {
      background-color: #d4edda;
      border-color: #28a745;
      color: #155724;
    }

    .badge-failed {
      background-color: #f8d7da;
      border-color: #dc3545;
      color: #721c24;
    }

    /* Footer */
    .plugin-footer {
      margin-top: 48px;
      text-align: center;
      animation: fadeUp .6s ease .15s both;
    }

    .plugin-footer .divider {
      width: 48px;
      height: 2px;
      background: var(--umb-border);
      margin: 0 auto 20px;
      border-radius: 2px;
    }

    .plugin-footer .brand-link {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      text-decoration: none;
      color: var(--umb-text-muted);
      font-size: 13px;
      font-weight: 400;
      padding: 10px 20px;
      border-radius: var(--umb-radius);
      transition: background .2s, color .2s, transform .15s;
    }

    .plugin-footer .brand-link:hover {
      background: rgba(27,38,79,0.04);
      color: var(--umb-blue);
      transform: translateY(-1px);
    }

    .plugin-footer .brand-link .heart {
      color: var(--umb-danger);
      font-size: 14px;
    }

    .plugin-footer .brand-link img {
      height: 18px;
      width: auto;
      opacity: 0.7;
      transition: opacity .2s;
    }

    .plugin-footer .brand-link:hover img {
      opacity: 1;
    }

    /* Loading state */
    .loading-state {
      margin-bottom: 20px;
      text-align: center;
    }

    .loading-state p {
      color: #666;
      margin-top: 10px;
    }

    /* Accessibility */
    uui-button:focus-visible,
    input:focus-visible {
      outline: 2px solid var(--umb-blue);
      outline-offset: 2px;
    }
  `;
}
