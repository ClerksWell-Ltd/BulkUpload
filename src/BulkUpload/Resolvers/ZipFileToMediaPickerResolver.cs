namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that looks up a media item uploaded from the ZIP archive during preprocessing
/// and returns it in Media Picker 3 format.
///
/// Delegates the cache lookup to <see cref="ZipFileToMediaResolver"/>, then transforms the
/// resulting UDI into the JSON array format expected by Media Picker 3 properties:
///   [{"key":"element-guid","mediaKey":"media-item-guid"}]
///
/// Use this resolver instead of zipFileToMedia when the target property uses Media Picker 3
/// (the standard media picker in Umbraco 13+ and 17+).
///
/// Usage in CSV header: propertyName|zipFileToMediaPicker
/// CSV value: filename.jpg (the filename from the ZIP file)
/// </summary>
public class ZipFileToMediaPickerResolver : IResolver
{
    private readonly ZipFileToMediaResolver _zipFileToMediaResolver;

    public ZipFileToMediaPickerResolver(ZipFileToMediaResolver zipFileToMediaResolver)
    {
        _zipFileToMediaResolver = zipFileToMediaResolver;
    }

    public string Alias() => "zipFileToMediaPicker";

    public object Resolve(object value)
    {
        var result = _zipFileToMediaResolver.Resolve(value);
        return MediaUdiHelper.WrapAsPickerArray(result) ?? result;
    }
}
