using BulkUpload.Services;

namespace Umbraco.Community.BulkUpload.Tests.Services;

public class DataUriParserTests
{
    // 1x1 transparent PNG
    private const string PngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    [Fact]
    public void IsDataUri_ReturnsTrue_ForDataScheme()
    {
        Assert.True(DataUriParser.IsDataUri("data:image/png;base64,AAAA"));
        Assert.True(DataUriParser.IsDataUri("DATA:image/png;base64,AAAA"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com/image.png")]
    [InlineData("/path/to/file.png")]
    public void IsDataUri_ReturnsFalse_ForNonDataValues(string? value)
    {
        Assert.False(DataUriParser.IsDataUri(value));
    }

    [Fact]
    public void TryParse_DecodesBase64Payload()
    {
        var dataUri = $"data:image/png;base64,{PngBase64}";

        var ok = DataUriParser.TryParse(dataUri, out var mimeType, out var bytes);

        Assert.True(ok);
        Assert.Equal("image/png", mimeType);
        Assert.NotEmpty(bytes);
        Assert.Equal(0x89, bytes[0]); // PNG magic byte
    }

    [Fact]
    public void TryParse_NormalisesMimeTypeCase()
    {
        var dataUri = $"data:IMAGE/JPEG;base64,{PngBase64}";

        var ok = DataUriParser.TryParse(dataUri, out var mimeType, out _);

        Assert.True(ok);
        Assert.Equal("image/jpeg", mimeType);
    }

    [Fact]
    public void TryParse_DefaultsMimeType_WhenMissing()
    {
        var dataUri = $"data:;base64,{PngBase64}";

        var ok = DataUriParser.TryParse(dataUri, out var mimeType, out _);

        Assert.True(ok);
        Assert.Equal("application/octet-stream", mimeType);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenNotBase64Encoded()
    {
        // URL-encoded data URIs are not supported in the current design
        var dataUri = "data:image/png,%89PNG%0D%0A";

        var ok = DataUriParser.TryParse(dataUri, out _, out var bytes);

        Assert.False(ok);
        Assert.Empty(bytes);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenPayloadIsMalformedBase64()
    {
        var dataUri = "data:image/png;base64,@@@not-base64@@@";

        var ok = DataUriParser.TryParse(dataUri, out _, out var bytes);

        Assert.False(ok);
        Assert.Empty(bytes);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenPayloadIsEmpty()
    {
        var dataUri = "data:image/png;base64,";

        var ok = DataUriParser.TryParse(dataUri, out _, out var bytes);

        Assert.False(ok);
        Assert.Empty(bytes);
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForNonDataUri()
    {
        var ok = DataUriParser.TryParse("https://example.com/x.png", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenCommaMissing()
    {
        var ok = DataUriParser.TryParse("data:image/png;base64", out _, out _);
        Assert.False(ok);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/svg+xml", ".svg")]
    [InlineData("image/bmp", ".bmp")]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("application/octet-stream", ".bin")]
    public void GenerateFileName_UsesCorrectExtension(string mimeType, string expectedExtension)
    {
        var bytes = new byte[] { 1, 2, 3, 4 };

        var name = DataUriParser.GenerateFileName(mimeType, bytes);

        Assert.EndsWith(expectedExtension, name);
        Assert.StartsWith("data-image-", name);
    }

    [Fact]
    public void GenerateFileName_IsDeterministic_ForIdenticalBytes()
    {
        var bytes = new byte[] { 10, 20, 30, 40, 50 };

        var a = DataUriParser.GenerateFileName("image/png", bytes);
        var b = DataUriParser.GenerateFileName("image/png", bytes);

        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateFileName_DiffersForDifferentBytes()
    {
        var a = DataUriParser.GenerateFileName("image/png", new byte[] { 1, 2, 3 });
        var b = DataUriParser.GenerateFileName("image/png", new byte[] { 4, 5, 6 });

        Assert.NotEqual(a, b);
    }
}
