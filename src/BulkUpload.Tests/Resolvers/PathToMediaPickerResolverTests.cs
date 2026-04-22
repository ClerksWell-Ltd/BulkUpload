using BulkUpload.Models;
using BulkUpload.Resolvers;
using BulkUpload.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class PathToMediaPickerResolverTests
{
    private readonly Mock<IMediaItemCache> _mediaItemCacheMock;
    private readonly PathToMediaPickerResolver _resolver;

    public PathToMediaPickerResolverTests()
    {
        _mediaItemCacheMock = new Mock<IMediaItemCache>();

        var pathToMediaResolver = new PathToMediaResolver(
            new Mock<IMediaService>().Object,
            null!, // MediaFileManager - sealed class, cannot be mocked
            new Mock<IMediaTypeService>().Object,
            new Mock<IShortStringHelper>().Object,
            new Mock<IContentTypeBaseServiceProvider>().Object,
            new Mock<IParentLookupCache>().Object,
            _mediaItemCacheMock.Object,
            new Mock<ILogger<PathToMediaResolver>>().Object);

        _resolver = new PathToMediaPickerResolver(pathToMediaResolver);
    }

    [Fact]
    public void Alias_ReturnsPathToMediaPicker()
    {
        Assert.Equal("pathToMediaPicker", _resolver.Alias());
    }

    [Fact]
    public void Resolve_ReturnsMediaPicker3Format_WhenMediaIsCached()
    {
        var path = "C:/temp/image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(path, out mediaGuid))
            .Returns(true);

        var result = _resolver.Resolve(path) as string;
        Assert.NotNull(result);

        var parsed = JArray.Parse(result);
        Assert.Single(parsed);
        Assert.Equal(mediaGuid.ToString(), parsed[0]["mediaKey"]!.Value<string>());
        Assert.NotNull(parsed[0]["key"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenFileDoesNotExistAndNotCached()
    {
        var path = "/path/to/nonexistent/image.jpg";
        var emptyGuid = Guid.Empty;

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(path, out emptyGuid))
            .Returns(false);

        // PathToMediaResolver returns empty string when file not found, which doesn't start
        // with umb://media/ so the picker resolver passes it through unchanged.
        var result = _resolver.Resolve(path);

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
        var path = "C:/temp/image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(path, out mediaGuid))
            .Returns(true);

        var result1 = JArray.Parse((string)_resolver.Resolve(path));
        var result2 = JArray.Parse((string)_resolver.Resolve(path));

        var key1 = result1[0]["key"]!.Value<string>();
        var key2 = result2[0]["key"]!.Value<string>();
        Assert.NotEqual(key1, key2);

        Assert.Equal(result1[0]["mediaKey"]!.Value<string>(), result2[0]["mediaKey"]!.Value<string>());
    }

    [Fact]
    public void Resolve_PassesParameterizedValueThroughToUnderlyingResolver()
    {
        // ParameterizedValue wraps the value when the resolver alias has a parameter like
        // `pathToMediaPicker:/Folder/`. The wrapper should pass it through so the underlying
        // resolver can still see and use the parent folder parameter.
        var parameterizedValue = new ParameterizedValue
        {
            Value = "/path/to/nonexistent/image.jpg",
            Parameter = "/Blog/Headers/"
        };
        var emptyGuid = Guid.Empty;

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(It.IsAny<string>(), out emptyGuid))
            .Returns(false);

        var result = _resolver.Resolve(parameterizedValue);

        // File doesn't exist so underlying resolver returns empty string — the wrapper
        // passes the empty string through without attempting to wrap it.
        Assert.Equal(string.Empty, result);
    }
}
