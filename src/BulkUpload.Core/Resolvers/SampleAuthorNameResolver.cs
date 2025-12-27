using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace BulkUpload.Core.Resolvers;

public class SampleAuthorNameResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public SampleAuthorNameResolver(IUmbracoContextFactory contextFactory) => _contextFactory = contextFactory;

    public string Alias() => "sampleAuthorName";

    public object Resolve(object value)
    {
        if (value is not string str)
            return string.Empty;

        using (var contextReference = _contextFactory.EnsureUmbracoContext())
        {
            var homePage = contextReference.UmbracoContext.Content?.GetAtRoot().FirstOrDefault();

            if (homePage is null)
                return string.Empty;

            var authors = homePage.ChildrenOfType("authorList").FirstOrDefault();

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