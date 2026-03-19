using BulkUpload.Resolvers;
using BulkUpload.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class UrlToMediaPickerResolverTests
{
    private readonly Mock<IMediaItemCache> _mediaItemCacheMock;
    private readonly UrlToMediaPickerResolver _resolver;

    public UrlToMediaPickerResolverTests()
    {
        _mediaItemCacheMock = new Mock<IMediaItemCache>();

        var urlToMediaResolver = new UrlToMediaResolver(
            new Mock<IHttpClientFactory>().Object,
            new Mock<IMediaService>().Object,
            null!, // MediaFileManager - sealed class, cannot be mocked
            new Mock<IMediaTypeService>().Object,
            new Mock<IShortStringHelper>().Object,
            new Mock<IContentTypeBaseServiceProvider>().Object,
            new Mock<IParentLookupCache>().Object,
            _mediaItemCacheMock.Object,
            new Mock<ILogger<UrlToMediaResolver>>().Object);

        _resolver = new UrlToMediaPickerResolver(urlToMediaResolver);
    }

    [Fact]
    public void Alias_ReturnsUrlToMediaPicker()
    {
        Assert.Equal("urlToMediaPicker", _resolver.Alias());
    }

    [Fact]
    public void Resolve_ReturnsMediaPicker3Format_WhenMediaIsCached()
    {
        var url = "https://example.com/image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(url, out mediaGuid))
            .Returns(true);

        var result = _resolver.Resolve(url) as string;
        Assert.NotNull(result);

        var parsed = JArray.Parse(result);
        Assert.Single(parsed);
        Assert.Equal(mediaGuid.ToString(), parsed[0]["mediaKey"]!.Value<string>());
        Assert.NotNull(parsed[0]["key"]!.Value<string>());
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenMediaNotCachedAndDownloadFails()
    {
        var url = "https://example.com/missing.jpg";
        var emptyGuid = Guid.Empty;

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(url, out emptyGuid))
            .Returns(false);

        var result = _resolver.Resolve(url);

        // UrlToMediaResolver returns empty string on failure, which doesn't start with umb://media/
        // so the picker resolver passes it through
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
        var url = "https://example.com/image.jpg";
        var mediaGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _mediaItemCacheMock
            .Setup(c => c.TryGetGuid(url, out mediaGuid))
            .Returns(true);

        var result1 = JArray.Parse((string)_resolver.Resolve(url));
        var result2 = JArray.Parse((string)_resolver.Resolve(url));

        // Each invocation should generate a unique element key
        var key1 = result1[0]["key"]!.Value<string>();
        var key2 = result2[0]["key"]!.Value<string>();
        Assert.NotEqual(key1, key2);

        // But the mediaKey should be the same
        Assert.Equal(result1[0]["mediaKey"]!.Value<string>(), result2[0]["mediaKey"]!.Value<string>());
    }
}
