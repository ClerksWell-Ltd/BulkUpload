/**
 * Bulk Upload API Client
 * Framework-agnostic API client that works in both Umbraco 13 and 17
 *
 * Uses the adapter pattern to abstract HTTP operations:
 * - In v13: Uses AngularHttpAdapter (wraps $http and ng-file-upload)
 * - In v17: Uses FetchHttpAdapter (wraps native Fetch API with Vite/Lit)
 *
 * NOTE: This file is written in ES5/IIFE format for Umbraco 13 compatibility.
 * In v17, this will be replaced with ES6 module syntax and bundled with Vite.
 */

(function(window) {
  'use strict';

  // Create namespace if it doesn't exist
  window.BulkUpload = window.BulkUpload || {};

  /**
   * Bulk Upload API Client
   * @param {Object} httpAdapter - HTTP adapter (AngularHttpAdapter or FetchHttpAdapter)
   */
  function BulkUploadApiClient(httpAdapter) {
    // Default to FetchHttpAdapter if no adapter provided (v17 scenario)
    this.http = httpAdapter || new window.BulkUpload.FetchHttpAdapter();
  }

  /**
   * Imports content from a CSV or ZIP file
   * @param {File} file - The CSV or ZIP file to import
   * @returns {Promise<Object>} Promise resolving to import results
   */
  BulkUploadApiClient.prototype.importContent = async function(file) {
    if (!file) {
      throw new Error('File is required for content import');
    }

    try {
      var response = await this.http.post(
        '/Umbraco/backoffice/Api/BulkUpload/ImportAll',
        file
      );
      return response;
    } catch (error) {
      throw new Error('Content import failed: ' + error.message);
    }
  };

  /**
   * Imports media from a CSV or ZIP file
   * @param {File} file - The CSV or ZIP file to import
   * @returns {Promise<Object>} Promise resolving to import results
   */
  BulkUploadApiClient.prototype.importMedia = async function(file) {
    if (!file) {
      throw new Error('File is required for media import');
    }

    try {
      var response = await this.http.post(
        '/Umbraco/backoffice/Api/MediaImport/ImportMedia',
        file
      );
      return response;
    } catch (error) {
      throw new Error('Media import failed: ' + error.message);
    }
  };

  /**
   * Exports content import results to CSV
   * @param {Array} results - Array of import result objects
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadApiClient.prototype.exportContentResults = async function(results) {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      var response = await this.http.post(
        '/Umbraco/backoffice/Api/BulkUpload/ExportResults',
        results,
        { responseType: 'text' }
      );
      return response;
    } catch (error) {
      throw new Error('Export failed: ' + error.message);
    }
  };

  /**
   * Exports media import results to CSV
   * @param {Array} results - Array of import result objects
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  BulkUploadApiClient.prototype.exportMediaResults = async function(results) {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      var response = await this.http.post(
        '/Umbraco/backoffice/Api/MediaImport/ExportResults',
        results,
        { responseType: 'text' }
      );
      return response;
    } catch (error) {
      throw new Error('Export failed: ' + error.message);
    }
  };

  /**
   * Validates a file before import
   * @param {File} file - The file to validate
   * @param {Object} options - Validation options
   * @returns {Object} Validation result with {valid, errors}
   */
  BulkUploadApiClient.prototype.validateFile = function(file, options) {
    options = options || {};
    var errors = [];

    if (!file) {
      errors.push('No file selected');
      return { valid: false, errors: errors };
    }

    // Check file type
    var acceptedTypes = options.acceptedTypes || ['.csv', '.zip'];
    var fileExt = file.name.split('.').pop().toLowerCase();
    var isValidType = acceptedTypes.some(function(type) {
      var ext = type.startsWith('.') ? type.slice(1) : type;
      return ext.toLowerCase() === fileExt;
    });

    if (!isValidType) {
      errors.push('File type .' + fileExt + ' is not accepted. Accepted types: ' + acceptedTypes.join(', '));
    }

    // Check file size (default 100MB)
    var maxSize = options.maxSizeInMB || 100;
    var maxBytes = maxSize * 1024 * 1024;
    if (file.size > maxBytes) {
      errors.push('File size (' + Math.round(file.size / 1024 / 1024) + 'MB) exceeds maximum (' + maxSize + 'MB)');
    }

    return {
      valid: errors.length === 0,
      errors: errors
    };
  };

  // Expose class
  window.BulkUpload.BulkUploadApiClient = BulkUploadApiClient;

})(window);
