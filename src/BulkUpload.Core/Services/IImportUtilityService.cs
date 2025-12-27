using BulkUpload.Core.Models;

namespace BulkUpload.Core.Services;

public interface IImportUtilityService
{
    public ImportObject CreateImportObject(dynamic? record);

    public ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false);
}