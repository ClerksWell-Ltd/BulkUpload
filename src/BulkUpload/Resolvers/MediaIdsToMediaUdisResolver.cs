using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class MediaIdsToMediaUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public MediaIdsToMediaUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "MediaIdsToMediaUdis";

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

            var mediaItem = contextReference.UmbracoContext.Media.GetById(id);
            if (mediaItem is not null)
            {
                var udi = Udi.Create("document", mediaItem.Key);
                udis.Add(udi.UriValue.ToString());
            }
        }

        return string.Join(",", udis);
    }
}