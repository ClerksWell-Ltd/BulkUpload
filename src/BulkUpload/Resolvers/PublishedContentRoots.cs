#if !NET8_0
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;

namespace BulkUpload.Resolvers;

/// <summary>
/// Reads the published content roots without <c>IPublishedContentQuery</c>.
/// </summary>
/// <remarks>
/// <c>IPublishedContentQuery</c> is registered as scoped, and <see cref="ResolverFactory"/> resolves every
/// <see cref="IResolver"/> from the root provider and caches the instance for the lifetime of the
/// application — so a resolver cannot hold one. The navigation service and the published content cache are
/// both singletons, and together answer the same question.
/// </remarks>
internal static class PublishedContentRoots
{
    /// <summary>The first published content root, or null if the site has none.</summary>
    public static IPublishedContent? First(
        IDocumentNavigationQueryService navigationQueryService,
        IPublishedContentCache publishedContentCache)
    {
        if (!navigationQueryService.TryGetRootKeys(out var rootKeys))
            return null;

        foreach (var rootKey in rootKeys)
        {
            // Null for an unpublished root, which ContentAtRoot() would also have skipped.
            var root = publishedContentCache.GetById(rootKey);
            if (root is not null)
                return root;
        }

        return null;
    }
}
#endif
