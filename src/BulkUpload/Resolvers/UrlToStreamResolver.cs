using BulkUpload.Models;

using Microsoft.Extensions.Logging;

namespace BulkUpload.Resolvers;

/// <summary>
/// Resolver that extracts URL information for streaming (does not create media).
/// Used by bulk upload to get file streams from URLs.
/// Alias: urlToStream
/// </summary>
public class UrlToStreamResolver : IResolver
{
    private readonly ILogger<UrlToStreamResolver> _logger;

    public UrlToStreamResolver(ILogger<UrlToStreamResolver> logger)
    {
        _logger = logger;
    }

    public string Alias() => "urlToStream";

    public object Resolve(object value)
    {
        try
        {
            if (value == null)
            {
                _logger.LogWarning("UrlToStreamResolver: Received null value");
                return string.Empty;
            }

            string? url = null;
            string? parameter = null;

            // Handle ParameterizedValue from resolver factory
            if (value is ParameterizedValue parameterizedValue)
            {
                url = parameterizedValue.Value?.ToString();
                parameter = parameterizedValue.Parameter;
            }
            else
            {
                var valueStr = value.ToString();
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    _logger.LogWarning("UrlToStreamResolver: Empty value provided");
                    return string.Empty;
                }

                // Check if value contains pipe separator for inline parameter
                var parts = valueStr.Split('|', 2);
                url = parts[0].Trim();

                if (parts.Length > 1)
                {
                    parameter = parts[1].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("UrlToStreamResolver: URL is empty");
                return string.Empty;
            }

            // Validate URL format
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _logger.LogError("UrlToStreamResolver: Invalid URL format: {Url}", url);
                return string.Empty;
            }

            // Validate URL scheme (only HTTP/HTTPS)
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogError("UrlToStreamResolver: Only HTTP/HTTPS URLs are supported: {Url}", url);
                return string.Empty;
            }

            // Return MediaSource object with URL information
            return new MediaSource
            {
                Type = MediaSourceType.Url,
                Value = url,
                Parameter = parameter
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UrlToStreamResolver: Error resolving URL");
            return string.Empty;
        }
    }
}