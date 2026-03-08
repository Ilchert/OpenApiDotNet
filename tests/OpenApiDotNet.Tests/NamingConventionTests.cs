using FluentAssertions;
using OpenApiDotNet.Generators;

namespace OpenApiDotNet.Tests;

public class NamingConventionTests
{
    [Theory]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("hello.world", "HelloWorld")]
    [InlineData("HELLO_WORLD", "HELLOWORLD")]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("HelloWorld", "HelloWorld")]
    [InlineData("pet_id", "PetId")]
    [InlineData("user-name", "UserName")]
    [InlineData("created_at", "CreatedAt")]
    [InlineData("birthDate", "BirthDate")]
    [InlineData("createdAt", "CreatedAt")]
    [InlineData("", "")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingConventions.ToPascalCase(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("hello-world", "helloWorld")]
    [InlineData("hello world", "helloWorld")]
    [InlineData("HELLO_WORLD", "hELLOWORLD")]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("pet_id", "petId")]
    [InlineData("user-name", "userName")]
    [InlineData("created_at", "createdAt")]
    [InlineData("birthDate", "birthDate")]
    [InlineData("", "")]
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingConventions.ToCamelCase(input);

        // Assert
        result.Should().Be(expected);
    }
}
