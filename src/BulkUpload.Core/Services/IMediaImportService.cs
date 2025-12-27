using BulkUpload.Core.Models;

namespace BulkUpload.Core.Services;

public interface IMediaImportService
{
    public MediaImportObject CreateMediaImportObject(dynamic? record);

    public MediaImportResult ImportSingleMediaItem(MediaImportObject importObject, Stream fileStream, bool publish = false);
}