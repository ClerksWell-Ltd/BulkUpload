using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using BulkUpload.Controllers;
using BulkUpload.Models;
using BulkUpload.Services;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

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
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = file! });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Uploaded file is not valid or is empty.", problemDetails.Detail);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("test.zip");

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Uploaded file is not valid or is empty.", problemDetails.Detail);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenFileIsNotZipOrCsv()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("test.txt");

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Please upload either a ZIP file (containing CSV and media files) or a CSV file (for URL-based media).", problemDetails.Detail);
    }

    [Fact]
    public async Task ImportMedia_ReturnsBadRequest_WhenNoCsvFoundInZip()
    {
        // Arrange
        var zipStream = CreateZipWithoutCsv();
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("No CSV file found in ZIP archive. Please ensure your ZIP contains at least one .csv file.", problemDetails.Detail);
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
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = false,
            BulkUploadErrorMessage = "File not found in ZIP archive: test.jpg"
        };

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(importObject);

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

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
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

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
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

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
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

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
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("No results to export. Please provide a non-empty array of media import results.", problemDetails.Detail);
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
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("No results to export. Please provide a non-empty array of media import results.", problemDetails.Detail);
    }

    [Fact]
    public void ExportResults_ReturnsFileResult_WithValidResults()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null
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
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        // bulkUploadErrorMessage and bulkUploadLegacyId are not included when there are no errors and no legacy IDs
        Assert.Contains("bulkUploadFileName,bulkUploadSuccess,bulkUploadMediaGuid,bulkUploadMediaUdi,bulkUploadShouldUpdate", csvContent);
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
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = guid,
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("test.jpg", csvContent);
        Assert.Contains("True", csvContent);
    }

    [Fact]
    public void ExportResults_HandlesMultipleResults()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                BulkUploadFileName = "test1.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null
            },
            new MediaImportResult
            {
                BulkUploadFileName = "test2.jpg",
                BulkUploadSuccess = false,
                BulkUploadMediaGuid = null,
                BulkUploadMediaUdi = null,
                BulkUploadErrorMessage = "File not found"
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
                BulkUploadFileName = "test,with,commas.jpg",
                BulkUploadSuccess = false,
                BulkUploadMediaGuid = null,
                BulkUploadMediaUdi = null,
                BulkUploadErrorMessage = "Error with \"quotes\""
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
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null,
                BulkUploadLegacyId = "legacy-999",
                OriginalCsvData = new Dictionary<string, string>
                {
                    { "bulkUploadLegacyId", "legacy-999" }
                }
            }
        };

        // Act
        var result = _controller.ExportResults(results);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("legacy-999", csvContent);
        Assert.Contains("bulkUploadLegacyId", csvContent); // Column header should be present
    }

    [Fact]
    public void ExportResults_HandlesNullBulkUploadLegacyId()
    {
        // Arrange
        var results = new List<MediaImportResult>
        {
            new MediaImportResult
            {
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null,
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
                BulkUploadFileName = "test.jpg",
                BulkUploadSuccess = true,
                BulkUploadMediaGuid = Guid.NewGuid(),
                BulkUploadMediaUdi = "umb://media/123",
                BulkUploadErrorMessage = null,
                BulkUploadLegacyId = "legacy\"with\"quotes",
                OriginalCsvData = new Dictionary<string, string>
                {
                    { "bulkUploadLegacyId", "legacy\"with\"quotes" }
                }
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

    [Fact]
    public async Task ImportMedia_FindsFileInSubfolder_WhenFileNameHasForwardSlash()
    {
        // Arrange
        var csvContent = "fileName,parentId\nimages/test.jpg,123";
        var zipStream = CreateZipWithCsvAndFile(csvContent, "images/test.jpg");
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var importObject = new MediaImportObject
        {
            FileName = "images/test.jpg",
            Parent = "123"
        };

        var importResult = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = true,
            BulkUploadMediaGuid = Guid.NewGuid(),
            BulkUploadMediaUdi = "umb://media/test123"
        };

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(importObject);

        _mockMediaImportService
            .Setup(s => s.ImportSingleMediaItem(
                It.IsAny<MediaImportObject>(),
                It.IsAny<Stream>(),
                It.IsAny<bool>()))
            .Returns(importResult);

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var value = okResult.Value;
        var successCountProp = value.GetType().GetProperty("SuccessCount");
        Assert.NotNull(successCountProp);
        var successCount = (int)successCountProp.GetValue(value)!;
        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task ImportMedia_FindsFileInSubfolder_WhenFileNameHasBackslash()
    {
        // Arrange
        var csvContent = "fileName,parentId\nimages\\test.jpg,123";
        var zipStream = CreateZipWithCsvAndFile(csvContent, "images/test.jpg"); // ZIP always uses forward slash
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var importObject = new MediaImportObject
        {
            FileName = "images\\test.jpg",
            Parent = "123"
        };

        var importResult = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = true,
            BulkUploadMediaGuid = Guid.NewGuid(),
            BulkUploadMediaUdi = "umb://media/test123"
        };

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(importObject);

        _mockMediaImportService
            .Setup(s => s.ImportSingleMediaItem(
                It.IsAny<MediaImportObject>(),
                It.IsAny<Stream>(),
                It.IsAny<bool>()))
            .Returns(importResult);

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var value = okResult.Value;
        var successCountProp = value.GetType().GetProperty("SuccessCount");
        Assert.NotNull(successCountProp);
        var successCount = (int)successCountProp.GetValue(value)!;
        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task ImportMedia_FindsFileInNestedSubfolders()
    {
        // Arrange
        var csvContent = "fileName,parentId\npath/to/images/test.jpg,123";
        var zipStream = CreateZipWithCsvAndFile(csvContent, "path/to/images/test.jpg");
        var mockFile = CreateMockFormFile("test.zip", zipStream);

        var importObject = new MediaImportObject
        {
            FileName = "path/to/images/test.jpg",
            Parent = "123"
        };

        var importResult = new MediaImportResult
        {
            BulkUploadFileName = "test.jpg",
            BulkUploadSuccess = true,
            BulkUploadMediaGuid = Guid.NewGuid(),
            BulkUploadMediaUdi = "umb://media/test123"
        };

        _mockMediaImportService
            .Setup(s => s.CreateMediaImportObject(It.IsAny<object>()))
            .Returns(importObject);

        _mockMediaImportService
            .Setup(s => s.ImportSingleMediaItem(
                It.IsAny<MediaImportObject>(),
                It.IsAny<Stream>(),
                It.IsAny<bool>()))
            .Returns(importResult);

        // Act
        var result = await _controller.ImportMedia(new ImportMediaRequestModel { File = mockFile.Object });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var value = okResult.Value;
        var successCountProp = value.GetType().GetProperty("SuccessCount");
        Assert.NotNull(successCountProp);
        var successCount = (int)successCountProp.GetValue(value)!;
        Assert.Equal(1, successCount);
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

    private MemoryStream CreateZipWithCsvAndFile(string csvContent, string filePathInZip)
    {
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Add CSV file
            var csvEntry = archive.CreateEntry("import.csv");
            using (var csvStream = csvEntry.Open())
            using (var writer = new StreamWriter(csvStream))
            {
                writer.Write(csvContent);
            }

            // Add media file at specified path
            var mediaEntry = archive.CreateEntry(filePathInZip);
            using (var mediaStream = mediaEntry.Open())
            {
                // Write some dummy image data
                var dummyImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
                mediaStream.Write(dummyImageData, 0, dummyImageData.Length);
            }
        }
        zipStream.Position = 0;
        return zipStream;
    }

    #endregion
}
