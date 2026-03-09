using System.Text.Json;

namespace OpenApiDotNet.Tests;

public class UnifiedPathJsonConverterTests
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = false };

    [Fact]
    public void Write_ReplacesBackslashesWithForwardSlashes()
    {
        var config = new GenerationConfig { OpenApiFile = @"..\..\petstore.json" };

        var json = JsonSerializer.Serialize(config, s_options);

        Assert.Contains("\"openApiFile\":\"../../petstore.json\"", json);
    }

    [Fact]
    public void Write_PreservesForwardSlashes()
    {
        var config = new GenerationConfig { OpenApiFile = "../../petstore.json" };

        var json = JsonSerializer.Serialize(config, s_options);

        Assert.Contains("\"openApiFile\":\"../../petstore.json\"", json);
    }

    [Fact]
    public void Read_ConvertsForwardSlashesToOsSeparator()
    {
        var json = """{"openApiFile":"../../petstore.json"}""";

        var config = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        var expected = $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}petstore.json";
        Assert.Equal(expected, config.OpenApiFile);
    }

    [Fact]
    public void RoundTrip_PreservesForwardSlashesInJson()
    {
        var config = new GenerationConfig
        {
            OpenApiFile = $"..{Path.DirectorySeparatorChar}api{Path.DirectorySeparatorChar}spec.json",
            OutputDirectory = ".",
            Namespace = "Test"
        };

        var json = JsonSerializer.Serialize(config, s_options);
        var deserialized = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        Assert.Equal(config.OpenApiFile, deserialized.OpenApiFile);
        Assert.Contains("\"openApiFile\":\"../api/spec.json\"", json);
    }
}

public class UnifiedPathListJsonConverterTests
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = false };

    [Fact]
    public void Write_ReplacesBackslashesInAllItems()
    {
        var config = new GenerationConfig
        {
            GeneratedFiles = [@"Models\Pet.cs", @"Builders\PetsBuilder.cs"]
        };

        var json = JsonSerializer.Serialize(config, s_options);

        Assert.Contains("\"Models/Pet.cs\"", json);
        Assert.Contains("\"Builders/PetsBuilder.cs\"", json);
    }

    [Fact]
    public void Read_ConvertsForwardSlashesToOsSeparatorInAllItems()
    {
        var json = """{"generatedFiles":["Models/Pet.cs","Builders/PetsBuilder.cs"]}""";

        var config = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        Assert.Equal(
        [
            $"Models{Path.DirectorySeparatorChar}Pet.cs",
            $"Builders{Path.DirectorySeparatorChar}PetsBuilder.cs"
        ], config.GeneratedFiles);
    }

    [Fact]
    public void Read_NullArray_ReturnsNull()
    {
        var json = """{"generatedFiles":null}""";

        var config = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        Assert.Null(config.GeneratedFiles);
    }

    [Fact]
    public void Read_EmptyArray_ReturnsEmptyList()
    {
        var json = """{"overlayFiles":[]}""";

        var config = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        Assert.Empty(config.OverlayFiles);
    }

    [Fact]
    public void RoundTrip_PreservesForwardSlashesInJson()
    {
        var config = new GenerationConfig
        {
            GeneratedFiles =
            [
                $"Models{Path.DirectorySeparatorChar}Pet.cs",
                $"Builders{Path.DirectorySeparatorChar}Pets{Path.DirectorySeparatorChar}IdBuilder.cs"
            ]
        };

        var json = JsonSerializer.Serialize(config, s_options);
        var deserialized = JsonSerializer.Deserialize<GenerationConfig>(json, s_options)!;

        Assert.Equal(config.GeneratedFiles, deserialized.GeneratedFiles);
        Assert.Contains("\"Models/Pet.cs\"", json);
        Assert.Contains("\"Builders/Pets/IdBuilder.cs\"", json);
    }
}
