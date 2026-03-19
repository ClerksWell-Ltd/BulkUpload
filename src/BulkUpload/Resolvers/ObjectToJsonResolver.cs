using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that serializes an object to a JSON string.
///
/// When the input is a JSON string, it also walks all string values within the JSON
/// and resolves any embedded resolver hints using the pipe separator syntax.
///
/// Embedded resolver syntax within JSON string values:
///   "actualValue|resolverAlias"
///   "actualValue|resolverAlias:parameter"
///
/// Examples:
///   "https://example.com/image.jpg|urlToMedia"
///   "/path/to/file.jpg|pathToMedia:/MediaFolder"
///   "image.jpg|zipFileToMedia"
///   "a1b2c3d4-e5f6-7890-abcd-ef1234567890|guidToMediaUdi"
///
/// The resolved value (typically a media UDI string) replaces the original string value
/// in the JSON before it is returned.
/// </summary>
public class ObjectToJsonResolver : IResolver
{
    private readonly IResolverFactory _resolverFactory;
    private readonly ILogger<ObjectToJsonResolver> _logger;

    public ObjectToJsonResolver(IResolverFactory resolverFactory, ILogger<ObjectToJsonResolver> logger)
    {
        _resolverFactory = resolverFactory;
        _logger = logger;
    }

    public string Alias() => "objectToJson";

    public object Resolve(object value)
    {
        if (value is null) return string.Empty;

        // For non-string inputs (e.g. programmatic use with a pre-built object), serialize directly
        if (value is not string jsonStr)
            return JsonConvert.SerializeObject(value);

        if (string.IsNullOrWhiteSpace(jsonStr))
            return string.Empty;

        // Try to parse as JSON so we can walk and resolve embedded resolver hints
        JToken token;
        try
        {
            token = JToken.Parse(jsonStr);
        }
        catch (JsonException)
        {
            // Not valid JSON - serialize as a plain string value
            return JsonConvert.SerializeObject(jsonStr);
        }

        ProcessToken(token);
        return token.ToString(Formatting.None);
    }

    private void ProcessToken(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                foreach (var property in obj.Properties())
                    ProcessProperty(property);
                break;

            case JArray arr:
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i] is JValue { Type: JTokenType.String } strVal)
                    {
                        var resolved = TryResolveString(strVal.Value<string>()!);
                        if (resolved != null)
                            arr[i] = resolved;
                    }
                    else
                    {
                        ProcessToken(arr[i]);
                    }
                }
                break;
        }
    }

    private void ProcessProperty(JProperty property)
    {
        if (property.Value is JValue { Type: JTokenType.String } strVal)
        {
            var resolved = TryResolveString(strVal.Value<string>()!);
            if (resolved != null)
                property.Value = resolved;
        }
        else
        {
            ProcessToken(property.Value);
        }
    }

    /// <summary>
    /// Checks whether a string contains an embedded resolver hint (value|resolverAlias),
    /// and if so resolves it, returning the replacement JToken.
    /// Returns null if no resolver hint is found or resolution fails.
    /// </summary>
    private JToken? TryResolveString(string str)
    {
        if (string.IsNullOrEmpty(str)) return null;

        var pipeIndex = str.LastIndexOf('|');
        if (pipeIndex <= 0 || pipeIndex == str.Length - 1) return null;

        var rawValue = str[..pipeIndex];
        var resolverAlias = str[(pipeIndex + 1)..];

        // Guard against self-reference (infinite recursion)
        var baseAlias = resolverAlias.Split(':')[0];
        if (string.Equals(baseAlias, Alias(), StringComparison.OrdinalIgnoreCase)) return null;

        var resolver = _resolverFactory.GetByAlias(resolverAlias);
        if (resolver == null) return null;

        try
        {
            var result = resolver.Resolve(rawValue);
            if (result is string resultStr)
            {
                // If the result is a JSON array or object, embed as structured JSON
                // so that resolvers returning e.g. Media Picker 3 format are stored
                // as proper JSON structures rather than escaped strings.
                if (!string.IsNullOrEmpty(resultStr))
                {
                    var trimmed = resultStr.TrimStart();
                    if (trimmed.Length > 0 && (trimmed[0] == '[' || trimmed[0] == '{'))
                    {
                        try { return JToken.Parse(resultStr); }
                        catch (JsonException) { /* Not valid JSON, fall through to JValue */ }
                    }
                }

                return new JValue(resultStr);
            }
            if (result != null)
                return JToken.FromObject(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ObjectToJsonResolver: Error resolving embedded value with resolver '{Alias}'", resolverAlias);
        }

        return null;
    }
}
