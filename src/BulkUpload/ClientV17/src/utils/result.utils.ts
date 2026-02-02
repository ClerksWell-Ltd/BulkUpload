/**
 * Framework-agnostic result processing utilities
 * TypeScript version for Umbraco 17 (Lit)
 *
 * These pure functions handle import result filtering, statistics, and exports.
 */

/**
 * Represents a single import result
 */
export interface ImportResult {
  success: boolean;
  errorMessage?: string;
  [key: string]: any;
}

/**
 * Statistics calculated from import results
 */
export interface ResultStats {
  total: number;
  success: number;
  failed: number;
  successRate: number;
}

/**
 * Grouped results by status
 */
export interface GroupedResults {
  successful: ImportResult[];
  failed: ImportResult[];
}

/**
 * Filters results to return only successful imports
 * @param results - Array of import results
 * @returns Array of successful results
 */
export function getSuccessResults(results: ImportResult[]): ImportResult[] {
  if (!Array.isArray(results)) return [];
  return results.filter(result => result.success === true);
}

/**
 * Calculates statistics from import results
 * @param results - Array of import results
 * @returns Object with total, success, and failed counts
 */
export function calculateResultStats(results: ImportResult[]): ResultStats {
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
 * @param results - Array of import results
 * @param itemType - Type of items imported (e.g., 'content', 'media')
 * @returns Human-readable summary message
 */
export function getResultSummaryMessage(results: ImportResult[], itemType: string = 'items'): string {
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
 * @param results - Array of import results
 * @returns Object with 'successful' and 'failed' arrays
 */
export function groupResultsByStatus(results: ImportResult[]): GroupedResults {
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
 * @param results - Array of import results
 * @returns True if all imports succeeded
 */
export function areAllSuccessful(results: ImportResult[]): boolean {
  if (!Array.isArray(results) || results.length === 0) return false;
  return results.every(r => r.success === true);
}

/**
 * Checks if any imports were successful
 * @param results - Array of import results
 * @returns True if at least one import succeeded
 */
export function hasAnySuccessful(results: ImportResult[]): boolean {
  if (!Array.isArray(results) || results.length === 0) return false;
  return results.some(r => r.success === true);
}

/**
 * Extracts unique error messages from failed results
 * @param results - Array of import results
 * @returns Array of unique error messages
 */
export function getUniqueErrorMessages(results: ImportResult[]): string[] {
  if (!Array.isArray(results)) return [];
  const failed = results.filter(result => result.success === false);
  const errorMessages = failed
    .map(r => r.errorMessage || 'Unknown error')
    .filter(msg => msg);

  return [...new Set(errorMessages)];
}

/**
 * Downloads a blob as a file
 * @param blob - The blob to download
 * @param filename - The filename to save as
 */
export function downloadBlob(blob: Blob, filename: string): void {
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
 * @param csvText - CSV formatted text
 * @returns Blob with CSV mime type
 */
export function createCsvBlob(csvText: string): Blob {
  return new Blob([csvText], { type: 'text/csv;charset=utf-8;' });
}

/**
 * Downloads a file from a fetch Response, automatically detecting ZIP vs CSV
 * @param response - Fetch Response object
 * @param defaultFilename - Default filename if Content-Disposition not provided
 */
export async function downloadResponseFile(response: Response, defaultFilename: string): Promise<void> {
  if (!response || !response.ok) {
    console.error('Invalid response for file download');
    return;
  }

  const blob = await response.blob();
  let filename = defaultFilename;
  const contentType = response.headers.get('content-type');

  // Try to extract filename from Content-Disposition header
  const contentDisposition = response.headers.get('content-disposition');
  if (contentDisposition) {
    const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
    if (matches != null && matches[1]) {
      filename = matches[1].replace(/['"]/g, '');
    }
  }

  // If no filename from header, determine from Content-Type
  if (!filename || filename === defaultFilename) {
    if (contentType && contentType.indexOf('application/zip') !== -1) {
      filename = defaultFilename.replace(/\.csv$/, '.zip');
    }
  }

  downloadBlob(blob, filename);
}
