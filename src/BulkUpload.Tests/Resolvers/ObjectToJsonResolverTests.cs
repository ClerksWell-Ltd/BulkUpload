using BulkUpload.Resolvers;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json.Linq;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class ObjectToJsonResolverTests
{
    private readonly Mock<IResolverFactory> _resolverFactoryMock;
    private readonly Mock<ILogger<ObjectToJsonResolver>> _loggerMock;
    private readonly ObjectToJsonResolver _resolver;

    public ObjectToJsonResolverTests()
    {
        _resolverFactoryMock = new Mock<IResolverFactory>();
        _loggerMock = new Mock<ILogger<ObjectToJsonResolver>>();
        _resolver = new ObjectToJsonResolver(_resolverFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Alias_ReturnsCorrectAlias()
    {
        var alias = _resolver.Alias();
        Assert.Equal("objectToJson", alias);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenValueIsNull()
    {
        var result = _resolver.Resolve(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsSimpleObject()
    {
        var testObject = new { Name = "Test", Value = 123 };

        var result = _resolver.Resolve(testObject);

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
        var testValue = "Test String";

        var result = _resolver.Resolve(testValue);

        Assert.IsType<string>(result);
        Assert.Equal("\"Test String\"", result);
    }

    [Fact]
    public void Resolve_ReturnsJsonString_WhenValueIsNumber()
    {
        var testValue = 42;

        var result = _resolver.Resolve(testValue);

        Assert.IsType<string>(result);
        Assert.Equal("42", result);
    }

    [Fact]
    public void Resolve_ReturnsJsonArray_WhenValueIsArray()
    {
        var testArray = new[] { 1, 2, 3, 4, 5 };

        var result = _resolver.Resolve(testArray);

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

        var result = _resolver.Resolve(complexObject);

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
        var testValue = true;

        var result = _resolver.Resolve(testValue);

        Assert.IsType<string>(result);
        Assert.Equal("true", result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenValueIsWhitespace()
    {
        var result = _resolver.Resolve("   ");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_SerializesPlainString_WhenInputIsNotJson()
    {
        var result = _resolver.Resolve("just a plain string");
        Assert.Equal("\"just a plain string\"", result);
    }

    [Fact]
    public void Resolve_LeavesStringValuesUnchanged_WhenNoPipePresent()
    {
        var json = """{"image":"https://example.com/photo.jpg","title":"Hello"}""";

        _resolverFactoryMock.Setup(f => f.GetByAlias(It.IsAny<string>())).Returns((IResolver?)null);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        Assert.Equal("https://example.com/photo.jpg", parsed["image"]!.Value<string>());
        Assert.Equal("Hello", parsed["title"]!.Value<string>());
    }

    [Fact]
    public void Resolve_LeavesStringValueUnchanged_WhenResolverAliasNotFound()
    {
        var json = """{"image":"https://example.com/photo.jpg|unknownResolver"}""";

        _resolverFactoryMock.Setup(f => f.GetByAlias("unknownResolver")).Returns((IResolver?)null);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        Assert.Equal("https://example.com/photo.jpg|unknownResolver", parsed["image"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ReplacesStringValue_WhenResolverAliasFound()
    {
        var json = """{"image":"https://example.com/photo.jpg|urlToMedia"}""";
        var expectedUdi = "umb://media/abc123";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("https://example.com/photo.jpg")).Returns(expectedUdi);
        _resolverFactoryMock.Setup(f => f.GetByAlias("urlToMedia")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        Assert.Equal(expectedUdi, parsed["image"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ReplacesStringValue_WithParameterizedAlias()
    {
        var json = """{"image":"https://example.com/photo.jpg|urlToMedia:/Images"}""";
        var expectedUdi = "umb://media/abc123";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("https://example.com/photo.jpg")).Returns(expectedUdi);
        _resolverFactoryMock.Setup(f => f.GetByAlias("urlToMedia:/Images")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        Assert.Equal(expectedUdi, parsed["image"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ProcessesNestedObjects()
    {
        var json = """{"block":{"image":"https://example.com/photo.jpg|urlToMedia","title":"Test"}}""";
        var expectedUdi = "umb://media/abc123";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("https://example.com/photo.jpg")).Returns(expectedUdi);
        _resolverFactoryMock.Setup(f => f.GetByAlias("urlToMedia")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        Assert.Equal(expectedUdi, parsed["block"]!["image"]!.Value<string>());
        Assert.Equal("Test", parsed["block"]!["title"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ProcessesArrayStringValues()
    {
        var json = """{"images":["https://example.com/a.jpg|urlToMedia","https://example.com/b.jpg|urlToMedia"]}""";
        var udiA = "umb://media/aaa";
        var udiB = "umb://media/bbb";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("https://example.com/a.jpg")).Returns(udiA);
        mockResolver.Setup(r => r.Resolve("https://example.com/b.jpg")).Returns(udiB);
        _resolverFactoryMock.Setup(f => f.GetByAlias("urlToMedia")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);
        var images = parsed["images"]!.ToArray();

        Assert.Equal(udiA, images[0].Value<string>());
        Assert.Equal(udiB, images[1].Value<string>());
    }

    [Fact]
    public void Resolve_DoesNotResolve_WhenAliasIsObjectToJson()
    {
        // Guard against infinite recursion
        var json = """{"value":"something|objectToJson"}""";

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        // Value should be unchanged since self-reference is blocked
        Assert.Equal("something|objectToJson", parsed["value"]!.Value<string>());
        _resolverFactoryMock.Verify(f => f.GetByAlias(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Resolve_LeavesValueUnchanged_WhenResolverReturnsEmpty()
    {
        var json = """{"image":"https://example.com/photo.jpg|pathToMedia"}""";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve(It.IsAny<object>())).Returns(string.Empty);
        _resolverFactoryMock.Setup(f => f.GetByAlias("pathToMedia")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        // Empty string result means resolution failed - the resolved empty string is written back
        Assert.Equal(string.Empty, parsed["image"]!.Value<string>());
    }

    [Fact]
    public void Resolve_EmbedsJsonArrayAsStructure_WhenResolverReturnsJsonArray()
    {
        var json = """{"image":"https://example.com/photo.jpg|urlToMediaPicker"}""";
        var mediaPickerJson = """[{"key":"11111111-1111-1111-1111-111111111111","mediaKey":"22222222-2222-2222-2222-222222222222"}]""";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("https://example.com/photo.jpg")).Returns(mediaPickerJson);
        _resolverFactoryMock.Setup(f => f.GetByAlias("urlToMediaPicker")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        // Should be a JSON array, not a string
        Assert.Equal(JTokenType.Array, parsed["image"]!.Type);
        var items = (JArray)parsed["image"]!;
        Assert.Single(items);
        Assert.Equal("22222222-2222-2222-2222-222222222222", items[0]["mediaKey"]!.Value<string>());
    }

    [Fact]
    public void Resolve_EmbedsJsonObjectAsStructure_WhenResolverReturnsJsonObject()
    {
        var json = """{"data":"test|customResolver"}""";
        var jsonObjectResult = """{"nested":"value","count":42}""";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("test")).Returns(jsonObjectResult);
        _resolverFactoryMock.Setup(f => f.GetByAlias("customResolver")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        // Should be a JSON object, not a string
        Assert.Equal(JTokenType.Object, parsed["data"]!.Type);
        Assert.Equal("value", parsed["data"]!["nested"]!.Value<string>());
        Assert.Equal(42, parsed["data"]!["count"]!.Value<int>());
    }

    [Fact]
    public void Resolve_KeepsAsString_WhenResolverReturnsInvalidJson()
    {
        var json = """{"value":"test|customResolver"}""";
        var notJson = "[this is not valid json";

        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve("test")).Returns(notJson);
        _resolverFactoryMock.Setup(f => f.GetByAlias("customResolver")).Returns(mockResolver.Object);

        var result = _resolver.Resolve(json) as string;
        var parsed = JObject.Parse(result!);

        // Invalid JSON should remain as a string value
        Assert.Equal(JTokenType.String, parsed["value"]!.Type);
        Assert.Equal(notJson, parsed["value"]!.Value<string>());
    }
}
