using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Core.Resolvers;

public class MediaIdsToMediaUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public MediaIdsToMediaUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "MediaIdsToMediaUdis";

    public object Resolve(object value)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        using var contextReference = _contextFactory.EnsureUmbracoContext();
        var udis = new List<string>();

        foreach (var item in str.Split(','))
        {
            if (!int.TryParse(item.Trim(), out var id))
                continue;

            var mediaItem = contextReference.UmbracoContext.Media?.GetById(id);
            if (mediaItem is not null)
            {
                var udi = Udi.Create("media", mediaItem.Key);
                if (udi.UriValue is not null)
                {
                    udis.Add(udi.UriValue.ToString());
                }
            }
        }

        return string.Join(",", udis);
    }
}