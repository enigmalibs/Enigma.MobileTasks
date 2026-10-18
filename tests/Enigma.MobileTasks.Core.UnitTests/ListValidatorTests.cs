using System.Linq;
using System.Text.Json;
using Enigma.MobileTasks.Core.Serialization;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// Every way a payload can be refused, with the code and the message the Listes web app uses.
/// </summary>
public sealed class ListValidatorTests
{
    [Fact]
    public void Validate_AcceptsAMinimalList()
    {
        Assert.True(Validate("""{ "categories": [] }""").IsValid);
    }

    [Theory]
    [InlineData("\"texte\"", "not-object")]
    [InlineData("[]", "not-object")]
    [InlineData("42", "not-object")]
    [InlineData("""{ "schemaVersion": "1", "categories": [] }""", "bad-schema-version")]
    [InlineData("""{ "schemaVersion": 0, "categories": [] }""", "bad-schema-version")]
    [InlineData("""{ "schemaVersion": 2, "categories": [] }""", "schema-too-new")]
    [InlineData("""{ "name": 3, "categories": [] }""", "bad-name")]
    [InlineData("{ }", "categories-missing")]
    [InlineData("""{ "categories": { } }""", "categories-missing")]
    [InlineData("""{ "categories": [ "Habits" ] }""", "category-not-object")]
    [InlineData("""{ "categories": [ { } ] }""", "category-name-missing")]
    [InlineData("""{ "categories": [ { "name": "  " } ] }""", "category-name-missing")]
    [InlineData("""{ "categories": [ { "name": "Habits", "items": { } } ] }""", "category-items-not-array")]
    [InlineData("""{ "categories": [ { "name": "Habits", "items": [ "Bodys" ] } ] }""", "item-not-object")]
    [InlineData("""{ "categories": [ { "name": "Habits", "items": [ { } ] } ] }""", "item-text-missing")]
    [InlineData("""{ "categories": [ { "name": "Habits", "items": [ { "text": " " } ] } ] }""", "item-text-missing")]
    [InlineData("""{ "categories": [ { "name": "H", "items": [ { "text": "B", "done": "oui" } ] } ] }""", "item-done-not-boolean")]
    public void Validate_ReportsTheRightCode(string json, string expectedCode)
    {
        ListValidationResult result = Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(expectedCode, result.Errors.Select(error => error.Code));
    }

    [Fact]
    public void Validate_PointsAtWhereTheProblemIs()
    {
        ListValidationResult result = Validate("""
            {
              "categories": [
                { "name": "Habits", "items": [ { "text": "Bodys" } ] },
                { "name": "Dormir", "items": [ { "text": "Doudou" }, { } ] }
              ]
            }
            """);

        ListValidationError error = Assert.Single(result.Errors);
        Assert.Equal("item-text-missing", error.Code);
        Assert.Equal("categories[1].items[1].text", error.Path);
        Assert.Equal("Item 2 of category 2 has no text.", error.Message);
    }

    [Fact]
    public void Validate_ReportsEverythingWrongAtOnce()
    {
        ListValidationResult result = Validate("""
            { "name": 1, "categories": [ { }, { "name": "Dormir", "items": [ { } ] } ] }
            """);

        Assert.Equal(
            ["bad-name", "category-name-missing", "item-text-missing"],
            result.Errors.Select(error => error.Code));
    }

    [Fact]
    public void Validate_StopsAtAMissingCategoriesArray()
    {
        ListValidationResult result = Validate("""{ "name": 1 }""");

        Assert.Equal(["bad-name", "categories-missing"], result.Errors.Select(error => error.Code));
    }

    private static ListValidationResult Validate(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return ListValidator.Validate(document.RootElement);
    }
}
