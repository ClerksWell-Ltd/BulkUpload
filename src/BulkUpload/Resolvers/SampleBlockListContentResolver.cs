using Microsoft.AspNetCore.Html;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class SampleBlockListContentResolver : IResolver
{
    public string Alias() => "sampleBlockListContent";

    public object Resolve(object value)
    {
        if (value is not string)
            return null;

        IHtmlContent newHtmlContent;


        var trimmed = value.ToString().Trim();
        var looksLikeHtml = trimmed.StartsWith("<") && trimmed.EndsWith(">");

        if (looksLikeHtml)
        {
            newHtmlContent = new HtmlString(trimmed);
        }
        else
        {
            newHtmlContent = new HtmlString($"<p>{value.ToString()}</p>");
        }

        var blockListModel = new BlockList();
        var contentUdiDictionary = new List<Dictionary<string, string>>();
        var settingsUdiDictionary = new List<Dictionary<string, string>>();

        var contentData = new List<Dictionary<string, string>>();

        var contentUdi = new GuidUdi("element", Guid.NewGuid());
        contentData.Add(new Dictionary<string, string>()
            {
                { "contentTypeKey", "dd183f78-7d69-4eda-9b4c-a25970583a28" },
                { "udi", contentUdi.ToString() },
                { "content", newHtmlContent.ToString() }
            });

        contentUdiDictionary.Add(new Dictionary<string, string> { { "contentUdi", contentUdi.ToString() } });

        var settingsData = new List<Dictionary<string, string>>();

        var settingsUdi = new GuidUdi("element", Guid.NewGuid());
        settingsData.Add(new Dictionary<string, string>()
            {
                { "contentTypeKey", "da15dc43-43f6-45f6-bda8-1fd17a49d25c" },
                { "udi", settingsUdi.ToString() }
            });

        settingsUdiDictionary.Add(new Dictionary<string, string> { { "contentUdi", settingsUdi.ToString() } });


        blockListModel.layout = new BlockListUdi(contentUdiDictionary);
        blockListModel.contentData = contentData;
        blockListModel.settingsData = settingsData;

        return JsonConvert.SerializeObject(blockListModel);
    }
}