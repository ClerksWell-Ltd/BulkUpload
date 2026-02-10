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

    var k = 1024;
    var sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    var i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
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

    var ext = getFileExtension(file.name).toLowerCase();

    return acceptedTypes.some(function(type) {
      // Handle both '.csv' and 'csv' formats
      var normalizedType = type.charAt(0) === '.' ? type.slice(1) : type;
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
    var maxBytes = maxSizeInMB * 1024 * 1024;
    return file.size <= maxBytes;
  }

  /**
   * Gets a human-readable file type description
   * @param {File} file - The file to describe
   * @returns {string} Description of file type
   */
  function getFileTypeDescription(file) {
    if (!file) return 'Unknown';

    var ext = getFileExtension(file.name).toLowerCase();

    var typeMap = {
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
   * Reads CSV headers from a file or blob
   * @param {File|Blob} file - The CSV file to read
   * @returns {Promise<string[]>} Promise resolving to array of header names
   */
   function readCSVHeaders(file) {
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

          // Parse CSV headers (handle quoted fields)
          var headers = firstLine
            .split(',')
            .map(function(h) { return h.trim().replace(/^["']|["']$/g, '').toLowerCase(); });

          resolve(headers);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = function() {
        reject(reader.error);
      };

      reader.readAsText(file);
    });
  }

  /**
   * Detects if CSV is content or media based on headers
   * @param {string[]} headers - Array of CSV header names
   * @returns {string} CSV type ('content', 'media', or 'unknown')
   */
  function detectCSVType(headers) {
    if (!headers || headers.length === 0) {
      return 'unknown';
    }

    // Normalize headers (remove resolver syntax, lowercase)
    var normalizedHeaders = headers.map(function(h) {
      return h.split('|')[0].trim().toLowerCase();
    });

    // Content CSV identifiers
    // CREATE MODE: requires name and doctypealias (parent is optional for root-level content or legacy migration)
    var hasContentCreateHeaders =
      normalizedHeaders.indexOf('doctypealias') !== -1 &&
      normalizedHeaders.indexOf('name') !== -1;

    // UPDATE MODE: requires bulkUploadContentGuid and bulkUploadShouldUpdate
    var hasContentUpdateHeaders =
      normalizedHeaders.indexOf('bulkuploadcontentguid') !== -1 &&
      normalizedHeaders.indexOf('bulkuploadshouldupdate') !== -1;

    // Media CSV identifiers
    // CREATE MODE: requires at least one
    var hasMediaCreateHeaders =
      normalizedHeaders.indexOf('filename') !== -1 ||
      normalizedHeaders.some(function(h) { return h.indexOf('mediasource') === 0; });

    // UPDATE MODE: requires bulkUploadMediaGuid and bulkUploadShouldUpdate
    var hasMediaUpdateHeaders =
      normalizedHeaders.indexOf('bulkuploadmediaguid') !== -1 &&
      normalizedHeaders.indexOf('bulkuploadshouldupdate') !== -1;

    // Content takes priority if both are present
    if (hasContentCreateHeaders || hasContentUpdateHeaders) {
      return 'content';
    }

    if (hasMediaCreateHeaders || hasMediaUpdateHeaders) {
      return 'media';
    }

    return 'unknown';
  }

  /**
   * Analyzes an uploaded file to detect its contents
   * @param {File} file - The uploaded file (CSV or ZIP)
   * @returns {Promise<Object>} Promise resolving to upload detection results
   */
  function analyzeUploadFile(file) {
    var ext = getFileExtension(file.name).toLowerCase();

    // Initialize detection result
    var detection = {
      hasMediaCSV: false,
      hasContentCSV: false,
      hasMediaFiles: false,
      mediaCSVFiles: [],
      contentCSVFiles: [],
      mediaFiles: [],
      summary: ''
    };

    if (ext === 'csv') {
      // Single CSV file
      return readCSVHeaders(file).then(function(headers) {
        var type = detectCSVType(headers);

        if (type === 'content') {
          detection.hasContentCSV = true;
          detection.contentCSVFiles.push(file.name);
          detection.summary = 'Content CSV';
        } else if (type === 'media') {
          detection.hasMediaCSV = true;
          detection.mediaCSVFiles.push(file.name);
          detection.summary = 'Media CSV';
        } else {
          detection.summary = 'Unknown CSV';
        }

        return detection;
      }).catch(function(error) {
        console.error('Error analyzing CSV file:', error);
        detection.summary = 'Error Analyzing File';
        return detection;
      });
    } else if (ext === 'zip') {
      // ZIP file - extract and analyze contents
      if (typeof JSZip === 'undefined') {
        console.error('JSZip library not loaded');
        detection.summary = 'ZIP Archive';
        return Promise.resolve(detection);
      }

      return JSZip.loadAsync(file).then(function(zip) {
        var csvFiles = [];
        var unknownCSVFiles = [];
        var mediaExtensions = ['jpg', 'jpeg', 'png', 'gif', 'svg', 'webp', 'pdf', 'mp4', 'mov', 'avi', 'mp3', 'wav'];
        var promises = [];

        // Analyze each file in ZIP
        Object.keys(zip.files).forEach(function(filename) {
          var zipEntry = zip.files[filename];
          if (zipEntry.dir) return;

          var fileExt = getFileExtension(filename).toLowerCase();

          if (fileExt === 'csv') {
            // Read and detect CSV type
            var promise = zipEntry.async('blob').then(function(csvContent) {
              return readCSVHeaders(csvContent).then(function(headers) {
                var type = detectCSVType(headers);
                csvFiles.push({ name: filename, type: type, headers: headers });

                if (type === 'content') {
                  detection.hasContentCSV = true;
                  detection.contentCSVFiles.push(filename);
                } else if (type === 'media') {
                  detection.hasMediaCSV = true;
                  detection.mediaCSVFiles.push(filename);
                } else {
                  unknownCSVFiles.push(filename);
                }
              });
            });
            promises.push(promise);
          } else if (mediaExtensions.indexOf(fileExt) !== -1) {
            // Media file
            detection.hasMediaFiles = true;
            detection.mediaFiles.push(filename);
          }
        });

        return Promise.all(promises).then(function() {
          // Generate summary
          var parts = [];
          var totalCSVCount = detection.mediaCSVFiles.length + detection.contentCSVFiles.length + unknownCSVFiles.length;

          if (totalCSVCount > 0) {
            // Show total CSV count first
            parts.push(totalCSVCount + ' CSV' + (totalCSVCount !== 1 ? ' Files' : ' File'));
          }

          if (detection.hasMediaFiles) {
            parts.push(detection.mediaFiles.length + ' Media File' + (detection.mediaFiles.length !== 1 ? 's' : ''));
          }

          detection.summary = parts.length > 0
            ? 'ZIP: ' + parts.join(' + ')
            : 'ZIP Archive';

          return detection;
        });
      }).catch(function(error) {
        console.error('Error analyzing ZIP file:', error);
        detection.summary = 'Error Analyzing File';
        return detection;
      });
    } else {
      detection.summary = 'Unsupported File Type';
      return Promise.resolve(detection);
    }
  }

  // Expose functions
  window.BulkUploadUtils.formatFileSize = formatFileSize;
  window.BulkUploadUtils.getFileExtension = getFileExtension;
  window.BulkUploadUtils.isValidFileType = isValidFileType;
  window.BulkUploadUtils.isValidFileSize = isValidFileSize;
  window.BulkUploadUtils.getFileTypeDescription = getFileTypeDescription;
  window.BulkUploadUtils.readCSVHeaders = readCSVHeaders;
  window.BulkUploadUtils.detectCSVType = detectCSVType;
  window.BulkUploadUtils.analyzeUploadFile = analyzeUploadFile;

})(window);
