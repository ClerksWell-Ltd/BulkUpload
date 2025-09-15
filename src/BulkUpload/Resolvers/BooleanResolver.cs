namespace Umbraco.Community.BulkUpload.Resolvers;

public class BooleanResolver : IResolver
{
    public string Alias() => "boolean";

    public object Resolve(object value)
    {
        if (value == null) return default(bool);

        if (value is bool output) return output;

        if (bool.TryParse(value.ToString(), out var boolValue)) return boolValue;

        return default(bool);
    }
}