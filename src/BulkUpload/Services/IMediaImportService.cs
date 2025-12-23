using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Services;

public interface IMediaImportService
{
    public MediaImportObject CreateMediaImportObject(dynamic? record);

    public MediaImportResult ImportSingleMediaItem(MediaImportObject importObject, Stream fileStream, bool publish = false);
}