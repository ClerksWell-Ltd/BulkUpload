using Umbraco.Community.BulkUpload.Core.Models;

namespace Umbraco.Community.BulkUpload.Core.Services;

public interface IMediaImportService
{
    public MediaImportObject CreateMediaImportObject(dynamic? record);

    public MediaImportResult ImportSingleMediaItem(MediaImportObject importObject, Stream fileStream, bool publish = false);
}