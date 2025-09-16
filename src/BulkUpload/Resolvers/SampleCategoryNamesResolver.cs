using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class SampleCategoryNamesResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public SampleCategoryNamesResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "sampleCategoryNames";

    public object Resolve(object value)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        using var contextReference = _contextFactory.EnsureUmbracoContext();
        var udis = new List<string>();

        var homePage = contextReference.UmbracoContext.Content?.GetAtRoot().FirstOrDefault();

        if (homePage is null)
            return string.Empty;

        var categories = homePage.ChildrenOfType("categoryList").FirstOrDefault();

        if (categories is null)
            return string.Empty;

        foreach (var item in str.Split(','))
        {
            var category = categories?.Children().FirstOrDefault(x => x.Name.InvariantEquals(item));
            if (category is not null)
            {
                var udi = Udi.Create("document", category.Key);
                if (udi.UriValue is not null)
                {
                    udis.Add(udi.UriValue.ToString());
                }
            }
        }

        return string.Join(",", udis);
    }
}