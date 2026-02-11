/**
 * Bulk Upload Service - Unified Version
 * TypeScript version for Umbraco 17 (Lit)
 *
 * Framework-agnostic business logic with unified upload support.
 * Handles both content and media in one upload field with automatic detection.
 */

import type { BulkUploadApiClient, ImportResultResponse, MediaPreprocessingResult } from '../api/bulk-upload-api';
import type { UploadDetection } from '../utils/file.utils';

/**
 * Notification object
 */
export interface Notification {
  type: 'success' | 'warning' | 'error' | 'info';
  headline: string;
  message: string;
}

/**
 * Unified application state
 */
export interface BulkUploadState {
  loading: boolean;
  file: File | null;
  fileElement: HTMLInputElement | null;
  detection: UploadDetection | null;
  results: {
    content: ImportResultResponse | null;
    media: ImportResultResponse | null;
    mediaPreprocessing: MediaPreprocessingResult[] | null;
  };
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
   * Creates initial state structure for unified upload
   */
  private createInitialState(): BulkUploadState {
    return {
      loading: false,
      file: null,
      fileElement: null,
      detection: null,
      results: {
        content: null,
        media: null,
        mediaPreprocessing: null
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
   * Sets file with detection results
   */
  public setFile(file: File, fileElement: HTMLInputElement | null, detection: UploadDetection | null): void {
    this.state.file = file;
    this.state.fileElement = fileElement;
    this.state.detection = detection;
    this.emitStateChange();
  }

  /**
   * Clears file
   */
  public clearFile(): void {
    this.state.file = null;
    this.state.detection = null;
    if (this.state.fileElement) {
      this.state.fileElement.value = '';
    }
    this.emitStateChange();
  }

  /**
   * Clears content results
   */
  public clearContentResults(): void {
    this.state.results.content = null;
    this.state.results.mediaPreprocessing = null;
    this.emitStateChange();
  }

  /**
   * Clears media results
   */
  public clearMediaResults(): void {
    this.state.results.media = null;
    this.emitStateChange();
  }

  /**
   * Unified import that handles media-first processing
   * Processes media CSV first (if present), then content CSV (if present)
   */
  public async importUnified(): Promise<{
    content: ImportResultResponse | null;
    media: ImportResultResponse | null;
    mediaPreprocessing: MediaPreprocessingResult[] | null;
  } | null> {
    const file = this.state.file;
    const detection = this.state.detection;

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
    this.state.loading = true;
    this.state.results.content = null;
    this.state.results.media = null;
    this.state.results.mediaPreprocessing = null;
    this.emitStateChange();

    try {
      const hasMediaCSV = detection?.hasMediaCSV ?? false;
      const hasContentCSV = detection?.hasContentCSV ?? false;

      // Process based on detection
      // Media CSV is always processed first (if present)
      if (hasMediaCSV) {
        await this.processMediaImport(file);
      }

      // Then process content CSV (if present)
      if (hasContentCSV) {
        await this.processContentImport(file);
      }

      // If detection failed or neither CSV type was found, try content import as fallback
      if (!hasMediaCSV && !hasContentCSV) {
        await this.processContentImport(file);
      }

      // Clear file after successful upload
      this.clearFile();

      // Build combined success message
      this.showCombinedSuccessMessage();

      return {
        content: this.state.results.content,
        media: this.state.results.media,
        mediaPreprocessing: this.state.results.mediaPreprocessing
      };

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Import Failed',
        message: error instanceof Error ? error.message : 'An error occurred during import.'
      });
      throw error;

    } finally {
      this.state.loading = false;
      this.emitStateChange();
    }
  }

  /**
   * Imports media from a ZIP file without requiring a CSV.
   * Automatically creates media items based on folder structure.
   * Downloads the results CSV automatically.
   */
  public async importMediaFromZipOnly(): Promise<Response | null> {
    const file = this.state.file;

    if (!file) {
      this.notify({
        type: 'warning',
        headline: 'No File Selected',
        message: 'Please select a ZIP file to import.'
      });
      return null;
    }

    // Validate file is a ZIP
    const validation = this.apiClient.validateFile(file, {
      acceptedTypes: ['.zip'],
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
    this.state.loading = true;
    this.emitStateChange();

    try {
      const response = await this.apiClient.importMediaFromZipOnly(file);

      // Clear file after successful upload
      this.clearFile();

      this.notify({
        type: 'success',
        headline: 'ZIP Media Import Successful',
        message: 'Media files have been imported from the ZIP archive. Results CSV will be downloaded automatically.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'ZIP Media Import Failed',
        message: error instanceof Error ? error.message : 'An error occurred during ZIP media import.'
      });
      throw error;

    } finally {
      this.state.loading = false;
      this.emitStateChange();
    }
  }

  /**
   * Process media import
   */
  private async processMediaImport(file: File): Promise<void> {
    try {
      const response = await this.apiClient.importMedia(file);
      this.state.results.media = response.data;
    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Media Import Failed',
        message: error instanceof Error ? error.message : 'An error occurred during media import.'
      });
      throw error;
    }
  }

  /**
   * Process content import
   */
  private async processContentImport(file: File): Promise<void> {
    try {
      const response = await this.apiClient.importContent(file);
      this.state.results.content = response.data;

      // Store media preprocessing results if present (from ZIP with media files)
      if (response.data?.mediaPreprocessingResults) {
        this.state.results.mediaPreprocessing = response.data.mediaPreprocessingResults;
      }
    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Content Import Failed',
        message: error instanceof Error ? error.message : 'An error occurred during content import.'
      });
      throw error;
    }
  }

  /**
   * Shows combined success message based on what was imported
   */
  private showCombinedSuccessMessage(): void {
    const messages: string[] = [];
    let hasErrors = false;

    // Media import results
    if (this.state.results.media) {
      const mediaStats = {
        total: this.state.results.media.totalCount || 0,
        success: this.state.results.media.successCount || 0,
        failed: this.state.results.media.failureCount || 0
      };

      if (mediaStats.total > 0) {
        if (mediaStats.failed === 0) {
          messages.push(`✓ Media: All ${mediaStats.total} items imported successfully.`);
        } else {
          messages.push(`⚠ Media: ${mediaStats.success} of ${mediaStats.total} items imported. ${mediaStats.failed} failed.`);
          hasErrors = true;
        }
      }
    }

    // Content import results
    if (this.state.results.content) {
      const contentStats = {
        total: this.state.results.content.totalCount || 0,
        success: this.state.results.content.successCount || 0,
        failed: this.state.results.content.failureCount || 0
      };

      if (contentStats.total > 0) {
        if (contentStats.failed === 0) {
          messages.push(`✓ Content: All ${contentStats.total} items imported successfully.`);
        } else {
          messages.push(`⚠ Content: ${contentStats.success} of ${contentStats.total} items imported. ${contentStats.failed} failed.`);
          hasErrors = true;
        }
      }
    }

    // Media preprocessing results (from content import with media files)
    if (this.state.results.mediaPreprocessing && this.state.results.mediaPreprocessing.length > 0) {
      const mediaPreprocessingSuccess = this.state.results.mediaPreprocessing.filter(r => r.success).length;
      const mediaPreprocessingFailed = this.state.results.mediaPreprocessing.filter(r => !r.success).length;

      if (mediaPreprocessingFailed === 0) {
        messages.push(`✓ Media Files: All ${this.state.results.mediaPreprocessing.length} files processed successfully.`);
      } else {
        messages.push(`⚠ Media Files: ${mediaPreprocessingSuccess} of ${this.state.results.mediaPreprocessing.length} files processed. ${mediaPreprocessingFailed} failed.`);
        hasErrors = true;
      }
    }

    const headline = hasErrors ? 'Import Completed with Warnings' : 'Import Successful';
    const message = messages.length > 0 ? messages.join('\n') : 'Import completed.';

    this.notify({
      type: hasErrors ? 'warning' : 'success',
      headline,
      message
    });
  }

  /**
   * Exports content import results to CSV
   */
  public async exportContentResults(): Promise<Response | null> {
    const results = this.state.results.content;

    if (!results?.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No content results available to export.'
      });
      return null;
    }

    try {
      const response = await this.apiClient.exportContentResults(results.results);

      this.notify({
        type: 'success',
        headline: 'Export Successful',
        message: 'Content results exported successfully.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Export Failed',
        message: 'Failed to export content results.'
      });
      throw error;
    }
  }

  /**
   * Exports media import results to CSV
   */
  public async exportMediaResults(): Promise<Response | null> {
    const results = this.state.results.media;

    if (!results?.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No media results available to export.'
      });
      return null;
    }

    try {
      const response = await this.apiClient.exportMediaResults(results.results);

      this.notify({
        type: 'success',
        headline: 'Export Successful',
        message: 'Media results exported successfully.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Export Failed',
        message: 'Failed to export media results.'
      });
      throw error;
    }
  }

  /**
   * Exports media preprocessing results to CSV
   */
  public async exportMediaPreprocessingResults(): Promise<Response | null> {
    const results = this.state.results.mediaPreprocessing;

    if (!results || results.length === 0) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No media preprocessing results available to export.'
      });
      return null;
    }

    try {
      const response = await this.apiClient.exportMediaPreprocessingResults(results);

      this.notify({
        type: 'success',
        headline: 'Export Successful',
        message: 'Media preprocessing results exported successfully.'
      });

      return response;

    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Export Failed',
        message: 'Failed to export media preprocessing results.'
      });
      throw error;
    }
  }

  /**
   * Gets current state (for debugging or serialization)
   */
  public getState(): BulkUploadState {
    return { ...this.state };
  }

  /**
   * Resets service to initial state
   */
  public reset(): void {
    this.state = this.createInitialState();
    this.emitStateChange();
  }
}
