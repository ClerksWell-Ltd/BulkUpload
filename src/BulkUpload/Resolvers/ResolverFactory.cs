using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Resolvers;

public class ResolverFactory : IResolverFactory
{
    private readonly Dictionary<string, IResolver> _resolvers;

    public ResolverFactory(IEnumerable<IResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(r => r.Alias(), r => r, StringComparer.OrdinalIgnoreCase);
    }

    public IResolver? GetByAlias(string alias)
    {
        // Parse parameterized alias format: "aliasName:parameter"
        var parts = alias.Split(':', 2, StringSplitOptions.None);
        var baseAlias = parts[0];
        var parameter = parts.Length > 1 ? parts[1] : null;

        if (!_resolvers.TryGetValue(baseAlias, out var resolver))
        {
            return null;
        }

        // If there's a parameter, wrap the resolver to inject it
        if (!string.IsNullOrEmpty(parameter))
        {
            return new ParameterizedResolverWrapper(resolver, parameter);
        }

        return resolver;
    }

    public IEnumerable<IResolver> GetAll() => _resolvers.Values;

    /// <summary>
    /// Wrapper that injects a parameter into the value before passing to the underlying resolver.
    /// This allows resolvers to receive parameters from the alias without changing the IResolver interface.
    /// </summary>
    private class ParameterizedResolverWrapper : IResolver
    {
        private readonly IResolver _innerResolver;
        private readonly string _parameter;

        public ParameterizedResolverWrapper(IResolver innerResolver, string parameter)
        {
            _innerResolver = innerResolver;
            _parameter = parameter;
        }

        public string Alias() => _innerResolver.Alias();

        public object Resolve(object value)
        {
            // Wrap the value with the parameter
            var parameterizedValue = new ParameterizedValue
            {
                Value = value,
                Parameter = _parameter
            };

            return _innerResolver.Resolve(parameterizedValue);
        }
    }
}