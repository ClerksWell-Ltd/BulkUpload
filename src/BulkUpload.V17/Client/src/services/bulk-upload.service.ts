/**
 * Bulk Upload Service
 * TypeScript version for Umbraco 17 (Lit)
 *
 * Framework-agnostic business logic and state management
 */

import type { BulkUploadApiClient, ImportResultResponse } from '../api/bulk-upload-api';

/**
 * Notification object
 */
export interface Notification {
  type: 'success' | 'warning' | 'error' | 'info';
  headline: string;
  message: string;
}

/**
 * Import tab state
 */
export interface ImportTabState {
  loading: boolean;
  file: File | null;
  fileElement: HTMLInputElement | null;
  results: ImportResultResponse | null;
}

/**
 * Application state
 */
export interface BulkUploadState {
  activeTab: 'content' | 'media';
  content: ImportTabState;
  media: ImportTabState;
}

/**
 * Notification handler callback type
 */
export type NotificationHandler = (notification: Notification) => void;

/**
 * State change handler callback type
 */
export type StateChangeHandler = (state: BulkUploadState) => void;

/**
 * Bulk Upload Service
 */
export class BulkUploadService {
  private apiClient: BulkUploadApiClient;
  private notify: NotificationHandler;
  private onStateChange: StateChangeHandler | null;
  private state: BulkUploadState;

  constructor(
    apiClient: BulkUploadApiClient,
    notificationHandler: NotificationHandler,
    stateChangeHandler?: StateChangeHandler
  ) {
    if (!apiClient) {
      throw new Error('API client is required');
    }
    if (!notificationHandler) {
      throw new Error('Notification handler is required');
    }

    this.apiClient = apiClient;
    this.notify = notificationHandler;
    this.onStateChange = stateChangeHandler || null;

    // Initialize state
    this.state = this.createInitialState();
  }

  /**
   * Creates initial state structure
   */
  private createInitialState(): BulkUploadState {
    return {
      activeTab: 'content',
      content: {
        loading: false,
        file: null,
        fileElement: null,
        results: null
      },
      media: {
        loading: false,
        file: null,
        fileElement: null,
        results: null
      }
    };
  }

  /**
   * Emits state change event
   */
  private emitStateChange(): void {
    if (this.onStateChange) {
      this.onStateChange({ ...this.state });
    }
  }

  /**
   * Sets the active tab
   */
  setActiveTab(tab: 'content' | 'media'): void {
    if (tab !== 'content' && tab !== 'media') {
      throw new Error('Invalid tab name. Must be "content" or "media"');
    }
    this.state.activeTab = tab;
    this.emitStateChange();
  }

  /**
   * Sets content file and file element
   */
  setContentFile(file: File, fileElement?: HTMLInputElement): void {
    this.state.content.file = file;
    this.state.content.fileElement = fileElement || null;
    this.emitStateChange();
  }

  /**
   * Sets media file and file element
   */
  setMediaFile(file: File, fileElement?: HTMLInputElement): void {
    this.state.media.file = file;
    this.state.media.fileElement = fileElement || null;
    this.emitStateChange();
  }

  /**
   * Clears content file
   */
  clearContentFile(): void {
    this.state.content.file = null;
    if (this.state.content.fileElement) {
      this.state.content.fileElement.value = '';
    }
    this.emitStateChange();
  }

  /**
   * Clears media file
   */
  clearMediaFile(): void {
    this.state.media.file = null;
    if (this.state.media.fileElement) {
      this.state.media.fileElement.value = '';
    }
    this.emitStateChange();
  }

  /**
   * Clears content results
   */
  clearContentResults(): void {
    this.state.content.results = null;
    this.emitStateChange();
  }

  /**
   * Clears media results
   */
  clearMediaResults(): void {
    this.state.media.results = null;
    this.emitStateChange();
  }

