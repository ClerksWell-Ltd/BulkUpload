angular
  .module("umbraco")
  .controller(
    "bulkUploadController",
    function ($scope, bulkUploadImportApiService, notificationsService, angularHelper) {
      // Initialize tabs
      $scope.activeTab = 'content';

      // Content import state
      $scope.loading = false;
      $scope.file = null;
      $scope.fileControlElement = null;
      $scope.contentResults = null;

      // Media import state
      $scope.loadingMedia = false;
      $scope.mediaFile = null;
      $scope.mediaFileControlElement = null;
      $scope.mediaResults = null;

      // Tab management
      $scope.setActiveTab = function (tab) {
        $scope.activeTab = tab;
      };

      // Utility function to format file size
      $scope.formatFileSize = function (bytes) {
        if (!bytes) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
      };

      // Filter function to get failed results
      $scope.getFailedResults = function (results) {
        if (!results) return [];
        return results.filter(function (result) {
          return result.success === false;
        });
      };

      // Content import handlers
      $scope.onFileSelected = function (bulkUploadImportFile, evt) {
        if (bulkUploadImportFile) {
          $scope.file = bulkUploadImportFile;
          $scope.fileControlElement = evt.target;
        }
      };

      $scope.clearContentFile = function () {
        $scope.file = null;
        if ($scope.fileControlElement) {
          $scope.fileControlElement.value = "";
        }
      };

      $scope.clearContentResults = function () {
        $scope.contentResults = null;
      };

      $scope.onUploadClicked = function () {
        if (!$scope.file || $scope.loading) return;

        $scope.loading = true;
        $scope.contentResults = null;

        const promise = bulkUploadImportApiService.Import(
          $scope.file
        );

        promise
          .then(function (response) {
            $scope.loading = false;
            if (response.status === 200) {
              if ($scope.fileControlElement) {
                $scope.file = null;
                $scope.fileControlElement.value = "";
              }

              $scope.contentResults = response.data;

              var successMsg = response.data.successCount + ' of ' + response.data.totalCount + ' content items imported successfully.';
              if (response.data.failureCount > 0) {
                successMsg += ' ' + response.data.failureCount + ' failed.';
              }

              $scope.successNotification = {
                type: response.data.failureCount > 0 ? 'warning' : 'success',
                headline: 'Content Import Complete',
                sticky: true,
                message: successMsg
              };
              notificationsService.add($scope.successNotification);
              setTimeout(function () {
                notificationsService.remove($scope.successNotification)
              }, 10000);
            } else {
              $scope.errorNotification = {
                type: 'error',
                headline: 'Error',
                sticky: true,
                message: '' + response.data
              };
              notificationsService.add($scope.errorNotification);
              setTimeout(function () {
                notificationsService.remove($scope.errorNotification)
              }, 10000);
            }
          })
          .catch(function (error) {
            $scope.errorNotification = {
              type: 'error',
              headline: 'Error',
              sticky: true,
              message: error.data || 'An error occurred during content import.'
            };
            notificationsService.add($scope.errorNotification);
            setTimeout(function () {
              notificationsService.remove($scope.errorNotification)
            }, 10000);
          })
          .finally(function () {
            $scope.loading = false;
            angularHelper.getCurrentForm($scope).$setPristine();
          });
      };

      $scope.onExportContentResultsClicked = function () {
        if (!$scope.contentResults || !$scope.contentResults.results) return;

        bulkUploadImportApiService.ExportContentResults($scope.contentResults.results)
          .then(function (response) {
            var blob = new Blob([response.data], { type: 'text/csv' });
            var link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = 'content-import-results.csv';
            link.click();

            notificationsService.add({
              type: 'success',
              headline: 'Success',
              message: 'Results exported successfully.'
            });
          })
          .catch(function (error) {
            notificationsService.add({
              type: 'error',
              headline: 'Error',
              message: 'Failed to export results.'
            });
          });
      };

      // Media import handlers
      $scope.onMediaFileSelected = function (mediaUploadFile, evt) {
        if (mediaUploadFile) {
          $scope.mediaFile = mediaUploadFile;
          $scope.mediaFileControlElement = evt.target;
        }
      };

      $scope.clearMediaFile = function () {
        $scope.mediaFile = null;
        if ($scope.mediaFileControlElement) {
          $scope.mediaFileControlElement.value = "";
        }
      };

      $scope.clearMediaResults = function () {
        $scope.mediaResults = null;
      };

      $scope.onMediaUploadClicked = function () {
        if (!$scope.mediaFile || $scope.loadingMedia) return;

        $scope.loadingMedia = true;
        $scope.mediaResults = null;

        const promise = bulkUploadImportApiService.ImportMedia(
          $scope.mediaFile
        );

        promise
          .then(function (response) {
            $scope.loadingMedia = false;
            if (response.status === 200) {
              if ($scope.mediaFileControlElement) {
                $scope.mediaFile = null;
                $scope.mediaFileControlElement.value = "";
              }

              $scope.mediaResults = response.data;

              var successMsg = response.data.successCount + ' of ' + response.data.totalCount + ' media items imported successfully.';
              if (response.data.failureCount > 0) {
                successMsg += ' ' + response.data.failureCount + ' failed.';
              }

              $scope.successNotification = {
                type: response.data.failureCount > 0 ? 'warning' : 'success',
                headline: 'Media Import Complete',
                sticky: true,
                message: successMsg
              };
              notificationsService.add($scope.successNotification);
              setTimeout(function () {
                notificationsService.remove($scope.successNotification)
              }, 10000);
            } else {
              $scope.errorNotification = {
                type: 'error',
                headline: 'Error',
                sticky: true,
                message: '' + response.data
              };
              notificationsService.add($scope.errorNotification);
              setTimeout(function () {
                notificationsService.remove($scope.errorNotification)
              }, 10000);
            }
          })
          .catch(function (error) {
            $scope.errorNotification = {
              type: 'error',
              headline: 'Error',
              sticky: true,
              message: error.data || 'An error occurred during media import.'
            };
            notificationsService.add($scope.errorNotification);
            setTimeout(function () {
              notificationsService.remove($scope.errorNotification)
            }, 10000);
          })
          .finally(function () {
            $scope.loadingMedia = false;
            angularHelper.getCurrentForm($scope).$setPristine();
          });
      };

      $scope.onExportResultsClicked = function () {
        if (!$scope.mediaResults || !$scope.mediaResults.results) return;

        bulkUploadImportApiService.ExportResults($scope.mediaResults.results)
          .then(function (response) {
            var blob = new Blob([response.data], { type: 'text/csv' });
            var link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = 'media-import-results.csv';
            link.click();

            notificationsService.add({
              type: 'success',
              headline: 'Success',
              message: 'Results exported successfully.'
            });
          })
          .catch(function (error) {
            notificationsService.add({
              type: 'error',
              headline: 'Error',
              message: 'Failed to export results.'
            });
          });
      };
    }
  );
