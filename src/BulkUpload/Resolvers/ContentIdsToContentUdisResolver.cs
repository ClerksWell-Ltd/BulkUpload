using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class ContentIdsToContentUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public ContentIdsToContentUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "ContentIdsToContentUdis";

    public object Resolve(object value)
    {
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string)
            return string.Empty;

        var udis = new List<string>();

        foreach (var item in value.ToString().Split(','))
        {
            if (!int.TryParse(item.Trim(), out var id))
                continue;

            var contentItem = contextReference.UmbracoContext.Content.GetById(id);
            if (contentItem is not null)
            {
                var udi = Udi.Create("document", contentItem.Key);
                udis.Add(udi.UriValue.ToString());
            }
        }

        return string.Join(",", udis);
    }
}