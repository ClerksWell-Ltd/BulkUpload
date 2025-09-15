using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class GuidsToContentUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidsToContentUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "guidsToContentUdis";

    public object Resolve(object value)
    {
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string)
            return Enumerable.Empty<string>();

        var udis = new List<string>();

        foreach (var item in value.ToString().Split(','))
        {
            if (item is null || !Guid.TryParse(item.ToString(), out var guid))
            {
                continue;
            }

            var contentItem = contextReference.UmbracoContext.Content.GetById(guid);
            if (contentItem is not null)
            {
                var udi = Udi.Create("document", guid);
                udis.Add(udi.UriValue.ToString());
            }
        }

        return string.Join(',', udis);
    }
}