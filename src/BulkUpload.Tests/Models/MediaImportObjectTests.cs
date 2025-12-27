using Umbraco.Community.BulkUpload.Core.Models;

namespace Umbraco.Community.BulkUpload.Tests.Models;

public class MediaImportObjectTests
{
    [Fact]
    public void CanImport_ReturnsFalse_WhenFileNameIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = null!,
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenFileNameIsEmpty()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = string.Empty,
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenFileNameIsWhitespace()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "   ",
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenParentIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = null
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenParentIsEmpty()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = string.Empty
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenFileNameAndParentAreValid()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenNameIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Name = null,
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenMediaTypeAliasIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            MediaTypeAlias = null,
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DisplayName_ReturnsName_WhenNameIsSet()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Name = "My Image",
            Parent = "1"
        };

        // Act
        var result = importObject.DisplayName;

        // Assert
        Assert.Equal("My Image", result);
    }

    [Fact]
    public void DisplayName_ReturnsFileName_WhenNameIsNull()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Name = null,
            Parent = "1"
        };

        // Act
        var result = importObject.DisplayName;

        // Assert
        Assert.Equal("test.jpg", result);
    }

    [Fact]
    public void DisplayName_ReturnsFileName_WhenNameIsEmpty()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Name = string.Empty,
            Parent = "1"
        };

        // Act
        var result = importObject.DisplayName;

        // Assert
        Assert.Equal("test.jpg", result);
    }

    [Fact]
    public void DisplayName_ReturnsFileName_WhenNameIsWhitespace()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Name = "   ",
            Parent = "1"
        };

        // Act
        var result = importObject.DisplayName;

        // Assert
        Assert.Equal("test.jpg", result);
    }

    [Fact]
    public void Properties_CanBeNull()
    {
        // Arrange & Act
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1",
            Properties = null
        };

        // Assert
        Assert.Null(importObject.Properties);
    }

    [Fact]
    public void Properties_CanBeEmpty()
    {
        // Arrange & Act
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1",
            Properties = new Dictionary<string, object>()
        };

        // Assert
        Assert.NotNull(importObject.Properties);
        Assert.Empty(importObject.Properties);
    }

    [Fact]
    public void Properties_CanContainMultipleValues()
    {
        // Arrange & Act
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1",
            Properties = new Dictionary<string, object>
            {
                { "altText", "Test Image" },
                { "caption", "A beautiful test image" }
            }
        };

        // Assert
        Assert.NotNull(importObject.Properties);
        Assert.Equal(2, importObject.Properties.Count);
        Assert.Equal("Test Image", importObject.Properties["altText"]);
        Assert.Equal("A beautiful test image", importObject.Properties["caption"]);
    }

    [Fact]
    public void BulkUploadLegacyId_CanBeNull()
    {
        // Arrange & Act
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1",
            BulkUploadLegacyId = null
        };

        // Assert
        Assert.Null(importObject.BulkUploadLegacyId);
    }

    [Fact]
    public void BulkUploadLegacyId_CanBeSet()
    {
        // Arrange & Act
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "1",
            BulkUploadLegacyId = "legacy-456"
        };

        // Assert
        Assert.Equal("legacy-456", importObject.BulkUploadLegacyId);
    }
}
