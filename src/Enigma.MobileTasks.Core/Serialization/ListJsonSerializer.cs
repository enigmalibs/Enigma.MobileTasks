using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Enigma.MobileTasks.Core.Identity;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Serialization;

/// <summary>
/// Reads and writes the list file format — the one the Listes web app defines.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The output is byte-for-byte what <c>JSON.stringify(list, null, 2)</c> produces</strong>:
/// the same property order, two-space indentation, <c>\n</c> line endings, no trailing newline,
/// UTF-8 without a byte-order mark, and accented characters written literally rather than
/// escaped. That is not cosmetic — it is what lets the same file be opened, edited and saved by
/// either application without the other one seeing a change.
/// </para>
/// <para>
/// Reading is forgiving in exactly the ways the web app is: <c>schemaVersion</c> may be absent,
/// identifiers may be missing or repeated, <c>done</c> may be absent, and text is trimmed. A
/// <c>schemaVersion</c> from the future is refused rather than half-read.
/// </para>
/// </remarks>
public sealed class ListJsonSerializer
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        IndentCharacter = ' ',
        IndentSize = 2,

        // JSON.stringify writes \n whatever the platform, and leaves everything that is not a
        // quote, a backslash or a control character alone. The relaxed encoder is the one that
        // matches; the default would escape every accent as \uXXXX.
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IListIdGenerator _ids;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="ids">Where identifiers come from for a file that is missing some.</param>
    public ListJsonSerializer(IListIdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _ids = ids;
    }

    /// <summary>
    /// Writes a list in the file format.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <returns>The file's text.</returns>
    public static string Serialize(TaskList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", ListSchema.Version);
            writer.WriteString("name", list.Name);
            writer.WritePropertyName("categories");
            writer.WriteStartArray();

            foreach (TaskCategory category in list.Categories)
            {
                writer.WriteStartObject();
                writer.WriteString("id", category.Id);
                writer.WriteString("name", category.Name);
                writer.WritePropertyName("items");
                writer.WriteStartArray();

                foreach (TaskItem item in category.Items)
                {
                    writer.WriteStartObject();
                    writer.WriteString("id", item.Id);
                    writer.WriteString("text", item.Text);
                    writer.WriteBoolean("done", item.Done);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes a list to a stream, in the file format and without a byte-order mark.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <param name="destination">Where to write it.</param>
    public static void Serialize(TaskList list, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        byte[] bytes = Encoding.UTF8.GetBytes(Serialize(list));
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Reads a list from the file format.
    /// </summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The list, with every identifier present and unique.</returns>
    /// <exception cref="ListValidationException">
    /// The text is not JSON, or is JSON that is not a list this application can read. The
    /// exception's message is the one to show the user.
    /// </exception>
    public TaskList Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new ListValidationException(new ListValidationError(
                "bad-json",
                string.Empty,
                "Unreadable JSON file: " + exception.Message));
        }

        using (document)
        {
            return Normalize(document.RootElement);
        }
    }

    /// <summary>
    /// Turns a validated payload into a list: trimmed text, a boolean for every
    /// <c>done</c>, and an identifier for everything that lacks one or repeats another's.
    /// </summary>
    /// <param name="root">The parsed JSON.</param>
    /// <returns>The list.</returns>
    /// <exception cref="ListValidationException">The payload does not validate.</exception>
    public TaskList Normalize(JsonElement root)
    {
        ListValidationResult result = ListValidator.Validate(root);

        if (!result.IsValid)
        {
            throw new ListValidationException(result.Errors);
        }

        HashSet<string> seen = [];
        string? name = ReadString(root, "name");

        TaskList list = new(string.IsNullOrWhiteSpace(name) ? ListSchema.DefaultListName : name.Trim());

        foreach (JsonElement category in root.GetProperty("categories").EnumerateArray())
        {
            TaskCategory target = new(
                UniqueId(seen, ReadString(category, "id"), "c"),
                ReadString(category, "name")!.Trim());

            if (category.TryGetProperty("items", out JsonElement items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    target.Items.Add(new TaskItem(
                        UniqueId(seen, ReadString(item, "id"), "i"),
                        ReadString(item, "text")!.Trim(),
                        item.TryGetProperty("done", out JsonElement done) && done.ValueKind == JsonValueKind.True));
                }
            }

            list.Categories.Add(target);
        }

        return list;
    }

    private static string? ReadString(JsonElement owner, string property)
        => owner.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Keeps the identifier the file carries when it is usable, and mints one when it is
    /// missing or already taken — so a hand-written file loads, and a file that repeats an
    /// identifier does not end up with two rows the application cannot tell apart.
    /// </summary>
    /// <param name="seen">The identifiers already used by this list.</param>
    /// <param name="candidate">What the file says, if anything.</param>
    /// <param name="prefix">The prefix for a freshly minted identifier.</param>
    /// <returns>The identifier to use.</returns>
    private string UniqueId(HashSet<string> seen, string? candidate, string prefix)
    {
        string id = string.IsNullOrWhiteSpace(candidate) ? _ids.NewId(prefix) : candidate.Trim();

        while (!seen.Add(id))
        {
            id = _ids.NewId(prefix);
        }

        return id;
    }
}
