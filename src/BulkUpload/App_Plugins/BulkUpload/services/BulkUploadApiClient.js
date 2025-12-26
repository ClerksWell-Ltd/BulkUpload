/**
 * Bulk Upload API Client
 * Framework-agnostic API client that works in both Umbraco 13 and 17
 *
 * Uses the adapter pattern to abstract HTTP operations:
 * - In v13: Uses AngularHttpAdapter (wraps $http and ng-file-upload)
 * - In v17: Uses FetchHttpAdapter (wraps native Fetch API)
 *
 * This allows the same business logic to work in both environments.
 */

import { FetchHttpAdapter } from './httpAdapters.js';

export class BulkUploadApiClient {
  /**
   * Creates a new API client instance
   * @param {Object} httpAdapter - HTTP adapter (AngularHttpAdapter or FetchHttpAdapter)
   */
  constructor(httpAdapter = null) {
    // Default to FetchHttpAdapter if no adapter provided (v17 scenario)
    this.http = httpAdapter || new FetchHttpAdapter();
  }

  /**
   * Imports content from a CSV or ZIP file
   * @param {File} file - The CSV or ZIP file to import
   * @returns {Promise<Object>} Promise resolving to import results
   */
  async importContent(file) {
    if (!file) {
      throw new Error('File is required for content import');
    }

    try {
      const response = await this.http.post(
        '/Umbraco/backoffice/Api/BulkUpload/ImportAll',
        file
      );
      return response;
    } catch (error) {
      throw new Error(`Content import failed: ${error.message}`);
    }
  }

  /**
   * Imports media from a CSV or ZIP file
   * @param {File} file - The CSV or ZIP file to import
   * @returns {Promise<Object>} Promise resolving to import results
   */
  async importMedia(file) {
    if (!file) {
      throw new Error('File is required for media import');
    }

    try {
      const response = await this.http.post(
        '/Umbraco/backoffice/Api/MediaImport/ImportMedia',
        file
      );
      return response;
    } catch (error) {
      throw new Error(`Media import failed: ${error.message}`);
    }
  }

  /**
   * Exports content import results to CSV
   * @param {Array} results - Array of import result objects
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  async exportContentResults(results) {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      const response = await this.http.post(
        '/Umbraco/backoffice/Api/BulkUpload/ExportResults',
        results,
        { responseType: 'text' }
      );
      return response;
    } catch (error) {
      throw new Error(`Export failed: ${error.message}`);
    }
  }

  /**
   * Exports media import results to CSV
   * @param {Array} results - Array of import result objects
   * @returns {Promise<Object>} Promise resolving to CSV data
   */
  async exportMediaResults(results) {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      const response = await this.http.post(
        '/Umbraco/backoffice/Api/MediaImport/ExportResults',
        results,
        { responseType: 'text' }
      );
      return response;
    } catch (error) {
      throw new Error(`Export failed: ${error.message}`);
    }
  }

  /**
   * Validates a file before import
   * @param {File} file - The file to validate
   * @param {Object} options - Validation options
   * @returns {Object} Validation result with {valid, errors}
   */
  validateFile(file, options = {}) {
    const errors = [];

    if (!file) {
      errors.push('No file selected');
      return { valid: false, errors };
    }

    // Check file type
    const acceptedTypes = options.acceptedTypes || ['.csv', '.zip'];
    const fileExt = file.name.split('.').pop().toLowerCase();
    const isValidType = acceptedTypes.some(type => {
      const ext = type.startsWith('.') ? type.slice(1) : type;
      return ext.toLowerCase() === fileExt;
    });

    if (!isValidType) {
      errors.push(`File type .${fileExt} is not accepted. Accepted types: ${acceptedTypes.join(', ')}`);
    }

    // Check file size (default 100MB)
    const maxSize = options.maxSizeInMB || 100;
    const maxBytes = maxSize * 1024 * 1024;
    if (file.size > maxBytes) {
      errors.push(`File size (${Math.round(file.size / 1024 / 1024)}MB) exceeds maximum (${maxSize}MB)`);
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }
}
