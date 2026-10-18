using System;
using System.Collections.Generic;
using Enigma.MobileTasks.Core.Identity;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Operations;

/// <summary>
/// Every change a list can undergo, in one place: creating, renaming, adding, removing,
/// ticking and reordering.
/// </summary>
/// <remarks>
/// <para>
/// This is Enigma.Tasks' port of the Listes web app's <c>js/core.js</c>, carried over rule for
/// rule, so that a list edited on the phone, on the desktop or in the browser ends up in the same
/// state. Nothing in this class touches
/// the file system, a dispatcher or a ViewModel: it changes the object it is handed and hands
/// back what the caller needs to undo it.
/// </para>
/// <para>
/// Blank names and blank item texts are refused rather than written — that is what makes an
/// accidental Enter in a rename box a no-op instead of a nameless category.
/// </para>
/// </remarks>
public sealed class ListOperations
{
    private readonly IListIdGenerator _ids;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="ids">The generator new categories and items take their identifiers from.</param>
    public ListOperations(IListIdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _ids = ids;
    }

    /* ------------------------------------------------------------------ creation */

    /// <summary>
    /// Creates an empty list.
    /// </summary>
    /// <param name="name">Its name; blank falls back to the default.</param>
    /// <returns>The new list.</returns>
    public TaskList CreateList(string? name = null)
        => new(IsBlank(name) ? ListSchema.DefaultListName : name!.Trim());

    /// <summary>
    /// Creates a category, detached from any list.
    /// </summary>
    /// <param name="name">Its name; blank falls back to the default.</param>
    /// <returns>The new category.</returns>
    public TaskCategory CreateCategory(string? name = null)
        => new(_ids.NewId("c"), IsBlank(name) ? ListSchema.DefaultCategoryName : name!.Trim());

    /// <summary>
    /// Creates an item, detached from any category.
    /// </summary>
    /// <param name="text">Its label.</param>
    /// <param name="done">Whether it starts ticked.</param>
    /// <returns>The new item.</returns>
    public TaskItem CreateItem(string? text, bool done = false)
        => new(_ids.NewId("i"), text is null ? string.Empty : text.Trim(), done);

    /* ------------------------------------------------------------------- lookups */

    /// <summary>
    /// Finds a category by identifier.
    /// </summary>
    /// <param name="list">The list to look in.</param>
    /// <param name="categoryId">The identifier to look for.</param>
    /// <returns>The category, or <see langword="null"/> when the list has no such category.</returns>
    public static TaskCategory? FindCategory(TaskList list, string? categoryId)
    {
        ArgumentNullException.ThrowIfNull(list);

        int index = IndexOfCategory(list, categoryId);

        return index < 0 ? null : list.Categories[index];
    }

    /// <summary>
    /// Finds an item anywhere in the list, together with where it sits.
    /// </summary>
    /// <param name="list">The list to look in.</param>
    /// <param name="itemId">The identifier to look for.</param>
    /// <returns>Where the item is, or <see langword="null"/> when the list has no such item.</returns>
    public static ItemLocation? Locate(TaskList list, string? itemId)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        for (int c = 0; c < list.Categories.Count; c++)
        {
            List<TaskItem> items = list.Categories[c].Items;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == itemId)
                {
                    return new ItemLocation(list.Categories[c], c, items[i], i);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an item by identifier.
    /// </summary>
    /// <param name="list">The list to look in.</param>
    /// <param name="itemId">The identifier to look for.</param>
    /// <returns>The item, or <see langword="null"/> when the list has no such item.</returns>
    public static TaskItem? FindItem(TaskList list, string? itemId) => Locate(list, itemId)?.Item;

    /* ----------------------------------------------------------------- mutations */

    /// <summary>
    /// Renames the list.
    /// </summary>
    /// <param name="list">The list to rename.</param>
    /// <param name="name">The new name; a blank one is refused.</param>
    /// <returns>The previous name, or <see langword="null"/> when nothing changed.</returns>
    public static string? RenameList(TaskList list, string? name)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (IsBlank(name))
        {
            return null;
        }

        string previous = list.Name;
        list.Name = name!.Trim();

        return previous == list.Name ? null : previous;
    }