  /**
   * Imports content from selected file
   */
  async importContent(): Promise<ImportResultResponse | null> {
    const file = this.state.content.file;

    if (!file) {
      this.notify({
        type: 'warning',
        headline: 'No File Selected',
        message: 'Please select a CSV or ZIP file to import.'
      });
      return null;
    }

    // Validate file
    const validation = this.apiClient.validateFile(file, {
      acceptedTypes: ['.csv', '.zip'],
      maxSizeInMB: 100
    });

    if (!validation.valid) {
      this.notify({
        type: 'error',
        headline: 'Invalid File',
        message: validation.errors.join(', ')
      });
      return null;
    }

    // Start import
    this.state.content.loading = true;
    this.state.content.results = null;
    this.emitStateChange();

    try {
      const response = await this.apiClient.importContent(file);

      // Clear file after successful upload
      this.clearContentFile();

      // Store results
      this.state.content.results = response.data;

      // Use pre-calculated stats from API response
      const stats = {
        total: response.data.totalCount || 0,
        success: response.data.successCount || 0,
        failed: response.data.failureCount || 0
      };

      // Create summary message
      let message: string;
      if (stats.total === 0) {
        message = 'No content items to import.';
      } else if (stats.failed === 0) {
        message = `All ${stats.total} content items imported successfully.`;
      } else if (stats.success === 0) {
        message = `All ${stats.total} content items failed to import.`;
      } else {
        message = `${stats.success} of ${stats.total} content items imported successfully. ${stats.failed} failed.`;
      }

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Content Import Complete',
        message
      });

      return response.data;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Import Failed',
        message: (error as Error).message || 'An error occurred during content import.'
      });
      throw error;

    } finally {
      this.state.content.loading = false;
      this.emitStateChange();
    }
  }

  /**
   * Imports media from selected file
   */
  async importMedia(): Promise<ImportResultResponse | null> {
    const file = this.state.media.file;

    if (!file) {
      this.notify({
        type: 'warning',
        headline: 'No File Selected',
        message: 'Please select a CSV or ZIP file to import.'
      });
      return null;
    }

    // Validate file
    const validation = this.apiClient.validateFile(file, {
      acceptedTypes: ['.csv', '.zip'],
      maxSizeInMB: 100
    });

    if (!validation.valid) {
      this.notify({
        type: 'error',
        headline: 'Invalid File',
        message: validation.errors.join(', ')
      });
      return null;
    }

    // Start import
    this.state.media.loading = true;
    this.state.media.results = null;
    this.emitStateChange();

    try {
      const response = await this.apiClient.importMedia(file);

      // Clear file after successful upload
      this.clearMediaFile();

      // Store results
      this.state.media.results = response.data;

      // Use pre-calculated stats from API response
      const stats = {
        total: response.data.totalCount || 0,
        success: response.data.successCount || 0,
        failed: response.data.failureCount || 0
      };

      // Create summary message
      let message: string;
      if (stats.total === 0) {
        message = 'No media items to import.';
      } else if (stats.failed === 0) {
        message = `All ${stats.total} media items imported successfully.`;
      } else if (stats.success === 0) {
        message = `All ${stats.total} media items failed to import.`;
      } else {
        message = `${stats.success} of ${stats.total} media items imported successfully. ${stats.failed} failed.`;
      }

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Media Import Complete',
        message
      });

      return response.data;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Import Failed',
        message: (error as Error).message || 'An error occurred during media import.'
      });
      throw error;

    } finally {
      this.state.media.loading = false;
      this.emitStateChange();
    }
  }

  /**
   * Exports content import results to CSV
   */
  async exportContentResults(): Promise<Response | null> {
    const results = this.state.content.results;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No results available to export.'
      });
      return null;
    }

    try {
      const response = await this.apiClient.exportContentResults(results.results);

      this.notify({
        type: 'success',
        headline: 'Export Successful',
        message: 'Results exported successfully.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Export Failed',
        message: 'Failed to export results.'
      });
      throw error;
    }
  }

  /**
   * Exports media import results to CSV
   */
  async exportMediaResults(): Promise<Response | null> {
    const results = this.state.media.results;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No results available to export.'
      });
      return null;
    }

    try {
      const response = await this.apiClient.exportMediaResults(results.results);

      this.notify({
        type: 'success',
        headline: 'Export Successful',
        message: 'Results exported successfully.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Export Failed',
        message: 'Failed to export results.'
      });
      throw error;
    }
  }

  /**
   * Gets current state (for debugging or serialization)
   */
  getState(): BulkUploadState {
    return { ...this.state };
  }

  /**
   * Resets service to initial state
   */
  reset(): void {
    this.state = this.createInitialState();
    this.emitStateChange();
  }
}
