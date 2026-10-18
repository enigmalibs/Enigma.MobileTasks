using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Serialization;

/// <summary>
/// Checks that a parsed JSON payload is a list this application can read, and says exactly what
/// is wrong when it is not.
/// </summary>
/// <remarks>
/// <para>
/// Validation runs over the raw <see cref="JsonElement"/> rather than over a deserialised
/// object, because half of what has to be said is about shapes a typed deserialiser would have
/// quietly coerced: a category that is a string, an <c>items</c> that is an object, a
/// <c>done</c> that is the text <c>"true"</c>.
/// </para>
/// <para>
/// The codes are the Listes web app's, one for one, and the messages are Enigma.Tasks' English
/// wording of them, so a file this application refuses is refused on the desktop for the same
/// stated reason.
/// </para>
/// </remarks>
public static class ListValidator
{
    /// <summary>
    /// Validates a payload.
    /// </summary>
    /// <param name="root">The parsed JSON.</param>
    /// <returns>What is wrong with it, if anything.</returns>
    public static ListValidationResult Validate(JsonElement root)
    {
        List<ListValidationError> errors = [];

        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ListValidationError("not-object", string.Empty, "The content is not a JSON object."));
            return new ListValidationResult(errors);
        }

        ValidateSchemaVersion(root, errors);
        ValidateName(root, errors);

        if (!root.TryGetProperty("categories", out JsonElement categories) ||
            categories.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ListValidationError(
                "categories-missing",
                "categories",
                "The file has no \"categories\" array."));

            return new ListValidationResult(errors);
        }

        int categoryIndex = 0;

        foreach (JsonElement category in categories.EnumerateArray())
        {
            ValidateCategory(category, categoryIndex, errors);
            categoryIndex++;
        }

        return errors.Count == 0 ? ListValidationResult.Ok : new ListValidationResult(errors);
    }

    private static void ValidateSchemaVersion(JsonElement root, List<ListValidationError> errors)
    {
        if (!root.TryGetProperty("schemaVersion", out JsonElement version) ||
            version.ValueKind == JsonValueKind.Undefined)
        {
            // A file written by hand, or by an older build, is read and normalised rather than
            // refused: the shape is what matters, and the shape has not changed.
            return;
        }

        if (version.ValueKind != JsonValueKind.Number ||
            !version.TryGetDouble(out double value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < 1)
        {
            errors.Add(new ListValidationError(
                "bad-schema-version",
                "schemaVersion",
                "The \"schemaVersion\" field is invalid."));

            return;
        }

        if (value > ListSchema.Version)
        {
            errors.Add(new ListValidationError(
                "schema-too-new",
                "schemaVersion",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"This file comes from a newer version of the application (schemaVersion {Number(value)}, highest supported: {ListSchema.Version}).")));
        }
    }

    private static void ValidateName(JsonElement root, List<ListValidationError> errors)
    {
        if (root.TryGetProperty("name", out JsonElement name) &&
            name.ValueKind is not (JsonValueKind.String or JsonValueKind.Undefined))
        {
            errors.Add(new ListValidationError("bad-name", "name", "The list's name must be text."));
        }
    }

    private static void ValidateCategory(JsonElement category, int index, List<ListValidationError> errors)
    {
        string path = string.Create(CultureInfo.InvariantCulture, $"categories[{index}]");
        int number = index + 1;

        if (category.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ListValidationError(
                "category-not-object",
                path,
                string.Create(CultureInfo.InvariantCulture, $"Category {number} is not an object.")));

            return;
        }

        if (!category.TryGetProperty("name", out JsonElement name) ||
            name.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(name.GetString()))
        {
            errors.Add(new ListValidationError(
                "category-name-missing",
                path + ".name",
                string.Create(CultureInfo.InvariantCulture, $"Category {number} has no name.")));
        }

        if (!category.TryGetProperty("items", out JsonElement items) ||
            items.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        if (items.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ListValidationError(
                "category-items-not-array",
                path + ".items",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The items of category {number} are not an array.")));

            return;
        }

        int itemIndex = 0;

        foreach (JsonElement item in items.EnumerateArray())
        {
            ValidateItem(item, path, number, itemIndex, errors);
            itemIndex++;
        }
    }

    private static void ValidateItem(
        JsonElement item,
        string categoryPath,
        int categoryNumber,
        int index,
        List<ListValidationError> errors)
    {
        string path = string.Create(CultureInfo.InvariantCulture, $"{categoryPath}.items[{index}]");
        int number = index + 1;

        if (item.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ListValidationError(
                "item-not-object",
                path,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Item {number} of category {categoryNumber} is not an object.")));

            return;
        }

        if (!item.TryGetProperty("text", out JsonElement text) ||
            text.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(text.GetString()))
        {
            errors.Add(new ListValidationError(
                "item-text-missing",
                path + ".text",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Item {number} of category {categoryNumber} has no text.")));
        }

        if (item.TryGetProperty("done", out JsonElement done) &&
            done.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Undefined))
        {
            errors.Add(new ListValidationError(
                "item-done-not-boolean",
                path + ".done",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The ticked state of item {number} of category {categoryNumber} is invalid.")));
        }
    }

    /// <summary>
    /// Writes a schema version the way JavaScript would, so <c>2</c> is not reported as
    /// <c>2.0</c> in the message the user reads.
    /// </summary>
    /// <param name="value">The version found in the file.</param>
    /// <returns>Its text form.</returns>
    private static string Number(double value)
        => value == Math.Floor(value) && !double.IsInfinity(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
