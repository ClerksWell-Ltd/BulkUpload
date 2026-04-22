# Plan — Add `pathToMediaPicker` and `zipFileToMediaPicker` resolvers

## Why

`urlToMediaPicker` is currently the only resolver that emits the Media Picker 3
array shape (`[{"key":"…","mediaKey":"…"}]`) for block grid / block list
properties. If the source of a media item is a **local file path** or a **file
inside the upload ZIP**, today the caller has to:

- Send the file bytes as a `data:` URI in the CSV (huge payloads — e.g. an
  11 MB CSV for a single Mantrac PDF with 6 embedded images), or
- Host the file on an HTTP endpoint and use `urlToMediaPicker`, which is
  blocked for localhost / private IPs and adds unnecessary infrastructure
  (web server just to serve one temp file).

This is painful for programmatic importers that generate the CSV server-side
from already-on-disk assets — e.g. the `pdf_to_block_grid` tool in
`codeshare-umbraco-cloud-new`, which renders PDFs, extracts image rasters
to a temp directory, then imports them via BulkUpload.

The existing `PathToMediaResolver` and `ZipFileToMediaResolver` both read a
file and upload it, but they only return a bare UDI string. Inside a block
grid property's MediaPicker3 value, a bare UDI serialises as a plain string
where the picker expects an array, so the image never renders in the
backoffice even though the media item was created correctly.

The fix: two small wrapper resolvers that reuse the existing path/zip upload
logic and transform the resulting UDI into the Media Picker 3 array shape,
mirroring exactly how `UrlToMediaPickerResolver` wraps `UrlToMediaResolver`.

## Scope

Two new resolver aliases:

| Alias                     | Underlying resolver         | Source |
|---------------------------|-----------------------------|--------|
| `pathToMediaPicker`       | `PathToMediaResolver`       | Local or network filesystem path |
| `zipFileToMediaPicker`    | `ZipFileToMediaResolver`    | File inside the uploaded ZIP archive |

Both emit `[{"key":"<new-guid>","mediaKey":"<uploaded-media-guid>"}]` —
identical shape to `urlToMediaPicker`'s output, ready to slot into any
Media Picker 3 property value including nested inside an `objectToJson` block
grid payload.

No changes to existing resolvers. No breaking changes. Purely additive.

## Changes

### 1. `src/BulkUpload/Resolvers/PathToMediaPickerResolver.cs` (new)

Copy `UrlToMediaPickerResolver.cs` verbatim, rename class and constructor
arg, change the injected dep to `PathToMediaResolver`, change `Alias()` to
return `"pathToMediaPicker"`. The transform logic (detect `umb://media/…`
UDI, parse the GUID, wrap in Media Picker 3 array) is identical.

```csharp
public class PathToMediaPickerResolver : IResolver
{
    private const string MediaUdiPrefix = "umb://media/";
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
```

Extract the "detect UDI → wrap as picker array" logic into a shared static
helper `MediaUdiHelper` (new file in `Resolvers/` or `Services/`) so the URL,
path, and zip picker resolvers all call the same code. Reduces copy-paste.
Update `UrlToMediaPickerResolver` to use the helper too.

### 2. `src/BulkUpload/Resolvers/ZipFileToMediaPickerResolver.cs` (new)

Same pattern, wrapping `ZipFileToMediaResolver`. Alias:
`"zipFileToMediaPicker"`.

### 3. `src/BulkUpload/Resolvers/MediaUdiHelper.cs` (new, optional but recommended)

Static helper containing the UDI → Media Picker 3 array conversion.
Consolidates the logic currently inline in `UrlToMediaPickerResolver.Resolve`.

### 4. `src/BulkUpload/BulkUploadComposer.cs`

Register the new resolvers next to the existing ones (around lines 50–52):

```csharp
builder.Services.AddTransient<UrlToMediaResolver>();
builder.Services.AddTransient<IResolver, UrlToMediaPickerResolver>();
builder.Services.AddTransient<PathToMediaResolver>();                   // NEW
builder.Services.AddTransient<IResolver, PathToMediaResolver>();
builder.Services.AddTransient<IResolver, PathToMediaPickerResolver>();  // NEW
builder.Services.AddTransient<ZipFileToMediaResolver>();                // NEW
builder.Services.AddTransient<IResolver, ZipFileToMediaResolver>();
builder.Services.AddTransient<IResolver, ZipFileToMediaPickerResolver>();// NEW
```

The pattern of registering the concrete `UrlToMediaResolver` type
separately (line 49) is already in place so the picker wrapper can inject
it; repeat for `PathToMediaResolver` and `ZipFileToMediaResolver`.

