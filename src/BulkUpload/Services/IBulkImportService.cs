using BulkUpload.Models;

namespace BulkUpload.Services;

/// <summary>
/// Runs the BulkUpload import sequence programmatically, from inside the Umbraco process.
/// </summary>
/// <remarks>
/// <para>
/// This is the same sequence the <c>/api/v1/content/importall</c> endpoint runs — cache clear, CSV read,
/// media preprocessing, resolver-driven property mapping, hierarchy sort, save — minus everything that is
/// specific to HTTP (file upload validation, ZIP extraction, the generic 500 mapping, temp-directory
/// cleanup). Nothing in it touches <c>HttpContext</c>, <c>UmbracoContext</c> or a back-office user, so an
/// in-process caller gets exactly the same result as a caller posting a CSV, without a self-call.
/// </para>
/// <para>
/// Prefer this over posting to the endpoint whenever the caller already runs inside the site. Exceptions
/// from a resolver or the hierarchy validator propagate with their real message instead of being flattened
/// into a 500, which is the main practical difference.
/// </para>
/// <para>
/// Implementations serialise imports: the import caches (<see cref="IParentLookupCache"/>,
/// <see cref="IMediaItemCache"/>, <see cref="ILegacyIdCache"/>) are process-wide and cleared at the start
/// of every import, so two concurrent imports would otherwise corrupt each other's state. A second call
/// queues until the first finishes.
/// </para>
/// </remarks>
public interface IBulkImportService
{
    /// <summary>
    /// Imports one or more CSV files. Rows across all files are read first, then sorted as a single set so
    /// cross-file legacy parent/child relationships resolve.
    /// </summary>
    /// <param name="csvFilePaths">Full paths of the CSV files to import.</param>
    /// <param name="mediaDirectory">
    /// Optional directory that file-based media resolvers (<c>zipFileToMedia</c> and friends) resolve
    /// relative paths against — the ZIP extraction directory for an uploaded archive. Null when the CSV
    /// carries no file-relative media references.
    /// </param>
    /// <param name="ct">Cancels the queue wait and the CSV read. Content writes are never cancelled.</param>
    Task<ContentImportResponse> ImportCsvFilesAsync(
        IReadOnlyList<string> csvFilePaths, string? mediaDirectory = null, CancellationToken ct = default);

    /// <summary>
    /// Imports records that are already in memory, bypassing the CSV reader. Each record is a column-name
    /// to value map using the same column syntax as a CSV header (<c>propertyAlias|resolverAlias</c>).
    /// </summary>
    /// <param name="records">The records to import, each paired with the source file name to report it under.</param>
    /// <param name="mediaDirectory">As <see cref="ImportCsvFilesAsync"/>.</param>
    /// <param name="ct">Cancels the queue wait. Content writes are never cancelled.</param>
    Task<ContentImportResponse> ImportRecordsAsync(
        IReadOnlyList<(IDictionary<string, object?> Record, string SourceFileName)> records,
        string? mediaDirectory = null, CancellationToken ct = default);
}
