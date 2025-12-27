using Umbraco.Community.BulkUpload.Core.Models;

namespace Umbraco.Community.BulkUpload.Tests.Models;

public class MediaImportResultTests
{
    [Fact]
    public void Constructor_InitializesWithRequiredFileName()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg"
        };

        // Assert
        Assert.Equal("test.jpg", result.BulkUploadFileName);
    }

    [Fact]
    public void BulkUploadSuccess_DefaultsToFalse()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg"
        };

        // Assert
        Assert.False(result.BulkUploadSuccess);
    }

    [Fact]
    public void BulkUploadSuccess_CanBeSetToTrue()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = true
        };

        // Assert
        Assert.True(result.BulkUploadSuccess);
    }

    [Fact]
    public void BulkUploadMediaGuid_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadMediaGuid = null
        };

        // Assert
        Assert.Null(result.BulkUploadMediaGuid);
    }

    [Fact]
    public void BulkUploadMediaGuid_CanBeSet()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadMediaGuid = guid
        };

        // Assert
        Assert.Equal(guid, result.BulkUploadMediaGuid);
    }

    [Fact]
    public void BulkUploadMediaUdi_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadMediaUdi = null
        };

        // Assert
        Assert.Null(result.BulkUploadMediaUdi);
    }

    [Fact]
    public void BulkUploadMediaUdi_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadMediaUdi = "umb://media/1234567890abcdef1234567890abcdef"
        };

        // Assert
        Assert.Equal("umb://media/1234567890abcdef1234567890abcdef", result.BulkUploadMediaUdi);
    }

    [Fact]
    public void BulkUploadErrorMessage_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadErrorMessage = null
        };

        // Assert
        Assert.Null(result.BulkUploadErrorMessage);
    }

    [Fact]
    public void BulkUploadErrorMessage_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadErrorMessage = "File not found"
        };

        // Assert
        Assert.Equal("File not found", result.BulkUploadErrorMessage);
    }

    [Fact]
    public void SuccessResult_HasAllPropertiesSet()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = true,
            BulkUploadMediaGuid = guid,
            BulkUploadMediaUdi = $"umb://media/{guid:N}",
            BulkUploadErrorMessage = null
        };

        // Assert
        Assert.Equal("test.jpg", result.BulkUploadFileName);
        Assert.True(result.BulkUploadSuccess);
        Assert.Equal(guid, result.BulkUploadMediaGuid);
        Assert.Equal($"umb://media/{guid:N}", result.BulkUploadMediaUdi);
        Assert.Null(result.BulkUploadErrorMessage);
    }

    [Fact]
    public void FailureResult_HasErrorMessage()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = false,
            BulkUploadMediaGuid = null,
            BulkUploadMediaUdi = null,
            BulkUploadErrorMessage = "Media type not found"
        };

        // Assert
        Assert.Equal("test.jpg", result.BulkUploadFileName);
        Assert.False(result.BulkUploadSuccess);
        Assert.Null(result.BulkUploadMediaGuid);
        Assert.Null(result.BulkUploadMediaUdi);
        Assert.Equal("Media type not found", result.BulkUploadErrorMessage);
    }

    [Fact]
    public void BulkUploadLegacyId_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadLegacyId = null
        };

        // Assert
        Assert.Null(result.BulkUploadLegacyId);
    }

    [Fact]
    public void BulkUploadLegacyId_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadLegacyId = "legacy-123"
        };

        // Assert
        Assert.Equal("legacy-123", result.BulkUploadLegacyId);
    }
}
