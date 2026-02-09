/**
 * Bulk Upload Service - Unified Version
 * Framework-agnostic business logic and state management
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit/Vite)
 *
 * This service encapsulates all business logic with unified upload support.
 * Handles both content and media in one upload field with automatic detection.
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
   * Creates initial state structure for unified upload
   * @returns {Object} Initial state object
   */
  BulkUploadService.prototype.createInitialState = function() {
    return {
      loading: false,
      file: null,
      fileElement: null,
      detection: null, // { hasMediaCSV, hasContentCSV, hasMediaFiles, summary, etc. }
      results: {
        content: null,
        media: null,
        mediaPreprocessing: null
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
   * Sets file with detection results
   * @param {File} file - The selected file
   * @param {HTMLElement} fileElement - The file input element
   * @param {Object} detection - Detection results from analyzeUploadFile
   */
  BulkUploadService.prototype.setFile = function(file, fileElement, detection) {
    this.state.file = file;
    this.state.fileElement = fileElement || null;
    this.state.detection = detection;
    this.emitStateChange();
  };

  /**
   * Clears file
   */
  BulkUploadService.prototype.clearFile = function() {
    this.state.file = null;
    this.state.detection = null;
    if (this.state.fileElement) {
      this.state.fileElement.value = '';
    }
    this.emitStateChange();
  };

  /**
   * Clears content results
   */
  BulkUploadService.prototype.clearContentResults = function() {
    this.state.results.content = null;
    this.state.results.mediaPreprocessing = null;
    this.emitStateChange();
  };

  /**
   * Clears media results
   */
  BulkUploadService.prototype.clearMediaResults = function() {
    this.state.results.media = null;
    this.emitStateChange();
  };

  /**
   * Unified import that handles media-first processing
   * Processes media CSV first (if present), then content CSV (if present)
   * @returns {Promise<Object>} Promise resolving to combined import results
   */
  BulkUploadService.prototype.importUnified = async function() {
    var self = this;
    var file = this.state.file;
    var detection = this.state.detection;

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
    this.state.loading = true;
    this.state.results.content = null;
    this.state.results.media = null;
    this.state.results.mediaPreprocessing = null;
    this.emitStateChange();

    try {
      var hasMediaCSV = detection && detection.hasMediaCSV;
      var hasContentCSV = detection && detection.hasContentCSV;

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
        // Try content import as default fallback
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
        message: error.message || 'An error occurred during import.'
      });
      throw error;

    } finally {
      this.state.loading = false;
      this.emitStateChange();
    }
  };

  /**
   * Process media import
   * @private
   * @param {File} file - The file to import
   */
  BulkUploadService.prototype.processMediaImport = async function(file) {
    try {
      var response = await this.apiClient.importMedia(file);

      // Normalize response data to camelCase (handles both Umbraco 13 PascalCase and Umbraco 17 camelCase)
      var normalizedData = {
        totalCount: response.data.totalCount || response.data.TotalCount || 0,
        successCount: response.data.successCount || response.data.SuccessCount || 0,
        failureCount: response.data.failureCount || response.data.FailureCount || 0,
        results: response.data.results || response.data.Results || []
      };

      // Store normalized results
      this.state.results.media = normalizedData;

    } catch (error) {
      // Store error but continue to content import if applicable
      this.notify({
        type: 'error',
        headline: 'Media Import Failed',
        message: error.message || 'An error occurred during media import.'
      });
      throw error;
    }
  };

  /**
   * Process content import
   * @private
   * @param {File} file - The file to import
   */
  BulkUploadService.prototype.processContentImport = async function(file) {
    try {
      var response = await this.apiClient.importContent(file);

      // Normalize response data to camelCase (handles both Umbraco 13 PascalCase and Umbraco 17 camelCase)
      var normalizedData = {
        totalCount: response.data.totalCount || response.data.TotalCount || 0,
        successCount: response.data.successCount || response.data.SuccessCount || 0,
        failureCount: response.data.failureCount || response.data.FailureCount || 0,
        results: response.data.results || response.data.Results || [],
        mediaPreprocessingResults: response.data.mediaPreprocessingResults || response.data.MediaPreprocessingResults || null
      };

      // Store normalized results
      this.state.results.content = normalizedData;

      // Store media preprocessing results if present (from ZIP with media files)
      if (normalizedData.mediaPreprocessingResults) {
        // Normalize individual result properties from PascalCase to camelCase
        this.state.results.mediaPreprocessing = normalizedData.mediaPreprocessingResults.map(function(result) {
          return {
            success: result.success !== undefined ? result.success : result.Success,
            fileName: result.fileName || result.FileName || '',
            value: result.value || result.Value || null,
            errorMessage: result.errorMessage || result.ErrorMessage || null,
            key: result.key || result.Key || '',
            sourceCsvFileName: result.sourceCsvFileName || result.SourceCsvFileName || null
          };
        });
      }
    } catch (error) {
      this.notify({
        type: 'error',
        headline: 'Content Import Failed',
        message: error.message || 'An error occurred during content import.'
      });
      throw error;
    }
  };

  /**
   * Shows combined success message based on what was imported
   * @private
   */
  BulkUploadService.prototype.showCombinedSuccessMessage = function() {
    var messages = [];
    var hasErrors = false;

    // Media import results
    if (this.state.results.media) {
      var mediaStats = {
        total: this.state.results.media.totalCount || 0,
        success: this.state.results.media.successCount || 0,
        failed: this.state.results.media.failureCount || 0
      };

      if (mediaStats.total > 0) {
        if (mediaStats.failed === 0) {
          messages.push('✓ Media: All ' + mediaStats.total + ' items imported successfully.');
        } else {
          messages.push('⚠ Media: ' + mediaStats.success + ' of ' + mediaStats.total + ' items imported. ' + mediaStats.failed + ' failed.');
          hasErrors = true;
        }
      }
    }

    // Content import results
    if (this.state.results.content) {
      var contentStats = {
        total: this.state.results.content.totalCount || 0,
        success: this.state.results.content.successCount || 0,
        failed: this.state.results.content.failureCount || 0
      };

      if (contentStats.total > 0) {
        if (contentStats.failed === 0) {
          messages.push('✓ Content: All ' + contentStats.total + ' items imported successfully.');
        } else {
          messages.push('⚠ Content: ' + contentStats.success + ' of ' + contentStats.total + ' items imported. ' + contentStats.failed + ' failed.');
          hasErrors = true;
        }
      }
    }

    // Media preprocessing results (from content import with media files)
    if (this.state.results.mediaPreprocessing && this.state.results.mediaPreprocessing.length > 0) {
      var mediaPreprocessingSuccess = this.state.results.mediaPreprocessing.filter(function(r) { return r.success; }).length;
      var mediaPreprocessingFailed = this.state.results.mediaPreprocessing.filter(function(r) { return !r.success; }).length;

      if (mediaPreprocessingFailed === 0) {
        messages.push('✓ Media Files: All ' + this.state.results.mediaPreprocessing.length + ' files processed successfully.');
      } else {
        messages.push('⚠ Media Files: ' + mediaPreprocessingSuccess + ' of ' + this.state.results.mediaPreprocessing.length + ' files processed. ' + mediaPreprocessingFailed + ' failed.');
        hasErrors = true;
      }
    }

    var headline = hasErrors ? 'Import Completed with Warnings' : 'Import Successful';
    var message = messages.length > 0 ? messages.join('\n') : 'Import completed.';

    this.notify({
      type: hasErrors ? 'warning' : 'success',
      headline: headline,
      message: message
    });
  };

  /**
   * Exports content import results to CSV
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadService.prototype.exportContentResults = async function() {
    var results = this.state.results.content;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No content results available to export.'
      });
      return null;
    }

    try {
      var response = await this.apiClient.exportContentResults(results.results);

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
  };

  /**
   * Exports media import results to CSV
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadService.prototype.exportMediaResults = async function() {
    var results = this.state.results.media;

    if (!results || !results.results) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No media results available to export.'
      });
      return null;
    }

    try {
      var response = await this.apiClient.exportMediaResults(results.results);

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
  };

  /**
   * Exports media preprocessing results to CSV
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadService.prototype.exportMediaPreprocessingResults = async function() {
    var results = this.state.results.mediaPreprocessing;

    if (!results || results.length === 0) {
      this.notify({
        type: 'warning',
        headline: 'No Results',
        message: 'No media preprocessing results available to export.'
      });
      return null;
    }

    try {
      var response = await this.apiClient.exportMediaPreprocessingResults(results);

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
