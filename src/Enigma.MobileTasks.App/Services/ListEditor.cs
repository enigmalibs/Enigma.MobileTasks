using System;
using System.Globalization;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Operations;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="IListEditor"/>.
/// </summary>
public sealed class ListEditor : IListEditor
{
    private readonly IActiveListService _lists;
    private readonly ListOperations _operations;
    private readonly INotificationService _notifications;
    private ListRemoval? _removal;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">Which list is open, and where a change becomes a scheduled save.</param>
    /// <param name="operations">The model's rules for every change.</param>
    /// <param name="notifications">Announces a deletion and offers to undo it.</param>
    public ListEditor(IActiveListService lists, ListOperations operations, INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(notifications);

        _lists = lists;
        _operations = operations;
        _notifications = notifications;

        // A removal holds a category or an item of one list; restoring it into another would be
        // nonsense.
        _lists.CurrentChanged += (_, _) => _removal = null;
    }

    /// <inheritdoc />
    public event EventHandler? StructureChanged;

    /// <inheritdoc />
    public bool CanUndo => _removal is not null;

    private TaskList? List => _lists.Current?.List;

    /// <inheritdoc />
    public bool Toggle(TaskItem item, bool done)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (List is not { } list || item.Done == done || ListOperations.Toggle(list, item.Id, done) is null)
        {
            return false;
        }

        _lists.Touch();

        return true;
    }

    /// <inheritdoc />
    public int SetAllDone(TaskCategory? category, bool done)
    {
        if (List is not { } list)
        {
            return 0;
        }

        int changed = ListOperations.SetAllDone(list, done, category?.Id);

        if (changed > 0)
        {
            Changed();
        }

        return changed;
    }

    /// <inheritdoc />
    public TaskCategory? AddCategory(string? name)
    {
        if (List is not { } list)
        {
            return null;
        }

        TaskCategory category = _operations.AddCategory(list, name);
        Changed();

        return category;
    }

    /// <inheritdoc />
    public bool RenameCategory(TaskCategory category, string? name)
    {
        ArgumentNullException.ThrowIfNull(category);

        return List is { } list && ListOperations.RenameCategory(list, category.Id, name) is not null && Changed();
    }

    /// <inheritdoc />
    public bool RemoveCategory(TaskCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return List is { } list && ListOperations.RemoveCategory(list, category.Id) is { } removal && Remember(removal);
    }

    /// <inheritdoc />
    public TaskItem? AddItem(TaskCategory category, string? text)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (List is not { } list || _operations.AddItem(list, category.Id, text) is not { } item)
        {
            return null;
        }

        Changed();

        return item;
    }

    /// <inheritdoc />
    public bool RenameItem(TaskItem item, string? text)
    {
        ArgumentNullException.ThrowIfNull(item);

        return List is { } list && ListOperations.RenameItem(list, item.Id, text) is not null && Changed();
    }

    /// <inheritdoc />
    public bool RemoveItem(TaskItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return List is { } list && ListOperations.RemoveItem(list, item.Id) is { } removal && Remember(removal);
    }

    /// <inheritdoc />
    public bool MoveCategory(TaskCategory category, int delta)
    {
        ArgumentNullException.ThrowIfNull(category);

        return List is { } list && ListOperations.MoveCategoryBy(list, category.Id, delta) && Changed();
    }

    /// <inheritdoc />
    public bool MoveItem(TaskItem item, int delta)
    {
        ArgumentNullException.ThrowIfNull(item);

        return List is { } list && ListOperations.MoveItemBy(list, item.Id, delta) && Changed();
    }

    /// <inheritdoc />
    public bool Undo()
    {
        if (List is not { } list || _removal is not { } removal || !ListOperations.Restore(list, removal))
        {
            return false;
        }

        _removal = null;
        Changed();
        _notifications.Show(Format(Strings.Restored, removal.Label));

        return true;
    }

    private bool Remember(ListRemoval removal)
    {
        _removal = removal;
        Changed();
        _notifications.Show(Format(Strings.Deleted, removal.Label), Strings.Undo, () => Undo());

        return true;
    }

    /// <summary>
    /// Schedules the save and tells the page to rebuild.
    /// </summary>
    /// <returns><see langword="true"/>, so a change can end a condition chain.</returns>
    private bool Changed()
    {
        _lists.Touch();
        StructureChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    private static string Format(string format, string argument)
        => string.Format(CultureInfo.CurrentCulture, format, argument);
}
