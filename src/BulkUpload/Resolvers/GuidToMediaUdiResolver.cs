using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace BulkUpload.Resolvers;

public class GuidToMediaUdiResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidToMediaUdiResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;

    public string Alias() => "guidToMediaUdi";

    public object Resolve(object value)
    {
        if (value is not string str || !Guid.TryParse(str, out var guid))
            return string.Empty;

        using var contextReference = _contextFactory.EnsureUmbracoContext();
        var mediaItem = contextReference.UmbracoContext.Media?.GetById(guid);

        if (mediaItem is null)
            return string.Empty;

        var udi = Udi.Create("media", guid);
        return udi.UriValue != null ? udi.UriValue.ToString() : string.Empty;
    }
}