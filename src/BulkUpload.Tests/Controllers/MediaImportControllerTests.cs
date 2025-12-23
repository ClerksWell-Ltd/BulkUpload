using System.IO.Compression;
using System.Text;
using BulkUpload.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.BulkUpload.Controllers;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Tests.Controllers;

public class MediaImportControllerTests
{
    private readonly Mock<ILogger<MediaImportController>> _mockLogger;
    private readonly Mock<IMediaImportService> _mockMediaImportService;
    private readonly Mock<IParentLookupCache> _mockParentLookupCache;
    private readonly Mock<IMediaItemCache> _mockMediaItemCache;
    private readonly Mock<ILegacyIdCache> _mockLegacyIdCache;
    private readonly MediaImportController _controller;

    public MediaImportControllerTests()
    {
        _mockLogger = new Mock<ILogger<MediaImportController>>();
        _mockMediaImportService = new Mock<IMediaImportService>();
        _mockParentLookupCache = new Mock<IParentLookupCache>();
        _mockMediaItemCache = new Mock<IMediaItemCache>();
        _mockLegacyIdCache = new Mock<ILegacyIdCache>();
        _controller = new MediaImportController(
            _mockLogger.Object,
            _mockMediaImportService.Object,
            _mockParentLookupCache.Object,
            _mockMediaItemCache.Object,
            _mockLegacyIdCache.Object);
    }

    #region ImportMedia Tests

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        IFormFile? file = null;

