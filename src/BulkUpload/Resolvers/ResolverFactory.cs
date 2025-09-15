namespace Umbraco.Community.BulkUpload.Resolvers;

public class ResolverFactory : IResolverFactory
{
    private readonly Dictionary<string, IResolver> _resolvers;

    public ResolverFactory(IEnumerable<IResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(r => r.Alias(), r => r, StringComparer.OrdinalIgnoreCase);
    }

    public IResolver GetByAlias(string alias)
    {
        return _resolvers.TryGetValue(alias, out var resolver) ? resolver : null;
    }

    public IEnumerable<IResolver> GetAll() => _resolvers.Values;
}