namespace Umbraco.Community.BulkUpload.Core.Resolvers;


public interface IResolverFactory
{
    IResolver? GetByAlias(string alias);
    IEnumerable<IResolver> GetAll();
}