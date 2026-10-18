namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// What was taken out of a list, and where from — everything needed to put it back.
/// </summary>
/// <remarks>
/// Deleting is the one destructive gesture of a touch-friendly checklist, so every removal
/// hands back one of these and the application keeps the last one for its undo command.
/// </remarks>
public abstract record ListRemoval
{
    private protected ListRemoval()
    {
    }

    /// <summary>
    /// Gets the label of whatever was removed, for the message that announces it.
    /// </summary>
    public abstract string Label { get; }
}

/// <summary>
/// A category that was removed, with the items it held.
/// </summary>
/// <param name="Index">The position it was at.</param>
/// <param name="Category">The category itself.</param>
public sealed record CategoryRemoval(int Index, TaskCategory Category) : ListRemoval
{
    /// <inheritdoc />
    public override string Label => Category.Name;
}

/// <summary>
/// An item that was removed.
/// </summary>
/// <param name="CategoryId">The category it was in.</param>
/// <param name="Index">The position it was at inside that category.</param>
/// <param name="Item">The item itself.</param>
public sealed record ItemRemoval(string CategoryId, int Index, TaskItem Item) : ListRemoval
{
    /// <inheritdoc />
    public override string Label => Item.Text;
}
