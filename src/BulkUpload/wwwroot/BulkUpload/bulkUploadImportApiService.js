/**
 * Bulk Upload Import API Service (AngularJS)
 * Thin wrapper around framework-agnostic BulkUploadApiClient
 *
 * This service acts as a thin wrapper around BulkUploadApiClient,
 * making it easy to migrate to v17 by simply removing this wrapper.
 *
 * NOTE: This file is written in ES5/IIFE format for Umbraco 13 compatibility.
 * In v17, this will be replaced with native Fetch API calls from Lit components.
 */

angular
  .module("umbraco")
  .factory("bulkUploadImportApiService", function ($http, Upload) {

    // Create HTTP adapter for AngularJS environment
    var httpAdapter = new window.BulkUpload.AngularHttpAdapter($http, Upload);

    // Create API client with AngularJS adapter
    var apiClient = new window.BulkUpload.BulkUploadApiClient(httpAdapter);

    // Expose API client methods through AngularJS service interface
    var bulkUploadImportApi = {
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
