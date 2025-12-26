/**
 * HTTP Adapters for Bulk Upload API Client
 * Provides abstraction layer for HTTP operations that works in both v13 and v17
 *
 * The adapter pattern allows the same API client to work with:
 * - Umbraco 13: AngularJS $http and ng-file-upload
 * - Umbraco 17: Native Fetch API
 */

/**
 * AngularJS HTTP Adapter
 * Wraps AngularJS $http and ng-file-upload for use in v13
 */
export class AngularHttpAdapter {
  constructor($http, Upload) {
    this.$http = $http;
    this.Upload = Upload;
  }

  /**
   * Performs a POST request
   * @param {string} url - The URL to POST to
   * @param {*} data - The data to send (FormData, File, or JSON)
   * @param {Object} options - Additional options
   * @returns {Promise} Promise resolving to response with {data, status}
   */
  async post(url, data, options = {}) {
    // Check if this is a file upload
    if (data instanceof File || (data && data.name && data.size)) {
      // Use ng-file-upload for file uploads
      return this.Upload.upload({
        url: url,
        file: data,
        ...options
      });
    } else {
      // Use $http for regular POST requests
      return this.$http.post(url, data, options);
    }
  }

  /**
   * Performs a GET request
   * @param {string} url - The URL to GET from
   * @param {Object} options - Additional options
   * @returns {Promise} Promise resolving to response with {data, status}
   */
  async get(url, options = {}) {
    return this.$http.get(url, options);
  }
}

/**
 * Fetch API HTTP Adapter
 * Uses native Fetch API for use in v17 Lit components
 */
export class FetchHttpAdapter {
  /**
   * Performs a POST request using Fetch API
   * @param {string} url - The URL to POST to
   * @param {*} data - The data to send (FormData, File, or JSON)
   * @param {Object} options - Additional options
   * @returns {Promise} Promise resolving to response with {data, status}
   */
  async post(url, data, options = {}) {
    let body = data;
    let headers = { ...options.headers };

    // Handle different data types
    if (data instanceof File) {
      // Wrap File in FormData
      const formData = new FormData();
      formData.append('file', data);
      body = formData;
      // Don't set Content-Type for FormData (browser will set it with boundary)
    } else if (data instanceof FormData) {
      // Use FormData as-is
      body = data;
    } else {
      // JSON data
      body = JSON.stringify(data);
      headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(url, {
      method: 'POST',
      body: body,
      headers: headers,
      ...options
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // Parse response based on expected type
    let responseData;
    if (options.responseType === 'text') {
      responseData = await response.text();
    } else {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        responseData = await response.json();
      } else {
        responseData = await response.text();
      }
    }

    return {
      data: responseData,
      status: response.status,
      headers: response.headers
    };
  }

  /**
   * Performs a GET request using Fetch API
   * @param {string} url - The URL to GET from
   * @param {Object} options - Additional options
   * @returns {Promise} Promise resolving to response with {data, status}
   */
  async get(url, options = {}) {
    const response = await fetch(url, {
      method: 'GET',
      headers: options.headers || {},
      ...options
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // Parse response based on expected type
    let responseData;
    if (options.responseType === 'text') {
      responseData = await response.text();
    } else {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        responseData = await response.json();
      } else {
        responseData = await response.text();
      }
    }

    return {
      data: responseData,
      status: response.status,
      headers: response.headers
    };
  }
}
