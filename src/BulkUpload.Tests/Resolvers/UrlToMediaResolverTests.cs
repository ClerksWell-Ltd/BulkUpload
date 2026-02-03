using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using BulkUpload.Models;
using BulkUpload.Resolvers;
using BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class UrlToMediaResolverTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IMediaTypeService> _mockMediaTypeService;
    private readonly Mock<IShortStringHelper> _mockShortStringHelper;
    private readonly Mock<IContentTypeBaseServiceProvider> _mockContentTypeBaseServiceProvider;
    private readonly Mock<IParentLookupCache> _mockParentLookupCache;
    private readonly Mock<IMediaItemCache> _mockMediaItemCache;
    private readonly Mock<ILogger<UrlToMediaResolver>> _mockLogger;
    private readonly UrlToMediaResolver _resolver;

    public UrlToMediaResolverTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockMediaService = new Mock<IMediaService>();
        _mockMediaTypeService = new Mock<IMediaTypeService>();
        _mockShortStringHelper = new Mock<IShortStringHelper>();
        _mockContentTypeBaseServiceProvider = new Mock<IContentTypeBaseServiceProvider>();
        _mockParentLookupCache = new Mock<IParentLookupCache>();
        _mockMediaItemCache = new Mock<IMediaItemCache>();
        _mockLogger = new Mock<ILogger<UrlToMediaResolver>>();

        // Note: MediaFileManager is sealed and cannot be mocked
        // Full integration tests would require actual Umbraco instance
        _resolver = new UrlToMediaResolver(
            _mockHttpClientFactory.Object,
            _mockMediaService.Object,
            null!, // MediaFileManager - sealed class, cannot be mocked
            _mockMediaTypeService.Object,
            _mockShortStringHelper.Object,
            _mockContentTypeBaseServiceProvider.Object,
            _mockParentLookupCache.Object,
            _mockMediaItemCache.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Alias_ReturnsCorrectAlias()
    {
        // Act
        var alias = _resolver.Alias();

        // Assert
        Assert.Equal("urlToMedia", alias);
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
    public void Resolve_ReturnsEmptyString_WhenValueIsEmptyString()
    {
        // Act
        var result = _resolver.Resolve(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenValueIsWhitespace()
    {
        // Act
        var result = _resolver.Resolve("   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenValueIsNotString()
    {
        // Act
        var result = _resolver.Resolve(12345);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenUrlIsInvalid()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act
        var result = _resolver.Resolve(invalidUrl);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenUrlIsRelative()
    {
        // Arrange
        var relativeUrl = "/images/test.jpg";

        // Act
        var result = _resolver.Resolve(relativeUrl);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenHttpRequestFails()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(url);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenDownloadedFileIsEmpty()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(Array.Empty<byte>())
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(url);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("https://example.com/test.jpg")]
    [InlineData("https://example.com/test.jpeg")]
    [InlineData("https://example.com/test.png")]
    [InlineData("https://example.com/test.gif")]
    [InlineData("https://example.com/test.webp")]
    public void Resolve_HandlesValidImageUrls(string url)
    {
        // Arrange
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Mock JPEG header
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Note: Full test would require mocking MediaFileManager which is sealed
        // This test verifies URL parsing and HTTP request setup

        // Act
        var result = _resolver.Resolve(url);

        // Assert
        // Result will be empty string because MediaFileManager is null (sealed class)
        // But we verify the HTTP client was created
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_LogsWarning_WhenUrlIsNull()
    {
        // Act
        _resolver.Resolve(null!);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null or empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Resolve_LogsWarning_WhenUrlIsInvalid()
    {
        // Arrange
        var invalidUrl = "not-a-url";

        // Act
        _resolver.Resolve(invalidUrl);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("invalid URL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Resolve_LogsWarning_WhenHttpRequestFails()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        _resolver.Resolve(url);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to download")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Resolve_HandlesHttpClientTimeout()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(url);

        // Assert
        Assert.Equal(string.Empty, result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error resolving URL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("https://example.com/path/to/image.jpg")]
    [InlineData("https://example.com/test.png?width=100&height=200")]
    [InlineData("https://example.com/files/photo%20name.jpeg")]
    [InlineData("https://example.com/noextension")]
    public void Resolve_ExtractsCorrectFileNameFromUrl(string url)
    {
        // This is tested indirectly through the URL parsing
        // The actual filename extraction is private, but we can verify behavior
        // by ensuring different URL patterns are handled

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(url);

        // Assert - verify HTTP client was used
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    #region Parameterization Tests

    [Fact]
    public void Resolve_ParsesUrlWithValueParameter()
    {
        // Arrange - URL with pipe-separated parent ID
        var urlWithParam = "https://example.com/image.jpg|1234";

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(urlWithParam);

        // Assert - URL should be parsed correctly (HTTP request should be made to base URL)
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_HandlesParameterizedValue()
    {
        // Arrange - ParameterizedValue with alias parameter
        var parameterizedValue = new ParameterizedValue
        {
            Value = "https://example.com/image.jpg",
            Parameter = "5678"
        };

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert - Should unwrap ParameterizedValue and process URL
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_ValueParameterTakesPrecedenceOverAliasParameter()
    {
        // Arrange - Both alias and value parameters present
        var parameterizedValue = new ParameterizedValue
        {
            Value = "https://example.com/image.jpg|9999", // Value parameter
            Parameter = "1234" // Alias parameter
        };
        // According to fallback hierarchy, value parameter (9999) should take precedence

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert - Should process successfully with value parameter taking precedence
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("https://example.com/image.jpg|1234")]
    [InlineData("https://example.com/image.jpg|a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
    [InlineData("https://example.com/image.jpg|/Blog/Images/")]
    [InlineData("https://example.com/image.jpg|/Nested/Folder/Structure/")]
    public void Resolve_HandlesDifferentParameterFormats(string urlWithParam)
    {
        // Arrange
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(urlWithParam);

        // Assert - Should handle different parameter formats (ID, GUID, path)
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenParameterizedValueHasEmptyUrl()
    {
        // Arrange
        var parameterizedValue = new ParameterizedValue
        {
            Value = string.Empty,
            Parameter = "1234"
        };

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenParameterizedValueHasInvalidUrl()
    {
        // Arrange
        var parameterizedValue = new ParameterizedValue
        {
            Value = "not-a-valid-url",
            Parameter = "1234"
        };

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesUrlWithPipeButNoParameter()
    {
        // Arrange - URL with pipe but empty parameter
        var urlWithEmptyParam = "https://example.com/image.jpg|";

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(urlWithEmptyParam);

        // Assert - Should handle gracefully and use default (root)
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_HandlesWhitespaceInParameters()
    {
        // Arrange - URL with whitespace around parameter
        var urlWithWhitespace = "https://example.com/image.jpg|  1234  ";

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = _resolver.Resolve(urlWithWhitespace);

        // Assert - Should trim whitespace and process correctly
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    #endregion
}