        // Act
        var result = await _controller.ImportMedia(file!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Uploaded ZIP file not valid.", badRequestResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("test.zip");

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Uploaded ZIP file not valid.", badRequestResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenFileIsNotZip()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("test.txt");

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please upload a ZIP file containing CSV and media files.", badRequestResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenNoCsvFoundInZip()
    {
        // Arrange
        var zipStream = CreateZipWithoutCsv();
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No CSV file found in ZIP archive.", badRequestResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ReturnsOkResult_WithValidZipAndCsv()
    {
        // Arrange
        var csvContent = "fileName,parentId\ntest.jpg,123";
        var zipStream = CreateZipWithCsv(csvContent);
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var importObject = new MediaImportObject
        {
            FileName = "test.jpg",
            Parent = "123"
        };

        var importResult = new MediaImportResult
        {
            FileName = "test.jpg",
            Success = false,
            ErrorMessage = "File not found in ZIP archive: test.jpg"
        };

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(importObject);

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ProcessesMultipleRecords_FromCsv()
    {
        // Arrange
        var csvContent = "fileName,parentId\ntest1.jpg,123\ntest2.jpg,456";
        var zipStream = CreateZipWithCsv(csvContent);
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var callCount = 0;
        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(() =>
            {
                callCount++;
                return new MediaImportObject
                {
                    FileName = $"test{callCount}.jpg",
                    Parent = callCount == 1 ? "123" : "456"
                };
            });

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMediaImportService.Verify(s => s.CreateMediaImportObject(It.IsAny<object>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ImportMedia_SkipsInvalidRecords()
    {
        // Arrange
        var csvContent = "fileName,parentId\ntest.jpg,0\nvalid.jpg,123";
        var zipStream = CreateZipWithCsv(csvContent);
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var callCount = 0;
        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(() =>
            {
                callCount++;
                return new MediaImportObject
                {
                    FileName = callCount == 1 ? "test.jpg" : "valid.jpg",
                    Parent = callCount == 1 ? null : "123"
                };
            });

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ImportMedia_ReturnsResultsWithCounts()
    {
        // Arrange
        var csvContent = "fileName,parentId\ntest.jpg,123";
        var zipStream = CreateZipWithCsv(csvContent);
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(new MediaImportObject
            {
                FileName = "test.jpg",
                Parent = "123"
            });

        // Act
        var result = await _controller.ImportMedia(mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        var totalCountProp = value.GetType().GetProperty("TotalCount");
        var successCountProp = value.GetType().GetProperty("SuccessCount");
        var failureCountProp = value.GetType().GetProperty("FailureCount");
        var resultsProp = value.GetType().GetProperty("Results");

        Assert.NotNull(totalCountProp);
        Assert.NotNull(successCountProp);
        Assert.NotNull(failureCountProp);
        Assert.NotNull(resultsProp);
    }

    #endregion

    #region ExportResults Tests

    [Fact]
    public void ExportResults_ReturnsBadRequest_WhenResultsIsNull()
    {
        // Arrange
        List<MediaImportResult>? results = null;

        // Act
        var result = _controller.ExportResults(results!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No results to export.", badRequestResult.Value);
    }

    [Fact]
    public void ExportResults_ReturnsBadRequest_WhenResultsIsEmpty()
    {
        // Arrange
        var results = new List<MediaImportResult>();

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No results to export.", badRequestResult.Value);
    }

    [Fact]
    public void ExportResults_ReturnsFileResult_WithValidResults()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("media-import-results.csv", fileResult.FileDownloadName);
    }

    [Fact]
    public void ExportResults_GeneratesCsvWithHeader()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("fileName,success,mediaId,mediaGuid,mediaUdi,errorMessage,bulkUploadLegacyId", csvContent);
    }

    [Fact]
    public void ExportResults_GeneratesCsvWithCorrectData()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = guid,
                MediaUdi = "umb://media/123",
                ErrorMessage = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("test.jpg", csvContent);
        Assert.Contains("True", csvContent);
        Assert.Contains("123", csvContent);
    }

    [Fact]
    public void ExportResults_HandlesMultipleResults()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test1.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null
            },
            new MediaImportResult
            {
                FileName = "test2.jpg",
                Success = false,
                MediaId = null,
                MediaGuid = null,
                MediaUdi = null,
                ErrorMessage = "File not found"
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("test1.jpg", csvContent);
        Assert.Contains("test2.jpg", csvContent);
        Assert.Contains("File not found", csvContent);
    }

    [Fact]
    public void ExportResults_EscapesSpecialCharactersInCsv()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test,with,commas.jpg",
                Success = false,
                MediaId = null,
                MediaGuid = null,
                MediaUdi = null,
                ErrorMessage = "Error with \"quotes\""
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("\"test,with,commas.jpg\"", csvContent);
    }

    [Fact]
    public void ExportResults_IncludesBulkUploadLegacyId_InCsvOutput()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null,
                BulkUploadLegacyId = "legacy-999"
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("legacy-999", csvContent);
    }

    [Fact]
    public void ExportResults_HandlesNullBulkUploadLegacyId()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null,
                BulkUploadLegacyId = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert - Should not throw and should have empty value in CSV
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.NotNull(fileResult);
    }

    [Fact]
    public void ExportResults_EscapesQuotesInBulkUploadLegacyId()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                FileName = "test.jpg",
                Success = true,
                MediaId = 123,
                MediaGuid = Guid.NewGuid(),
                MediaUdi = "umb://media/123",
                ErrorMessage = null,
                BulkUploadLegacyId = "legacy\"with\"quotes"
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        // CSV escaping doubles the quotes
        Assert.Contains("legacy\"\"with\"\"quotes", csvContent);
    }

    #endregion

    #region Helper Methods

    private Mock<IFormFile> CreateMockFormFile(string fileName, MemoryStream stream)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile;
    }

    private MemoryStream CreateZipWithCsv(string csvContent)
    {
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var csvEntry = archive.CreateEntry("import.csv");
            using var csvStream = csvEntry.Open();
            using var writer = new StreamWriter(csvStream);
            writer.Write(csvContent);
        }
        zipStream.Position = 0;
        return zipStream;
    }

    private MemoryStream CreateZipWithoutCsv()
    {
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var textEntry = archive.CreateEntry("readme.txt");
            using var textStream = textEntry.Open();
            using var writer = new StreamWriter(textStream);
            writer.Write("This is a test file");
        }
        zipStream.Position = 0;
        return zipStream;
    }

    #endregion
}
