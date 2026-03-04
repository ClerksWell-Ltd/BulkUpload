/**
 * HTTP Adapters for Bulk Upload API Client
 * Provides abstraction layer for HTTP operations that works in both v13 and v17
 *
 * The adapter pattern allows the same API client to work with:
 * - Umbraco 13: AngularJS $http and ng-file-upload (loaded as regular script)
 * - Umbraco 17: Native Fetch API (will be bundled as ES6 module with Vite)
 *
 * NOTE: This file is written in ES5/IIFE format for Umbraco 13 compatibility.
 * In v17, this will be replaced with ES6 module syntax.
 */

(function(window) {
  'use strict';

  // Create namespace if it doesn't exist
  window.BulkUpload = window.BulkUpload || {};

  /**
   * AngularJS HTTP Adapter
   * Wraps AngularJS $http and ng-file-upload for use in v13
   */
  function AngularHttpAdapter($http, Upload) {
    this.$http = $http;
    this.Upload = Upload;
  }

  AngularHttpAdapter.prototype.post = async function(url, data, options) {
    options = options || {};
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
  };

  AngularHttpAdapter.prototype.get = async function(url, options) {
    options = options || {};
    return this.$http.get(url, options);
  };

  /**
   * Fetch API HTTP Adapter
   * Uses native Fetch API for use in v17 Lit components
   */
  function FetchHttpAdapter() {}

  FetchHttpAdapter.prototype.post = async function(url, data, options) {
    options = options || {};
    var body = data;
    var headers = options.headers ? Object.assign({}, options.headers) : {};

    // Handle different data types
    if (data instanceof File) {
      // Wrap File in FormData
      var formData = new FormData();
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

    var fetchOptions = {
      method: 'POST',
      body: body,
      headers: headers
    };
    Object.assign(fetchOptions, options);

    var response = await fetch(url, fetchOptions);

    if (!response.ok) {
      throw new Error('HTTP ' + response.status + ': ' + response.statusText);
    }

    // Parse response based on expected type
    var responseData;
    if (options.responseType === 'text') {
      responseData = await response.text();
    } else {
      var contentType = response.headers.get('content-type');
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
  };

  FetchHttpAdapter.prototype.get = async function(url, options) {
    options = options || {};
    var fetchOptions = {
      method: 'GET',
      headers: options.headers || {}
    };
    Object.assign(fetchOptions, options);

    var response = await fetch(url, fetchOptions);

    if (!response.ok) {
      throw new Error('HTTP ' + response.status + ': ' + response.statusText);
    }

    // Parse response based on expected type
    var responseData;
    if (options.responseType === 'text') {
      responseData = await response.text();
    } else {
      var contentType = response.headers.get('content-type');
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
  };

  // Expose classes
  window.BulkUpload.AngularHttpAdapter = AngularHttpAdapter;
  window.BulkUpload.FetchHttpAdapter = FetchHttpAdapter;

})(window);
