namespace PetStore;

/// <summary>
/// Fixture used by CompilationVerificationTests to exercise every code-generator feature: object models with required/optional properties, all scalar types, component-level enums (plain and hyphenated member names), GET/POST/PUT/DELETE operations, single/multiple/nested path parameters, optional and required query parameters of both value types and reference types, $ref request and response bodies, x-bodyName extension, value-type return, inline object request/response schemas generating nested builder classes, and inline enum/object properties in component schemas generating nested types.
/// </summary>
public interface IPetStoreApi : IOpenApiClient
{
    OwnersBuilder Owners { get => new(this); }
    PetsBuilder Pets { get => new(this); }
    ReportsBuilder Reports { get => new(this); }
}
