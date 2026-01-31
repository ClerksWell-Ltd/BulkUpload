using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using BulkUpload.Core.Models;
using BulkUpload.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Core.Resolvers;

/// <summary>
/// Resolver that looks up media items created from ZIP file during preprocessing.
/// This resolver does NOT create media - it only retrieves media that was already
/// created by MediaPreprocessorService and cached in IMediaItemCache.
///
/// Usage in CSV header: propertyName|zipFileToMedia
/// CSV value: filename.jpg (the filename from the ZIP file)
///
/// The MediaPreprocessorService must run before content import to create and cache
/// the media items. This resolver then looks up the cached media by filename and
/// returns the UDI.
/// </summary>
public class ZipFileToMediaResolver : IResolver
{
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILogger<ZipFileToMediaResolver> _logger;

    public ZipFileToMediaResolver(
        IMediaItemCache mediaItemCache,
        ILogger<ZipFileToMediaResolver> logger)
    {
        _mediaItemCache = mediaItemCache;
        _logger = logger;
    }

    public string Alias() => "zipFileToMedia";

    public object Resolve(object value)
    {
        // Extract the filename from the value
        // The value can be either a string or a ParameterizedValue (when resolver has a parameter like zipFileToMedia:/folder/)
        string? fileName = null;
        object actualValue = value;

        // If the resolver was called with a parameter (e.g., zipFileToMedia:/folder/),
        // the value will be wrapped in a ParameterizedValue
        if (value is ParameterizedValue parameterizedValue)
        {
            actualValue = parameterizedValue.Value;
            // Note: The parameter is the parent folder, but for zipFileToMedia,
            // the parent was already used during preprocessing. We just need the filename.
        }

        // Extract filename from the actual value
        if (actualValue is string str && !string.IsNullOrWhiteSpace(str))
        {
            // Support optional parent parameter in value: filename.jpg|parentId
            // (though this is less common since the parent is usually in the alias parameter)
            var parts = str.Split('|', 2);
            fileName = parts[0]?.Trim();
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            _logger.LogWarning("ZipFileToMediaResolver received null or empty filename. Value type: {ValueType}, Value: {Value}",
                value?.GetType().Name ?? "null",
                value?.ToString() ?? "null");
            return string.Empty;
        }

        // Look up the media item in the cache
        // The MediaPreprocessorService should have already created and cached this media
        if (_mediaItemCache.TryGetGuid(fileName, out var mediaGuid))
        {
            var udi = Udi.Create(UmbracoConstants.UdiEntityType.Media, mediaGuid);
            _logger.LogDebug("Found cached media for ZIP file: {FileName}, UDI: {Udi}", fileName, udi);
            return udi.ToString();
        }

        // Media not found in cache - this means MediaPreprocessorService didn't run
        // or the file wasn't in the ZIP
        _logger.LogWarning("Media not found in cache for ZIP file: {FileName}. " +
            "Ensure the file exists in the ZIP and MediaPreprocessorService ran before content import.",
            fileName);
        return string.Empty;
    }
}
