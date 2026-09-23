using BulkUpload.Models;

namespace BulkUpload.Services;

public interface IImportUtilityService
{
    public ImportObject CreateImportObject(dynamic? record);

    /// <summary>
    /// Creates or updates a single content item.
    /// </summary>
    /// <param name="importObject">The row to import.</param>
    /// <param name="publish">Publish the item. Ignored when <paramref name="unpublish"/> is true.</param>
    /// <param name="unpublish">Unpublish the item if it is published. Wins over <paramref name="publish"/>.</param>
    public ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false, bool unpublish = false);
}