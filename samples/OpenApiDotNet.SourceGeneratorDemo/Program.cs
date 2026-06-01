using OpenApiDotNet.SourceGeneratorDemo;
using OpenApiDotNet.SourceGeneratorDemo.Models;

IPetStoreAPIClient? client = null;
Pet? pet = null;

Console.WriteLine(client is null && pet is null
    ? "Source generator demo compiled."
    : "Unexpected state.");
