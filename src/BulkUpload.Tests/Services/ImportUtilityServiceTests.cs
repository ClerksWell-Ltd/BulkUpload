using Microsoft.Extensions.Logging;
using Moq;
using BulkUpload.Models;
using BulkUpload.Resolvers;
using BulkUpload.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.BulkUpload.Tests.Services;

public class ImportUtilityServiceTests
{
    private const int RootId = Umbraco.Cms.Core.Constants.System.Root;

    private readonly Mock<IContentService> _mockContentService;
    private readonly Mock<IResolverFactory> _mockResolverFactory;
    private readonly Mock<IParentLookupCache> _mockParentLookupCache;
    private readonly Mock<ILegacyIdCache> _mockLegacyIdCache;
    private readonly Mock<IIdKeyMap> _mockIdKeyMap;
    private readonly Mock<ILogger<ImportUtilityService>> _mockLogger;
    private readonly ImportUtilityService _service;

    public ImportUtilityServiceTests()
    {
        _mockContentService = new Mock<IContentService>();
        _mockResolverFactory = new Mock<IResolverFactory>();
        _mockParentLookupCache = new Mock<IParentLookupCache>();
        _mockLegacyIdCache = new Mock<ILegacyIdCache>();
        _mockIdKeyMap = new Mock<IIdKeyMap>();
        _mockLogger = new Mock<ILogger<ImportUtilityService>>();

        _mockContentService
            .Setup(s => s.SaveAndPublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns((IContent c, string _, int _) => new PublishResult(PublishResultType.SuccessPublish, null, c));
        _mockContentService
            .Setup(s => s.Unpublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns((IContent c, string _, int _) => new PublishResult(PublishResultType.SuccessUnpublish, null, c));

        var textResolver = new Mock<IResolver>();
        textResolver.Setup(r => r.Resolve(It.IsAny<object>())).Returns((object v) => v);
        _mockResolverFactory.Setup(f => f.GetByAlias("text")).Returns(textResolver.Object);

        _service = new ImportUtilityService(
            _mockContentService.Object,
            _mockResolverFactory.Object,
            _mockParentLookupCache.Object,
            _mockLegacyIdCache.Object,
            _mockIdKeyMap.Object,
            _mockLogger.Object);
    }

    private Mock<IContent> SetupExistingContent(Guid key, bool published)
    {
        var content = new Mock<IContent>();
        content.SetupProperty(c => c.Name, "Existing Page");
        content.SetupGet(c => c.Key).Returns(key);
        content.SetupGet(c => c.ParentId).Returns(RootId);
        content.SetupGet(c => c.Published).Returns(published);
        _mockContentService.Setup(s => s.GetById(key)).Returns(content.Object);
        return content;
    }

    private Mock<IContent> SetupCreatedContent()
    {
        var content = new Mock<IContent>();
        content.SetupProperty(c => c.Name, "New Page");
        content.SetupGet(c => c.Key).Returns(Guid.NewGuid());
        content.SetupGet(c => c.ParentId).Returns(RootId);
        content.SetupGet(c => c.Published).Returns(false);
        _mockContentService
            .Setup(s => s.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(content.Object);
        return content;
    }

    private static ImportObject ExistingRow(Guid key, bool shouldUpdate, bool shouldPublish = false, bool shouldUnpublish = false)
    {
        return new ImportObject
        {
            Name = "",
            ContentTypeAlais = "",
            BulkUploadContentGuid = key,
            BulkUploadShouldUpdate = shouldUpdate,
            BulkUploadShouldUpdateColumnExisted = true,
            BulkUploadShouldPublish = shouldPublish,
            BulkUploadShouldPublishColumnExisted = true,
            BulkUploadShouldUnpublish = shouldUnpublish,
            BulkUploadShouldUnpublishColumnExisted = true,
            Properties = new Dictionary<string, object> { { "title", "New title" } }
        };
    }

    private static ImportObject NewRow(bool shouldPublish, bool shouldUnpublish)
    {
        return new ImportObject
        {
            Name = "New Page",
            ContentTypeAlais = "article",
            BulkUploadShouldPublish = shouldPublish,
            BulkUploadShouldPublishColumnExisted = true,
            BulkUploadShouldUnpublish = shouldUnpublish,
            BulkUploadShouldUnpublishColumnExisted = true,
            Properties = new Dictionary<string, object> { { "title", "New title" } }
        };
    }

    private ContentImportResult Import(ImportObject importObject)
    {
        return _service.ImportSingleItem(importObject, importObject.BulkUploadShouldPublish, importObject.BulkUploadShouldUnpublish);
    }

    private void VerifySave(Times times) =>
        _mockContentService.Verify(s => s.Save(It.IsAny<IContent>(), It.IsAny<int?>(), It.IsAny<ContentScheduleCollection?>()), times);

    private void VerifyPublish(Times times) =>
        _mockContentService.Verify(s => s.SaveAndPublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()), times);

    private void VerifyUnpublish(Times times) =>
        _mockContentService.Verify(s => s.Unpublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()), times);

    private static void VerifyDataWritten(Mock<IContent> content, Times times) =>
        content.Verify(c => c.SetValue("title", "New title", It.IsAny<string?>(), It.IsAny<string?>()), times);

    #region ImportSingleItem - existing content

    [Fact]
    public void ImportSingleItem_SavesWithoutUnpublishing_WhenPublishedNodeIsUpdatedWithoutPublishFlags()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate: true);

