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
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string || !int.TryParse(value.ToString(), out var id))
            return string.Empty;

        var mediaItem = contextReference.UmbracoContext.Media.GetById(id);
        if (mediaItem is null)
            return string.Empty;

        var udi = Udi.Create("media", mediaItem.Key);
        return udi.UriValue.ToString();
    }
}