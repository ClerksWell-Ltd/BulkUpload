using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.Community.BulkUpload.Resolvers;

/// <summary>
/// Resolver that downloads an image from a URL and uploads it to Umbraco,
/// returning the UDI of the created media item.
/// </summary>
public class UrlToMediaResolver : IResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly ILogger<UrlToMediaResolver> _logger;

    public UrlToMediaResolver(
        IHttpClientFactory httpClientFactory,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ILogger<UrlToMediaResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _logger = logger;
    }

    public string Alias() => "urlToMedia";

    public object Resolve(object value)
    {
        if (value is not string urlString || string.IsNullOrWhiteSpace(urlString))
        {
            _logger.LogWarning("UrlToMediaResolver received null or empty URL");
            return string.Empty;
        }

        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("UrlToMediaResolver received invalid URL: {Url}", urlString);
            return string.Empty;
        }

        try
        {
            // Download the media file from the URL
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = httpClient.GetAsync(uri).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download media from URL: {Url}, Status: {StatusCode}",
                    urlString, response.StatusCode);
                return string.Empty;
            }

            var fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("Downloaded file from URL is empty: {Url}", urlString);
                return string.Empty;
            }

            // Extract filename from URL or generate one
            var fileName = GetFileNameFromUrl(uri);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            // Determine media type from extension
            var mediaTypeAlias = DetermineMediaTypeFromExtension(extension);

            // Create media item in Umbraco
            var mediaType = _mediaTypeService.Get(mediaTypeAlias);
            if (mediaType == null)
            {
                _logger.LogWarning("Media type '{MediaType}' not found", mediaTypeAlias);
                return string.Empty;
            }

            var mediaItem = _mediaService.CreateMedia(fileName, Constants.System.Root, mediaTypeAlias);

            // Upload the file
            using (var fileStream = new MemoryStream(fileBytes))
            {
                // Get the property type for umbracoFile
                var propertyType = mediaItem.Properties["umbracoFile"]?.PropertyType;
                if (propertyType == null)
                {
                    _logger.LogWarning("Media type does not have umbracoFile property");
                    return string.Empty;
                }

                // Get the media path
                var mediaPath = _mediaFileManager.GetMediaPath(fileName, propertyType.Key, mediaItem.Key);

                // Save the file to the media file system
                _mediaFileManager.FileSystem.AddFile(mediaPath, fileStream, true);

                // Set the file path on the media item
                mediaItem.SetValue("umbracoFile", mediaPath);
            }

            // Save the media item
            var saveResult = _mediaService.Save(mediaItem);
            if (!saveResult.Success)
            {
                _logger.LogWarning("Failed to save media item for URL: {Url}", urlString);
                return string.Empty;
            }

            // Return the UDI
            var udi = Udi.Create(Constants.UdiEntityType.Media, mediaItem.Key);
            var udiString = udi.ToString();

            _logger.LogInformation("Successfully created media from URL: {Url}, UDI: {Udi}", urlString, udiString);
            return udiString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving URL to media: {Url}", urlString);
            return string.Empty;
        }
    }

    private string GetFileNameFromUrl(Uri uri)
    {
        var segments = uri.Segments;
        var lastSegment = segments.Length > 0 ? segments[^1] : "downloaded-file";

        // Decode URL-encoded characters
        var fileName = Uri.UnescapeDataString(lastSegment);

        // Remove query strings if present
        var queryIndex = fileName.IndexOf('?');
        if (queryIndex >= 0)
        {
            fileName = fileName.Substring(0, queryIndex);
        }

        // Ensure we have an extension
        if (!Path.HasExtension(fileName))
        {
            fileName += ".jpg"; // Default to .jpg if no extension
        }

        return fileName;
    }

    private string DetermineMediaTypeFromExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => "Image",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => "File",
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".webm" => "Video",
            ".mp3" or ".wav" or ".ogg" or ".wma" => "Audio",
            _ => "File"
        };
    }
}
