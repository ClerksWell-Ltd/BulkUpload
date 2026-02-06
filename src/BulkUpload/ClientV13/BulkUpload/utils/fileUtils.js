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

  /**
   * Reads the first line (headers) from a CSV file
   * @param {File} file - The CSV file to read
   * @returns {Promise<string[]>} Promise resolving to array of header column names
   */
  function readCsvHeaders(file) {
    return new Promise(function(resolve, reject) {
      var reader = new FileReader();

      reader.onload = function(e) {
        try {
          var text = e.target.result;
          if (!text) {
            resolve([]);
            return;
          }

          // Get first line (headers)
          var firstLine = text.split('\n')[0];
          if (!firstLine) {
            resolve([]);
            return;
          }

          // Parse CSV headers (handle quoted values)
          var headers = firstLine.split(',').map(function(h) {
            return h.trim().replace(/^["']|["']$/g, '');
          });

          resolve(headers);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = function() {
        reject(reader.error);
      };

      // Read only first 1KB to get headers
      var blob = file.slice(0, 1024);
      reader.readAsText(blob);
    });
  }

  /**
   * Reads CSV headers from a ZIP file (extracts first CSV found)
   * @param {File} file - The ZIP file to read
   * @returns {Promise<string[]>} Promise resolving to array of header column names
   */
  function readCsvHeadersFromZip(file) {
    return new Promise(function(resolve, reject) {
      // Check if JSZip is available
      if (typeof JSZip === 'undefined') {
        reject(new Error('JSZip library not loaded'));
        return;
      }

      JSZip.loadAsync(file)
        .then(function(zip) {
          // Find first CSV file
          var csvFiles = Object.keys(zip.files).filter(function(name) {
            return name.toLowerCase().endsWith('.csv') && !zip.files[name].dir;
          });

          if (csvFiles.length === 0) {
            resolve([]);
            return;
          }

          // Read first CSV file
          return zip.files[csvFiles[0]].async('text');
        })
        .then(function(csvContent) {
          var firstLine = csvContent.split('\n')[0];

          if (!firstLine) {
            resolve([]);
            return;
          }

          // Parse CSV headers
          var headers = firstLine.split(',').map(function(h) {
            return h.trim().replace(/^["']|["']$/g, '');
          });

          resolve(headers);
        })
        .catch(function(error) {
          reject(error);
        });
    });
  }

  /**
   * Detects whether a file is for content or media import based on CSV headers
   * @param {File} file - The CSV or ZIP file to analyze
   * @returns {Promise<Object>} Promise resolving to detection result
   */
  function detectImportType(file) {
    return new Promise(function(resolve) {
      var ext = getFileExtension(file.name).toLowerCase();
      var headersPromise;

      if (ext === 'csv') {
        headersPromise = readCsvHeaders(file);
      } else if (ext === 'zip') {
        headersPromise = readCsvHeadersFromZip(file);
      } else {
        resolve({
          importType: 'unknown',
          confidence: 'low',
          detectedHeaders: []
        });
        return;
      }

      headersPromise
        .then(function(headers) {
          // Normalize headers (remove resolver syntax and convert to lowercase)
          var normalizedHeaders = headers.map(function(h) {
            return h.split('|')[0].toLowerCase().trim();
          });

          // Content-specific columns
          var contentIndicators = [
            'doctypealias',
            'contenttype',
            'bulkuploadlegacyparentid'
          ];

          // Media-specific columns
          var mediaIndicators = [
            'filename',
            'mediatypealias',
            'mediasource'
          ];

          // Count matches
          var contentMatches = contentIndicators.filter(function(indicator) {
            return normalizedHeaders.indexOf(indicator) !== -1;
          }).length;

          var mediaMatches = mediaIndicators.filter(function(indicator) {
            return normalizedHeaders.indexOf(indicator) !== -1;
          }).length;

          // Determine type based on indicators
          if (contentMatches > 0 && mediaMatches === 0) {
            resolve({
              importType: 'content',
              confidence: contentMatches >= 2 ? 'high' : 'medium',
              detectedHeaders: headers
            });
            return;
          }

          if (mediaMatches > 0 && contentMatches === 0) {
            resolve({
              importType: 'media',
              confidence: mediaMatches >= 1 ? 'high' : 'medium',
              detectedHeaders: headers
            });
            return;
          }

          // If both have matches, prioritize based on count
          if (contentMatches > mediaMatches) {
            resolve({
              importType: 'content',
              confidence: 'medium',
              detectedHeaders: headers
            });
            return;
          }

          if (mediaMatches > contentMatches) {
            resolve({
              importType: 'media',
              confidence: 'medium',
              detectedHeaders: headers
            });
            return;
          }

          // If we only have common columns, return unknown
          resolve({
            importType: 'unknown',
            confidence: 'low',
            detectedHeaders: headers
          });
        })
        .catch(function(error) {
          console.error('Error detecting import type:', error);
          resolve({
            importType: 'unknown',
            confidence: 'low',
            detectedHeaders: []
          });
        });
    });
  }

  // Expose functions
  window.BulkUploadUtils.formatFileSize = formatFileSize;
  window.BulkUploadUtils.getFileExtension = getFileExtension;
  window.BulkUploadUtils.isValidFileType = isValidFileType;
  window.BulkUploadUtils.isValidFileSize = isValidFileSize;
  window.BulkUploadUtils.getFileTypeDescription = getFileTypeDescription;
  window.BulkUploadUtils.detectImportType = detectImportType;

})(window);
