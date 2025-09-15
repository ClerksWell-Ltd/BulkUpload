using Umbraco.Cms.Core.Web;
namespace Umbraco.Community.BulkUpload.Resolvers;

public class StringArrayResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public StringArrayResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "stringArray";

    public object Resolve(object value)
    {
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string)
            return Enumerable.Empty<string>();

        return Newtonsoft.Json.JsonConvert.SerializeObject(value.ToString().Split(','));

    }
}