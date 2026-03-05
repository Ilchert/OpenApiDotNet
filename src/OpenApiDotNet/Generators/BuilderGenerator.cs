using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class BuilderGenerator : BaseGenerator
{
    public override string Namespace { get; }
    public string Key { get; }

    public BuilderGenerator(string key, IOpenApiPathItem apiPathItem, GeneratorContext context) : base(context)
    {
        Key = key;



    }

    public override void Write(CodeWriter writer)
    {
    }
}
