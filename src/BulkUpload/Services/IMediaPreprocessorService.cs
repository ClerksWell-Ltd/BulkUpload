using BulkUpload.Models;

namespace BulkUpload.Services;

/// <summary>
/// Interface for preprocessing media items before content import.
/// </summary>
public interface IMediaPreprocessorService
{
    /// <summary>
    /// Preprocesses media items from CSV records.
    /// Extracts all unique media references, creates them, and caches them.
    /// </summary>
    /// <param name="csvRecordsWithSource">CSV records with their source filenames to extract media references from</param>
    /// <param name="zipExtractDirectory">Optional directory where ZIP was extracted (for zipFileToMedia resolver)</param>
    /// <returns>List of media preprocessing results containing cache keys and values</returns>
    List<MediaPreprocessingResult> PreprocessMediaItems(List<(dynamic record, string sourceFileName)> csvRecordsWithSource, string? zipExtractDirectory = null);
}