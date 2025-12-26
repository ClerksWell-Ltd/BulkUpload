/**
 * Bulk Upload Import API Service (AngularJS)
 * Refactored to use framework-agnostic BulkUploadApiClient
 *
 * This service now acts as a thin wrapper around BulkUploadApiClient,
 * making it easy to migrate to v17 by simply removing this wrapper.
 */

import { BulkUploadApiClient } from './services/BulkUploadApiClient.js';
import { AngularHttpAdapter } from './services/httpAdapters.js';

angular
  .module("umbraco")
  .factory("bulkUploadImportApiService", function ($http, Upload) {

    // Create HTTP adapter for AngularJS environment
    const httpAdapter = new AngularHttpAdapter($http, Upload);

    // Create API client with AngularJS adapter
    const apiClient = new BulkUploadApiClient(httpAdapter);

    // Expose API client methods through AngularJS service interface
    const bulkUploadImportApi = {
      /**
       * Imports content from a CSV or ZIP file
       * @param {File} fileToUpload - The file to import
       * @returns {Promise} Promise resolving to import results
       */
      Import: function (fileToUpload) {
        return apiClient.importContent(fileToUpload);
      },

      /**
       * Imports media from a CSV or ZIP file
       * @param {File} fileToUpload - The file to import
       * @returns {Promise} Promise resolving to import results
       */
      ImportMedia: function (fileToUpload) {
        return apiClient.importMedia(fileToUpload);
      },

      /**
       * Exports content import results to CSV
       * @param {Array} results - Array of import result objects
       * @returns {Promise} Promise resolving to CSV data
       */
      ExportContentResults: function (results) {
        return apiClient.exportContentResults(results);
      },

      /**
       * Exports media import results to CSV
       * @param {Array} results - Array of import result objects
       * @returns {Promise} Promise resolving to CSV data
       */
      ExportResults: function (results) {
        return apiClient.exportMediaResults(results);
      },

      /**
       * Validates a file before import
       * @param {File} file - The file to validate
       * @param {Object} options - Validation options
       * @returns {Object} Validation result
       */
      ValidateFile: function (file, options) {
        return apiClient.validateFile(file, options);
      }
    };

    return bulkUploadImportApi;
  });
