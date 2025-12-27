using Newtonsoft.Json;

using Umbraco.Cms.Core.Web;
namespace BulkUpload.Core.Resolvers;

public class StringArrayResolver : IResolver
{
    private readonly IUmbracoContextFactory _contextFactory;

    public StringArrayResolver(IUmbracoContextFactory contextFactory)
        => _contextFactory = contextFactory;

    public string Alias() => "stringArray";

    public object Resolve(object value)
    {
        using var contextReference = _contextFactory.EnsureUmbracoContext();

        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return string.Empty;

        return JsonConvert.SerializeObject(str.Split(',').Select(s => s.Trim()));
    }
}