using System;
using System.Linq;
using System.Text.Json;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The compatibility contract with the Listes web app, asserted against the file that app
/// actually exported.
/// </summary>
public sealed class ListJsonSerializerTests
{
    private readonly SequentialIdGenerator _ids = new();

    private ListJsonSerializer Serializer => new(_ids);

    [Fact]
    public void Deserialize_ReadsTheReferenceList()
    {
        TaskList list = Serializer.Deserialize(TestLists.ReadJudithJson());

        Assert.Equal("Liste Judith", list.Name);
        Assert.Equal(
            ["Habits", "Dormir", "Change / bain", "Pharmacie", "Jouets", "Manger", "Promener", "Autre"],
            list.Categories.Select(category => category.Name));
        Assert.Equal(
            [18, 13, 6, 13, 4, 18, 14, 8],
            list.Categories.Select(category => category.Items.Count));

        ListProgress progress = ListProgress.Of(list);
        Assert.Equal(94, progress.Total);
        Assert.Equal(80, progress.Done);
    }

    [Fact]
    public void Deserialize_KeepsTheIdentifiersTheFileCarries()
    {
        TaskList list = Serializer.Deserialize(TestLists.ReadJudithJson());

        Assert.Equal("c-mtzxeu79-2-2lsj", list.Categories[0].Id);
        Assert.Equal("i-mtzxeu79-3-9zk2", list.Categories[0].Items[0].Id);
        Assert.Equal(0, _ids.Count);
    }

    /// <summary>
    /// The whole point of the exercise: what this application writes is what the web app wrote.
    /// </summary>
    [Fact]
    public void RoundTrip_OfTheReferenceList_IsByteIdentical()
    {
        string expected = TestLists.ReadJudithJson();

        string actual = ListJsonSerializer.Serialize(Serializer.Deserialize(expected));

        Assert.Equal(expected.TrimEnd('\n'), actual);
    }

    [Fact]
    public void Serialize_UsesTwoSpaceIndentationAndLineFeeds()
    {
        string json = ListJsonSerializer.Serialize(TestLists.Sample(_ids));

        Assert.StartsWith("{\n  \"schemaVersion\": 1,\n  \"name\": \"Essai\",\n", json, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', json);
        Assert.EndsWith("}", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shape a list freshly made on the phone has — an empty category, a quote in the name —
    /// written exactly as <c>JSON.stringify(list, null, 2)</c> writes it.
    /// </summary>
    [Fact]
    public void Serialize_WritesAnEmptyCategoryAndEscapesQuotesAsJsonStringify()
    {
        TaskList list = new("Liste \"vacances\"", [new TaskCategory("c-1", "Vide")]);

        string json = ListJsonSerializer.Serialize(list);

        Assert.Equal(
            "{\n" +
            "  \"schemaVersion\": 1,\n" +
            "  \"name\": \"Liste \\\"vacances\\\"\",\n" +
            "  \"categories\": [\n" +
            "    {\n" +
            "      \"id\": \"c-1\",\n" +
            "      \"name\": \"Vide\",\n" +
            "      \"items\": []\n" +
            "    }\n" +
            "  ]\n" +
            "}",
            json);
    }

    [Fact]
    public void Serialize_WritesAccentsLiterally()
    {
        TaskList list = Serializer.Deserialize(TestLists.ReadJudithJson());

        string json = ListJsonSerializer.Serialize(list);

        Assert.Contains("\"text\": \"Écharpe\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u00c9", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_KeepsThePropertyOrderTheFormatDefines()
    {
        string json = ListJsonSerializer.Serialize(TestLists.Sample(_ids));

        Assert.True(json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal)
            < json.IndexOf("\"name\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"name\"", StringComparison.Ordinal)
            < json.IndexOf("\"categories\"", StringComparison.Ordinal));

        int item = json.IndexOf("\"text\"", StringComparison.Ordinal);
        Assert.True(json.IndexOf("\"id\"", StringComparison.Ordinal) < item);
        Assert.True(item < json.IndexOf("\"done\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_AcceptsAHandWrittenFileWithNoVersionNoIdsAndNoDone()
    {
        const string Json = """
            {
              "name": "À la main",
              "categories": [
                { "name": "Habits", "items": [ { "text": "Bodys" }, { "text": "Pulls" } ] }
              ]
            }
            """;

        TaskList list = Serializer.Deserialize(Json);

        Assert.Equal("À la main", list.Name);
        Assert.Equal("c-1", list.Categories[0].Id);
        Assert.Equal(["i-2", "i-3"], list.Categories[0].Items.Select(item => item.Id));
        Assert.All(list.Categories[0].Items, item => Assert.False(item.Done));
    }

    [Fact]
    public void Deserialize_TrimsTextAndFallsBackToTheDefaultName()
    {
        const string Json = """
            { "name": "   ", "categories": [ { "name": " Habits ", "items": [ { "text": "  Bodys  " } ] } ] }
            """;

        TaskList list = Serializer.Deserialize(Json);

        Assert.Equal(ListSchema.DefaultListName, list.Name);
        Assert.Equal("Habits", list.Categories[0].Name);
        Assert.Equal("Bodys", list.Categories[0].Items[0].Text);
    }

    [Fact]
    public void Deserialize_ReplacesRepeatedIdentifiers()
    {
        const string Json = """
            {
              "categories": [
                { "id": "c-x", "name": "Une", "items": [ { "id": "same", "text": "A" }, { "id": "same", "text": "B" } ] },
                { "id": "c-x", "name": "Deux", "items": [] }
              ]
            }
            """;

        TaskList list = Serializer.Deserialize(Json);

        string[] ids =
        [
            list.Categories[0].Id,
            list.Categories[1].Id,
            list.Categories[0].Items[0].Id,
            list.Categories[0].Items[1].Id,
        ];

        Assert.Equal(4, ids.Distinct().Count());
        Assert.Equal("c-x", list.Categories[0].Id);
        Assert.Equal("same", list.Categories[0].Items[0].Id);
    }

    [Fact]
    public void Deserialize_RefusesAFileFromANewerVersion()
    {
        ListValidationException exception = Assert.Throws<ListValidationException>(
            () => Serializer.Deserialize("""{ "schemaVersion": 2, "categories": [] }"""));

        Assert.Equal("schema-too-new", exception.Errors[0].Code);
        Assert.Contains("schemaVersion 2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("highest supported: 1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_RefusesTextThatIsNotJson()
    {
        ListValidationException exception = Assert.Throws<ListValidationException>(
            () => Serializer.Deserialize("# Liste Judith"));

        Assert.Equal("bad-json", exception.Errors[0].Code);
        Assert.StartsWith("Unreadable JSON file: ", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_RefusesATruncatedFile()
    {
        string truncated = TestLists.ReadJudithJson()[..500];

        Assert.Throws<ListValidationException>(() => Serializer.Deserialize(truncated));
    }

    [Fact]
    public void Normalize_RefusesAPayloadThatIsNotAList()
    {
        using JsonDocument document = JsonDocument.Parse("[]");

        ListValidationException exception = Assert.Throws<ListValidationException>(
            () => Serializer.Normalize(document.RootElement));

        Assert.Equal("not-object", exception.Errors[0].Code);
    }
}
