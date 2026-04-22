using BulkUpload.Models;
using BulkUpload.Resolvers;
using BulkUpload.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json.Linq;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class ZipFileToMediaPickerResolverTests
{
    private readonly Mock<IMediaItemCache> _mediaItemCacheMock;
    private readonly ZipFileToMediaPickerResolver _resolver;

    public ZipFileToMediaPickerResolverTests()
    {
        _mediaItemCacheMock = new Mock<IMediaItemCache>();

        var zipFileToMediaResolver = new ZipFileToMediaResolver(
            _mediaItemCacheMock.Object,
            new Mock<ILogger<ZipFileToMediaResolver>>().Object);

        _resolver = new ZipFileToMediaPickerResolver(zipFileToMediaResolver);
    }

    [Fact]
    public void Alias_ReturnsZipFileToMediaPicker()
    {
        Assert.Equal("zipFileToMediaPicker", _resolver.Alias());
    }

    [Fact]
    public void Resolve_ReturnsMediaPicker3Format_WhenMediaIsCached()
    {
        var fileName = "hero-image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(fileName, out mediaGuid))
            .Returns(true);

        var result = _resolver.Resolve(fileName) as string;
        Assert.NotNull(result);

        var parsed = JArray.Parse(result);
        Assert.Single(parsed);
        Assert.Equal(mediaGuid.ToString(), parsed[0]["mediaKey"]!.Value<string>());
        Assert.NotNull(parsed[0]["key"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenMediaNotCached()
    {
        var fileName = "missing.jpg";
        var emptyGuid = Guid.Empty;

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(fileName, out emptyGuid))
            .Returns(false);

        // ZipFileToMediaResolver returns empty string when the file wasn't preprocessed,
        // which doesn't start with umb://media/ so the picker resolver passes it through.
        var result = _resolver.Resolve(fileName);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_ForNullInput()
    {
        var result = _resolver.Resolve(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_GeneratesUniqueKeyPerInvocation()
    {
        var fileName = "hero-image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(fileName, out mediaGuid))
            .Returns(true);

        var result1 = JArray.Parse((string)_resolver.Resolve(fileName));
        var result2 = JArray.Parse((string)_resolver.Resolve(fileName));

        var key1 = result1[0]["key"]!.Value<string>();
        var key2 = result2[0]["key"]!.Value<string>();
        Assert.NotEqual(key1, key2);

        Assert.Equal(result1[0]["mediaKey"]!.Value<string>(), result2[0]["mediaKey"]!.Value<string>());
    }

    [Fact]
    public void Resolve_PassesParameterizedValueThroughToUnderlyingResolver()
    {
        var parameterizedValue = new ParameterizedValue
        {
            Value = "hero-image.jpg",
            Parameter = "/Blog/Headers/"
        };
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid("hero-image.jpg", out mediaGuid))
            .Returns(true);

        var result = _resolver.Resolve(parameterizedValue) as string;
        Assert.NotNull(result);

        var parsed = JArray.Parse(result);
        Assert.Single(parsed);
        Assert.Equal(mediaGuid.ToString(), parsed[0]["mediaKey"]!.Value<string>());
    }
}
