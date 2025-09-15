namespace Umbraco.Community.BulkUpload.Resolvers;

public interface IComplexResolver : IResolver
{
    object Resolve(string propertyName, IDictionary<string, object> value);
}