### 5. `objectToJson-media-values.md`

Update the list of supported media resolvers to include the two new picker
variants. Add a side-by-side syntax example showing when to use each.

### 6. `README.md`

Add an example in the media-reference section showing the picker variants:

```
heroImage|pathToMediaPicker:/Blog/Headers/
heroImage|zipFileToMediaPicker:/Blog/Headers/
```

Also add a note that these are required (not `pathToMedia` / `zipFileToMedia`)
when the target property is `Umbraco.MediaPicker3`, which is the default in
Umbraco 13+.

### 7. Tests

New fixtures mirroring the existing ones:

- `src/BulkUpload.Tests/Resolvers/PathToMediaPickerResolverTests.cs`
- `src/BulkUpload.Tests/Resolvers/ZipFileToMediaPickerResolverTests.cs`

Reuse the mocks/harness from `UrlToMediaPickerResolverTests.cs`. Minimum
coverage:

- happy path: file → wrapped array with correct `mediaKey` GUID
- empty/null input → returns empty (don't throw)
- underlying resolver returns empty string → returns empty (don't wrap)
- underlying resolver returns non-UDI string → returns the bare string
  unchanged (back-compat with the current `UrlToMediaPickerResolver`
  fallback behaviour)
- `ParameterizedValue` wrapping with a parent folder parameter is passed
  through to the underlying resolver (so
  `file.jpg|pathToMediaPicker:/Folder/` writes the media into
  `/Folder/`, identical to `urlToMediaPicker`)

If `MediaUdiHelper` is extracted, add a unit test file for it covering the
three branches (valid UDI, malformed UDI, already-wrapped array).

### 8. `CHANGELOG.md`

Under `[Unreleased]`:

```
### Added
- `pathToMediaPicker` resolver: emits Media Picker 3 array format for local
  or network file paths. Previously only `urlToMediaPicker` supported this
  shape, forcing callers to base64-encode file bytes as data URIs or host
  files on HTTP.
- `zipFileToMediaPicker` resolver: same but for files bundled in the
  upload ZIP.

### Changed
- Shared the UDI → Media Picker 3 array conversion between the URL, path,
  and zip picker resolvers (no behavioural change to `urlToMediaPicker`).
```

## Non-goals

- **Do NOT** change how `pathToMedia` / `zipFileToMedia` / `urlToMedia`
  resolve — they still return bare UDI strings and remain the right choice
  for media uploads where the target column is a plain media column (not
  a picker array).
- **Do NOT** auto-detect picker vs non-picker targets. Keeping the aliases
  explicit matches how `urlToMedia` vs `urlToMediaPicker` are already split
  and avoids mystery behaviour in block grids.
- **Do NOT** add `contentPicker` variants in this PR — scope creep; open a
  separate issue if that's desired.

## Version impact

Semver MINOR bump — purely additive, no breaking changes. Target
**v2.1.0** (from current v2.0.5).

## Downstream impact

`codeshare-umbraco-cloud-new` (PDF → Block Grid CLI + Umbraco backoffice
tool) can drop its data URI emission once this ships:

- `BlockGridBuilder.ResolveMediaPicker` today produces
  `data:image/jpeg;base64,…|urlToMediaPicker:/SiteRoot/<slug>`
- After v2.1.0 upgrade it can produce
  `<absolute-temp-path>.jpg|pathToMediaPicker:/SiteRoot/<slug>`
- CSV payload drops from ~11 MB to ~20 KB for a typical single-page PDF
- Upload time drops from seconds to sub-second
- Multi-page PDFs with many images stop bumping against Kestrel body size
  limits

## Estimated effort

- New resolvers (1 class each × 2): ~30 mins
- Shared helper refactor: ~15 mins
- DI registration: ~2 mins
- Tests: ~45 mins
- Docs: ~15 mins

Total: **~2 hours** of focused work, one PR.

## Implementation order

1. Add `MediaUdiHelper` and refactor `UrlToMediaPickerResolver` to use it.
   Run the existing `UrlToMediaPickerResolverTests` unchanged — must stay
   green. This de-risks step 2/3.
2. Add `PathToMediaPickerResolver` + tests.
3. Add `ZipFileToMediaPickerResolver` + tests.
4. Register in composer, wire up any concrete-type registrations missing.
5. Docs and changelog.
6. Manual smoke test against the PDF pipeline: generate a CSV using the new
   alias against a temp-path image, import, verify the MediaPicker renders in
   the backoffice and the content publishes.
