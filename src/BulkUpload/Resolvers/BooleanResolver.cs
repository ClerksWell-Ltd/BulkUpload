namespace BulkUpload.Resolvers;

public class BooleanResolver : IResolver
{
    public string Alias() => "boolean";

    public object Resolve(object value)
    {
        if (value is null)
            return false;

        if (value is bool output)
            return output;

        if (value is string str && bool.TryParse(str, out var boolValue))
            return boolValue;

        return false;
    }
}