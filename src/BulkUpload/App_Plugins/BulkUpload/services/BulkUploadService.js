/**
 * Bulk Upload Service
 * Framework-agnostic business logic and state management
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit)
 *
 * This service encapsulates all business logic, making it reusable
 * across different UI frameworks.
 */

import { calculateResultStats, getResultSummaryMessage } from '../utils/resultUtils.js';

export class BulkUploadService {
  /**
   * Creates a new BulkUploadService instance
   * @param {Object} apiClient - API client instance (BulkUploadApiClient)
   * @param {Function} notificationHandler - Callback for notifications (headline, message, type)
   * @param {Function} stateChangeHandler - Optional callback when state changes
   */
  constructor(apiClient, notificationHandler, stateChangeHandler = null) {
    if (!apiClient) {
      throw new Error('API client is required');
    }
    if (!notificationHandler) {
      throw new Error('Notification handler is required');
    }

    this.apiClient = apiClient;
    this.notify = notificationHandler;
    this.onStateChange = stateChangeHandler;

    // Initialize state
    this.state = this.createInitialState();
  }

  /**
   * Creates initial state structure
   * @returns {Object} Initial state object
   */
  createInitialState() {
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
   * @private
   */
  emitStateChange() {
    if (this.onStateChange && typeof this.onStateChange === 'function') {
      this.onStateChange(this.state);
    }
  }

  /**
   * Sets the active tab
   * @param {string} tab - Tab name ('content' or 'media')
   */
  setActiveTab(tab) {
    if (tab !== 'content' && tab !== 'media') {
      throw new Error('Invalid tab name. Must be "content" or "media"');
    }
    this.state.activeTab = tab;
    this.emitStateChange();
  }

  /**
   * Sets content file and file element
   * @param {File} file - The selected file
   * @param {HTMLElement} fileElement - The file input element
   */
  setContentFile(file, fileElement = null) {
    this.state.content.file = file;
    this.state.content.fileElement = fileElement;
    this.emitStateChange();
  }

  /**
   * Sets media file and file element
   * @param {File} file - The selected file
   * @param {HTMLElement} fileElement - The file input element
   */
  setMediaFile(file, fileElement = null) {
    this.state.media.file = file;
    this.state.media.fileElement = fileElement;
    this.emitStateChange();
  }

  /**
   * Clears content file
   */
  clearContentFile() {
    this.state.content.file = null;
    if (this.state.content.fileElement) {
      this.state.content.fileElement.value = '';
    }
    this.emitStateChange();
  }

  /**
   * Clears media file
   */
  clearMediaFile() {
    this.state.media.file = null;
    if (this.state.media.fileElement) {
      this.state.media.fileElement.value = '';
    }
    this.emitStateChange();
  }

  /**
   * Clears content results
   */
  clearContentResults() {
    this.state.content.results = null;
    this.emitStateChange();
  }

  /**
   * Clears media results
   */
  clearMediaResults() {
    this.state.media.results = null;
    this.emitStateChange();
  }

  /**
   * Imports content from selected file
   * @returns {Promise<Object>} Promise resolving to import results
   */
  async importContent() {
    const { file } = this.state.content;

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

      // Calculate stats and notify
      const stats = calculateResultStats(response.data.results);
      const message = getResultSummaryMessage(response.data.results, 'content items');

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Content Import Complete',
        message: message
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
      this.emitStateChange();
    }
  }

  /**
   * Imports media from selected file
   * @returns {Promise<Object>} Promise resolving to import results
   */
  async importMedia() {
    const { file } = this.state.media;

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

      // Calculate stats and notify
      const stats = calculateResultStats(response.data.results);
      const message = getResultSummaryMessage(response.data.results, 'media items');

      this.notify({
        type: stats.failed > 0 ? 'warning' : 'success',
        headline: 'Media Import Complete',
        message: message
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
      this.emitStateChange();
    }
  }

  /**
   * Exports content import results to CSV
   * @returns {Promise<Blob>} Promise resolving to CSV blob
   */
  async exportContentResults() {
    const { results } = this.state.content;

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
   * @returns {Promise<Blob>} Promise resolving to CSV blob
   */
  async exportMediaResults() {
    const { results } = this.state.media;

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
   * @returns {Object} Current state object
   */
  getState() {
    return { ...this.state };
  }

  /**
   * Resets service to initial state
   */
  reset() {
    this.state = this.createInitialState();
    this.emitStateChange();
  }
}
