using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// Turns a list name into a file name — <c>Liste Judith</c> becomes <c>liste-judith.json</c>.
/// </summary>
/// <remarks>
/// The rule is the web app's <c>suggestName</c>: fold the accents away, lower-case it, and
/// replace every run of anything else with a single hyphen. It matters beyond looks — these
/// files are copied to phones, attached to messages and synced through folders that are not all
/// as relaxed about accents, spaces and slashes as the local file system is.
/// </remarks>
public static class ListFileName
{
    /// <summary>
    /// The extension of a saved list.
    /// </summary>
    public const string JsonExtension = ".json";

    /// <summary>
    /// The extension of an exported Markdown list.
    /// </summary>
    public const string MarkdownExtension = ".md";

    /// <summary>
    /// What a list with no usable name is called.
    /// </summary>
    private const string Fallback = "list";

    /// <summary>
    /// Suggests a file name for a list.
    /// </summary>
    /// <param name="listName">The list's name.</param>
    /// <param name="extension">The extension to append, dot included.</param>
    /// <returns>The file name.</returns>
    public static string Suggest(string? listName, string extension = JsonExtension)
        => Slug(listName) + extension;

    /// <summary>
    /// Folds a list name down to the slug part of a file name, without an extension.
    /// </summary>
    /// <param name="listName">The list's name.</param>
    /// <returns>The slug.</returns>
    public static string Slug(string? listName)
    {
        string source = (listName ?? string.Empty).Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(source.Length);
        bool pendingSeparator = false;

        foreach (char character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char lowered = char.ToLowerInvariant(character);

            if (lowered is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingSeparator = false;
                builder.Append(lowered);
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return builder.Length == 0 ? Fallback : builder.ToString();
    }

    /// <summary>
    /// Finds a file name that is free in a folder, adding <c>-2</c>, <c>-3</c>… until it is.
    /// </summary>
    /// <param name="folder">The folder the file goes in.</param>
    /// <param name="listName">The list's name.</param>
    /// <param name="extension">The extension to append, dot included.</param>
    /// <param name="ignore">
    /// A path that does not count as taken — the file being renamed, so that changing a list's
    /// capitalisation does not push it to <c>-2</c>.
    /// </param>
    /// <returns>The full path to use.</returns>
    public static string Unique(string folder, string? listName, string extension = JsonExtension, string? ignore = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);

        string slug = Slug(listName);
        string candidate = Path.Combine(folder, slug + extension);
        int suffix = 1;

        while (IsTaken(candidate, ignore))
        {
            suffix++;
            candidate = Path.Combine(
                folder,
                string.Create(CultureInfo.InvariantCulture, $"{slug}-{suffix}{extension}"));
        }

        return candidate;
    }

    private static bool IsTaken(string candidate, string? ignore)
        => File.Exists(candidate) &&
           !(ignore is not null && string.Equals(
               Path.GetFullPath(candidate),
               Path.GetFullPath(ignore),
               StringComparison.OrdinalIgnoreCase));
}