    /// <summary>
    /// Adds a category.
    /// </summary>
    /// <param name="list">The list to add to.</param>
    /// <param name="name">The category name; blank falls back to the default.</param>
    /// <param name="atIndex">Where to put it; <see langword="null"/> appends.</param>
    /// <returns>The created category.</returns>
    public TaskCategory AddCategory(TaskList list, string? name = null, int? atIndex = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        TaskCategory category = CreateCategory(name);
        list.Categories.Insert(Clamp(atIndex, list.Categories.Count), category);

        return category;
    }

    /// <summary>
    /// Renames a category.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="categoryId">Which category.</param>
    /// <param name="name">The new name; a blank one is refused.</param>
    /// <returns>The previous name, or <see langword="null"/> when nothing changed.</returns>
    public static string? RenameCategory(TaskList list, string? categoryId, string? name)
    {
        TaskCategory? category = FindCategory(list, categoryId);

        if (category is null || IsBlank(name))
        {
            return null;
        }

        string previous = category.Name;
        category.Name = name!.Trim();

        return previous == category.Name ? null : previous;
    }

    /// <summary>
    /// Removes a category, items included.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="categoryId">Which category.</param>
    /// <returns>What to hand <see cref="Restore"/> to put it back, or <see langword="null"/>.</returns>
    public static ListRemoval? RemoveCategory(TaskList list, string? categoryId)
    {
        ArgumentNullException.ThrowIfNull(list);

        int index = IndexOfCategory(list, categoryId);

        if (index < 0)
        {
            return null;
        }

        TaskCategory category = list.Categories[index];
        list.Categories.RemoveAt(index);

        return new CategoryRemoval(index, category);
    }

    /// <summary>
    /// Adds an item to a category.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="categoryId">Which category.</param>
    /// <param name="text">The item's label; a blank one is refused.</param>
    /// <param name="atIndex">Where to put it; <see langword="null"/> appends.</param>
    /// <returns>The created item, or <see langword="null"/> when nothing was added.</returns>
    public TaskItem? AddItem(TaskList list, string? categoryId, string? text, int? atIndex = null)
    {
        TaskCategory? category = FindCategory(list, categoryId);

        if (category is null || IsBlank(text))
        {
            return null;
        }

        TaskItem item = CreateItem(text);
        category.Items.Insert(Clamp(atIndex, category.Items.Count), item);

        return item;
    }

