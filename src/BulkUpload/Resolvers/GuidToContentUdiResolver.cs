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
        if (value is not string str || !Guid.TryParse(str, out var guid))
            return string.Empty;

        using (var contextReference = _contextFactory.EnsureUmbracoContext())
        {
            var contentItem = contextReference.UmbracoContext.Content?.GetById(guid);

            if (contentItem is null)
                return string.Empty;

            var udi = Udi.Create("document", guid);
            return udi.UriValue != null ? udi.UriValue.ToString() : string.Empty;
        }
    }
}