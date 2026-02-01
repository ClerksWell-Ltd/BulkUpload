using BulkUpload.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BulkUpload.Core.Resolvers;

public class ResolverFactory : IResolverFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Dictionary<string, Type>> _resolverTypes;

    public ResolverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Lazy initialization to avoid DI validation issues with transient resolvers
        _resolverTypes = new Lazy<Dictionary<string, Type>>(() =>
        {
            var resolvers = serviceProvider.GetServices<IResolver>();
            return resolvers.ToDictionary(r => r.Alias(), r => r.GetType(), StringComparer.OrdinalIgnoreCase);
        });
    }

    public IResolver? GetByAlias(string alias)
    {
        // Parse parameterized alias format: "aliasName:parameter"
        var parts = alias.Split(':', 2, StringSplitOptions.None);
        var baseAlias = parts[0];
        var parameter = parts.Length > 1 ? parts[1] : null;

        if (!_resolverTypes.Value.TryGetValue(baseAlias, out var resolverType))
        {
            return null;
        }

        // Resolve the resolver from the service provider (respects lifetime)
        var resolver = (IResolver)_serviceProvider.GetRequiredService(resolverType);

        // If there's a parameter, wrap the resolver to inject it
        if (!string.IsNullOrEmpty(parameter))
        {
            return new ParameterizedResolverWrapper(resolver, parameter);
        }

        return resolver;
    }

    public IEnumerable<IResolver> GetAll()
    {
        // Get all registered IResolver services directly from the service provider
        return _serviceProvider.GetServices<IResolver>();
    }

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