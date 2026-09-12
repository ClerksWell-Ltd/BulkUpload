using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
#if !NET8_0
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
#endif

namespace BulkUpload.Resolvers;

public class SampleAuthorNameResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;
#if !NET8_0
    private readonly IDocumentNavigationQueryService _navigationQueryService;
    private readonly IPublishedContentCache _publishedContentCache;

    public SampleAuthorNameResolver(
        IUmbracoContextFactory contextFactory,
        IDocumentNavigationQueryService navigationQueryService,
        IPublishedContentCache publishedContentCache)
    {
        _contextFactory = contextFactory;
        _navigationQueryService = navigationQueryService;
        _publishedContentCache = publishedContentCache;
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
            // Umbraco 17: the navigation service and published cache are both singletons, unlike
            // IPublishedContentQuery — see PublishedContentRoots.
            var homePage = PublishedContentRoots.First(_navigationQueryService, _publishedContentCache);
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