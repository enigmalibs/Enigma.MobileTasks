using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Everything the application remembers between runs.
/// </summary>
/// <remarks>
/// Immutable: a change is a new instance produced with <c>with</c>, which is what lets the
/// settings service hand the same snapshot to every reader without any of them being able to
/// change it behind the others' backs.
/// </remarks>
public sealed record AppSettings
{
    /// <summary>
    /// Gets the theme to paint in.
    /// </summary>
    /// <remarks>
    /// System by default: a phone reports its dark mode reliably, and following it is what a mobile
    /// user expects. (The desktop application defaults to Dark only because a Linux desktop cannot
    /// always say which variant it is in.)
    /// </remarks>
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    /// <summary>
    /// Gets the folded categories, keyed by the list's file name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> CollapsedCategories { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Returns these settings with one list's folded categories replaced.
    /// </summary>
    /// <param name="listFile">The list's file name.</param>
    /// <param name="categoryIds">The categories that are folded.</param>
    /// <returns>The new settings.</returns>
    public AppSettings WithCollapsed(string listFile, IReadOnlyList<string> categoryIds)
    {
        Dictionary<string, IReadOnlyList<string>> collapsed = new(CollapsedCategories);

        if (categoryIds.Count == 0)
        {
            collapsed.Remove(listFile);
        }
        else
        {
            collapsed[listFile] = categoryIds;
        }

        return this with { CollapsedCategories = collapsed };
    }

    /// <summary>
    /// Gets the folded categories of one list.
    /// </summary>
    /// <param name="listFile">The list's file name.</param>
    /// <returns>The folded category identifiers, empty when none are.</returns>
    public IReadOnlyList<string> CollapsedIn(string listFile)
        => CollapsedCategories.TryGetValue(listFile, out IReadOnlyList<string>? ids) ? ids : [];
}
