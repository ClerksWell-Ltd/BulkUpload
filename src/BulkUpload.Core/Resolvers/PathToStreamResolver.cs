using Microsoft.Extensions.Logging;

using BulkUpload.Core.Models;

namespace BulkUpload.Core.Resolvers;

/// <summary>
/// Resolver that extracts file path information for streaming (does not create media).
/// Used by bulk upload to get file streams from local/network paths.
/// Alias: pathToStream
/// </summary>
public class PathToStreamResolver : IResolver
{
    private readonly ILogger<PathToStreamResolver> _logger;

    public PathToStreamResolver(ILogger<PathToStreamResolver> logger)
    {
        _logger = logger;
    }

    public string Alias() => "pathToStream";

    public object Resolve(object value)
    {
        try
        {
            if (value == null)
            {
                _logger.LogWarning("PathToStreamResolver: Received null value");
                return string.Empty;
            }

            string? filePath = null;
            string? parameter = null;

            // Handle ParameterizedValue from resolver factory
            if (value is ParameterizedValue parameterizedValue)
            {
                filePath = parameterizedValue.Value?.ToString();
                parameter = parameterizedValue.Parameter;
            }
            else
            {
                var valueStr = value.ToString();
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    _logger.LogWarning("PathToStreamResolver: Empty value provided");
                    return string.Empty;
                }

                // Check if value contains pipe separator for inline parameter
                var parts = valueStr.Split('|', 2);
                filePath = parts[0].Trim();

                if (parts.Length > 1)
                {
                    parameter = parts[1].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("PathToStreamResolver: File path is empty");
                return string.Empty;
            }

            // Validate file exists
            if (!File.Exists(filePath))
            {
                _logger.LogError("PathToStreamResolver: File not found: {FilePath}", filePath);
                return string.Empty;
            }

            // Return MediaSource object with file path information
            return new MediaSource
            {
                Type = MediaSourceType.FilePath,
                Value = filePath,
                Parameter = parameter
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PathToStreamResolver: Error resolving file path");
            return string.Empty;
        }
    }
}