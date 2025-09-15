angular
  .module("umbraco")
  .controller(
    "bulkUploadController",
    function ($scope, bulkUploadImportApiService, notificationsService, angularHelper) {
      $scope.loading = false;
      $scope.file = null;
      $scope.fileControlElement = null;

      $scope.onFileSelected = function (bulkUploadImportFile, evt) {
        if (bulkUploadImportFile) {
          $scope.file = bulkUploadImportFile;

          $scope.fileControlElement = evt.target;
        }
      };

      $scope.onUploadClicked = function () {
        if (!$scope.file || $scope.loading) return;

        $scope.loading = true;

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
              $scope.successNotification = {
                type: 'success',
                headline: 'Success',
                sticky: true,
                message: 'CSV file has been imported successfully.'
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
              message: error.data
            };
            notificationsService.add($scope.errorNotification);
            setTimeout(function () {
              notificationsService.remove($scope.errorNotification)
            }, 10000);
          })
          .finally(function () {
            $scope.loading = false;

            //TO remove the unsaved changes popup which comes after going away from the page
            angularHelper.getCurrentForm($scope).$setPristine();
          });
      };
    }
  );
