using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace BulkUpload.Resolvers;

public class ContentIdToContentUdiResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public ContentIdToContentUdiResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "contentIdToContentUdi";

    public object Resolve(object value)
    {
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string || !int.TryParse(value.ToString(), out var id))
            return string.Empty;

        var contentItem = contextReference.UmbracoContext.Content?.GetById(id);
        if (contentItem is null)
            return string.Empty;

        var udi = Udi.Create("document", contentItem.Key);
        return udi.UriValue?.ToString() ?? string.Empty;
    }
}