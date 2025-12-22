using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Tests.Models;

public class MediaImportResultTests
{
    [Fact]
    public void Constructor_InitializesWithRequiredFileName()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg"
        };

        // Assert
        Assert.Equal("test.jpg", result.FileName);
    }

    [Fact]
    public void Success_DefaultsToFalse()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg"
        };

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void Success_CanBeSetToTrue()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            Success = true
        };

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void MediaId_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaId = null
        };

        // Assert
        Assert.Null(result.MediaId);
    }

    [Fact]
    public void MediaId_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaId = 123
        };

        // Assert
        Assert.Equal(123, result.MediaId);
    }

    [Fact]
    public void MediaGuid_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaGuid = null
        };

        // Assert
        Assert.Null(result.MediaGuid);
    }

    [Fact]
    public void MediaGuid_CanBeSet()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaGuid = guid
        };

        // Assert
        Assert.Equal(guid, result.MediaGuid);
    }

    [Fact]
    public void MediaUdi_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaUdi = null
        };

        // Assert
        Assert.Null(result.MediaUdi);
    }

    [Fact]
    public void MediaUdi_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            MediaUdi = "umb://media/1234567890abcdef1234567890abcdef"
        };

        // Assert
        Assert.Equal("umb://media/1234567890abcdef1234567890abcdef", result.MediaUdi);
    }

    [Fact]
    public void ErrorMessage_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            ErrorMessage = null
        };

        // Assert
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            ErrorMessage = "File not found"
        };

        // Assert
        Assert.Equal("File not found", result.ErrorMessage);
    }

    [Fact]
    public void SuccessResult_HasAllPropertiesSet()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            Success = true,
            MediaId = 123,
            MediaGuid = guid,
            MediaUdi = $"umb://media/{guid:N}",
            ErrorMessage = null
        };

        // Assert
        Assert.Equal("test.jpg", result.FileName);
        Assert.True(result.Success);
        Assert.Equal(123, result.MediaId);
        Assert.Equal(guid, result.MediaGuid);
        Assert.Equal($"umb://media/{guid:N}", result.MediaUdi);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void FailureResult_HasErrorMessage()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
            Success = false,
            MediaId = null,
            MediaGuid = null,
            MediaUdi = null,
            ErrorMessage = "Media type not found"
        };

        // Assert
        Assert.Equal("test.jpg", result.FileName);
        Assert.False(result.Success);
        Assert.Null(result.MediaId);
        Assert.Null(result.MediaGuid);
        Assert.Null(result.MediaUdi);
        Assert.Equal("Media type not found", result.ErrorMessage);
    }

    [Fact]
    public void BulkUploadLegacyId_CanBeNull()
    {
        // Arrange & Act
        var result = new MediaImportResult
        {
            FileName = "test.jpg",
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
            FileName = "test.jpg",
            BulkUploadLegacyId = "legacy-123"
        };

        // Assert
        Assert.Equal("legacy-123", result.BulkUploadLegacyId);
    }
}
