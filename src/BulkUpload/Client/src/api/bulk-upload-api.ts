/**
 * Bulk Upload API Client
 * TypeScript version for Umbraco 17 (Lit)
 *
 * Uses native Fetch API for HTTP operations
 */

import type { ImportResult } from '../utils/result.utils';
import { apiContext } from './api-context.js';

/**
 * API response wrapper
 */
export interface ApiResponse<T = any> {
  data: T;
  status: number;
  headers: Headers;
}

/**
 * Import result response from API
 */
export interface ImportResultResponse {
  totalCount: number;
  successCount: number;
  failureCount: number;
  results: ImportResult[];
}

/**
 * File validation options
 */
export interface ValidationOptions {
  acceptedTypes?: string[];
  maxSizeInMB?: number;
}

/**
 * File validation result
 */
export interface ValidationResult {
  valid: boolean;
  errors: string[];
}

/**
 * Bulk Upload API Client for Umbraco 17
 */
export class BulkUploadApiClient {
  /**
   * Imports content from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importContent(file: File): Promise<ApiResponse<ImportResultResponse>> {
    if (!file) {
      throw new Error('File is required for content import');
    }

    try {
      const response = await this.post<ImportResultResponse>(
        '/api/v1/content/importall',
        file
      );
      return response;
    } catch (error) {
      throw new Error('Content import failed: ' + (error as Error).message);
    }
  }

  /**
   * Imports media from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importMedia(file: File): Promise<ApiResponse<ImportResultResponse>> {
    if (!file) {
      throw new Error('File is required for media import');
    }

    try {
      const response = await this.post<ImportResultResponse>(
        '/api/v1/media/importmedia',
        file
      );
      return response;
    } catch (error) {
      throw new Error('Media import failed: ' + (error as Error).message);
    }
  }

  /**
   * Exports content import results to CSV or ZIP
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportContentResults(results: ImportResult[]): Promise<Response> {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      const response = await this.postForBlob(
        '/api/v1/content/exportresults',
        results
      );
      return response;
    } catch (error) {
      throw new Error('Export failed: ' + (error as Error).message);
    }
  }

  /**
   * Exports media import results to CSV
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportMediaResults(results: ImportResult[]): Promise<Response> {
    if (!results || !Array.isArray(results)) {
      throw new Error('Results array is required for export');
    }

    try {
      const response = await this.postForBlob(
        '/api/v1/media/exportresults',
        results
      );
      return response;
    } catch (error) {
      throw new Error('Export failed: ' + (error as Error).message);
    }
  }

  /**
   * Validates a file before import
   * @param file - The file to validate
   * @param options - Validation options
   * @returns Validation result with valid flag and errors array
   */
  validateFile(file: File, options: ValidationOptions = {}): ValidationResult {
    const errors: string[] = [];

    if (!file) {
      errors.push('No file selected');
      return { valid: false, errors };
    }

    // Check file type
    const acceptedTypes = options.acceptedTypes || ['.csv', '.zip'];
    const fileExt = file.name.split('.').pop()?.toLowerCase() || '';
    const isValidType = acceptedTypes.some(type => {
      const ext = type.startsWith('.') ? type.slice(1) : type;
      return ext.toLowerCase() === fileExt;
    });

    if (!isValidType) {
      errors.push(`File type .${fileExt} is not accepted. Accepted types: ${acceptedTypes.join(', ')}`);
    }

    // Check file size (default 100MB)
    const maxSize = options.maxSizeInMB || 100;
    const maxBytes = maxSize * 1024 * 1024;
    if (file.size > maxBytes) {
      errors.push(`File size (${Math.round(file.size / 1024 / 1024)}MB) exceeds maximum (${maxSize}MB)`);
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Internal: POST request for JSON data
   */
  private async post<T>(url: string, data: File | object): Promise<ApiResponse<T>> {
    let body: FormData | string;
    const headers: Record<string, string> = {};

    // Get auth headers from context
    const authHeaders = await apiContext.getAuthHeaders();
    Object.assign(headers, authHeaders);

    // Handle different data types
    if (data instanceof File) {
      // Wrap File in FormData
      const formData = new FormData();
      formData.append('file', data);
      body = formData;
      // Don't set Content-Type for FormData (browser will set it with boundary)
    } else {
      // JSON data
      body = JSON.stringify(data);
      headers['Content-Type'] = 'application/json';
    }

    // Get base URL and build full URL
    const baseUrl = apiContext.getBaseUrl();
    const fullUrl = baseUrl ? `${baseUrl}${url}` : url;
    const credentials = apiContext.getCredentials();

    const response = await fetch(fullUrl, {
      method: 'POST',
      body,
      headers,
      credentials
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // Parse JSON response
    const responseData = await response.json();

    return {
      data: responseData,
      status: response.status,
      headers: response.headers
    };
  }

  /**
   * Internal: POST request for blob/file downloads
   */
  private async postForBlob(url: string, data: object): Promise<Response> {
    // Get auth headers from context
    const authHeaders = await apiContext.getAuthHeaders();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...authHeaders
    };

    // Get base URL and build full URL
    const baseUrl = apiContext.getBaseUrl();
    const fullUrl = baseUrl ? `${baseUrl}${url}` : url;
    const credentials = apiContext.getCredentials();

    const response = await fetch(fullUrl, {
      method: 'POST',
      body: JSON.stringify(data),
      headers,
      credentials
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    return response;
  }
}
