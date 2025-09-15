using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class MediaIdToMediaUdiResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public MediaIdToMediaUdiResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "mediaIdToMediaUdi";

    public object Resolve(object value)
    {
        if (value is not string str || !int.TryParse(str, out var id))
            return string.Empty;

        using var contextReference = _contextFactory.EnsureUmbracoContext();
        var mediaItem = contextReference.UmbracoContext.Media?.GetById(id);

        if (mediaItem is null)
            return string.Empty;

        var udi = Udi.Create("media", mediaItem.Key);
        return udi.UriValue != null ? udi.UriValue.ToString() : string.Empty;
    }
}