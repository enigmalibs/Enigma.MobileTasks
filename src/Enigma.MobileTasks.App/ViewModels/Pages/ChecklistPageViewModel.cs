using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// Backs the Checklist page: the open list, one card per category.
/// </summary>
/// <remarks>
/// A singleton bound to <see cref="IActiveListService.Current"/>: opening another list rebuilds it, so
/// every Checklist page Android builds shows the list that is open.
/// </remarks>
public sealed class ChecklistPageViewModel : ObservableObject
{
    private readonly IActiveListService _lists;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">The lists the application knows about, and the open one.</param>
    public ChecklistPageViewModel(IActiveListService lists)
    {
        ArgumentNullException.ThrowIfNull(lists);

        _lists = lists;
        _lists.CurrentChanged += (_, _) => Rebuild();
        _lists.EntriesChanged += (_, _) => Title = _lists.Current?.List.Name ?? string.Empty;

        Rebuild();
    }

    /// <summary>
    /// Gets the open list's name, which is the page's header.
    /// </summary>
    public string Title { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets one card per category of the open list.
    /// </summary>
    public ObservableCollection<CategoryCardViewModel> Categories { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the open list has no category yet.
    /// </summary>
    public bool IsEmpty { get; private set => SetProperty(ref field, value); } = true;

    private void Rebuild()
    {
        WorkspaceEntry? entry = _lists.Current;

        Title = entry?.List.Name ?? string.Empty;
        Categories.Clear();

        if (entry is not null)
        {
            foreach (TaskCategory category in entry.List.Categories)
            {
                Categories.Add(new CategoryCardViewModel(category));
            }
        }

        IsEmpty = Categories.Count == 0;
    }
}
