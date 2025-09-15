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
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        using var contextReference = _contextFactory.EnsureUmbracoContext();
        var udis = new List<string>();

        foreach (var item in str.Split(','))
        {
            if (!Guid.TryParse(item.Trim(), out var guid))
                continue;

            var contentItem = contextReference.UmbracoContext.Content?.GetById(guid);
            if (contentItem is not null)
            {
                var udi = Udi.Create("document", guid);
                if (udi.UriValue is not null)
                {
                    udis.Add(udi.UriValue.ToString());
                }
            }
        }

        return string.Join(",", udis);
    }
}