using Newtonsoft.Json;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that downloads media from a URL and returns it in Media Picker 3 format.
///
/// Delegates media creation to <see cref="UrlToMediaResolver"/>, then transforms the
/// resulting UDI into the JSON array format expected by Media Picker 3 properties:
///   [{"key":"element-guid","mediaKey":"media-item-guid"}]
///
/// Use this resolver instead of urlToMedia when the target property uses Media Picker 3
/// (the standard media picker in Umbraco 13+ and 17+).
///
/// Supports the same parent folder specification as urlToMedia:
///   imageUrl|urlToMediaPicker
///   imageUrl|urlToMediaPicker:/Folder/Path/
/// </summary>
public class UrlToMediaPickerResolver : IResolver
{
    private const string MediaUdiPrefix = "umb://media/";
    private readonly UrlToMediaResolver _urlToMediaResolver;

    public UrlToMediaPickerResolver(UrlToMediaResolver urlToMediaResolver)
    {
        _urlToMediaResolver = urlToMediaResolver;
    }

    public string Alias() => "urlToMediaPicker";

    public object Resolve(object value)
    {
        var result = _urlToMediaResolver.Resolve(value);

        if (result is string udiStr && udiStr.StartsWith(MediaUdiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var guidStr = udiStr[MediaUdiPrefix.Length..];
            if (Guid.TryParse(guidStr, out var mediaGuid))
            {
                var pickerItems = new[]
                {
                    new { key = Guid.NewGuid(), mediaKey = mediaGuid }
                };
                return JsonConvert.SerializeObject(pickerItems);
            }
        }

        return result;
    }
}
