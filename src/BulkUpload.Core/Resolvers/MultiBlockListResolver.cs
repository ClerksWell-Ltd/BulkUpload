using Newtonsoft.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using BulkUpload.Core.Services;
using Microsoft.Extensions.Logging;
using UmbracoConstants = Umbraco.Cms.Core.Constants;
using IOFile = System.IO.File;

namespace BulkUpload.Core.Resolvers;

/// <summary>
/// Resolver for creating BlockList structures with multiple block types from CSV data.
/// Format: blockType::content;;blockType::content
/// Supported block types: richtext, image, video, code, carousel, articlelist, iconlink
///
/// Image blocks support:
/// - URLs: image::https://example.com/photo.jpg|Caption
/// - File paths: image::/path/to/photo.jpg|Caption
/// - GUIDs: image::a1b2c3d4-e5f6-7890-abcd-ef1234567890|Caption
/// </summary>
public class MultiBlockListResolver : IResolver
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IMediaItemCache _mediaItemCache;
    private readonly ILogger<MultiBlockListResolver> _logger;

    public MultiBlockListResolver(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMediaItemCache mediaItemCache,
        ILogger<MultiBlockListResolver> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _mediaItemCache = mediaItemCache;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string Alias() => "multiBlockList";

    public object Resolve(object value)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var blocks = str.Split(";;", StringSplitOptions.RemoveEmptyEntries);

        var layoutList = new List<Dictionary<string, string>>();
        var contentDataList = new List<Dictionary<string, object>>();
        var settingsDataList = new List<Dictionary<string, string>>();

        foreach (var block in blocks)
        {
            var parts = block.Split("::", 2);
            if (parts.Length < 2) continue;

            var blockType = parts[0].Trim().ToLower();
            var blockContent = parts[1].Trim();

            var contentUdi = new GuidUdi("element", Guid.NewGuid());
            var settingsUdi = new GuidUdi("element", Guid.NewGuid());

            // Add to layout
            layoutList.Add(new Dictionary<string, string>
            {
                { "contentUdi", contentUdi.ToString() },
                { "settingsUdi", settingsUdi.ToString() }
            });

            // Add settings (common for all blocks)
            settingsDataList.Add(new Dictionary<string, string>
            {
                { "contentTypeKey", GetSettingsTypeKey(blockType) },
                { "udi", settingsUdi.ToString() },
                { "hide", "0" }
            });

            // Add content based on block type
            switch (blockType)
            {
                case "richtext":
                    contentDataList.Add(CreateRichTextBlock(blockContent, contentUdi));
                    break;

                case "image":
                    contentDataList.Add(CreateImageBlock(blockContent, contentUdi));
                    break;

                case "video":
                    contentDataList.Add(CreateVideoBlock(blockContent, contentUdi));
                    break;

                case "code":
                    contentDataList.Add(CreateCodeBlock(blockContent, contentUdi));
                    break;

                case "carousel":
                    contentDataList.Add(CreateCarouselBlock(blockContent, contentUdi));
                    break;

                case "articlelist":
                    contentDataList.Add(CreateArticleListBlock(blockContent, contentUdi));
                    break;

                case "iconlink":
                    contentDataList.Add(CreateIconLinkBlock(blockContent, contentUdi));
                    break;

                default:
                    // Skip unknown block types
                    continue;
            }
        }

        var blockListModel = new
        {
            layout = new
            {
                Umbraco_BlockList = layoutList
            },
            contentData = contentDataList,
            settingsData = settingsDataList
        };

        // Use custom serializer settings to handle property names
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
        };

        var json = JsonConvert.SerializeObject(blockListModel, settings);

        // Replace Umbraco_BlockList with Umbraco.BlockList (JSON property name with dot)
        json = json.Replace("\"Umbraco_BlockList\":", "\"Umbraco.BlockList\":");

        return json;
    }

    private Dictionary<string, object> CreateRichTextBlock(string content, GuidUdi udi)
    {
        var looksLikeHtml = content.Trim().StartsWith("<") && content.Trim().EndsWith(">");
        var htmlContent = looksLikeHtml ? content : $"<p>{content}</p>";

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "dd183f78-7d69-4eda-9b4c-a25970583a28" }, // Rich text block
            { "udi", udi.ToString() },
            {
                "content", new
                {
                    markup = htmlContent,
                    blocks = new
                    {
                        contentData = Array.Empty<object>(),
                        settingsData = Array.Empty<object>()
                    }
                }
            }
        };
    }

    private Dictionary<string, object> CreateImageBlock(string content, GuidUdi udi)
    {
        // Format: "mediaReference|caption"
        // mediaReference can be: GUID, URL, or file path
        var parts = content.Split('|', 2);
        var mediaReference = parts[0].Trim();
        var caption = parts.Length > 1 ? parts[1].Trim() : "";

        // Resolve the media reference (handles GUID, URL, or file path)
        var mediaGuid = ResolveMediaReference(mediaReference);

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "e0df4794-063a-4450-8f4f-c615a5d902e2" }, // Image block
            { "udi", udi.ToString() },
            { "caption", caption },
            {
                "image", new[]
                {
                    new
                    {
                        key = Guid.NewGuid(),
                        mediaKey = mediaGuid
                    }
                }
            }
        };
    }

    private Dictionary<string, object> CreateVideoBlock(string content, GuidUdi udi)
    {
        // Format: "videoUrl|caption"
        var parts = content.Split('|', 2);
        var videoUrl = parts[0].Trim();
        var caption = parts.Length > 1 ? parts[1].Trim() : "";

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "f43c8349-0801-44b8-9113-9f7c62cd44fe" }, // Video block
            { "udi", udi.ToString() },
            { "videoUrl", videoUrl },
            { "caption", caption }
        };
    }

    private Dictionary<string, object> CreateCodeBlock(string content, GuidUdi udi)
    {
        // Format: "code|title"
        var parts = content.Split('|', 2);
        var code = parts[0].Trim();
        var title = parts.Length > 1 ? parts[1].Trim() : "";

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "f37c2c28-c8ab-48cd-ac07-b13e38bd900f" }, // Code block
            { "udi", udi.ToString() },
            { "code", code },
            { "title", title }
        };
    }

    private Dictionary<string, object> CreateCarouselBlock(string content, GuidUdi udi)
    {
        // Format: "mediaReference1,mediaReference2,mediaReference3,..."
        // Each mediaReference can be: GUID, URL, or file path
        var mediaReferences = content.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(reference => reference.Trim())
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference => new
            {
                key = Guid.NewGuid(),
                mediaKey = ResolveMediaReference(reference)
            })
            .ToArray();

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "1c43fe2d-4a9a-4336-923f-9d0214950d48" }, // Image carousel block
            { "udi", udi.ToString() },
            { "images", mediaReferences }
        };
    }

    private Dictionary<string, object> CreateArticleListBlock(string content, GuidUdi udi)
    {
        // Format: "documentUdi|pageSize:value|showPagination:value"
        var parts = content.Split('|');
        var articleListUdi = parts.Length > 0 ? parts[0].Trim() : "";
        var pageSize = "5";
        var showPagination = "1";

        // Parse optional parameters
        foreach (var part in parts.Skip(1))
        {
            var keyValue = part.Split(':', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLower();
                var value = keyValue[1].Trim();

                if (key == "pagesize")
                    pageSize = value;
                else if (key == "showpagination")
                    showPagination = value;
            }
        }

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "60085a63-b77b-4509-9df4-bcb75db2755f" }, // Article list block
            { "udi", udi.ToString() },
            { "articleList", articleListUdi },
            { "pageSize", pageSize },
            { "showPagination", showPagination }
        };
    }

    private Dictionary<string, object> CreateIconLinkBlock(string content, GuidUdi udi)
    {
        // Format: "iconMediaReference|linkUrl|linkName"
        // iconMediaReference can be: GUID, URL, or file path
        var parts = content.Split('|', 3);
        var iconMediaReference = parts.Length > 0 ? parts[0].Trim() : "";
        var linkUrl = parts.Length > 1 ? parts[1].Trim() : "";
        var linkName = parts.Length > 2 ? parts[2].Trim() : "";

        // Resolve the icon media reference (handles GUID, URL, or file path)
        var iconMediaGuid = ResolveMediaReference(iconMediaReference);

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "17db13ba-bbd9-4a44-b28f-986301156754" }, // Icon link block
            { "udi", udi.ToString() },
            {
                "icon", new[]
                {
                    new
                    {
                        key = Guid.NewGuid(),
                        mediaKey = iconMediaGuid
                    }
                }
            },
            {
                "link", new[]
                {
                    new
                    {
                        name = linkName,
                        target = "_blank",
                        url = linkUrl
                    }
                }
            }
        };
    }

    /// <summary>
    /// Resolves a media reference (GUID, URL, or file path) to a media GUID.
    /// If the input is a URL or file path, creates the media item.
    /// </summary>
    private Guid ResolveMediaReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            _logger.LogWarning("Empty media reference provided");
            return Guid.NewGuid(); // Return random GUID for invalid input
        }

        reference = reference.Trim();

        // Check if it's already a GUID
        if (Guid.TryParse(reference, out var guid))
        {
            _logger.LogDebug("Using existing media GUID: {Guid}", guid);
            return guid;
        }

        // Check cache first to avoid creating duplicates
        if (_mediaItemCache.TryGetGuid(reference, out var cachedGuid))
        {
            _logger.LogDebug("Found cached media for reference: {Reference}, GUID: {Guid}", reference, cachedGuid);
            return cachedGuid;
        }

        // Check if it's a URL
        if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var mediaGuid = CreateMediaFromUrl(reference);
            if (mediaGuid != Guid.Empty)
            {
                return mediaGuid;
            }
        }

        // Check if it's a file path
        if (IOFile.Exists(reference))
        {
            var mediaGuid = CreateMediaFromFilePath(reference);
            if (mediaGuid != Guid.Empty)
            {
                return mediaGuid;
            }
        }

        _logger.LogWarning("Could not resolve media reference: {Reference}", reference);
        return Guid.NewGuid(); // Return random GUID for unresolvable references
    }

    /// <summary>
    /// Creates a media item from a URL.
    /// </summary>
    private Guid CreateMediaFromUrl(string urlString)
    {
        if (_httpClientFactory == null)
        {
            _logger.LogWarning("HTTP client factory not available, cannot download from URL: {Url}", urlString);
            return Guid.Empty;
        }

        try
        {
            var uri = new Uri(urlString);
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = httpClient.GetAsync(uri).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download media from URL: {Url}, Status: {StatusCode}",
                    urlString, response.StatusCode);
                return Guid.Empty;
            }

            var fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("Downloaded file from URL is empty: {Url}", urlString);
                return Guid.Empty;
            }

            var fileName = GetFileNameFromUrl(uri);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var mediaTypeAlias = DetermineMediaTypeFromExtension(extension);

            var mediaGuid = CreateAndUploadMedia(fileName, fileBytes, mediaTypeAlias);

            if (mediaGuid != Guid.Empty)
            {
                _mediaItemCache.TryAdd(urlString, mediaGuid);
                _logger.LogInformation("Successfully created media from URL: {Url}, GUID: {Guid}", urlString, mediaGuid);
            }

            return mediaGuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating media from URL: {Url}", urlString);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Creates a media item from a file path.
    /// </summary>
    private Guid CreateMediaFromFilePath(string filePath)
    {
        try
        {
            var fileBytes = IOFile.ReadAllBytes(filePath);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("File is empty: {FilePath}", filePath);
                return Guid.Empty;
            }

            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "uploaded-file";
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var mediaTypeAlias = DetermineMediaTypeFromExtension(extension);

            var mediaGuid = CreateAndUploadMedia(fileName, fileBytes, mediaTypeAlias);

            if (mediaGuid != Guid.Empty)
            {
                _mediaItemCache.TryAdd(filePath, mediaGuid);
                _logger.LogInformation("Successfully created media from file: {FilePath}, GUID: {Guid}", filePath, mediaGuid);
            }

            return mediaGuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating media from file path: {FilePath}", filePath);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Creates and uploads a media item to Umbraco.
    /// </summary>
    private Guid CreateAndUploadMedia(string fileName, byte[] fileBytes, string mediaTypeAlias)
    {
        try
        {
            // Verify media type exists
            var mediaType = _mediaTypeService.Get(mediaTypeAlias);
            if (mediaType == null)
            {
                _logger.LogWarning("Media type '{MediaType}' not found", mediaTypeAlias);
                return Guid.Empty;
            }

            // Create media item in root folder
            var mediaItem = _mediaService.CreateMedia(fileName, UmbracoConstants.System.Root, mediaTypeAlias);

            // Upload the file
            using (var fileStream = new MemoryStream(fileBytes))
            {
                var propertyType = mediaItem.Properties["umbracoFile"]?.PropertyType;
                if (propertyType == null)
                {
                    _logger.LogWarning("Media type does not have umbracoFile property");
                    return Guid.Empty;
                }

                var mediaPath = _mediaFileManager.GetMediaPath(fileName, propertyType.Key, mediaItem.Key);
                _mediaFileManager.FileSystem.AddFile(mediaPath, fileStream, true);
                mediaItem.SetValue("umbracoFile", mediaPath);
            }

            // Save the media item
            var saveResult = _mediaService.Save(mediaItem);
            if (!saveResult.Success)
            {
                _logger.LogWarning("Failed to save media item for file: {FileName}", fileName);
                return Guid.Empty;
            }

            return mediaItem.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating and uploading media: {FileName}", fileName);
            return Guid.Empty;
        }
    }

    private string GetFileNameFromUrl(Uri uri)
    {
        var segments = uri.Segments;
        var lastSegment = segments.Length > 0 ? segments[^1] : "downloaded-file";
        var fileName = Uri.UnescapeDataString(lastSegment);

        var queryIndex = fileName.IndexOf('?');
        if (queryIndex >= 0)
        {
            fileName = fileName.Substring(0, queryIndex);
        }

        if (!Path.HasExtension(fileName))
        {
            fileName += ".jpg";
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

    private string GetSettingsTypeKey(string blockType)
    {
        return blockType switch
        {
            "image" => "fed88ec5-c150-42af-b444-1f9ac5a100ba",
            "video" => "eef34ceb-ddf6-4894-b1ac-f96c8c05d3d2",
            "code" => "93638715-f76c-4a11-86b1-6a9d66504901",
            "carousel" => "378fde96-51b6-4506-93e3-ec3038e636bb",
            "articlelist" => "c56fb5b8-0b89-4206-847e-a6fecd865b84",
            "iconlink" => "84e89805-5a53-4dcf-930d-fd87c48572dd",
            _ => "da15dc43-43f6-45f6-bda8-1fd17a49d25c" // Default settings (used for richtext)
        };
    }
}
