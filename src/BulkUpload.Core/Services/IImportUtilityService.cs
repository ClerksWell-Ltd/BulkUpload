using Umbraco.Community.BulkUpload.Core.Models;

namespace Umbraco.Community.BulkUpload.Core.Services;

public interface IImportUtilityService
{
    public ImportObject CreateImportObject(dynamic? record);

    public ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false);
}