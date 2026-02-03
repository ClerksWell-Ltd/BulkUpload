using Umbraco.Cms.Core;

using Umbraco.Cms.Core.Web;

namespace BulkUpload.Resolvers;

public class GuidsToMediaUdisResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidsToMediaUdisResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "guidsToMediaUdis";

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

            var mediaItem = contextReference.UmbracoContext.Media?.GetById(guid);
            if (mediaItem is not null)
            {
                var udi = Udi.Create("media", guid);
                if (udi.UriValue is not null)
                    udis.Add(udi.UriValue.ToString());
            }
        }

        return string.Join(",", udis);
    }
}