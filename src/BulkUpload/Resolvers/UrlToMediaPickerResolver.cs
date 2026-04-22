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
    private readonly UrlToMediaResolver _urlToMediaResolver;

    public UrlToMediaPickerResolver(UrlToMediaResolver urlToMediaResolver)
    {
        _urlToMediaResolver = urlToMediaResolver;
    }

    public string Alias() => "urlToMediaPicker";

    public object Resolve(object value)
    {
        var result = _urlToMediaResolver.Resolve(value);
        return MediaUdiHelper.WrapAsPickerArray(result) ?? result;
    }
}
