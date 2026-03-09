using System.CommandLine;
using System.CommandLine.Completions;
using Microsoft.Extensions.FileProviders;
using OpenApiDotNet;
using OpenApiDotNet.IO;

var service = new GenerationService();

// default generate command
var openApiFileArgument = new Argument<FileInfo>("openapi-file")
{
    Description = "Path to the OpenAPI specification file (JSON or YAML)",
    CompletionSources = { JsonYamlCompletion }
}.AcceptExistingOnly();

var outputOption = new Option<DirectoryInfo>("--output", "-o")
{
    Description = "Directory where generated code will be placed",
    DefaultValueFactory = _ => new DirectoryInfo("./Generated"),
}.AcceptLegalFilePathsOnly();

var namespaceOption = new Option<string>("--namespace", "-n")
{
    Description = "Namespace for generated code",
    DefaultValueFactory = _ => "GeneratedClient"
};

var namespacePrefixOption = new Option<string?>("--namespace-prefix", "-p")
{
    Description = "Strip this dotted prefix from schema names when generating namespaces (e.g. 'Commerce' turns 'Commerce.Order' into 'Order')"
};

var clientNameOption = new Option<string?>("--client-name", "-c")
{
    Description = "Custom name for the generated client class (default: derived from API title)"
};

var overlayOption = new Option<FileInfo[]>("--overlay")
{
    Description = "Path to overlay file(s) to apply before generation. Can be specified multiple times.",
    Arity = ArgumentArity.ZeroOrMore,
    CompletionSources = { JsonYamlCompletion }
}.AcceptExistingOnly();

var rootCommand = new RootCommand
{
    openApiFileArgument,
    outputOption,
    namespaceOption,
    namespacePrefixOption,
    clientNameOption,
    overlayOption,
};

rootCommand.SetAction(async parseResult =>
{
    var openApiFile = parseResult.GetValue(openApiFileArgument)!;
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var namespaceName = parseResult.GetValue(namespaceOption)!;
    var namespacePrefix = parseResult.GetValue(namespacePrefixOption);
    var clientName = parseResult.GetValue(clientNameOption);
    var overlayFiles = parseResult.GetValue(overlayOption) ?? [];
    Directory.CreateDirectory(outputDirectory.FullName);
    using var outputProvider = new PhysicalWritableFileProvider(outputDirectory.FullName);
    var openApiFileInfo = new PhysicalWritableFileInfo(openApiFile);
    var overlayFileInfos = overlayFiles.Select(f => (IFileInfo)new PhysicalWritableFileInfo(f)).ToArray();
    await service.GenerateAsync(openApiFileInfo, outputProvider, namespaceName, namespacePrefix, clientName, overlayFileInfos, null);
});


// update comand config
var updateConfigArgument = new Argument<FileInfo>("config-file")
{
    Description = $"Path to the {GenerationConfig.FileName} configuration file",
    DefaultValueFactory = _ => new FileInfo(GenerationConfig.FileName)
}.AcceptExistingOnly();

var updateCommand = new Command("update", "Re-generate client code using a previously saved configuration file")
{
    updateConfigArgument
};

updateCommand.SetAction(async parseResult =>
{
    var configFile = parseResult.GetValue(updateConfigArgument)!;
    await service.UpdateAsync(configFile);
});

rootCommand.Subcommands.Add(updateCommand);

var convertOutputArgument = new Argument<FileInfo>("output-file")
{
    Description = "Path for the converted output file"
}.AcceptLegalFileNamesOnly();

var versionOption = new Option<string>("--version", "-v")
{
    Description = "Target OpenAPI specification version",
    DefaultValueFactory = _ => "3.2"
}.AcceptOnlyFromAmong("2.0", "3.0", "3.1", "3.2");

var formatOption = new Option<string>("--format", "-f")
{
    Description = "Output format",
    DefaultValueFactory = _ => "json"
}.AcceptOnlyFromAmong("json", "yaml");

var convertCommand = new Command("convert", "Convert an OpenAPI specification to a specific version and format")
{
    openApiFileArgument,
    convertOutputArgument,
    versionOption,
    formatOption,
};

convertCommand.SetAction(async parseResult =>
{
    var inputFile = parseResult.GetValue(openApiFileArgument)!;
    var outputFile = parseResult.GetValue(convertOutputArgument)!;
    var version = parseResult.GetValue(versionOption)!;
    var format = parseResult.GetValue(formatOption)!;
    var inputFileInfo = new PhysicalWritableFileInfo(inputFile);
    var outputFileInfo = new PhysicalWritableFileInfo(outputFile);
    await service.ConvertAsync(inputFileInfo, outputFileInfo, version, format);
});

rootCommand.Subcommands.Add(convertCommand);

return rootCommand.Parse(args).Invoke();

static IEnumerable<string> JsonYamlCompletion(CompletionContext ctx)
{
    var pattern = ctx.WordToComplete;
    var directory = Path.GetDirectoryName(pattern);
    if (string.IsNullOrEmpty(directory))
        directory = ".";

    if (!Directory.Exists(directory))
        return [];

    return Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
        .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                  || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                  || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
}
