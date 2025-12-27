/**
 * Framework-agnostic result processing utilities
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit)
 *
 * These pure functions handle import result filtering, statistics, and exports.
 */

(function(window) {
  'use strict';

  // Create namespace if it doesn't exist
  window.BulkUploadUtils = window.BulkUploadUtils || {};

  /**
   * Filters results to return only failed imports
   * @param {Array} results - Array of import results
   * @returns {Array} Array of failed results
   */
  function getFailedResults(results) {
    if (!Array.isArray(results)) return [];
    return results.filter(result => result.success === false);
  }

/**
   * Filters results to return only successful imports
   * @param {Array} results - Array of import results
   * @returns {Array} Array of successful results
   */
  function getSuccessResults(results) {
    if (!Array.isArray(results)) return [];
    return results.filter(result => result.success === true);
  }

  /**
   * Calculates statistics from import results
   * @param {Array} results - Array of import results
   * @returns {Object} Object with total, success, and failed counts
   */
  function calculateResultStats(results) {
    if (!Array.isArray(results)) {
      return { total: 0, success: 0, failed: 0, successRate: 0 };
    }

    const total = results.length;
    const success = results.filter(r => r.success === true).length;
    const failed = total - success;
    const successRate = total > 0 ? Math.round((success / total) * 100) : 0;

    return { total, success, failed, successRate };
  }

  /**
   * Gets a summary message for import results
   * @param {Array} results - Array of import results
   * @param {string} itemType - Type of items imported (e.g., 'content', 'media')
   * @returns {string} Human-readable summary message
   */
  function getResultSummaryMessage(results, itemType) {
    itemType = itemType || 'items';
    const stats = calculateResultStats(results);

    if (stats.total === 0) {
      return `No ${itemType} to import.`;
    }

    if (stats.failed === 0) {
      return `All ${stats.total} ${itemType} imported successfully.`;
    }

    if (stats.success === 0) {
      return `All ${stats.total} ${itemType} failed to import.`;
    }

    return `${stats.success} of ${stats.total} ${itemType} imported successfully. ${stats.failed} failed.`;
  }

  /**
   * Groups results by success/failure status
   * @param {Array} results - Array of import results
   * @returns {Object} Object with 'successful' and 'failed' arrays
   */
  function groupResultsByStatus(results) {
    if (!Array.isArray(results)) {
      return { successful: [], failed: [] };
    }

    return {
      successful: results.filter(r => r.success === true),
      failed: results.filter(r => r.success === false)
    };
  }

  /**
   * Checks if all imports were successful
   * @param {Array} results - Array of import results
   * @returns {boolean} True if all imports succeeded
   */
  function areAllSuccessful(results) {
    if (!Array.isArray(results) || results.length === 0) return false;
    return results.every(r => r.success === true);
  }

  /**
   * Checks if any imports were successful
   * @param {Array} results - Array of import results
   * @returns {boolean} True if at least one import succeeded
   */
  function hasAnySuccessful(results) {
    if (!Array.isArray(results) || results.length === 0) return false;
    return results.some(r => r.success === true);
  }

  /**
   * Extracts unique error messages from failed results
   * @param {Array} results - Array of import results
   * @returns {Array} Array of unique error messages
   */
  function getUniqueErrorMessages(results) {
    const failed = getFailedResults(results);
    const errorMessages = failed
      .map(r => r.errorMessage || 'Unknown error')
      .filter(msg => msg);

    return [...new Set(errorMessages)];
  }

  /**
   * Downloads a blob as a file
   * @param {Blob} blob - The blob to download
   * @param {string} filename - The filename to save as
   */
  function downloadBlob(blob, filename) {
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = filename;
    link.click();

    // Clean up the URL object
    setTimeout(() => {
      window.URL.revokeObjectURL(link.href);
    }, 100);
  }

  /**
   * Creates a CSV blob from text data
   * @param {string} csvText - CSV formatted text
   * @returns {Blob} Blob with CSV mime type
   */
  function createCsvBlob(csvText) {
    return new Blob([csvText], { type: 'text/csv;charset=utf-8;' });
  }

  // Expose functions
  window.BulkUploadUtils.getFailedResults = getFailedResults;
  window.BulkUploadUtils.getSuccessResults = getSuccessResults;
  window.BulkUploadUtils.calculateResultStats = calculateResultStats;
  window.BulkUploadUtils.getResultSummaryMessage = getResultSummaryMessage;
  window.BulkUploadUtils.groupResultsByStatus = groupResultsByStatus;
  window.BulkUploadUtils.areAllSuccessful = areAllSuccessful;
  window.BulkUploadUtils.hasAnySuccessful = hasAnySuccessful;
  window.BulkUploadUtils.getUniqueErrorMessages = getUniqueErrorMessages;
  window.BulkUploadUtils.downloadBlob = downloadBlob;
  window.BulkUploadUtils.createCsvBlob = createCsvBlob;

})(window);
