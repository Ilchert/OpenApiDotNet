using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

#if NET8_0_OR_GREATER
using BinkyLabs.OpenApi.Overlays;
#endif

namespace OpenApiDotNet;

internal static class OpenApiDocumentLoader
{
#if NET8_0_OR_GREATER
    public static async Task<OpenApiDocument> LoadWithOverlaysAsync(
        Stream openApiStream,
        string? openApiPath,
        IEnumerable<(Stream Stream, string? Path)> overlayStreams,
        OpenApiReaderSettings openApiReaderSettings,
        Action<string>? onError = null,
        Action<string>? onWarning = null)
    {
        var readerSettings = new OverlayReaderSettings { OpenApiSettings = openApiReaderSettings };

        var overlayDocument = new OverlayDocument();

        foreach (var (stream, path) in overlayStreams)
        {
            var overlayDisplayPath = path ?? "overlay";
            var (overlay, diagnostic) = await OverlayDocument.LoadFromStreamAsync(stream, DetectFormat(path), readerSettings);

            if (diagnostic?.Errors.Count > 0)
            {
                onError?.Invoke($"Errors found in overlay '{overlayDisplayPath}':");
                foreach (var error in diagnostic.Errors)
                    onError?.Invoke($"  - {error.Message}");
            }

            if (diagnostic?.Warnings.Count > 0)
            {
                onWarning?.Invoke($"Warnings found in overlay '{overlayDisplayPath}':");
                foreach (var warning in diagnostic.Warnings)
                    onWarning?.Invoke($"  - {warning.Message}");
            }

            if (overlay != null)
                overlayDocument = overlayDocument.CombineWith(overlay);
        }

        var documentUri = new Uri(openApiPath ?? "file:///document", UriKind.RelativeOrAbsolute);
        if (!documentUri.IsAbsoluteUri)
            documentUri = new Uri($"file:///{openApiPath}", UriKind.Absolute);

        var (result, overlayDiagnostic) = await overlayDocument.ApplyToDocumentStreamAndLoadAsync(
            openApiStream,
            documentUri,
            DetectFormat(openApiPath),
            readerSettings);

        if (overlayDiagnostic?.Errors.Count > 0)
        {
            onError?.Invoke("Errors found after applying overlays:");
            foreach (var error in overlayDiagnostic.Errors)
                onError?.Invoke($"  - {error.Message}");
        }

        if (overlayDiagnostic?.Warnings.Count > 0)
        {
            onWarning?.Invoke("Warnings:");
            foreach (var warning in overlayDiagnostic.Warnings)
                onWarning?.Invoke($"  - {warning.Message}");
        }

        return result ?? throw new InvalidOperationException("Cannot load document after applying overlays");
    }
#endif

    public static async Task<OpenApiDocument> LoadAsync(
        Stream openApiStream,
        string? openApiPath,
        OpenApiReaderSettings openApiReaderSettings,
        Action<string>? onError = null,
        Action<string>? onWarning = null)
    {
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(openApiStream, DetectFormat(openApiPath), openApiReaderSettings);

        if (diagnostic?.Errors.Count > 0)
        {
            onError?.Invoke("Errors found in OpenAPI document:");
            foreach (var error in diagnostic.Errors)
                onError?.Invoke($"  - {error.Message}");
        }

        if (diagnostic?.Warnings.Count > 0)
        {
            onWarning?.Invoke("Warnings:");
            foreach (var warning in diagnostic.Warnings)
                onWarning?.Invoke($"  - {warning.Message}");
        }

        return document ?? throw new InvalidOperationException($"Cannot load document from {openApiPath ?? "stream"}");
    }

    private static string? DetectFormat(string? path)
    {
        if (path is null)
            return null;

        return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            ? "yaml"
            : null;
    }
}
