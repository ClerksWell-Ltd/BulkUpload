using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Tests.Models;

public class ImportObjectTests
{
    [Fact]
    public void CanImport_ReturnsFalse_WhenNameIsNull()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = null!,
            ContentTypeAlais = "testType",
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenNameIsEmpty()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = string.Empty,
            ContentTypeAlais = "testType",
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenNameIsWhitespace()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "   ",
            ContentTypeAlais = "testType",
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenContentTypeAlaisIsNull()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = null!,
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenContentTypeAlaisIsEmpty()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = string.Empty,
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsFalse_WhenContentTypeAlaisIsWhitespace()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "   ",
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
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = null
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
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = null
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenAllRequiredFieldsAreValid()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            ParentId = 1
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenPropertiesIsNull()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            ParentId = 1,
            Properties = null
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenPropertiesIsEmpty()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            ParentId = 1,
            Properties = new Dictionary<string, object>()
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenPropertiesHasValues()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            ParentId = 1,
            Properties = new Dictionary<string, object>
            {
                { "Title", "Test Title" },
                { "Description", "Test Description" }
            }
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }
}
