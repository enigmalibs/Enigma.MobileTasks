using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// A whole checklist: a name and its categories.
/// </summary>
/// <remarks>
/// The schema version is deliberately not a field here. It belongs to the file, not to the
/// list: a file is read only when its version is one this application understands, and it is
/// always written as <see cref="ListSchema.Version"/>.
/// </remarks>
public sealed class TaskList
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="name">The list name.</param>
    /// <param name="categories">The categories it starts with, if any.</param>
    public TaskList(string name, IEnumerable<TaskCategory>? categories = null)
    {
        Name = name;
        Categories = categories is null ? [] : [.. categories];
    }

    /// <summary>
    /// Gets or sets the list name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the categories of this list, in the order they are shown.
    /// </summary>
    public List<TaskCategory> Categories { get; }
}
