namespace OpenApiDotNet;

/// <summary>
/// Helper class for building URLs with proper encoding
/// </summary>
public static class UrlBuilder
{
    /// <summary>
    /// Builds a URL from a template and parameters with proper encoding
    /// </summary>
    public static string Build(string template, Dictionary<string, object?>? pathParameters = null, Dictionary<string, object?>? queryParameters = null)
    {
        var url = template;

        // Replace path parameters
        if (pathParameters != null)
        {
            foreach (var param in pathParameters)
            {
                var encodedValue = Uri.EscapeDataString(param.Value?.ToString() ?? string.Empty);
                url = url.Replace($"{{{param.Key}}}", encodedValue);
            }
        }

        // Add query parameters
        if (queryParameters != null && queryParameters.Any())
        {
            var queryString = BuildQueryString(queryParameters);
            url = $"{url}?{queryString}";
        }

        return url;
    }

    /// <summary>
    /// Builds a query string from parameters with proper encoding
    /// </summary>
    public static string BuildQueryString(Dictionary<string, object?> parameters)
    {
        var queryParts = new List<string>();

        foreach (var param in parameters)
        {
            if (param.Value == null)
                continue;

            // Handle array/list parameters
            if (param.Value is System.Collections.IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var encodedKey = Uri.EscapeDataString(param.Key);
                        var encodedValue = Uri.EscapeDataString(item.ToString() ?? string.Empty);
                        queryParts.Add($"{encodedKey}={encodedValue}");
                    }
                }
            }
            else
            {
                var encodedKey = Uri.EscapeDataString(param.Key);
                var encodedValue = Uri.EscapeDataString(param.Value.ToString() ?? string.Empty);
                queryParts.Add($"{encodedKey}={encodedValue}");
            }
        }

        return string.Join("&", queryParts);
    }

    /// <summary>
    /// Encodes a path segment
    /// </summary>
    public static string EncodePath(string value)
    {
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Encodes a query parameter value
    /// </summary>
    public static string EncodeQuery(string value)
    {
        return Uri.EscapeDataString(value);
    }
}
