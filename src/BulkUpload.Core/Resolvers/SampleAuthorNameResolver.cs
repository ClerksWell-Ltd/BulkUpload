using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
#if !NET8_0
using Umbraco.Cms.Core.PublishedCache;
#endif

namespace BulkUpload.Core.Resolvers;

public class SampleAuthorNameResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;
#if !NET8_0
    private readonly IPublishedContentQuery _publishedContentQuery;

    public SampleAuthorNameResolver(IUmbracoContextFactory contextFactory, IPublishedContentQuery publishedContentQuery)
    {
        _contextFactory = contextFactory;
        _publishedContentQuery = publishedContentQuery;
    }
#else
    public SampleAuthorNameResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;
#endif

    public string Alias() => "sampleAuthorName";

    public object Resolve(object value)
    {
        if (value is not string str)
            return string.Empty;

        using (var contextReference = _contextFactory.EnsureUmbracoContext())
        {
#if NET8_0
            var homePage = contextReference.UmbracoContext.Content?.GetAtRoot().FirstOrDefault();
#else
            // Umbraco 17: Use IPublishedContentQuery.ContentAtRoot()
            var homePage = _publishedContentQuery.ContentAtRoot()?.FirstOrDefault();
#endif

            if (homePage is null)
                return string.Empty;

            var authors = homePage.ChildrenOfType("authorList")?.FirstOrDefault();

            if (authors is null)
                return string.Empty;

            var author = authors?.Children().FirstOrDefault(x => x.Name.InvariantEquals(str));

            if (author is null)
                return string.Empty;

            var udi = Udi.Create("document", author.Key);
            return udi.UriValue != null ? udi.UriValue.ToString() : string.Empty;
        }
    }
}