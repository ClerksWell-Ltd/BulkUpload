using Umbraco.Community.BulkUpload.Core.Models;

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
            Parent = "1"
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
            Parent = "1"
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
            Parent = "1"
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
            Parent = "1"
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
            Parent = "1"
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
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenParentIsNull()
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
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenParentIsEmpty()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = string.Empty
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenParentIsWhitespace()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = "   "
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenAllRequiredFieldsAreValid_WithIntegerParent()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = "1"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenAllRequiredFieldsAreValid_WithGuidParent()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        };

        // Act
        var result = importObject.CanImport;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanImport_ReturnsTrue_WhenAllRequiredFieldsAreValid_WithPathParent()
    {
        // Arrange
        var importObject = new ImportObject
        {
            Name = "Test Name",
            ContentTypeAlais = "testType",
            Parent = "/Blog/Articles"
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
            Parent = "1",
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
            Parent = "1",
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
            Parent = "1",
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
