using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class GuidsToMediaUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidsToMediaUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "guidsToMediaUdis";

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

            var mediaItem = contextReference.UmbracoContext.Media.GetById(guid);
            if (mediaItem is not null)
            {
                var udi = Udi.Create("media", guid);
                udis.Add(udi.UriValue.ToString());
            }
        }

        return string.Join(',', udis);
    }
}