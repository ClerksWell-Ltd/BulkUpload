using Newtonsoft.Json;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class ObjectToJsonResolver : IResolver
{
    public string Alias() => "objectToJson";

    public object Resolve(object value)
    {
        if (value is not null) return JsonConvert.SerializeObject(value);

        return value;
    }
}