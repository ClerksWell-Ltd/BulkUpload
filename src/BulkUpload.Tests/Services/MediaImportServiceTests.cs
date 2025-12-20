using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Resolvers;
using Umbraco.Community.BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Tests.Services;

public class MediaImportServiceTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IMediaTypeService> _mockMediaTypeService;
    private readonly Mock<MediaFileManager> _mockMediaFileManager;
    private readonly Mock<IShortStringHelper> _mockShortStringHelper;
    private readonly Mock<IContentTypeBaseServiceProvider> _mockContentTypeBaseServiceProvider;
    private readonly Mock<IResolverFactory> _mockResolverFactory;
    private readonly Mock<ILogger<MediaImportService>> _mockLogger;
    private readonly MediaImportService _service;

    public MediaImportServiceTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockMediaTypeService = new Mock<IMediaTypeService>();
        _mockMediaFileManager = new Mock<MediaFileManager>();
        _mockShortStringHelper = new Mock<IShortStringHelper>();
        _mockContentTypeBaseServiceProvider = new Mock<IContentTypeBaseServiceProvider>();
        _mockResolverFactory = new Mock<IResolverFactory>();
        _mockLogger = new Mock<ILogger<MediaImportService>>();

        _service = new MediaImportService(
            _mockMediaService.Object,
            _mockMediaTypeService.Object,
            _mockMediaFileManager.Object,
            _mockShortStringHelper.Object,
            _mockContentTypeBaseServiceProvider.Object,
            _mockResolverFactory.Object,
            _mockLogger.Object
        );
    }

    #region CreateMediaImportObject Tests

    [Fact]
    public void CreateMediaImportObject_ThrowsArgumentNullException_WhenRecordIsNull()
    {
        // Arrange
        dynamic? record = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.CreateMediaImportObject(record));
    }

    [Fact]
    public void CreateMediaImportObject_ReturnsValidObject_WithRequiredFields()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.jpg", result.FileName);
        Assert.Equal(123, result.ParentId);
    }

    [Fact]
    public void CreateMediaImportObject_SetsName_WhenProvided()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "name", "My Test Image" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Equal("My Test Image", result.Name);
    }

    [Fact]
    public void CreateMediaImportObject_SetsNameToNull_WhenNotProvided()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Null(result.Name);
    }

    [Fact]
    public void CreateMediaImportObject_SetsMediaTypeAlias_WhenProvided()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "mediaTypeAlias", "Image" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Equal("Image", result.MediaTypeAlias);
    }

    [Fact]
    public void CreateMediaImportObject_ParsesParentIdAsZero_WhenInvalid()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "invalid" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Equal(0, result.ParentId);
    }

    [Fact]
    public void CreateMediaImportObject_HandlesEmptyFileName()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "" },
            { "parentId", "123" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Equal("", result.FileName);
    }

    [Fact]
    public void CreateMediaImportObject_ProcessesCustomProperties()
    {
        // Arrange
        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve(It.IsAny<object>())).Returns("resolved value");
        _mockResolverFactory.Setup(f => f.GetByAlias("text")).Returns(mockResolver.Object);

        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "altText", "Alt text value" },
            { "caption", "Caption value" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.NotNull(result.Properties);
        Assert.Equal(2, result.Properties.Count);
        Assert.Contains("altText", result.Properties.Keys);
        Assert.Contains("caption", result.Properties.Keys);
    }

    [Fact]
    public void CreateMediaImportObject_UsesResolverAlias_WhenSpecifiedInColumnName()
    {
        // Arrange
        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve(It.IsAny<object>())).Returns("resolved json");
        _mockResolverFactory.Setup(f => f.GetByAlias("json")).Returns(mockResolver.Object);

        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "customProperty|json", "{\"key\":\"value\"}" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        _mockResolverFactory.Verify(f => f.GetByAlias("json"), Times.Once);
    }

    #endregion

    #region ImportSingleMediaItem Tests

    [Fact]
    public void ImportSingleMediaItem_ReturnsFailure_WhenCanImportIsFalse()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "",
            ParentId = 0
        };
        using var stream = new MemoryStream();

        // Act
        var result = _service.ImportSingleMediaItem(importObject, stream);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Missing required fields", result.ErrorMessage);
    }

    [Fact]
    public void ImportSingleMediaItem_ReturnsFailure_WhenMediaTypeNotFound()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 123,
            MediaTypeAlias = "NonExistentType"
        };
        using var stream = new MemoryStream();

        _mockMediaTypeService.Setup(s => s.Get("NonExistentType")).Returns((IMediaType?)null);

        // Act
        var result = _service.ImportSingleMediaItem(importObject, stream);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Media type 'NonExistentType' not found", result.ErrorMessage);
    }

    [Fact]
    public void ImportSingleMediaItem_ReturnsFailure_WhenFileStreamIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 123
        };
        var mockMediaType = new Mock<IMediaType>();
        _mockMediaTypeService.Setup(s => s.Get("Image")).Returns(mockMediaType.Object);

        // Act
        var result = _service.ImportSingleMediaItem(importObject, null!);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File stream is null or empty", result.ErrorMessage);
    }

    [Fact]
    public void ImportSingleMediaItem_ReturnsFailure_WhenFileStreamIsEmpty()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 123
        };
        using var stream = new MemoryStream();
        var mockMediaType = new Mock<IMediaType>();
        _mockMediaTypeService.Setup(s => s.Get("Image")).Returns(mockMediaType.Object);

        // Act
        var result = _service.ImportSingleMediaItem(importObject, stream);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File stream is null or empty", result.ErrorMessage);
    }

    [Fact]
    public void ImportSingleMediaItem_DeterminesMediaTypeFromExtension_WhenNotProvided()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 123,
            MediaTypeAlias = null
        };
        using var stream = new MemoryStream();

        var mockMediaType = new Mock<IMediaType>();
        _mockMediaTypeService.Setup(s => s.Get("Image")).Returns(mockMediaType.Object);

        // Act
        var result = _service.ImportSingleMediaItem(importObject, stream);

        // Assert
        _mockMediaTypeService.Verify(s => s.Get("Image"), Times.Once);
    }

    [Theory]
    [InlineData("test.jpg", "Image")]
    [InlineData("test.png", "Image")]
    [InlineData("test.gif", "Image")]
    [InlineData("test.pdf", "File")]
    [InlineData("test.doc", "File")]
    [InlineData("test.mp4", "Video")]
    [InlineData("test.mp3", "Audio")]
    [InlineData("test.unknown", "File")]
    public void ImportSingleMediaItem_DeterminesCorrectMediaType_BasedOnExtension(string fileName, string expectedType)
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = fileName,
            ParentId = 123,
            MediaTypeAlias = null
        };
        using var stream = new MemoryStream();

        var mockMediaType = new Mock<IMediaType>();
        _mockMediaTypeService.Setup(s => s.Get(expectedType)).Returns(mockMediaType.Object);

        // Act
        var result = _service.ImportSingleMediaItem(importObject, stream);

        // Assert
        _mockMediaTypeService.Verify(s => s.Get(expectedType), Times.Once);
    }

    #endregion
}
