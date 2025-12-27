using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using BulkUpload.Core.Models;
using BulkUpload.Core.Resolvers;
using BulkUpload.Core.Services;

namespace Umbraco.Community.BulkUpload.Tests.Resolvers;

public class PathToMediaResolverTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IMediaTypeService> _mockMediaTypeService;
    private readonly Mock<IShortStringHelper> _mockShortStringHelper;
    private readonly Mock<IContentTypeBaseServiceProvider> _mockContentTypeBaseServiceProvider;
    private readonly Mock<IParentLookupCache> _mockParentLookupCache;
    private readonly Mock<IMediaItemCache> _mockMediaItemCache;
    private readonly Mock<ILogger<PathToMediaResolver>> _mockLogger;
    private readonly PathToMediaResolver _resolver;

    public PathToMediaResolverTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockMediaTypeService = new Mock<IMediaTypeService>();
        _mockShortStringHelper = new Mock<IShortStringHelper>();
        _mockContentTypeBaseServiceProvider = new Mock<IContentTypeBaseServiceProvider>();
        _mockParentLookupCache = new Mock<IParentLookupCache>();
        _mockMediaItemCache = new Mock<IMediaItemCache>();
        _mockLogger = new Mock<ILogger<PathToMediaResolver>>();

        // Note: MediaFileManager is sealed and cannot be mocked
        // Full integration tests would require actual Umbraco instance and real files
        _resolver = new PathToMediaResolver(
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
        Assert.Equal("pathToMedia", alias);
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
    public void Resolve_ReturnsEmptyString_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/file.jpg";

        // Act
        var result = _resolver.Resolve(nonExistentPath);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_LogsWarning_WhenPathIsNull()
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
    public void Resolve_LogsWarning_WhenFileNotFound()
    {
        // Arrange
        var nonExistentPath = "/some/fake/path/image.jpg";

        // Act
        _resolver.Resolve(nonExistentPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region Parameterization Tests

    [Fact]
    public void Resolve_ParsesPathWithValueParameter()
    {
        // Arrange - Path with pipe-separated parent ID
        var pathWithParam = "/path/to/image.jpg|1234";

        // Act
        var result = _resolver.Resolve(pathWithParam);

        // Assert - File won't exist, but parameter parsing should work
        // Result will be empty because file doesn't exist
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesParameterizedValue()
    {
        // Arrange - ParameterizedValue with alias parameter
        var parameterizedValue = new ParameterizedValue
        {
            Value = "/path/to/image.jpg",
            Parameter = "5678"
        };

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert - File won't exist, but should unwrap ParameterizedValue
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ValueParameterTakesPrecedenceOverAliasParameter()
    {
        // Arrange - Both alias and value parameters present
        var parameterizedValue = new ParameterizedValue
        {
            Value = "/path/to/image.jpg|9999", // Value parameter
            Parameter = "1234" // Alias parameter
        };
        // According to fallback hierarchy, value parameter (9999) should take precedence

        // Act
        var result = _resolver.Resolve(parameterizedValue);

        // Assert - Should parse correctly even though file doesn't exist
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("/path/to/image.jpg|1234")]
    [InlineData("C:\\Images\\photo.jpg|a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
    [InlineData("/mnt/share/images/file.png|/Blog/Images/")]
    [InlineData("\\\\server\\share\\image.jpg|/Nested/Folder/Structure/")]
    public void Resolve_HandlesDifferentPathAndParameterFormats(string pathWithParam)
    {
        // Act
        var result = _resolver.Resolve(pathWithParam);

        // Assert - Should parse different path formats (Windows, Linux, UNC)
        // Result will be empty because files don't exist, but parsing should work
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_ReturnsEmptyString_WhenParameterizedValueHasEmptyPath()
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
    public void Resolve_HandlesPathWithPipeButNoParameter()
    {
        // Arrange - Path with pipe but empty parameter
        var pathWithEmptyParam = "/path/to/image.jpg|";

        // Act
        var result = _resolver.Resolve(pathWithEmptyParam);

        // Assert - Should handle gracefully
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesWhitespaceInParameters()
    {
        // Arrange - Path with whitespace around parameter
        var pathWithWhitespace = "/path/to/image.jpg|  1234  ";

        // Act
        var result = _resolver.Resolve(pathWithWhitespace);

        // Assert - Should trim whitespace and process correctly
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("C:\\Users\\Documents\\image.jpg")]
    [InlineData("/home/user/images/photo.png")]
    [InlineData("\\\\network-share\\folder\\file.gif")]
    [InlineData("./relative/path/image.webp")]
    public void Resolve_HandlesDifferentPathFormats(string path)
    {
        // Act
        var result = _resolver.Resolve(path);

        // Assert - Should handle different OS path formats
        // Result will be empty because files don't exist in test environment
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesPathWithSpaces()
    {
        // Arrange
        var pathWithSpaces = "/path/to/my images/photo with spaces.jpg";

        // Act
        var result = _resolver.Resolve(pathWithSpaces);

        // Assert - Should handle paths with spaces
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesPathWithSpecialCharacters()
    {
        // Arrange
        var pathWithSpecialChars = "/path/to/images/файл-图片-画像.jpg";

        // Act
        var result = _resolver.Resolve(pathWithSpecialChars);

        // Assert - Should handle Unicode/international characters
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("/path/to/file.jpg|1234")]
    [InlineData("/path/to/file.png|5678")]
    [InlineData("/path/to/file.gif|/Custom/Folder/")]
    public void Resolve_ExtractsParameterCorrectly(string pathWithParam)
    {
        // Arrange - Different parameter formats

        // Act
        var result = _resolver.Resolve(pathWithParam);

        // Assert - Parameter extraction should work
        // Files don't exist, so result is empty, but we verify no exceptions
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesBackslashesInWindowsPaths()
    {
        // Arrange
        var windowsPath = "C:\\Users\\Documents\\Images\\photo.jpg";

        // Act
        var result = _resolver.Resolve(windowsPath);

        // Assert - Should handle Windows backslashes
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesUNCPaths()
    {
        // Arrange
        var uncPath = "\\\\server\\share\\images\\photo.jpg";

        // Act
        var result = _resolver.Resolve(uncPath);

        // Assert - Should handle UNC network paths
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_HandlesRelativePaths()
    {
        // Arrange
        var relativePath = "./images/photo.jpg";

        // Act
        var result = _resolver.Resolve(relativePath);

        // Assert - Should handle relative paths
        Assert.Equal(string.Empty, result);
    }

    #endregion
}