        // Act
        var result = _service.ImportSingleItem(importObject, publish: false);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifySave(Times.Once());
        VerifyUnpublish(Times.Never());
    }

    [Fact]
    public void ImportSingleItem_SavesThenUnpublishes_WhenPublishedNodeIsUpdatedWithUnpublish()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var calls = new List<string>();
        _mockContentService
            .Setup(s => s.Save(It.IsAny<IContent>(), It.IsAny<int?>(), It.IsAny<ContentScheduleCollection?>()))
            .Callback(() => calls.Add("Save"));
        _mockContentService
            .Setup(s => s.Unpublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()))
            .Callback(() => calls.Add("Unpublish"))
            .Returns((IContent c, string _, int _) => new PublishResult(PublishResultType.SuccessUnpublish, null, c));
        var importObject = ExistingRow(key, shouldUpdate: true, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        Assert.Equal(new[] { "Save", "Unpublish" }, calls);
    }

    [Fact]
    public void ImportSingleItem_DoesNotCallUnpublish_WhenNodeIsNotPublished()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: false);
        var importObject = ExistingRow(key, shouldUpdate: true, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifySave(Times.Once());
        VerifyUnpublish(Times.Never());
    }

    [Fact]
    public void ImportSingleItem_UnpublishesAndNeverPublishes_WhenBothFlagsAreTrue()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate: true, shouldPublish: true, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifyUnpublish(Times.Once());
        VerifyPublish(Times.Never());
    }

    [Fact]
    public void ImportSingleItem_PublishesAndNeverUnpublishes_WhenOnlyPublishIsTrue()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate: true, shouldPublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifyPublish(Times.Once());
        VerifyUnpublish(Times.Never());
    }

    [Fact]
    public void ImportSingleItem_PublishesWithoutWritingData_WhenShouldUpdateIsFalse()
    {
        // Arrange
        var key = Guid.NewGuid();
        var newParentKey = Guid.NewGuid();
        var content = SetupExistingContent(key, published: false);
        var importObject = ExistingRow(key, shouldUpdate: false, shouldPublish: true);
        importObject.Name = "Renamed Page";
        importObject.BulkUploadParentGuid = newParentKey;

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifyPublish(Times.Once());
        VerifyDataWritten(content, Times.Never());
        content.Verify(c => c.SetValue(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        Assert.Equal("Existing Page", content.Object.Name);
        _mockContentService.Verify(s => s.GetById(newParentKey), Times.Never);
        _mockContentService.Verify(s => s.Move(It.IsAny<IContent>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ImportSingleItem_ReportsDataNotWritten_WhenPropertiesPresentButShouldUpdateIsFalse()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate: false, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        Assert.Contains("data not written", result.BulkUploadInfoMessage);
        Assert.DoesNotContain("No properties were updated", result.BulkUploadInfoMessage);
    }

    [Fact]
    public void ImportSingleItem_KeepsSuccessAndReportsResult_WhenPublishFails()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: false);
        _mockContentService
            .Setup(s => s.SaveAndPublish(It.IsAny<IContent>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns((IContent c, string _, int _) => new PublishResult(PublishResultType.FailedPublishPathNotPublished, null, c));
        var importObject = ExistingRow(key, shouldUpdate: true, shouldPublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        Assert.Contains("FailedPublishPathNotPublished", result.BulkUploadInfoMessage);
    }

    [Fact]
    public void ImportSingleItem_EchoesUnpublishFlags_OnResult()
    {
        // Arrange
        var key = Guid.NewGuid();
        SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate: false, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadShouldUnpublish);
        Assert.True(result.BulkUploadShouldUnpublishColumnExisted);
    }

    [Fact]
    public void ImportSingleItem_EchoesUnpublishFlags_WhenContentNotFound()
    {
        // Arrange
        var importObject = ExistingRow(Guid.NewGuid(), shouldUpdate: false, shouldUnpublish: true);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.False(result.BulkUploadSuccess);
        Assert.True(result.BulkUploadShouldUnpublish);
        Assert.True(result.BulkUploadShouldUnpublishColumnExisted);
    }

    #endregion

    #region ImportSingleItem - existing content truth table

    // One case per row of the existing-content truth table, run against a published node.
    // Columns: shouldUpdate, shouldPublish, shouldUnpublish, data updated, expected publish state.
    [Theory]
    [InlineData(true, false, false, true, "Unchanged")]
    [InlineData(true, true, false, true, "Published")]
    [InlineData(true, false, true, true, "Unpublished")]
    [InlineData(true, true, true, true, "Unpublished")]
    [InlineData(false, false, false, false, "Unchanged")]
    [InlineData(false, true, false, false, "Published")]
    [InlineData(false, false, true, false, "Unpublished")]
    [InlineData(false, true, true, false, "Unpublished")]
    public void ImportSingleItem_FollowsExistingContentTruthTable(
        bool shouldUpdate, bool shouldPublish, bool shouldUnpublish, bool expectDataUpdated, string expectedPublishState)
    {
        // Arrange
        var key = Guid.NewGuid();
        var content = SetupExistingContent(key, published: true);
        var importObject = ExistingRow(key, shouldUpdate, shouldPublish, shouldUnpublish);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        VerifyDataWritten(content, expectDataUpdated ? Times.Once() : Times.Never());
        VerifyPublish(expectedPublishState == "Published" ? Times.Once() : Times.Never());
        VerifyUnpublish(expectedPublishState == "Unpublished" ? Times.Once() : Times.Never());

        // Saving a draft without publishing only happens when data is written and not published
        VerifySave(expectDataUpdated && expectedPublishState != "Published" ? Times.Once() : Times.Never());
    }

    #endregion

    #region ImportSingleItem - new content truth table

    // One case per row of the new-content truth table.
    // Columns: shouldPublish, shouldUnpublish, expected to be published.
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void ImportSingleItem_FollowsNewContentTruthTable(bool shouldPublish, bool shouldUnpublish, bool expectPublished)
    {
        // Arrange
        var content = SetupCreatedContent();
        var importObject = NewRow(shouldPublish, shouldUnpublish);

        // Act
        var result = Import(importObject);

        // Assert
        Assert.True(result.BulkUploadSuccess);
        _mockContentService.Verify(s => s.Create("New Page", RootId, "article", It.IsAny<int>()), Times.Once);
        VerifyDataWritten(content, Times.Once());
        if (expectPublished)
        {
            VerifyPublish(Times.Once());
            VerifySave(Times.Never());
        }
        else
        {
            // Created, saved as a draft
            VerifySave(Times.Once());
            VerifyPublish(Times.Never());
        }
        VerifyUnpublish(Times.Never());
    }

    #endregion

    #region CreateImportObject - bulkUploadShouldUnpublish parsing

    private static Dictionary<string, object> RecordWithUnpublish(string header, object value)
    {
        return new Dictionary<string, object>
        {
            { "name", "Page" },
            { "docTypeAlias", "article" },
            { header, value }
        };
    }

    [Theory]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("TRUE")]
    [InlineData("  Yes  ")]
    public void CreateImportObject_SetsShouldUnpublish_WhenValueIsTruthy(string value)
    {
        // Arrange
        var record = RecordWithUnpublish("bulkUploadShouldUnpublish", value);

        // Act
        var result = _service.CreateImportObject(record);

        // Assert
        Assert.True(result.BulkUploadShouldUnpublish);
        Assert.True(result.BulkUploadShouldUnpublishColumnExisted);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("no")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("maybe")]
    public void CreateImportObject_ClearsShouldUnpublish_WhenValueIsFalsy(string value)
    {
        // Arrange
        var record = RecordWithUnpublish("bulkUploadShouldUnpublish", value);

        // Act
        var result = _service.CreateImportObject(record);

        // Assert
        Assert.False(result.BulkUploadShouldUnpublish);
        Assert.True(result.BulkUploadShouldUnpublishColumnExisted);
    }

    [Fact]
    public void CreateImportObject_ClearsShouldUnpublish_WhenColumnIsAbsent()
    {
        // Arrange
        var record = new Dictionary<string, object>
        {
            { "name", "Page" },
            { "docTypeAlias", "article" }
        };

        // Act
        var result = _service.CreateImportObject(record);

        // Assert
        Assert.False(result.BulkUploadShouldUnpublish);
        Assert.False(result.BulkUploadShouldUnpublishColumnExisted);
    }

    [Theory]
    [InlineData("bulkUploadShouldUnpublish|text")]
    [InlineData("BULKUPLOADSHOULDUNPUBLISH")]
    [InlineData("bulkuploadshouldunpublish|boolean")]
    public void CreateImportObject_SetsShouldUnpublish_WhenHeaderHasResolverSuffixOrOddCasing(string header)
    {
        // Arrange
        var record = RecordWithUnpublish(header, "true");

        // Act
        var result = _service.CreateImportObject(record);

        // Assert
        Assert.True(result.BulkUploadShouldUnpublish);
        Assert.True(result.BulkUploadShouldUnpublishColumnExisted);
    }

    [Fact]
    public void CreateImportObject_DoesNotMapShouldUnpublishToAProperty()
    {
        // Arrange
        var record = RecordWithUnpublish("bulkUploadShouldUnpublish", "true");
        record.Add("title", "Hello");

        // Act
        var result = _service.CreateImportObject(record);

        // Assert
        Assert.NotNull(result.Properties);
        Assert.True(result.Properties!.ContainsKey("title"));
        Assert.False(result.Properties.ContainsKey("bulkUploadShouldUnpublish"));
    }

    #endregion
}
