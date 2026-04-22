namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that reads a media file from a local or network path and returns it in
/// Media Picker 3 format.
///
/// Delegates media creation to <see cref="PathToMediaResolver"/>, then transforms the
/// resulting UDI into the JSON array format expected by Media Picker 3 properties:
///   [{"key":"element-guid","mediaKey":"media-item-guid"}]
///
/// Use this resolver instead of pathToMedia when the target property uses Media Picker 3
/// (the standard media picker in Umbraco 13+ and 17+).
///
/// Supports the same parent folder specification as pathToMedia:
///   imageFile|pathToMediaPicker
///   imageFile|pathToMediaPicker:/Folder/Path/
/// </summary>
public class PathToMediaPickerResolver : IResolver
{
    private readonly PathToMediaResolver _pathToMediaResolver;

    public PathToMediaPickerResolver(PathToMediaResolver pathToMediaResolver)
    {
        _pathToMediaResolver = pathToMediaResolver;
    }

    public string Alias() => "pathToMediaPicker";

    public object Resolve(object value)
    {
        var result = _pathToMediaResolver.Resolve(value);
        return MediaUdiHelper.WrapAsPickerArray(result) ?? result;
    }
}
