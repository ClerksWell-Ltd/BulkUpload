using Microsoft.Extensions.Logging;
using Moq;
using BulkUpload.Models;
using BulkUpload.Resolvers;
using BulkUpload.Services;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.BulkUpload.Tests.Services;

public class BulkImportServiceTests
{
    private readonly Mock<IImportUtilityService> _mockImportUtilityService;
    private readonly List<(ImportObject importObject, bool publish, bool unpublish)> _imported = new();
    private readonly BulkImportService _service;

    public BulkImportServiceTests()
    {
        // Parse rows with the real CreateImportObject so the skip rule sees exactly what a CSV import produces
        var parser = new ImportUtilityService(
            new Mock<IContentService>().Object,
            new Mock<IResolverFactory>().Object,
            new Mock<IParentLookupCache>().Object,
            new Mock<ILegacyIdCache>().Object,
            new Mock<IIdKeyMap>().Object,
            new Mock<ILogger<ImportUtilityService>>().Object);

        _mockImportUtilityService = new Mock<IImportUtilityService>();
        _mockImportUtilityService
            .Setup(s => s.CreateImportObject(It.IsAny<object>()))
            .Returns((object record) => parser.CreateImportObject(record));
        _mockImportUtilityService
            .Setup(s => s.ImportSingleItem(It.IsAny<ImportObject>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback((ImportObject o, bool publish, bool unpublish) => _imported.Add((o, publish, unpublish)))
            .Returns(new ContentImportResult { BulkUploadSuccess = true });

        var mockHierarchyResolver = new Mock<IHierarchyResolver>();
        mockHierarchyResolver
            .Setup(h => h.ValidateAndSort(It.IsAny<List<ImportObject>>()))
            .Returns((List<ImportObject> items) => items);

        var mockMediaPreprocessor = new Mock<IMediaPreprocessorService>();
        mockMediaPreprocessor
            .Setup(m => m.PreprocessMediaItems(It.IsAny<List<(dynamic record, string sourceFileName)>>(), It.IsAny<string?>()))
            .Returns(new List<MediaPreprocessingResult>());

        _service = new BulkImportService(
            _mockImportUtilityService.Object,
            mockHierarchyResolver.Object,
            mockMediaPreprocessor.Object,
            new Mock<IParentLookupCache>().Object,
            new Mock<IMediaItemCache>().Object,
            new Mock<ILegacyIdCache>().Object,
            new Mock<ILogger<BulkImportService>>().Object);
    }

    private static (IDictionary<string, object?> Record, string SourceFileName) Row(params (string key, object? value)[] columns)
    {
        IDictionary<string, object?> record = new Dictionary<string, object?>();
        foreach (var (key, value) in columns)
        {
            record[key] = value;
        }
        return (record, "content.csv");
    }

    private static (IDictionary<string, object?> Record, string SourceFileName) ExistingRow(
        Guid key, string shouldUpdate, string shouldPublish, string shouldUnpublish)
    {
        return Row(
            ("bulkUploadContentGuid", key.ToString()),
            ("bulkUploadShouldUpdate", shouldUpdate),
            ("bulkUploadShouldPublish", shouldPublish),
            ("bulkUploadShouldUnpublish", shouldUnpublish));
    }

    [Fact]
    public async Task ImportRecordsAsync_SkipsExistingRowSilently_WhenNoFlagIsTrue()
    {
        // Arrange
        var records = new[] { ExistingRow(Guid.NewGuid(), "false", "false", "false") };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        Assert.Empty(_imported);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task ImportRecordsAsync_SkipsExistingRowSilently_WhenFlagColumnsAreAbsent()
    {
        // Arrange
        var records = new[] { Row(("bulkUploadContentGuid", Guid.NewGuid().ToString()), ("title", "Hello")) };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        Assert.Empty(_imported);
        Assert.Equal(0, response.TotalCount);
    }

    [Theory]
    [InlineData("true", "false", "false")]
    [InlineData("false", "true", "false")]
    [InlineData("false", "false", "true")]
    [InlineData("false", "true", "true")]
    public async Task ImportRecordsAsync_ProcessesExistingRow_WhenAnyFlagIsTrue(
        string shouldUpdate, string shouldPublish, string shouldUnpublish)
    {
        // Arrange
        var key = Guid.NewGuid();
        var records = new[] { ExistingRow(key, shouldUpdate, shouldPublish, shouldUnpublish) };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        var imported = Assert.Single(_imported);
        Assert.Equal(key, imported.importObject.BulkUploadContentGuid);
        Assert.Equal(shouldPublish == "true", imported.publish);
        Assert.Equal(shouldUnpublish == "true", imported.unpublish);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task ImportRecordsAsync_SkipsNewRow_WhenShouldUpdateColumnIsPresentAndFalse()
    {
        // Arrange
        var records = new[]
        {
            Row(("name", "Page"), ("docTypeAlias", "article"), ("bulkUploadShouldUpdate", "false"), ("bulkUploadShouldPublish", "true"))
        };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        Assert.Empty(_imported);
        Assert.Equal(0, response.TotalCount);
    }

    [Theory]
    [InlineData("false", "false")]
    [InlineData("true", "false")]
    [InlineData("false", "true")]
    [InlineData("true", "true")]
    public async Task ImportRecordsAsync_CreatesNewRow_WhenShouldUpdateColumnIsAbsent(string shouldPublish, string shouldUnpublish)
    {
        // Arrange
        var records = new[]
        {
            Row(("name", "Page"), ("docTypeAlias", "article"),
                ("bulkUploadShouldPublish", shouldPublish), ("bulkUploadShouldUnpublish", shouldUnpublish))
        };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        var imported = Assert.Single(_imported);
        Assert.Null(imported.importObject.BulkUploadContentGuid);
        Assert.Equal(shouldPublish == "true", imported.publish);
        Assert.Equal(shouldUnpublish == "true", imported.unpublish);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task ImportRecordsAsync_OnlyPassesRowsWithWorkToDo_WhenFileMixesRows()
    {
        // Arrange
        var keep = Guid.NewGuid();
        var records = new[]
        {
            ExistingRow(Guid.NewGuid(), "false", "false", "false"),
            ExistingRow(keep, "false", "false", "true"),
            ExistingRow(Guid.NewGuid(), "", "", "")
        };

        // Act
        var response = await _service.ImportRecordsAsync(records);

        // Assert
        var imported = Assert.Single(_imported);
        Assert.Equal(keep, imported.importObject.BulkUploadContentGuid);
        Assert.Equal(1, response.TotalCount);
    }
}
