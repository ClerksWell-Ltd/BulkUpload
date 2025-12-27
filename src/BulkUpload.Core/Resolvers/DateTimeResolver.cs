namespace BulkUpload.Core.Resolvers;

public class DateTimeResolver : IResolver
{
    public string Alias() => "dateTime";

    public object Resolve(object value)
    {
        if (value is not string str || !DateTime.TryParse(str, out var dateTime))
            return string.Empty;

        // Format as ISO 8601 (e.g., "2025-09-12T14:30:00")
        return dateTime.ToString("o");
    }
}