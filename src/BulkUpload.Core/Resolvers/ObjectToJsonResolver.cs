using Newtonsoft.Json;

namespace BulkUpload.Core.Resolvers;

public class ObjectToJsonResolver : IResolver
{
    public string Alias() => "objectToJson";

    public object Resolve(object value)
    {
        return value is not null ? JsonConvert.SerializeObject(value) : string.Empty;
    }
}