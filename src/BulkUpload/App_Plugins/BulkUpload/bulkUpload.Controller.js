/**
 * Bulk Upload Dashboard Controller
 * Thin wrapper around framework-agnostic BulkUploadService
 *
 * This controller acts as a bridge between AngularJS and the business logic.
 * In Umbraco 17, this file will be replaced by a Lit component that uses
 * the same BulkUploadService directly.
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

      // Tab management
      $scope.setActiveTab = function(tab) {
        service.setActiveTab(tab);
      };

      // Content import handlers
      $scope.onFileSelected = function(file, evt) {
        service.setContentFile(file, evt ? evt.target : null);
      };

      $scope.clearContentFile = function() {
        service.clearContentFile();
      };

      $scope.clearContentResults = function() {
        service.clearContentResults();
      };

      $scope.onUploadClicked = async function() {
        try {
          await service.importContent();
          angularHelper.getCurrentForm($scope).$setPristine();
        } catch (error) {
          // Error already handled by service
          console.error('Import failed:', error);
        }
      };

      $scope.onExportContentResultsClicked = async function() {
        try {
          var response = await service.exportContentResults();
          if (response) {
            var blob = window.BulkUploadUtils.createCsvBlob(response.data);
            window.BulkUploadUtils.downloadBlob(blob, 'content-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };

      // Media import handlers
      $scope.onMediaFileSelected = function(file, evt) {
        service.setMediaFile(file, evt ? evt.target : null);
      };

      $scope.clearMediaFile = function() {
        service.clearMediaFile();
      };

      $scope.clearMediaResults = function() {
        service.clearMediaResults();
      };

      $scope.onMediaUploadClicked = async function() {
        try {
          await service.importMedia();
          angularHelper.getCurrentForm($scope).$setPristine();
        } catch (error) {
          // Error already handled by service
          console.error('Import failed:', error);
        }
      };

      $scope.onExportResultsClicked = async function() {
        try {
          var response = await service.exportMediaResults();
          if (response) {
            var blob = window.BulkUploadUtils.createCsvBlob(response.data);
            window.BulkUploadUtils.downloadBlob(blob, 'media-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };
    }
  );
