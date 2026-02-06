/**
 * Framework-agnostic file utilities
 * TypeScript version for Umbraco 17 (Lit)
 *
 * These pure functions can be used in any JavaScript environment.
 */

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
 * Import type detected from CSV headers
 */
export type ImportType = 'content' | 'media' | 'unknown';

/**
 * Result of CSV file type detection
 */
export interface FileTypeDetectionResult {
  importType: ImportType;
  confidence: 'high' | 'medium' | 'low';
  detectedHeaders: string[];
}

/**
 * Reads the first line (headers) from a CSV file
 * @param file - The CSV file to read
 * @returns Promise resolving to array of header column names
 */
async function readCsvHeaders(file: File): Promise<string[]> {
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

        // Parse CSV headers (handle quoted values)
        const headers = firstLine.split(',').map(h =>
          h.trim().replace(/^["']|["']$/g, '')
        );

        resolve(headers);
      } catch (error) {
        reject(error);
      }
    };

    reader.onerror = () => reject(reader.error);

    // Read only first 1KB to get headers
    const blob = file.slice(0, 1024);
    reader.readAsText(blob);
  });
}

/**
 * Reads CSV headers from ALL CSV files in a ZIP and combines them
 * @param file - The ZIP file to read
 * @returns Promise resolving to combined array of header column names from all CSVs
 */
async function readCsvHeadersFromZip(file: File): Promise<string[]> {
  return new Promise(async (resolve, reject) => {
    try {
      // Dynamically import JSZip
      const JSZip = (await import('jszip')).default;

      const zip = await JSZip.loadAsync(file);

      // Find ALL CSV files
      const csvFiles = Object.keys(zip.files).filter(name =>
        name.toLowerCase().endsWith('.csv') && !zip.files[name].dir
      );

      if (csvFiles.length === 0) {
        resolve([]);
        return;
      }

      // Read ALL CSV files and collect unique headers
      const allHeaders = new Set<string>();

      for (const csvFileName of csvFiles) {
        try {
          const csvContent = await zip.files[csvFileName].async('text');
          const firstLine = csvContent.split('\n')[0];

          if (firstLine) {
            const headers = firstLine.split(',').map(h =>
              h.trim().replace(/^["']|["']$/g, '')
            );
            headers.forEach(h => allHeaders.add(h));
          }
        } catch (err) {
          console.warn(`Failed to read CSV ${csvFileName}:`, err);
          // Continue processing other CSVs
        }
      }

      resolve(Array.from(allHeaders));
    } catch (error) {
      reject(error);
    }
  });
}

/**
 * Detects whether a file is for content or media import based on CSV headers
 * @param file - The CSV or ZIP file to analyze
 * @returns Promise resolving to detection result
 */
export async function detectImportType(file: File): Promise<FileTypeDetectionResult> {
  try {
    const ext = getFileExtension(file.name).toLowerCase();
    let headers: string[] = [];

    if (ext === 'csv') {
      headers = await readCsvHeaders(file);
    } else if (ext === 'zip') {
      // For ZIP files, we need to check ALL CSVs, not just the first one
      headers = await readCsvHeadersFromZip(file);
    } else {
      return {
        importType: 'unknown',
        confidence: 'low',
        detectedHeaders: []
      };
    }

    // Normalize headers (remove resolver syntax and convert to lowercase)
    const normalizedHeaders = headers.map(h =>
      h.split('|')[0].toLowerCase().trim()
    );

    // Content-specific columns
    const contentIndicators = [
      'doctypealias',
      'contenttype',
      'bulkuploadlegacyparentid'
    ];

    // Media-specific columns
    const mediaIndicators = [
      'filename',
      'mediatypealias',
      'mediasource'
    ];

    // Count matches
    const contentMatches = contentIndicators.filter(indicator =>
      normalizedHeaders.includes(indicator)
    ).length;

    const mediaMatches = mediaIndicators.filter(indicator =>
      normalizedHeaders.includes(indicator)
    ).length;

    // IMPORTANT: If we detect BOTH content and media indicators, treat as content import
    // because the content import endpoint already does media preprocessing.
    // A ZIP with both content CSVs and media CSVs should go through the content endpoint.
    if (contentMatches > 0 && mediaMatches > 0) {
      return {
        importType: 'content',
        confidence: 'high',
        detectedHeaders: headers
      };
    }

    // Pure content import
    if (contentMatches > 0 && mediaMatches === 0) {
      return {
        importType: 'content',
        confidence: contentMatches >= 2 ? 'high' : 'medium',
        detectedHeaders: headers
      };
    }

    // Pure media import (standalone media, no content)
    if (mediaMatches > 0 && contentMatches === 0) {
      return {
        importType: 'media',
        confidence: mediaMatches >= 1 ? 'high' : 'medium',
        detectedHeaders: headers
      };
    }

    // If we only have common columns, return unknown
    return {
      importType: 'unknown',
      confidence: 'low',
      detectedHeaders: headers
    };

  } catch (error) {
    console.error('Error detecting import type:', error);
    return {
      importType: 'unknown',
      confidence: 'low',
      detectedHeaders: []
    };
  }
}
