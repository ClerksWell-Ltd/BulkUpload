/**
 * Framework-agnostic file utilities
 * Works in both Umbraco 13 (AngularJS) and Umbraco 17 (Lit)
 *
 * These pure functions can be used in any JavaScript environment.
 */

(function(window) {
  'use strict';

  // Create namespace if it doesn't exist
  window.BulkUploadUtils = window.BulkUploadUtils || {};

  /**
   * Formats a file size in bytes to a human-readable string
   * @param {number} bytes - The file size in bytes
   * @returns {string} Formatted file size (e.g., "1.5 MB")
   */
  function formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
  }

  /**
   * Extracts the file extension from a filename
   * @param {string} filename - The filename to extract extension from
   * @returns {string} The file extension without the dot
   */
  function getFileExtension(filename) {
    if (!filename) return '';
    return filename.slice((filename.lastIndexOf('.') - 1 >>> 0) + 2);
  }

  /**
   * Checks if a file type is in the accepted types list
   * @param {File} file - The file to check
   * @param {string[]} acceptedTypes - Array of accepted file types (e.g., ['.csv', '.zip'])
   * @returns {boolean} True if file type is accepted
   */
  function isValidFileType(file, acceptedTypes) {
    if (!file || !acceptedTypes || acceptedTypes.length === 0) return true;

    const ext = getFileExtension(file.name).toLowerCase();

    return acceptedTypes.some(type => {
      // Handle both '.csv' and 'csv' formats
      const normalizedType = type.startsWith('.') ? type.slice(1) : type;
      return normalizedType.toLowerCase() === ext;
    });
  }

  /**
   * Validates if a file size is within acceptable limits
   * @param {File} file - The file to check
   * @param {number} maxSizeInMB - Maximum file size in megabytes
   * @returns {boolean} True if file size is acceptable
   */
  function isValidFileSize(file, maxSizeInMB) {
    if (!file || !maxSizeInMB) return true;
    const maxBytes = maxSizeInMB * 1024 * 1024;
    return file.size <= maxBytes;
  }

  /**
   * Gets a human-readable file type description
   * @param {File} file - The file to describe
   * @returns {string} Description of file type
   */
  function getFileTypeDescription(file) {
    if (!file) return 'Unknown';

    const ext = getFileExtension(file.name).toLowerCase();

    const typeMap = {
      'csv': 'CSV Spreadsheet',
      'zip': 'ZIP Archive',
      'xlsx': 'Excel Spreadsheet',
      'xls': 'Excel Spreadsheet',
      'json': 'JSON Data',
      'xml': 'XML Document'
    };

    return typeMap[ext] || ext.toUpperCase() + ' File';
  }

  // Expose functions
  window.BulkUploadUtils.formatFileSize = formatFileSize;
  window.BulkUploadUtils.getFileExtension = getFileExtension;
  window.BulkUploadUtils.isValidFileType = isValidFileType;
  window.BulkUploadUtils.isValidFileSize = isValidFileSize;
  window.BulkUploadUtils.getFileTypeDescription = getFileTypeDescription;

})(window);
