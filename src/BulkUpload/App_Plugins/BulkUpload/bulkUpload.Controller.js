/**
 * Bulk Upload Dashboard Controller
 * Thin wrapper around framework-agnostic BulkUploadService
 *
 * This controller acts as a bridge between AngularJS and the business logic.
 * In Umbraco 17, this file will be replaced by a Lit component that uses
 * the same BulkUploadService directly.
 */

// Import framework-agnostic modules (works in both v13 and v17)
import { formatFileSize, getFileTypeDescription } from './utils/fileUtils.js';
import { getFailedResults, downloadBlob, createCsvBlob } from './utils/resultUtils.js';
import { BulkUploadApiClient } from './services/BulkUploadApiClient.js';
import { AngularHttpAdapter } from './services/httpAdapters.js';
import { BulkUploadService } from './services/BulkUploadService.js';

angular
  .module("umbraco")
  .controller(
    "bulkUploadController",
    function ($scope, $http, Upload, notificationsService, angularHelper) {

      // Create HTTP adapter for AngularJS environment
      const httpAdapter = new AngularHttpAdapter($http, Upload);

      // Create API client with AngularJS adapter
      const apiClient = new BulkUploadApiClient(httpAdapter);

      // Create notification handler for AngularJS
      const notificationHandler = (notification) => {
        const notif = {
          type: notification.type,
          headline: notification.headline,
          message: notification.message,
          sticky: true
        };
        notificationsService.add(notif);

        // Auto-remove after 10 seconds
        setTimeout(() => {
          notificationsService.remove(notif);
        }, 10000);
      };

      // Create state change handler to trigger AngularJS digest
      const stateChangeHandler = (state) => {
        // Update scope with new state
        $scope.state = state;
        // Trigger AngularJS digest cycle if not already in one
        if (!$scope.$$phase) {
          $scope.$apply();
        }
      };

      // Create service instance with all dependencies
      const service = new BulkUploadService(
        apiClient,
        notificationHandler,
        stateChangeHandler
      );

      // Bind service state to scope for AngularJS data binding
      $scope.state = service.state;

      // Expose utility functions to the view
      $scope.formatFileSize = formatFileSize;
      $scope.getFailedResults = getFailedResults;
      $scope.getFileTypeDescription = getFileTypeDescription;

      // Tab management
      $scope.setActiveTab = (tab) => {
        service.setActiveTab(tab);
      };

      // Content import handlers
      $scope.onFileSelected = (file, evt) => {
        service.setContentFile(file, evt ? evt.target : null);
      };

      $scope.clearContentFile = () => {
        service.clearContentFile();
      };

      $scope.clearContentResults = () => {
        service.clearContentResults();
      };

      $scope.onUploadClicked = async () => {
        try {
          await service.importContent();
          angularHelper.getCurrentForm($scope).$setPristine();
        } catch (error) {
          // Error already handled by service
          console.error('Import failed:', error);
        }
      };

      $scope.onExportContentResultsClicked = async () => {
        try {
          const response = await service.exportContentResults();
          if (response) {
            const blob = createCsvBlob(response.data);
            downloadBlob(blob, 'content-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };

      // Media import handlers
      $scope.onMediaFileSelected = (file, evt) => {
        service.setMediaFile(file, evt ? evt.target : null);
      };

      $scope.clearMediaFile = () => {
        service.clearMediaFile();
      };

      $scope.clearMediaResults = () => {
        service.clearMediaResults();
      };

      $scope.onMediaUploadClicked = async () => {
        try {
          await service.importMedia();
          angularHelper.getCurrentForm($scope).$setPristine();
        } catch (error) {
          // Error already handled by service
          console.error('Import failed:', error);
        }
      };

      $scope.onExportResultsClicked = async () => {
        try {
          const response = await service.exportMediaResults();
          if (response) {
            const blob = createCsvBlob(response.data);
            downloadBlob(blob, 'media-import-results.csv');
          }
        } catch (error) {
          // Error already handled by service
          console.error('Export failed:', error);
        }
      };
    }
  );
