angular
  .module("umbraco")
  .factory("bulkUploadImportApiService", function ($http, Upload) {
    var bulkUploadImportApi = {};

    bulkUploadImportApi.Import = function (fileToUpload) {

      //make use of Upload service from ng-file-upload which is used by Umbraco to upload the file

      const result = Upload.upload({
        url: "/Umbraco/backoffice/Api/BulkUpload/ImportAll",
        file: fileToUpload
      }).success(function (data, status) {
        return { data, status };
      }).error(function (evt, status) {
        return { evt, status };
      });

      return result;
    };


    return bulkUploadImportApi;
  });
