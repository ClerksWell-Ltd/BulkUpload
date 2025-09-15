using Newtonsoft.Json;

using Umbraco.Cms.Core.Models;
namespace Umbraco.Community.BulkUpload.Resolvers;

public class TextToLinkResolver : IResolver
{
    public string Alias() => "textToLink";

    public object Resolve(object value)
    {
        if (value is not string linkText)
            return null;

        if (string.IsNullOrWhiteSpace(linkText))
            return null;

        if (!linkText.StartsWith("http://") && !linkText.StartsWith("https://"))
        {
            linkText = "https://" + linkText;
        }

        var links = new List<Link>();

        links.Add(new Link()
        {
            Url = linkText,
            Name = linkText,
            Type = LinkType.External
        });

        return JsonConvert.SerializeObject(links);
    }
}