using System.Security.Cryptography;

namespace BulkUpload.Services;

/// <summary>
/// Parses RFC 2397 base64 data URIs of the form <c>data:[&lt;mediatype&gt;][;base64],&lt;data&gt;</c>
/// and generates deterministic file names for the decoded payload.
/// Only the <c>;base64</c> encoding is supported.
/// </summary>
public static class DataUriParser
{
    private const string DataUriScheme = "data:";

    public static bool IsDataUri(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith(DataUriScheme, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a base64 data URI into its MIME type and decoded bytes.
    /// Returns false for non-data URIs, non-base64 data URIs, or malformed payloads.
    /// </summary>
    public static bool TryParse(string dataUri, out string mimeType, out byte[] bytes)
    {
        mimeType = string.Empty;
        bytes = Array.Empty<byte>();

        if (!IsDataUri(dataUri))
            return false;

        var commaIndex = dataUri.IndexOf(',');
        if (commaIndex < 0)
            return false;

        var header = dataUri.Substring(DataUriScheme.Length, commaIndex - DataUriScheme.Length);
        var payload = dataUri.Substring(commaIndex + 1);

        var headerParts = header.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var hasBase64 = false;
        string? parsedMime = null;

        foreach (var part in headerParts)
        {
            var trimmed = part.Trim();
            if (string.Equals(trimmed, "base64", StringComparison.OrdinalIgnoreCase))
                hasBase64 = true;
            else if (parsedMime == null && trimmed.Contains('/'))
                parsedMime = trimmed.ToLowerInvariant();
        }

        if (!hasBase64)
            return false;

        mimeType = string.IsNullOrEmpty(parsedMime) ? "application/octet-stream" : parsedMime;

        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }

        return bytes.Length > 0;
    }

    /// <summary>
    /// Generates a deterministic file name from the content hash and MIME type,
    /// e.g. <c>data-image-3f9ac0b1.png</c>. Identical payloads always produce the
    /// same name, which keeps the media folder tidy on repeat imports.
    /// </summary>
    public static string GenerateFileName(string mimeType, byte[] bytes)
    {
        var hashBytes = SHA256.HashData(bytes);
        var hash = Convert.ToHexString(hashBytes, 0, 4).ToLowerInvariant();
        var extension = GetExtensionForMimeType(mimeType);
        return $"data-image-{hash}{extension}";
    }

    private static string GetExtensionForMimeType(string? mimeType) =>
        (mimeType ?? string.Empty).ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "image/tiff" => ".tiff",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
}
