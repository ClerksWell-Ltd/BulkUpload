/**
 * Bulk Upload Dashboard Controller
 * Thin wrapper around framework-agnostic BulkUploadService
 *
 * This controller acts as a bridge between AngularJS and the business logic.
 * Unified upload version - handles both content and media in one upload field.
 *
 * NOTE: This file is written in ES5/IIFE format for Umbraco 13 compatibility.
 * In v17, this will be replaced with a Lit component using Vite.
 */

angular
  .module("umbraco")
  .controller(
    "bulkUploadController",
    function ($scope, $http, Upload, notificationsService, angularHelper) {

      // Create HTTP adapter for AngularJS environment
      var httpAdapter = new window.BulkUpload.AngularHttpAdapter($http, Upload);

      // Create API client with AngularJS adapter
      var apiClient = new window.BulkUpload.BulkUploadApiClient(httpAdapter);

      // Create notification handler for AngularJS
      var notificationHandler = function(notification) {
        var notif = {
          type: notification.type,
          headline: notification.headline,
          message: notification.message,
          sticky: true
        };
        notificationsService.add(notif);

        // Auto-remove after 10 seconds
        setTimeout(function() {
          notificationsService.remove(notif);
        }, 10000);
      };

      // Create state change handler to trigger AngularJS digest
      var stateChangeHandler = function(state) {
        // Update scope with new state
        $scope.state = state;
        // Trigger AngularJS digest cycle if not already in one
        if (!$scope.$$phase) {
          $scope.$apply();
        }
      };

      // Create service instance with all dependencies
      var service = new window.BulkUpload.BulkUploadService(
        apiClient,
        notificationHandler,
        stateChangeHandler
      );

      // Bind service state to scope for AngularJS data binding
      $scope.state = service.state;

      // Expose utility functions to the view
      $scope.formatFileSize = window.BulkUploadUtils.formatFileSize;
      $scope.getFileTypeDescription = window.BulkUploadUtils.getFileTypeDescription;

      // File input trigger for drop zone
      $scope.triggerFileInput = function() {
        var fileInput = document.getElementById('unified-file-input');
        if (fileInput) {
          fileInput.click();
        }
      };

      // Unified file handler with detection
      $scope.onFileSelected = async function(file, evt) {
        if (!file) return;

        try {
          // Analyze file to detect contents
          var detection = await window.BulkUploadUtils.analyzeUploadFile(file);
          service.setFile(file, evt ? evt.target : null, detection);
        } catch (error) {
          console.error('Error analyzing file:', error);
          service.setFile(file, evt ? evt.target : null, null);
        }
      };

      $scope.clearFile = function() {
        service.clearFile();
      };

      $scope.clearContentResults = function() {
        service.clearContentResults();
      };

      $scope.clearMediaResults = function() {
        service.clearMediaResults();
      };

      // Unified upload handler (processes media first, then content)
      $scope.onUploadClicked = async function() {
        try {
          await service.importUnified();
          angularHelper.getCurrentForm($scope).$setPristine();
          // Scroll to top to show results
          setTimeout(function() {
            window.scrollTo({ top: 0, behavior: 'smooth' });
          }, 100);
        } catch (error) {
          // Error already handled by service
          console.error('Import failed:', error);
        }
      };

      // Export handlers
      $scope.onExportContentResultsClicked = async function() {
        try {
          var response = await service.exportContentResults();
          if (response) {
            // Automatically detect and download ZIP or CSV based on Content-Type
            window.BulkUploadUtils.downloadResponseFile(response, 'content-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };

      $scope.onExportMediaResultsClicked = async function() {
        try {
          var response = await service.exportMediaResults();
          if (response) {
            // Automatically detect and download ZIP or CSV based on Content-Type
            window.BulkUploadUtils.downloadResponseFile(response, 'media-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };

      $scope.onExportMediaPreprocessingResultsClicked = async function() {
        try {
          var response = await service.exportMediaPreprocessingResults();
          if (response) {
            // Automatically detect and download ZIP or CSV based on Content-Type
            window.BulkUploadUtils.downloadResponseFile(response, 'media-preprocessing-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };

      // Helper functions for media preprocessing results
      $scope.countSuccessfulMedia = function() {
        if (!$scope.state.results.mediaPreprocessing) return 0;
        return $scope.state.results.mediaPreprocessing.filter(function(r) { return r.success; }).length;
      };

      $scope.countFailedMedia = function() {
        if (!$scope.state.results.mediaPreprocessing) return 0;
        return $scope.state.results.mediaPreprocessing.filter(function(r) { return !r.success; }).length;
      };
    }
  );
