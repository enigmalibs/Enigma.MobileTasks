using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// Backs the Lists page: every list on the phone, with its progress.
/// </summary>
/// <remarks>
/// A singleton that lives as long as the application, so its subscriptions to the active-list
/// service are never detached.
/// </remarks>
public sealed class ListsPageViewModel : ObservableObject
{
    private readonly IActiveListService _lists;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">The lists the application knows about.</param>
    public ListsPageViewModel(IActiveListService lists)
    {
        ArgumentNullException.ThrowIfNull(lists);

        _lists = lists;
        _lists.EntriesChanged += (_, _) => Rebuild();
        _lists.Touched += (_, entry) => Cards.FirstOrDefault(card => ReferenceEquals(card.Entry, entry))?.Refresh();

        Rebuild();
    }

    /// <summary>
    /// Gets one card per list, in the order the service keeps them (by name).
    /// </summary>
    public ObservableCollection<ListCardViewModel> Cards { get; } = [];

    /// <summary>
    /// Gets a value indicating whether there is no list at all.
    /// </summary>
    public bool IsEmpty { get; private set => SetProperty(ref field, value); } = true;

    private void Rebuild()
    {
        Cards.Clear();

        foreach (WorkspaceEntry entry in _lists.Entries)
        {
            Cards.Add(new ListCardViewModel(entry));
        }

        IsEmpty = Cards.Count == 0;
    }
}
