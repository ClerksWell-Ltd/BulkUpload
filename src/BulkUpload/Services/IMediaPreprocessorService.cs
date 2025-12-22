namespace Umbraco.Community.BulkUpload.Services;

/// <summary>
/// Interface for preprocessing media items before content import.
/// </summary>
public interface IMediaPreprocessorService
{
    /// <summary>
    /// Preprocesses media items from CSV records.
    /// Extracts all unique media references, creates them, and caches them.
    /// </summary>
    void PreprocessMediaItems(List<dynamic> csvRecords);
}
