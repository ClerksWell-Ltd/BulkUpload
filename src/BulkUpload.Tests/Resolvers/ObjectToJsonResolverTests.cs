using Umbraco.Community.BulkUpload.Core.Resolvers;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class ObjectToJsonResolverTests
{
    private readonly ObjectToJsonResolver _resolver;

    public ObjectToJsonResolverTests()
    {
        _resolver = new ObjectToJsonResolver();
    }

    [Fact]
    public void Alias_ReturnsCorrectAlias()
    {
        // Act
        var alias = _resolver.Alias();

        // Assert
        Assert.Equal("objectToJson", alias);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenValueIsNull()
    {
        // Act
        var result = _resolver.Resolve(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsSimpleObject()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };

        // Act
        var result = _resolver.Resolve(testObject);

        // Assert
        Assert.IsType<string>(result);
        var jsonString = result as string;
        Assert.Contains("\"Name\"", jsonString);
        Assert.Contains("\"Test\"", jsonString);
        Assert.Contains("\"Value\"", jsonString);
        Assert.Contains("123", jsonString);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsString()
    {
        // Arrange
        var testValue = "Test String";

        // Act
        var result = _resolver.Resolve(testValue);

        // Assert
        Assert.IsType<string>(result);
        Assert.Equal("\"Test String\"", result);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsNumber()
    {
        // Arrange
        var testValue = 42;

        // Act
        var result = _resolver.Resolve(testValue);

        // Assert
        Assert.IsType<string>(result);
        Assert.Equal("42", result);
    }

    [Fact]
    public void Resolve_ReturnsJsonArray_WhenValueIsArray()
    {
        // Arrange
        var testArray = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _resolver.Resolve(testArray);

        // Assert
        Assert.IsType<string>(result);
        var jsonString = result as string;
        Assert.StartsWith("[", jsonString);
        Assert.EndsWith("]", jsonString);
        Assert.Contains("1", jsonString);
        Assert.Contains("5", jsonString);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsComplexObject()
    {
        // Arrange
        var complexObject = new
        {
            Id = 1,
            Name = "Test",
            Nested = new
            {
                Property1 = "Value1",
                Property2 = 100
            },
            Items = new[] { "Item1", "Item2", "Item3" }
        };

        // Act
        var result = _resolver.Resolve(complexObject);

        // Assert
        Assert.IsType<string>(result);
        var jsonString = result as string;
        Assert.Contains("\"Id\"", jsonString);
        Assert.Contains("\"Name\"", jsonString);
        Assert.Contains("\"Nested\"", jsonString);
        Assert.Contains("\"Items\"", jsonString);
        Assert.Contains("\"Item1\"", jsonString);
    }

    [Fact]
    public void Resolve_ReturnsJsonBoolean_WhenValueIsBoolean()
    {
        // Arrange
        var testValue = true;

        // Act
        var result = _resolver.Resolve(testValue);

        // Assert
        Assert.IsType<string>(result);
        Assert.Equal("true", result);
    }
}
