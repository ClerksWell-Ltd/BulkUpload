namespace Umbraco.Community.BulkUpload.Models;

/// <summary>
/// Wraps a value with an optional parameter from the resolver alias.
/// Used to pass alias-level parameters to resolvers without changing the IResolver interface.
/// Example: For alias "urlToMedia:1234", parameter would be "1234"
/// </summary>
public class ParameterizedValue
{
    public required object Value { get; init; }
    public string? Parameter { get; init; }
}