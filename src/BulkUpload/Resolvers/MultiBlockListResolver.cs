using Newtonsoft.Json;
using Umbraco.Cms.Core;

namespace Umbraco.Community.BulkUpload.Resolvers;

/// <summary>
/// Resolver for creating BlockList structures with multiple block types from CSV data.
/// Format: blockType::content;;blockType::content
/// Supported block types: richtext, image, video, code, carousel, articlelist, iconlink
/// </summary>
public class MultiBlockListResolver : IResolver
{
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
        // Format: "mediaGuid|caption"
        var parts = content.Split('|', 2);
        var mediaGuidStr = parts[0].Trim();
        var caption = parts.Length > 1 ? parts[1].Trim() : "";

        Guid mediaGuid;
        if (!Guid.TryParse(mediaGuidStr, out mediaGuid))
        {
            // If parsing fails, generate a random GUID
            mediaGuid = Guid.NewGuid();
        }

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
        // Format: "mediaGuid1,mediaGuid2,mediaGuid3,..."
        var mediaGuids = content.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim())
            .Where(g => Guid.TryParse(g, out _))
            .Select(g => new
            {
                key = Guid.NewGuid(),
                mediaKey = Guid.Parse(g)
            })
            .ToArray();

        return new Dictionary<string, object>
        {
            { "contentTypeKey", "1c43fe2d-4a9a-4336-923f-9d0214950d48" }, // Image carousel block
            { "udi", udi.ToString() },
            { "images", mediaGuids }
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
        // Format: "iconMediaGuid|linkUrl|linkName"
        var parts = content.Split('|', 3);
        var iconMediaGuidStr = parts.Length > 0 ? parts[0].Trim() : "";
        var linkUrl = parts.Length > 1 ? parts[1].Trim() : "";
        var linkName = parts.Length > 2 ? parts[2].Trim() : "";

        Guid iconMediaGuid;
        if (!Guid.TryParse(iconMediaGuidStr, out iconMediaGuid))
        {
            iconMediaGuid = Guid.NewGuid();
        }

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
