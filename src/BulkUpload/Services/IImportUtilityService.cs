using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Services;

public interface IImportUtilityService
{
    public ImportObject CreateImportObject(dynamic? record);

    public ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false);
}