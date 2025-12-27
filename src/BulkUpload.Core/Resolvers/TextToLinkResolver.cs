using Newtonsoft.Json;

using Umbraco.Cms.Core.Models;

namespace BulkUpload.Core.Resolvers;

public class TextToLinkResolver : IResolver
{
    public string Alias() => "textToLink";

    public object Resolve(object value)
    {
        if (value is not string linkText || string.IsNullOrWhiteSpace(linkText))
            return string.Empty;

        if (!linkText.StartsWith("http://") && !linkText.StartsWith("https://"))
            linkText = "https://" + linkText;

        var links = new List<Link>
        {
            new()
            {
                Url = linkText,
                Name = linkText,
                Type = LinkType.External
            }
        };

        return JsonConvert.SerializeObject(links);
    }
}