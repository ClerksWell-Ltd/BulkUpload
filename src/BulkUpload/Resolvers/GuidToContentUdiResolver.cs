using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class GuidToContentUdiResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public GuidToContentUdiResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;

    public string Alias() => "guidToContentUdi";

    public object Resolve(object value)
    {
        using (var contextReference = _contextFactory.EnsureUmbracoContext())
        {
            if (value is not string)
                return default(string);

            var guid = new Guid(value.ToString());

            var contentItem = contextReference.UmbracoContext.Content.GetById(guid);

            if (contentItem is null)
                return default(string);

            var udi = Udi.Create("document", guid);
            return udi.UriValue;
        }
    }
}