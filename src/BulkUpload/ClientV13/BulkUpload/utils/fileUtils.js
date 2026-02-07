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
   * Reads CSV headers from a file or blob
   * @param {File|Blob} file - The CSV file to read
   * @returns {Promise<string[]>} Promise resolving to array of header names
   */
   function readCSVHeaders(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = function(e) {
        try {
          const text = e.target.result;
          if (!text) {
            resolve([]);
            return;
          }

          // Get first line (headers)
          const firstLine = text.split('\n')[0];
          if (!firstLine) {
            resolve([]);
            return;
          }

          // Parse CSV headers (handle quoted fields)
          const headers = firstLine
            .split(',')
            .map(h => h.trim().replace(/^["']|["']$/g, '').toLowerCase());

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
    const normalizedHeaders = headers.map(h =>
      h.split('|')[0].trim().toLowerCase()
    );

    // Content CSV identifiers - requires all three
    const hasContentHeaders =
      normalizedHeaders.indexOf('parent') !== -1 &&
      normalizedHeaders.indexOf('doctypealias') !== -1 &&
      normalizedHeaders.indexOf('name') !== -1;

    // Media CSV identifiers - requires at least one
    const hasMediaHeaders =
      normalizedHeaders.indexOf('filename') !== -1 ||
      normalizedHeaders.some(h => h.indexOf('mediasource') === 0);

    // Content takes priority if both are present
    if (hasContentHeaders) {
      return 'content';
    }

    if (hasMediaHeaders) {
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
    const ext = getFileExtension(file.name).toLowerCase();

    // Initialize detection result
    const detection = {
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
      return readCSVHeaders(file).then(headers => {
        const type = detectCSVType(headers);

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
      }).catch(error => {
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

      return JSZip.loadAsync(file).then(zip => {
        const csvFiles = [];
        const mediaExtensions = ['jpg', 'jpeg', 'png', 'gif', 'svg', 'webp', 'pdf', 'mp4', 'mov', 'avi', 'mp3', 'wav'];
        const promises = [];

        // Analyze each file in ZIP
        Object.keys(zip.files).forEach(filename => {
          const zipEntry = zip.files[filename];
          if (zipEntry.dir) return;

          const fileExt = getFileExtension(filename).toLowerCase();

          if (fileExt === 'csv') {
            // Read and detect CSV type
            const promise = zipEntry.async('blob').then(csvContent => {
              return readCSVHeaders(csvContent).then(headers => {
                const type = detectCSVType(headers);
                csvFiles.push({ name: filename, type: type, headers: headers });

                if (type === 'content') {
                  detection.hasContentCSV = true;
                  detection.contentCSVFiles.push(filename);
                } else if (type === 'media') {
                  detection.hasMediaCSV = true;
                  detection.mediaCSVFiles.push(filename);
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

        return Promise.all(promises).then(() => {
          // Generate summary
          const parts = [];
          if (detection.hasMediaCSV) parts.push('Media CSV');
          if (detection.hasContentCSV) parts.push('Content CSV');
          if (detection.hasMediaFiles) {
            parts.push(detection.mediaFiles.length + ' Media File' + (detection.mediaFiles.length !== 1 ? 's' : ''));
          }

          detection.summary = parts.length > 0
            ? 'ZIP: ' + parts.join(' + ')
            : 'ZIP Archive';

          return detection;
        });
      }).catch(error => {
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
