using Newtonsoft.Json;

namespace BulkUpload.Resolvers;

/// <summary>
/// Shared helpers for converting between media UDI strings and Media Picker 3 array shapes.
///
/// Media Picker 3 (the standard media picker in Umbraco 13+ and 17+) stores its value as a
/// JSON array of objects, each with a unique element <c>key</c> and the target media's
/// <c>mediaKey</c>:
///
///   [{"key":"&lt;element-guid&gt;","mediaKey":"&lt;media-item-guid&gt;"}]
///
/// The various *ToMedia resolvers return a bare UDI string (<c>umb://media/&lt;guid&gt;</c>),
/// which is the right shape for plain media reference properties but is rendered as an
/// empty picker when written to a Media Picker 3 property. The *ToMediaPicker wrapper
/// resolvers use this helper to bridge between the two shapes.
/// </summary>
internal static class MediaUdiHelper
{
    private const string MediaUdiPrefix = "umb://media/";

    /// <summary>
    /// If <paramref name="resolverResult"/> is a media UDI string, returns the serialised
    /// Media Picker 3 array for that media item. Otherwise returns null, so the caller can
    /// pass the original value through unchanged (e.g. an empty string from a failed upload).
    /// </summary>
    public static string? WrapAsPickerArray(object? resolverResult)
    {
        if (resolverResult is not string udiStr || string.IsNullOrEmpty(udiStr))
        {
            return null;
        }

        if (!udiStr.StartsWith(MediaUdiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var guidStr = udiStr[MediaUdiPrefix.Length..];
        if (!Guid.TryParse(guidStr, out var mediaGuid))
        {
            return null;
        }

        var pickerItems = new[]
        {
            new
            {
                key = Guid.NewGuid(),
                mediaKey = mediaGuid,
                mediaTypeAlias = string.Empty,
                crops = Array.Empty<object>(),
                focalPoint = (object?)null
            }
        };
        return JsonConvert.SerializeObject(pickerItems);
    }
}
