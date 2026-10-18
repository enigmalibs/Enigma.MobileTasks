using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Operations;

/// <summary>
/// Where an item sits in a list.
/// </summary>
/// <param name="Category">The category holding it.</param>
/// <param name="CategoryIndex">That category's position in the list.</param>
/// <param name="Item">The item itself.</param>
/// <param name="Index">Its position inside the category.</param>
public sealed record ItemLocation(TaskCategory Category, int CategoryIndex, TaskItem Item, int Index);
