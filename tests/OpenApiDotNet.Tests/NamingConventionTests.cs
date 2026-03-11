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
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingConventions.ToPascalCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "_unnamed")]
    [InlineData("-", "_unnamed")]
    [InlineData("___", "_unnamed")]
    public void ToPascalCase_EmptyOrSeparatorsOnly_ReturnsFallback(string input, string expected)
    {
        var result = NamingConventions.ToPascalCase(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("3dModel", "_3dModel")]
    [InlineData("123abc", "_123abc")]
    public void ToPascalCase_DigitLeading_PrefixesUnderscore(string input, string expected)
    {
        var result = NamingConventions.ToPascalCase(input);
        Assert.Equal(expected, result);
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
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingConventions.ToCamelCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("event", "@event")]
    [InlineData("class", "@class")]
    [InlineData("object", "@object")]
    [InlineData("default", "@default")]
    [InlineData("string", "@string")]
    [InlineData("int", "@int")]
    [InlineData("new", "@new")]
    [InlineData("namespace", "@namespace")]
    public void ToCamelCase_CSharpKeyword_EscapesWithAtSign(string input, string expected)
    {
        var result = NamingConventions.ToCamelCase(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Event", false)]
    [InlineData("MyClass", false)]
    [InlineData("PetId", false)]
    public void ToPascalCase_NotAKeyword_NoEscaping(string input, bool startsWithAt)
    {
        var result = NamingConventions.ToPascalCase(input);
        Assert.Equal(startsWithAt, result.StartsWith('@'));
    }

    [Theory]
    [InlineData("event")]
    [InlineData("class")]
    [InlineData("int")]
    public void EscapeIfKeyword_Keywords_ReturnsEscaped(string input)
    {
        var result = NamingConventions.EscapeIfKeyword(input);
        Assert.Equal($"@{input}", result);
    }

    [Theory]
    [InlineData("Event")]
    [InlineData("myVar")]
    [InlineData("petId")]
    public void EscapeIfKeyword_NonKeywords_ReturnsUnchanged(string input)
    {
        var result = NamingConventions.EscapeIfKeyword(input);
        Assert.Equal(input, result);
    }
}
