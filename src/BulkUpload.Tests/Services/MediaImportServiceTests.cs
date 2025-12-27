using Microsoft.Extensions.Logging;
using Moq;
using BulkUpload.Core.Models;
using BulkUpload.Core.Resolvers;
using BulkUpload.Core.Services;

namespace Umbraco.Community.BulkUpload.Tests.Services;

public class MediaImportServiceTests
{
    private readonly Mock<IResolverFactory> _mockResolverFactory;
    private readonly Mock<IParentLookupCache> _mockParentLookupCache;
    private readonly Mock<ILogger<MediaImportService>> _mockLogger;
    private readonly MediaImportService _service;

    public MediaImportServiceTests()
    {
        _mockResolverFactory = new Mock<IResolverFactory>();
        _mockParentLookupCache = new Mock<IParentLookupCache>();
        _mockLogger = new Mock<ILogger<MediaImportService>>();

        // Note: We can only test CreateMediaImportObject as it doesn't require sealed classes
        // ImportSingleMediaItem tests are omitted due to MediaFileManager being sealed and unmockable
        _service = new MediaImportService(
            null!, // IMediaService - not needed for CreateMediaImportObject tests
            null!, // IMediaTypeService - not needed for CreateMediaImportObject tests
            null!, // MediaFileManager - sealed class, unmockable
            null!, // MediaUrlGeneratorCollection - not needed for CreateMediaImportObject tests
            null!, // IShortStringHelper - not needed for CreateMediaImportObject tests
            null!, // IContentTypeBaseServiceProvider - not needed for CreateMediaImportObject tests
            _mockResolverFactory.Object,
            _mockParentLookupCache.Object,
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
        Assert.Equal("123", result.Parent);
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
    public void CreateMediaImportObject_ParsesParentAsNull_WhenInvalid()
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
        Assert.Null(result.Parent);
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

    [Fact]
    public void CreateMediaImportObject_SetsBulkUploadLegacyId_WhenProvided()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "bulkUploadLegacyId", "legacy-789" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.Equal("legacy-789", result.BulkUploadLegacyId);
    }

    [Fact]
    public void CreateMediaImportObject_SetsBulkUploadLegacyIdToNull_WhenNotProvided()
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
        Assert.Null(result.BulkUploadLegacyId);
    }

    [Fact]
    public void CreateMediaImportObject_DoesNotTreatBulkUploadLegacyIdAsProperty()
    {
        // Arrange
        var mockResolver = new Mock<IResolver>();
        mockResolver.Setup(r => r.Resolve(It.IsAny<object>())).Returns("resolved value");
        _mockResolverFactory.Setup(f => f.GetByAlias("text")).Returns(mockResolver.Object);

        var record = new Dictionary<string, object>
        {
            { "fileName", "test.jpg" },
            { "parentId", "123" },
            { "bulkUploadLegacyId", "legacy-abc" },
            { "altText", "Alt text value" }
        };

        // Act
        var result = _service.CreateMediaImportObject(record);

        // Assert
        Assert.NotNull(result.Properties);
        Assert.Single(result.Properties); // Only altText, not bulkUploadLegacyId
        Assert.Contains("altText", result.Properties.Keys);
        Assert.DoesNotContain("bulkUploadLegacyId", result.Properties.Keys);
    }

    #endregion

    // Note: ImportSingleMediaItem tests are omitted because they require mocking MediaFileManager,
    // which is a sealed class and cannot be mocked with Moq. These tests would be better suited
    // as integration tests where the actual MediaFileManager implementation can be used.
}
