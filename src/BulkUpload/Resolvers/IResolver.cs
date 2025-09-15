namespace Umbraco.Community.BulkUpload.Resolvers;

public interface IResolver
{
    string Alias();
    object Resolve(object value);
}