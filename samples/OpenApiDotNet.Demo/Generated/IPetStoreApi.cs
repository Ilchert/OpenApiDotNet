namespace PetStore;

/// <summary>
/// Fixture used by CompilationVerificationTests to exercise every code-generator feature: object models with required/optional properties, all scalar types, component-level enums (plain and hyphenated member names), GET/POST/PUT/DELETE operations, single/multiple/nested path parameters, optional and required query parameters of both value types and reference types, System.Object query parameter (untyped schema), $ref request and response bodies, x-bodyName extension, value-type return, inline object request/response schemas generating nested builder classes, inline enum/object properties in component schemas generating nested types, and component schema with explicit $id.
/// </summary>
public interface IPetStoreApi : IOpenApiClient
{
    public virtual PetStore.Builders.PetsBuilder Pets => new(this);

    public virtual PetStore.Builders.ReportsBuilder Reports => new(this);

    public virtual PetStore.Builders.OwnersBuilder Owners => new(this);

    public virtual PetStore.Builders.SearchBuilder Search => new(this);

    public virtual PetStore.Builders.ClinicsBuilder Clinics => new(this);

}
