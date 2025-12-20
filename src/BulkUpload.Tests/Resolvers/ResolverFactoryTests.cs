using Umbraco.Community.BulkUpload.Resolvers;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class ResolverFactoryTests
{
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

        // Act
        var factory = new ResolverFactory(resolvers);
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
        var factory = new ResolverFactory(resolvers);

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
        var factory = new ResolverFactory(resolvers);

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
        var factory = new ResolverFactory(resolvers);

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
        var factory = new ResolverFactory(resolvers);

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

        // Act
        var factory = new ResolverFactory(resolvers);
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
        var factory = new ResolverFactory(resolvers);

        // Act
        var resolver = factory.GetByAlias("anyAlias");

        // Assert
        Assert.Null(resolver);
    }
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
