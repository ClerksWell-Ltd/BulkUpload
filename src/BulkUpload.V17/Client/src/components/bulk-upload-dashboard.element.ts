/**
 * Bulk Upload Dashboard Component
 * Lit-based component for Umbraco 17
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

  private async handleContentImport(): Promise<void> {
    await this.service.importContent();
  }

  private async handleMediaImport(): Promise<void> {
    await this.service.importMedia();
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
      <uui-box>
        <div slot="header" class="dashboard-header">
          <h2>Bulk Upload</h2>
        </div>

        <!-- Tab Navigation -->
        <uui-tab-group>
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
            ${this.renderInfoBox('content')}
            ${this.renderUploadSection('content', content)}
            ${content.loading ? this.renderLoadingState('content') : nothing}
            ${content.results && !content.loading ? this.renderResults('content', content.results) : nothing}
          </div>
        ` : nothing}

        <!-- Media Import Panel -->
        ${activeTab === 'media' ? html`
          <div class="tab-panel">
            ${this.renderInfoBox('media')}
            ${this.renderUploadSection('media', media)}
            ${media.loading ? this.renderLoadingState('media') : nothing}
            ${media.results && !media.loading ? this.renderResults('media', media.results) : nothing}
          </div>
        ` : nothing}
      </uui-box>
    `;
  }

  private renderInfoBox(type: 'content' | 'media') {
    if (type === 'content') {
      return html`
        <uui-box look="outline" class="info-box">
          <div class="info-content">
            <div class="info-icon">ℹ️</div>
            <div>
              <h4>Requirements</h4>
              <ul>
                <li>The <code>parent</code>, <code>docTypeAlias</code>, and <code>name</code> columns are required</li>
                <li>Upload a ZIP file if you have media files to include with your content</li>
                <li>Use resolvers like <code>zipFileToMedia</code> to reference media files</li>
              </ul>
            </div>
          </div>
        </uui-box>
      `;
    } else {
      return html`
        <uui-box look="outline" class="info-box">
          <div class="info-content">
            <div class="info-icon">ℹ️</div>
            <div>
              <h4>Requirements</h4>
              <ul>
                <li>The <code>fileName</code> column is required (name of media file in ZIP)</li>
                <li>Upload a ZIP file containing both CSV and media files</li>
                <li>The <code>parent</code> column can specify folder path or ID</li>
                <li>Media type is auto-detected from file extension</li>
              </ul>
            </div>
          </div>
        </uui-box>
      `;
    }
  }

  private renderUploadSection(type: 'content' | 'media', tabState: any) {
    const isContent = type === 'content';
    const fileInputId = isContent ? 'content-file-input' : 'media-file-input';

    return html`
      <uui-box headline="Upload File" class="upload-section">
        <div class="upload-content">
          <label for=${fileInputId} class="file-label">
            Select CSV or ZIP file
          </label>
          <input
            type="file"
            id=${fileInputId}
            accept=".csv,.zip"
            ?disabled=${tabState.loading}
            @change=${isContent ? this.handleContentFileChange : this.handleMediaFileChange}
            class="file-input" />

          ${tabState.file && !tabState.loading ? html`
            <div class="file-info">
              <span class="file-icon">📄</span>
              <div class="file-details">
                <strong>${tabState.file.name}</strong>
                <span class="file-size">(${formatFileSize(tabState.file.size)})</span>
              </div>
            </div>
          ` : nothing}

          <div class="button-group">
            <uui-button
              label=${isContent ? 'Import Content' : 'Import Media'}
              look="primary"
              color="positive"
              ?disabled=${!tabState.file || tabState.loading}
              @click=${isContent ? this.handleContentImport : this.handleMediaImport}>
              ${tabState.loading ? 'Processing...' : `▲ Import ${isContent ? 'Content' : 'Media'}`}
            </uui-button>
            ${tabState.file && !tabState.loading ? html`
              <uui-button
                label="Clear File"
                look="outline"
                @click=${() => isContent ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            ` : nothing}
          </div>
        </div>
      </uui-box>
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

  private renderResults(type: 'content' | 'media', results: any) {
    const stats = {
      total: results.totalCount || 0,
      success: results.successCount || 0,
      failed: results.failureCount || 0,
      successRate: results.totalCount > 0
        ? Math.round((results.successCount / results.totalCount) * 100)
        : 0
    };

    return html`
      <uui-box headline="Import Results" class="results-section">
        <div class="results-content">
          <!-- Statistics -->
          <div class="stats-grid">
            <div class="stat-card stat-total">
              <div class="stat-label">Total</div>
              <div class="stat-value">${stats.total}</div>
            </div>
            <div class="stat-card stat-success">
              <div class="stat-label">Success</div>
              <div class="stat-value">${stats.success}</div>
            </div>
            <div class="stat-card stat-failed">
              <div class="stat-label">Failed</div>
              <div class="stat-value">${stats.failed}</div>
            </div>
            <div class="stat-card stat-rate">
              <div class="stat-label">Success Rate</div>
              <div class="stat-value">${stats.successRate}%</div>
            </div>
          </div>

          <!-- Export Button -->
          <div class="export-section">
            <uui-button
              label="Export Results"
              look="outline"
              color="default"
              @click=${type === 'content' ? this.handleContentExport : this.handleMediaExport}>
              ⬇ Export Results
            </uui-button>
          </div>

          <!-- Results Table -->
          ${results.results && results.results.length > 0 ? html`
            <div class="results-table-container">
              <table class="results-table">
                <thead>
                  <tr>
                    <th>Status</th>
                    <th>Name</th>
                    ${type === 'content' ? html`<th>Doc Type</th>` : html`<th>Media Type</th>`}
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  ${results.results.map((result: any) => html`
                    <tr class=${result.success ? 'success' : 'failed'}>
                      <td>${result.success ? '✅' : '❌'}</td>
                      <td>${result.name || '-'}</td>
                      <td>${type === 'content' ? result.docTypeAlias : result.mediaTypeAlias || '-'}</td>
                      <td class="message-cell">${result.errorMessage || 'Success'}</td>
                    </tr>
                  `)}
                </tbody>
              </table>
            </div>
          ` : nothing}
        </div>
      </uui-box>
    `;
  }

  static styles = css`
    :host {
      display: block;
      padding: 20px;
    }

    .dashboard-header h2 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    uui-tab-group {
      margin-bottom: 20px;
    }

    .tab-panel {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .info-box {
      border-color: #f0ad4e;
      background-color: #fcf8e3;
    }

    .info-content {
      padding: 1em;
      color: #8a6d3b;
      display: flex;
      align-items: start;
      gap: 10px;
    }

    .info-icon {
      font-size: 1.5em;
    }

    .info-content h4 {
      margin: 0 0 0.5em 0;
    }

    .info-content ul {
      margin: 0.5em 0;
      padding-left: 1.5em;
    }

    .info-content code {
      background-color: rgba(0, 0, 0, 0.05);
      padding: 2px 4px;
      border-radius: 3px;
    }

    .upload-content {
      padding: 1em;
    }

    .file-label {
      display: block;
      font-weight: 600;
      margin-bottom: 0.5em;
    }

    .file-input {
      margin-bottom: 1em;
      width: 100%;
    }

    .file-info {
      margin-bottom: 1em;
      padding: 0.75em;
      background-color: #f5f5f5;
      border-radius: 4px;
      border-left: 3px solid #1b264f;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .file-icon {
      font-size: 1.2em;
    }

    .file-size {
      color: #666;
      margin-left: 8px;
    }

    .button-group {
      display: flex;
      gap: 10px;
    }

    .loading-state {
      margin-bottom: 20px;
    }

    .loading-state p {
      text-align: center;
      color: #666;
      margin-top: 10px;
    }

    .results-content {
      padding: 1em;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 15px;
      margin-bottom: 20px;
    }

    .stat-card {
      padding: 15px;
      border-radius: 8px;
      text-align: center;
    }

    .stat-total {
      background-color: #e3f2fd;
      border-left: 4px solid #1976d2;
    }

    .stat-success {
      background-color: #e8f5e9;
      border-left: 4px solid #4caf50;
    }

    .stat-failed {
      background-color: #ffebee;
      border-left: 4px solid #f44336;
    }

    .stat-rate {
      background-color: #f3e5f5;
      border-left: 4px solid #9c27b0;
    }

    .stat-label {
      font-size: 0.875rem;
      color: #666;
      margin-bottom: 5px;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 600;
    }

    .export-section {
      margin-bottom: 20px;
    }

    .results-table-container {
      overflow-x: auto;
      border: 1px solid #ddd;
      border-radius: 4px;
    }

    .results-table {
      width: 100%;
      border-collapse: collapse;
    }

    .results-table th,
    .results-table td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    .results-table th {
      background-color: #f5f5f5;
      font-weight: 600;
    }

    .results-table tr.success {
      background-color: #f1f8f4;
    }

    .results-table tr.failed {
      background-color: #fef5f5;
    }

    .message-cell {
      max-width: 300px;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `;
}
