/**
 * Framework-agnostic file utilities
 * TypeScript version for Umbraco 17 (Lit)
 *
 * These pure functions can be used in any JavaScript environment.
 */

import JSZip from 'jszip';

/**
 * Types for upload detection
 */
export type CSVType = 'content' | 'media' | 'unknown';

export interface UploadDetection {
  hasMediaCSV: boolean;
  hasContentCSV: boolean;
  hasMediaFiles: boolean;
  mediaCSVFiles: string[];
  contentCSVFiles: string[];
  mediaFiles: string[];
  summary: string;
}

export interface CSVFileInfo {
  name: string;
  type: CSVType;
  headers: string[];
}

/**
 * Formats a file size in bytes to a human-readable string
 * @param bytes - The file size in bytes
 * @returns Formatted file size (e.g., "1.5 MB")
 */
export function formatFileSize(bytes: number): string {
  if (!bytes || bytes === 0) return '0 Bytes';

  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
}

/**
 * Extracts the file extension from a filename
 * @param filename - The filename to extract extension from
 * @returns The file extension without the dot
 */
export function getFileExtension(filename: string): string {
  if (!filename) return '';
  return filename.slice((filename.lastIndexOf('.') - 1 >>> 0) + 2);
}

/**
 * Checks if a file type is in the accepted types list
 * @param file - The file to check
 * @param acceptedTypes - Array of accepted file types (e.g., ['.csv', '.zip'])
 * @returns True if file type is accepted
 */
export function isValidFileType(file: File, acceptedTypes: string[]): boolean {
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
 * @param file - The file to check
 * @param maxSizeInMB - Maximum file size in megabytes
 * @returns True if file size is acceptable
 */
export function isValidFileSize(file: File, maxSizeInMB: number): boolean {
  if (!file || !maxSizeInMB) return true;
  const maxBytes = maxSizeInMB * 1024 * 1024;
  return file.size <= maxBytes;
}

/**
 * Gets a human-readable file type description
 * @param file - The file to describe
 * @returns Description of file type
 */
export function getFileTypeDescription(file: File): string {
  if (!file) return 'Unknown';

  const ext = getFileExtension(file.name).toLowerCase();

  const typeMap: Record<string, string> = {
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
 * @param file - The CSV file to read
 * @returns Promise resolving to array of header names
 */
export async function readCSVHeaders(file: File | Blob): Promise<string[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      try {
        const text = e.target?.result as string;
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

    reader.onerror = () => reject(reader.error);
    reader.readAsText(file);
  });
}

/**
 * Detects if CSV is content or media based on headers
 * @param headers - Array of CSV header names
 * @returns CSV type ('content', 'media', or 'unknown')
 */
export function detectCSVType(headers: string[]): CSVType {
  if (!headers || headers.length === 0) {
    return 'unknown';
  }

  // Normalize headers (remove resolver syntax, lowercase)
  const normalizedHeaders = headers.map(h =>
    h.split('|')[0].trim().toLowerCase()
  );

  // Content CSV identifiers - requires all three
  const hasContentHeaders =
    normalizedHeaders.includes('parent') &&
    normalizedHeaders.includes('doctypealias') &&
    normalizedHeaders.includes('name');

  // Media CSV identifiers - requires at least one
  const hasMediaHeaders =
    normalizedHeaders.includes('filename') ||
    normalizedHeaders.some(h => h.startsWith('mediasource'));

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
 * @param file - The uploaded file (CSV or ZIP)
 * @returns Promise resolving to upload detection results
 */
export async function analyzeUploadFile(file: File): Promise<UploadDetection> {
  const ext = getFileExtension(file.name).toLowerCase();

  // Initialize detection result
  const detection: UploadDetection = {
    hasMediaCSV: false,
    hasContentCSV: false,
    hasMediaFiles: false,
    mediaCSVFiles: [],
    contentCSVFiles: [],
    mediaFiles: [],
    summary: ''
  };

  try {
    if (ext === 'csv') {
      // Single CSV file
      const headers = await readCSVHeaders(file);
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
    } else if (ext === 'zip') {
      // ZIP file - extract and analyze contents
      const zip = await JSZip.loadAsync(file);
      const csvFiles: CSVFileInfo[] = [];
      const mediaExtensions = ['jpg', 'jpeg', 'png', 'gif', 'svg', 'webp', 'pdf', 'mp4', 'mov', 'avi', 'mp3', 'wav'];

      // Analyze each file in ZIP
      for (const [filename, zipEntry] of Object.entries(zip.files)) {
        if (zipEntry.dir) continue;

        const fileExt = getFileExtension(filename).toLowerCase();

        if (fileExt === 'csv') {
          // Read and detect CSV type
          const csvContent = await zipEntry.async('blob');
          const headers = await readCSVHeaders(csvContent);
          const type = detectCSVType(headers);

          csvFiles.push({ name: filename, type, headers });

          if (type === 'content') {
            detection.hasContentCSV = true;
            detection.contentCSVFiles.push(filename);
          } else if (type === 'media') {
            detection.hasMediaCSV = true;
            detection.mediaCSVFiles.push(filename);
          }
        } else if (mediaExtensions.includes(fileExt)) {
          // Media file
          detection.hasMediaFiles = true;
          detection.mediaFiles.push(filename);
        }
      }

      // Generate summary
      const parts: string[] = [];
      if (detection.hasMediaCSV) parts.push('Media CSV');
      if (detection.hasContentCSV) parts.push('Content CSV');
      if (detection.hasMediaFiles) parts.push(`${detection.mediaFiles.length} Media File${detection.mediaFiles.length !== 1 ? 's' : ''}`);

      detection.summary = parts.length > 0
        ? `ZIP: ${parts.join(' + ')}`
        : 'ZIP Archive';
    } else {
      detection.summary = 'Unsupported File Type';
    }
  } catch (error) {
    console.error('Error analyzing upload file:', error);
    detection.summary = 'Error Analyzing File';
  }

  return detection;
}
