namespace BulkUpload.Core.Resolvers;

public interface IResolver
{
    string Alias();
    object Resolve(object value);
}