    /// <summary>
    /// Renames an item.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="itemId">Which item.</param>
    /// <param name="text">The new label; a blank one is refused.</param>
    /// <returns>The previous label, or <see langword="null"/> when nothing changed.</returns>
    public static string? RenameItem(TaskList list, string? itemId, string? text)
    {
        TaskItem? item = FindItem(list, itemId);

        if (item is null || IsBlank(text))
        {
            return null;
        }

        string previous = item.Text;
        item.Text = text!.Trim();

        return previous == item.Text ? null : previous;
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="itemId">Which item.</param>
    /// <returns>What to hand <see cref="Restore"/> to put it back, or <see langword="null"/>.</returns>
    public static ListRemoval? RemoveItem(TaskList list, string? itemId)
    {
        ItemLocation? found = Locate(list, itemId);

        if (found is null)
        {
            return null;
        }

        found.Category.Items.RemoveAt(found.Index);

        return new ItemRemoval(found.Category.Id, found.Index, found.Item);
    }

    /// <summary>
    /// Puts back whatever <see cref="RemoveItem"/> or <see cref="RemoveCategory"/> took out.
    /// </summary>
    /// <param name="list">The list to restore into.</param>
    /// <param name="removal">What was removed.</param>
    /// <returns><see langword="true"/> when it went back.</returns>
    public static bool Restore(TaskList list, ListRemoval? removal)
    {
        ArgumentNullException.ThrowIfNull(list);

        switch (removal)
        {
            case CategoryRemoval category:
                list.Categories.Insert(Clamp(category.Index, list.Categories.Count), category.Category);
                return true;

            case ItemRemoval item:
                TaskCategory? target = FindCategory(list, item.CategoryId);

                if (target is null)
                {
                    return false;
                }

                target.Items.Insert(Clamp(item.Index, target.Items.Count), item.Item);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Ticks or unticks one item.
    /// </summary>
    /// <param name="list">The list it belongs to.</param>
    /// <param name="itemId">Which item.</param>
    /// <param name="value">The state to set; <see langword="null"/> flips it.</param>
    /// <returns>The new state, or <see langword="null"/> when the item is unknown.</returns>
    public static bool? Toggle(TaskList list, string? itemId, bool? value = null)
    {
        TaskItem? item = FindItem(list, itemId);

        if (item is null)
        {
            return null;
        }

        item.Done = value ?? !item.Done;

        return item.Done;
    }

    /// <summary>
    /// Ticks or unticks every item of the list, or of one category.
    /// </summary>
    /// <param name="list">The list to change.</param>
    /// <param name="done">The state to set.</param>
    /// <param name="categoryId">One category, or <see langword="null"/> for the whole list.</param>
    /// <returns>How many items actually changed.</returns>
    public static int SetAllDone(TaskList list, bool done, string? categoryId = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        IEnumerable<TaskCategory> categories = categoryId is null
            ? list.Categories
            : FindCategory(list, categoryId) is { } one ? [one] : [];

        int changed = 0;

        foreach (TaskCategory category in categories)
        {
            foreach (TaskItem item in category.Items)
            {
                if (item.Done != done)
                {
                    item.Done = done;
                    changed++;
                }
            }
        }

        return changed;
    }

    /* ---------------------------------------------------------------- reordering */

    /// <summary>
    /// Moves a category to a position.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="categoryId">Which category.</param>
    /// <param name="toIndex">Where it should end up.</param>
    /// <returns><see langword="true"/> when the order actually changed.</returns>
    public static bool MoveCategory(TaskList list, string? categoryId, int toIndex)
    {
        ArgumentNullException.ThrowIfNull(list);

        int from = IndexOfCategory(list, categoryId);

        if (from < 0)
        {
            return false;
        }

        int to = Clamp(toIndex, list.Categories.Count - 1);

        if (to == from)
        {
            return false;
        }

        TaskCategory category = list.Categories[from];
        list.Categories.RemoveAt(from);
        list.Categories.Insert(to, category);

        return true;
    }

    /// <summary>
    /// Moves a category up or down by one position.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="categoryId">Which category.</param>
    /// <param name="delta">-1 to move it up, 1 to move it down.</param>
    /// <returns><see langword="true"/> when the order actually changed.</returns>
    public static bool MoveCategoryBy(TaskList list, string? categoryId, int delta)
    {
        ArgumentNullException.ThrowIfNull(list);

        int from = IndexOfCategory(list, categoryId);

        return from >= 0 && MoveCategory(list, categoryId, from + delta);
    }

    /// <summary>
    /// Moves an item, possibly into another category.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="itemId">Which item.</param>
    /// <param name="toCategoryId">The category it should end up in.</param>
    /// <param name="toIndex">
    /// The wanted position in the target category <em>after</em> the item has been taken out of
    /// its current one.
    /// </param>
    /// <returns><see langword="true"/> when something actually moved.</returns>
    public static bool MoveItem(TaskList list, string? itemId, string? toCategoryId, int toIndex)
    {
        ItemLocation? found = Locate(list, itemId);

        if (found is null)
        {
            return false;
        }

        TaskCategory? target = FindCategory(list, toCategoryId);

        if (target is null)
        {
            return false;
        }

        bool sameCategory = ReferenceEquals(target, found.Category);
        int wanted = Clamp(toIndex, sameCategory ? found.Category.Items.Count - 1 : target.Items.Count);

        if (sameCategory && wanted == found.Index)
        {
            return false;
        }

        found.Category.Items.RemoveAt(found.Index);
        target.Items.Insert(Clamp(wanted, target.Items.Count), found.Item);

        return true;
    }

    /// <summary>
    /// Moves an item up or down by one position.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="itemId">Which item.</param>
    /// <param name="delta">-1 to move it up, 1 to move it down.</param>
    /// <returns><see langword="true"/> when something actually moved.</returns>
    /// <remarks>
    /// At the top or the bottom of a category the item hops into the neighbouring one, so the
    /// two buttons alone can move an item anywhere in the list — which is what makes reordering
    /// work without a pointer.
    /// </remarks>
    public static bool MoveItemBy(TaskList list, string? itemId, int delta)
    {
        ItemLocation? found = Locate(list, itemId);

        if (found is null || (delta != 1 && delta != -1))
        {
            return false;
        }

        int next = found.Index + delta;

        if (next >= 0 && next < found.Category.Items.Count)
        {
            return MoveItem(list, itemId, found.Category.Id, next);
        }

        int neighbourIndex = found.CategoryIndex + delta;

        if (neighbourIndex < 0 || neighbourIndex >= list.Categories.Count)
        {
            return false;
        }

        TaskCategory neighbour = list.Categories[neighbourIndex];

        return MoveItem(list, itemId, neighbour.Id, delta == -1 ? neighbour.Items.Count : 0);
    }

    /// <summary>
    /// Moves an item onto a drop slot.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="itemId">Which item.</param>
    /// <param name="toCategoryId">The category dropped into.</param>
    /// <param name="slotIndex">The slot, counted in the layout as it looks before the item leaves.</param>
    /// <returns><see langword="true"/> when something actually moved.</returns>
    /// <remarks>
    /// A slot index is what a drag gesture measures on screen — the gaps between the rows as
    /// they are drawn, with the dragged row still among them. Dropping below your own position
    /// therefore has to account for the gap you are about to leave behind.
    /// </remarks>
    public static bool MoveItemToSlot(TaskList list, string? itemId, string? toCategoryId, int slotIndex)
    {
        ItemLocation? found = Locate(list, itemId);

        if (found is null)
        {
            return false;
        }

        int target = slotIndex;

        if (found.Category.Id == toCategoryId)
        {
            if (target > found.Index)
            {
                target--;
            }

            if (target == found.Index)
            {
                return false;
            }
        }

        return MoveItem(list, itemId, toCategoryId, target);
    }

    /// <summary>
    /// Moves a category onto a drop slot, with the same convention as
    /// <see cref="MoveItemToSlot"/>.
    /// </summary>
    /// <param name="list">The list to reorder.</param>
    /// <param name="categoryId">Which category.</param>
    /// <param name="slotIndex">The slot, counted in the layout as it looks before the card leaves.</param>
    /// <returns><see langword="true"/> when the order actually changed.</returns>
    public static bool MoveCategoryToSlot(TaskList list, string? categoryId, int slotIndex)
    {
        ArgumentNullException.ThrowIfNull(list);

        int from = IndexOfCategory(list, categoryId);

        if (from < 0)
        {
            return false;
        }

        int target = slotIndex;

        if (target > from)
        {
            target--;
        }

        return target != from && MoveCategory(list, categoryId, target);
    }

    /* --------------------------------------------------------------------- copies */

    /// <summary>
    /// Deep-copies a list.
    /// </summary>
    /// <param name="list">The list to copy.</param>
    /// <param name="withNewIds">
    /// <see langword="true"/> to hand the copy brand new identifiers, which is what a duplicate
    /// needs so the two lists never share one.
    /// </param>
    /// <returns>The copy.</returns>
    public TaskList Clone(TaskList list, bool withNewIds = false)
    {
        ArgumentNullException.ThrowIfNull(list);

        TaskList copy = new(list.Name);

        foreach (TaskCategory category in list.Categories)
        {
            TaskCategory categoryCopy = new(withNewIds ? _ids.NewId("c") : category.Id, category.Name);

            foreach (TaskItem item in category.Items)
            {
                categoryCopy.Items.Add(new TaskItem(
                    withNewIds ? _ids.NewId("i") : item.Id,
                    item.Text,
                    item.Done));
            }

            copy.Categories.Add(categoryCopy);
        }

        return copy;
    }

    /* --------------------------------------------------------------------- helpers */

    private static int IndexOfCategory(TaskList list, string? categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
        {
            return -1;
        }

        for (int i = 0; i < list.Categories.Count; i++)
        {
            if (list.Categories[i].Id == categoryId)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Keeps an index inside <c>[0, length]</c>, treating "no index given" as "at the end" —
    /// the web app's <c>clampIndex</c>.
    /// </summary>
    /// <param name="index">The wanted index, or <see langword="null"/>.</param>
    /// <param name="length">The upper bound.</param>
    /// <returns>The index to use.</returns>
    private static int Clamp(int? index, int length)
    {
        if (index is not { } value)
        {
            return length;
        }

        return value < 0 ? 0 : value > length ? length : value;
    }
}
