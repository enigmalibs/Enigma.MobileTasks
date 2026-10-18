using System;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Changes the open list — ticks, categories, items — and remembers the last thing deleted so it can
/// be put back.
/// </summary>
/// <remarks>
/// Every method works on the list <see cref="IActiveListService.Current"/> points at, applies the
/// change through the model's <c>ListOperations</c>, and schedules a save. A method that returns
/// <see langword="false"/> or <see langword="null"/> changed nothing at all.
/// </remarks>
public interface IListEditor
{
    /// <summary>
    /// Raised after any change beyond a single tick — what was shown has to be rebuilt.
    /// </summary>
    event EventHandler? StructureChanged;

    /// <summary>
    /// Gets a value indicating whether there is a deletion to undo.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Ticks or unticks one item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="done">The state to set.</param>
    /// <returns><see langword="true"/> when the state actually changed.</returns>
    bool Toggle(TaskItem item, bool done);

    /// <summary>
    /// Ticks or unticks every item of the list, or of one category.
    /// </summary>
    /// <param name="category">One category, or <see langword="null"/> for the whole list.</param>
    /// <param name="done">The state to set.</param>
    /// <returns>How many items changed.</returns>
    int SetAllDone(TaskCategory? category, bool done);

    /// <summary>
    /// Adds a category at the end of the list.
    /// </summary>
    /// <param name="name">Its name; blank falls back to the default.</param>
    /// <returns>The new category, or <see langword="null"/> when no list is open.</returns>
    TaskCategory? AddCategory(string? name);

    /// <summary>
    /// Renames a category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="name">Its new name; a blank one is refused.</param>
    /// <returns><see langword="true"/> when the name actually changed.</returns>
    bool RenameCategory(TaskCategory category, string? name);

    /// <summary>
    /// Removes a category, items included, and offers to undo it.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns><see langword="true"/> when it was removed.</returns>
    bool RemoveCategory(TaskCategory category);

    /// <summary>
    /// Adds an item at the end of a category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="text">The item's label; a blank one is refused.</param>
    /// <returns>The new item, or <see langword="null"/> when nothing was added.</returns>
    TaskItem? AddItem(TaskCategory category, string? text);

    /// <summary>
    /// Renames an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="text">Its new label; a blank one is refused.</param>
    /// <returns><see langword="true"/> when the label actually changed.</returns>
    bool RenameItem(TaskItem item, string? text);

    /// <summary>
    /// Removes an item, and offers to undo it.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when it was removed.</returns>
    bool RemoveItem(TaskItem item);

    /// <summary>
    /// Moves a category up or down by one.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="delta">-1 for up, 1 for down.</param>
    /// <returns><see langword="true"/> when the order actually changed.</returns>
    bool MoveCategory(TaskCategory category, int delta);

    /// <summary>
    /// Moves an item up or down by one, hopping into the neighbouring category at a bound.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="delta">-1 for up, 1 for down.</param>
    /// <returns><see langword="true"/> when something actually moved.</returns>
    bool MoveItem(TaskItem item, int delta);

    /// <summary>
    /// Puts back the last thing deleted.
    /// </summary>
    /// <returns><see langword="true"/> when something was put back.</returns>
    bool Undo();
}
