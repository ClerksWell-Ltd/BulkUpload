namespace Umbraco.Community.BulkUpload.Resolvers;

public class TextResolver : IResolver
{
    public string Alias() => "text";

    public object Resolve(object value)
    {
        if (value is string output) return output;

        if (value is not null) return value.ToString();

        return default(string);
    }
}