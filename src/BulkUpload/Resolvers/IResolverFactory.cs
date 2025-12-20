namespace Umbraco.Community.BulkUpload.Resolvers;


public interface IResolverFactory
{
    IResolver? GetByAlias(string alias);
    IEnumerable<IResolver> GetAll();
}