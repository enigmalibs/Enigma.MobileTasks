namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// One line of a checklist: a label and whether it is ticked off.
/// </summary>
/// <remarks>
/// Mutable on purpose. A list is edited in place — ticked, renamed, moved — and every change
/// goes through <see cref="Operations.ListOperations"/>, which is what keeps the rules in one
/// place while the model stays a plain object the UI can bind to.
/// </remarks>
public sealed class TaskItem
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="id">The stable identifier, unique within its list.</param>
    /// <param name="text">The label shown to the user.</param>
    /// <param name="done">Whether the item is ticked off.</param>
    public TaskItem(string id, string text, bool done = false)
    {
        Id = id;
        Text = text;
        Done = done;
    }

    /// <summary>
    /// Gets or sets the stable identifier, unique within its list.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the label shown to the user.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is ticked off.
    /// </summary>
    public bool Done { get; set; }
}
