using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// A named group of items — the "Habits" or "Pharmacie" card of a packing list.
/// </summary>
public sealed class TaskCategory
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="id">The stable identifier, unique within its list.</param>
    /// <param name="name">The category name.</param>
    /// <param name="items">The items it starts with, if any.</param>
    public TaskCategory(string id, string name, IEnumerable<TaskItem>? items = null)
    {
        Id = id;
        Name = name;
        Items = items is null ? [] : [.. items];
    }

    /// <summary>
    /// Gets or sets the stable identifier, unique within its list.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the items of this category, in the order they are shown.
    /// </summary>
    public List<TaskItem> Items { get; }
}
