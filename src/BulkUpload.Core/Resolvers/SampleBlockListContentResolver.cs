using Microsoft.AspNetCore.Html;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Community.BulkUpload.Core.Models;

namespace Umbraco.Community.BulkUpload.Core.Resolvers;

public class SampleBlockListContentResolver : IResolver
{
    public string Alias() => "sampleBlockListContent";

    public object Resolve(object value)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var trimmed = str.Trim();
        var looksLikeHtml = trimmed.StartsWith("<") && trimmed.EndsWith(">");

        var newHtmlContent = new HtmlString(looksLikeHtml ? trimmed : $"<p>{trimmed}</p>");

        var contentUdi = new GuidUdi("element", Guid.NewGuid());
        var settingsUdi = new GuidUdi("element", Guid.NewGuid());

        var blockListModel = new BlockList
        {
            layout = new BlockListUdi(new List<Dictionary<string, string>>
            {
                new() { { "contentUdi", contentUdi.ToString() } }
            }),
            contentData = new List<Dictionary<string, string>>
            {
                new()
                {
                    { "contentTypeKey", "dd183f78-7d69-4eda-9b4c-a25970583a28" },
                    { "udi", contentUdi.ToString() },
                    { "content", newHtmlContent.ToString() }
                }
            },
            settingsData = new List<Dictionary<string, string>>
            {
                new()
                {
                    { "contentTypeKey", "da15dc43-43f6-45f6-bda8-1fd17a49d25c" },
                    { "udi", settingsUdi.ToString() }
                }
            }
        };

        return JsonConvert.SerializeObject(blockListModel);
    }
}