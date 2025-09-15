using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class GuidToMediaUdiResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidToMediaUdiResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;

    public string Alias() => "guidToMediaUdi";

    public object Resolve(object value)
    {
        using (var contextReference = _contextFactory.EnsureUmbracoContext())
        {
            if (value is not string)
                return default(string);

            var guid = new Guid(value.ToString());

            var mediaItem = contextReference.UmbracoContext.Media.GetById(guid);

            if (mediaItem is null)
                return default(string);

            var udi = Udi.Create("media", guid);
            return udi.UriValue;
        }
    }
}