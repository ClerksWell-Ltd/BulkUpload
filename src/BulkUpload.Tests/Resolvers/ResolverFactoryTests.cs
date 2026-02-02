using BulkUpload.Core.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class ResolverFactoryTests
{
    private static IServiceProvider CreateServiceProvider(List<IResolver> resolvers)
    {
        var services = new ServiceCollection();

        // Group resolvers by type to handle multiple instances of same type
        var resolversByType = resolvers.GroupBy(r => r.GetType());

        foreach (var group in resolversByType)
        {
            var resolverType = group.Key;
            var instances = group.ToList();

            if (instances.Count == 1)
            {
                // Single instance: register by concrete type and as IResolver
                var instance = instances[0];
                services.AddSingleton(resolverType, instance);
                services.AddSingleton<IResolver>(sp => (IResolver)sp.GetRequiredService(resolverType));
            }
            else
            {
                // Multiple instances of same type: register each directly as IResolver
                foreach (var instance in instances)
                {
                    services.AddSingleton<IResolver>(instance);
                }
            }
        }

        return services.BuildServiceProvider();
    }
    [Fact]
    public void Constructor_InitializesWithResolvers()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new ObjectToJsonResolver(),
            new TestResolver("test1"),
            new TestResolver("test2")
        };
        var serviceProvider = CreateServiceProvider(resolvers);

        // Act
        var factory = new ResolverFactory(serviceProvider);
        var allResolvers = factory.GetAll();

        // Assert
        Assert.NotNull(allResolvers);
        Assert.Equal(3, allResolvers.Count());
    }

    [Fact]
    public void GetByAlias_ReturnsCorrectResolver_WhenAliasExists()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new ObjectToJsonResolver(),
            new TestResolver("test1"),
            new TestResolver("test2")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var resolver = factory.GetByAlias("objectToJson");

        // Assert
        Assert.NotNull(resolver);
        Assert.IsType<ObjectToJsonResolver>(resolver);
    }

    [Fact]
    public void GetByAlias_ReturnsNull_WhenAliasDoesNotExist()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new ObjectToJsonResolver()
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var resolver = factory.GetByAlias("nonExistent");

        // Assert
        Assert.Null(resolver);
    }

    [Fact]
    public void GetByAlias_IsCaseInsensitive()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new ObjectToJsonResolver()
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var resolver1 = factory.GetByAlias("objectToJson");
        var resolver2 = factory.GetByAlias("OBJECTTOJSON");
        var resolver3 = factory.GetByAlias("ObjectToJson");

        // Assert
        Assert.NotNull(resolver1);
        Assert.NotNull(resolver2);
        Assert.NotNull(resolver3);
        Assert.Same(resolver1, resolver2);
        Assert.Same(resolver1, resolver3);
    }

    [Fact]
    public void GetAll_ReturnsAllResolvers()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new ObjectToJsonResolver(),
            new TestResolver("test1"),
            new TestResolver("test2"),
            new TestResolver("test3")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var allResolvers = factory.GetAll().ToList();

        // Assert
        Assert.Equal(4, allResolvers.Count);
        Assert.Contains(allResolvers, r => r.Alias() == "objectToJson");
        Assert.Contains(allResolvers, r => r.Alias() == "test1");
        Assert.Contains(allResolvers, r => r.Alias() == "test2");
        Assert.Contains(allResolvers, r => r.Alias() == "test3");
    }

    [Fact]
    public void Constructor_HandlesEmptyResolverList()
    {
        // Arrange
        var resolvers = new List<IResolver>();
        var serviceProvider = CreateServiceProvider(resolvers);

        // Act
        var factory = new ResolverFactory(serviceProvider);
        var allResolvers = factory.GetAll();

        // Assert
        Assert.NotNull(allResolvers);
        Assert.Empty(allResolvers);
    }

    [Fact]
    public void GetByAlias_ReturnsNull_WhenFactoryIsEmpty()
    {
        // Arrange
        var resolvers = new List<IResolver>();
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var resolver = factory.GetByAlias("anyAlias");

        // Assert
        Assert.Null(resolver);
    }

    #region Parameterized Alias Tests

    [Fact]
    public void GetByAlias_ParsesParameterizedAlias()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new TestResolver("test")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act - Request with parameter
        var resolver = factory.GetByAlias("test:1234");

        // Assert - Should return a resolver (wrapped)
        Assert.NotNull(resolver);
        Assert.Equal("test", resolver.Alias()); // Wrapper returns inner resolver's alias
    }

    [Fact]
    public void GetByAlias_ReturnsNull_WhenParameterizedAliasNotFound()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new TestResolver("test")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act - Request non-existent resolver with parameter
        var resolver = factory.GetByAlias("nonexistent:1234");

        // Assert
        Assert.Null(resolver);
    }

    [Fact]
    public void GetByAlias_HandlesMultipleColonsInParameter()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new TestResolver("test")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act - Parameter contains colon (like a URL or path)
        var resolver = factory.GetByAlias("test:/Blog/Images:/Subfolder/");

        // Assert - Should parse only first colon as separator
        Assert.NotNull(resolver);
        Assert.Equal("test", resolver.Alias());
    }

    [Fact]
    public void GetByAlias_ReturnsUnwrappedResolver_WhenNoParameter()
    {
        // Arrange
        var testResolver = new TestResolver("test");
        var resolvers = new List<IResolver> { testResolver };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act - No parameter
        var resolver = factory.GetByAlias("test");

        // Assert - Should return original resolver, not wrapped
        Assert.NotNull(resolver);
        Assert.Same(testResolver, resolver); // Same instance
    }

    [Fact]
    public void GetByAlias_ParameterizedAliasIsCaseInsensitive()
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new TestResolver("test")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act - Different cases
        var resolver1 = factory.GetByAlias("test:1234");
        var resolver2 = factory.GetByAlias("TEST:1234");
        var resolver3 = factory.GetByAlias("Test:1234");

        // Assert - All should resolve
        Assert.NotNull(resolver1);
        Assert.NotNull(resolver2);
        Assert.NotNull(resolver3);
    }

    [Theory]
    [InlineData("test:")]
    [InlineData("test: ")]
    [InlineData("test:   ")]
    public void GetByAlias_HandlesEmptyOrWhitespaceParameter(string aliasWithParam)
    {
        // Arrange
        var resolvers = new List<IResolver>
        {
            new TestResolver("test")
        };
        var serviceProvider = CreateServiceProvider(resolvers);
        var factory = new ResolverFactory(serviceProvider);

        // Act
        var resolver = factory.GetByAlias(aliasWithParam);

        // Assert - Should still resolve (parameter is empty/whitespace)
        Assert.NotNull(resolver);
    }

    #endregion
}

internal class TestResolver : IResolver
{
    private readonly string _alias;

    public TestResolver(string alias)
    {
        _alias = alias;
    }

    public string Alias() => _alias;

    public object Resolve(object value) => value;
}
