using PetStore;
using PetStore.Models;

// Create the HTTP client pointing at a pet store API
var httpClient = new HttpClient { BaseAddress = new Uri("https://petstore.example.com") };
IOpenApiClient client = new PetStoreApiClient(httpClient);

// ── List all pets (GET /pets?limit=10) ───────────────────────────
Console.WriteLine("=== List pets (limit 10) ===");
var pets = await client.Pets.Get(limit: 10);
foreach (var pet in pets)
{
    Console.WriteLine($"  #{pet.Id} {pet.Name} — status: {pet.Status}");
}

// ── Create a new pet (POST /pets) ────────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Create a pet ===");
var newPet = new NewPet { Name = "Buddy", Tag = "dog" };
var created = await client.Pets.Post(newPet);
Console.WriteLine($"  Created #{created.Id} {created.Name}");

// ── Get a single pet (GET /pets/123) ─────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Get pet 123 ===");
var pet123 = await client.Pets[123].Get();
Console.WriteLine($"  #{pet123.Id} {pet123.Name}, vaccinated: {pet123.Vaccinated}");

// ── Delete a pet (DELETE /pets/123) ──────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Delete pet 123 ===");
await client.Pets[123].Delete();
Console.WriteLine("  Deleted.");

// ── Deep path navigation (GET /pets/123/photos/{photoId}) ────────
Console.WriteLine();
Console.WriteLine("=== Get photo of pet 123 ===");
var photo = await client.Pets[123].Photos[Guid.NewGuid()].Get();
Console.WriteLine($"  Photo URL: {photo.Url}");

// ── Owner-scoped pet (GET /owners/{ownerId}/pets/{petId}) ────────
Console.WriteLine();
Console.WriteLine("=== Get owner's pet ===");
var ownerPet = await client.Owners["owner-42"].Pets[1].Get();
Console.WriteLine($"  Owner's pet: #{ownerPet.Id} {ownerPet.Name}");
