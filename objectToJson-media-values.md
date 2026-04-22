# Setting Media Values in `objectToJson` JSON

When a CSV column uses the `objectToJson` resolver, any string value within the JSON can reference a media file using the pipe syntax. The preprocessor will upload the media and the resolver will replace the value with the resulting media UDI.

## Syntax

```
"propertyName": "mediaReference|resolverAlias"
"propertyName": "mediaReference|resolverAlias:parentFolder"
```

## Supported resolvers

| Resolver | Input | Output shape | Parent folder support |
|---|---|---|---|
| `urlToMedia` | Absolute HTTP/HTTPS URL | Bare media UDI | Yes |
| `urlToMediaPicker` | Absolute HTTP/HTTPS URL | Media Picker 3 array | Yes |
| `pathToMedia` | Local or network file path | Bare media UDI | Yes |
| `pathToMediaPicker` | Local or network file path | Media Picker 3 array | Yes |
| `zipFileToMedia` | Filename from the uploaded ZIP | Bare media UDI | Yes |
| `zipFileToMediaPicker` | Filename from the uploaded ZIP | Media Picker 3 array | Yes |
| `guidToMediaUdi` | Existing media item GUID | Bare media UDI | No |

### Which variant to use

- Use the **`…ToMedia`** variants when the target property stores a plain media UDI string (e.g. a custom property or the legacy media picker).
- Use the **`…ToMediaPicker`** variants when the target property is `Umbraco.MediaPicker3` (the default in Umbraco 13+). This includes media pickers nested inside block list / block grid JSON. A bare UDI string is rendered as an empty picker in the backoffice even though the underlying media item is created correctly.

## Parent folder formats (optional)

- Integer ID: `urlToMedia:1234`
- GUID: `urlToMedia:a1b2c3d4-e5f6-7890-abcd-ef1234567890`
- Path (created if missing): `urlToMedia:/Blog/Images`

## Examples

**URL:**
```json
{ "image": "https://cdn.example.com/photo.jpg|urlToMedia" }
{ "image": "https://cdn.example.com/photo.jpg|urlToMedia:/Blog/Images" }
```

**Local file path:**
```json
{ "image": "/mnt/assets/photo.jpg|pathToMedia" }
{ "image": "/mnt/assets/photo.jpg|pathToMedia:/Blog/Images" }
{ "image": "/mnt/assets/photo.jpg|pathToMediaPicker:/Blog/Images" }
```

**File from uploaded ZIP:**
```json
{ "image": "photo.jpg|zipFileToMedia" }
{ "image": "subfolder/photo.jpg|zipFileToMedia:/Blog/Images" }
{ "image": "photo.jpg|zipFileToMediaPicker" }
```

**Existing media by GUID:**
```json
{ "image": "a1b2c3d4-e5f6-7890-abcd-ef1234567890|guidToMediaUdi" }
```

## Works anywhere in the JSON tree

The resolver walks the entire object recursively, so media references work at any depth and inside arrays:

```json
{
  "layout": { "Umbraco.BlockList": [...] },
  "contentData": [
    {
      "udi": "umb://element/abc",
      "heroImage": "https://cdn.example.com/hero.jpg|urlToMedia:/Heroes",
      "thumbnail": "thumb.jpg|zipFileToMedia",
      "gallery": [
        "https://cdn.example.com/a.jpg|urlToMedia",
        "https://cdn.example.com/b.jpg|urlToMedia"
      ]
    }
  ]
}
```

## Strings without a recognised resolver alias are left unchanged

If the part after the last `|` does not match any registered resolver, the string is written to the output as-is. This means normal text values containing `|` are safe as long as the suffix is not a valid resolver alias.
