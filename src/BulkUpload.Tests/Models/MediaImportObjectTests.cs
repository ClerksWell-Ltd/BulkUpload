using Umbraco.Community.BulkUpload.Models;

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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenParentIdIsZero()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 0
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenParentIdIsNegative()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = -1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenFileNameAndParentIdAreValid()
    {
        // Arrange
        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1
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
            ParentId = 1,
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
            ParentId = 1,
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
            ParentId = 1,
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
}
