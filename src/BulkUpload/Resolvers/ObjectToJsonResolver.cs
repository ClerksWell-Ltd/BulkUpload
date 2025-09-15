using Newtonsoft.Json;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class ObjectToJsonResolver : IResolver
{
    public string Alias() => "objectToJson";

    public object Resolve(object value)
    {
        return value is not null ? JsonConvert.SerializeObject(value) : string.Empty;
    }
}