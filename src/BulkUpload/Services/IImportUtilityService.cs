using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Services;

public interface IImportUtilityService
{
    public ImportObject CreateImportObject(dynamic? record);

    public void ImportSingleItem(ImportObject importObject, bool publish = false);
}