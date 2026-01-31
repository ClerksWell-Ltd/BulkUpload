using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

#if !NET8_0
using Umbraco.Cms.Core.Services.Navigation;
#endif

namespace BulkUpload.Core.Resolvers;

public class SampleAuthorNameResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;
#if !NET8_0
    private readonly IDocumentNavigationQueryService _documentNavigationQueryService;
#endif

#if NET8_0
    public SampleAuthorNameResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;
#else
    public SampleAuthorNameResolver(
        IUmbracoContextFactory contextFactory,
        IDocumentNavigationQueryService documentNavigationQueryService)
    {
        _contextFactory = contextFactory;
        _documentNavigationQueryService = documentNavigationQueryService;
    }
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
            // Umbraco 17: Use IDocumentNavigationQueryService to get root content
            var homePage = _documentNavigationQueryService.TryGetRootKeys(out var rootKeys)
                ? contextReference.UmbracoContext.Content?.GetById(rootKeys.FirstOrDefault())
                : null;
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