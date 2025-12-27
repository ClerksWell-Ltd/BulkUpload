/**
 * Bulk Upload Service
 * Framework-agnostic business logic and state management
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit/Vite)
 *
 * This service encapsulates all business logic, making it reusable
 * across different UI frameworks.
 *
 * NOTE: This file is written in ES5/IIFE format for Umbraco 13 compatibility.
 * In v17, this will be replaced with ES6 module syntax and bundled with Vite.
 */

(function(window) {
  'use strict';

  // Create namespace if it doesn't exist
  window.BulkUpload = window.BulkUpload || {};

  /**
   * Bulk Upload Service
   * @param {Object} apiClient - API client instance (BulkUploadApiClient)
   * @param {Function} notificationHandler - Callback for notifications (headline, message, type)
   * @param {Function} stateChangeHandler - Optional callback when state changes
   */
  function BulkUploadService(apiClient, notificationHandler, stateChangeHandler) {
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
   * @returns {Object} Initial state object
   */
  BulkUploadService.prototype.createInitialState = function() {
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
  };

  /**
   * Emits state change event
   * @private
   */
  BulkUploadService.prototype.emitStateChange = function() {
    if (this.onStateChange && typeof this.onStateChange === 'function') {
      this.onStateChange(this.state);
    }
  };

  /**
   * Sets the active tab
   * @param {string} tab - Tab name ('content' or 'media')
   */
  BulkUploadService.prototype.setActiveTab = function(tab) {
    if (tab !== 'content' && tab !== 'media') {
      throw new Error('Invalid tab name. Must be "content" or "media"');
    }
    this.state.activeTab = tab;
    this.emitStateChange();
  };

  /**
   * Sets content file and file element
   * @param {File} file - The selected file
   * @param {HTMLElement} fileElement - The file input element
   */
  BulkUploadService.prototype.setContentFile = function(file, fileElement) {
    this.state.content.file = file;
    this.state.content.fileElement = fileElement || null;
    this.emitStateChange();
  };

  /**
   * Sets media file and file element
   * @param {File} file - The selected file
   * @param {HTMLElement} fileElement - The file input element
   */
  BulkUploadService.prototype.setMediaFile = function(file, fileElement) {
    this.state.media.file = file;
    this.state.media.fileElement = fileElement || null;
    this.emitStateChange();
  };

  /**
   * Clears content file
   */
  BulkUploadService.prototype.clearContentFile = function() {
    this.state.content.file = null;
    if (this.state.content.fileElement) {
      this.state.content.fileElement.value = '';
    }
    this.emitStateChange();
  };

  /**
   * Clears media file
   */
  BulkUploadService.prototype.clearMediaFile = function() {
    this.state.media.file = null;
    if (this.state.media.fileElement) {
      this.state.media.fileElement.value = '';
    }
    this.emitStateChange();
  };

  /**
   * Clears content results
   */
  BulkUploadService.prototype.clearContentResults = function() {
    this.state.content.results = null;
    this.emitStateChange();
  };

  /**
   * Clears media results
   */
  BulkUploadService.prototype.clearMediaResults = function() {
    this.state.media.results = null;
    this.emitStateChange();
  };

  /**
   * Imports content from selected file
   * @returns {Promise<Object>} Promise resolving to import results
   */
  BulkUploadService.prototype.importContent = async function() {
    var self = this;
    var file = this.state.content.file;

    if (!file) {
      this.notify({
        type: 'warning',
        headline: 'No File Selected',
        message: 'Please select a CSV or ZIP file to import.'
      });
      return null;
    }

    // Validate file
    var validation = this.apiClient.validateFile(file, {
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
      var response = await this.apiClient.importContent(file);

      // Clear file after successful upload
      this.clearContentFile();

      // Store results
      this.state.content.results = response.data;

      // Use pre-calculated stats from API response
      var stats = {
        total: response.data.totalCount || 0,
        success: response.data.successCount || 0,
        failed: response.data.failureCount || 0
      };

      // Create summary message
      var message;
      if (stats.total === 0) {
        message = 'No content items to import.';
      } else if (stats.failed === 0) {
        message = 'All ' + stats.total + ' content items imported successfully.';
      } else if (stats.success === 0) {
        message = 'All ' + stats.total + ' content items failed to import.';
      } else {
        message = stats.success + ' of ' + stats.total + ' content items imported successfully. ' + stats.failed + ' failed.';
      }

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
  };

  /**
   * Imports media from selected file
   * @returns {Promise<Object>} Promise resolving to import results
   */
  BulkUploadService.prototype.importMedia = async function() {
    var self = this;
    var file = this.state.media.file;

    if (!file) {
      this.notify({
        type: 'warning',
        headline: 'No File Selected',
        message: 'Please select a CSV or ZIP file to import.'
      });
      return null;
    }

    // Validate file
    var validation = this.apiClient.validateFile(file, {
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
      var response = await this.apiClient.importMedia(file);

      // Clear file after successful upload
      this.clearMediaFile();

      // Store results
      this.state.media.results = response.data;

      // Use pre-calculated stats from API response
      var stats = {
        total: response.data.totalCount || 0,
        success: response.data.successCount || 0,
        failed: response.data.failureCount || 0
      };

      // Create summary message
      var message;
      if (stats.total === 0) {
        message = 'No media items to import.';
      } else if (stats.failed === 0) {
        message = 'All ' + stats.total + ' media items imported successfully.';
      } else if (stats.success === 0) {
        message = 'All ' + stats.total + ' media items failed to import.';
      } else {
        message = stats.success + ' of ' + stats.total + ' media items imported successfully. ' + stats.failed + ' failed.';
      }

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
  };

  /**
   * Exports content import results to CSV
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadService.prototype.exportContentResults = async function() {
    var results = this.state.content.results;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No results available to export.'
      });
      return null;
    }

    try {
      var response = await this.apiClient.exportContentResults(results.results);

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
  };

  /**
   * Exports media import results to CSV
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadService.prototype.exportMediaResults = async function() {
    var results = this.state.media.results;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No results available to export.'
      });
      return null;
    }

    try {
      var response = await this.apiClient.exportMediaResults(results.results);

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
  };

  /**
   * Gets current state (for debugging or serialization)
   * @returns {Object} Current state object
   */
  BulkUploadService.prototype.getState = function() {
    return Object.assign({}, this.state);
  };

  /**
   * Resets service to initial state
   */
  BulkUploadService.prototype.reset = function() {
    this.state = this.createInitialState();
    this.emitStateChange();
  };

  // Expose class
  window.BulkUpload.BulkUploadService = BulkUploadService;

})(window);
