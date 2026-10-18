using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// Backs the Lists page: every list on the phone, with its progress, and the commands that create,
/// open, rename, duplicate and delete them.
/// </summary>
/// <remarks>
/// A singleton that lives as long as the application, so its subscriptions to the active-list
/// service are never detached.
/// </remarks>
public sealed class ListsPageViewModel : ObservableObject
{
    private readonly IActiveListService _lists;
    private readonly IListActions _actions;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">The lists the application knows about.</param>
    /// <param name="actions">Creates, copies, renames and deletes lists.</param>
    /// <param name="dialogs">Asks for names and confirmations.</param>
    /// <param name="navigation">Opens a list's checklist.</param>
    public ListsPageViewModel(
        IActiveListService lists,
        IListActions actions,
        IDialogService dialogs,
        INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(navigation);

        _lists = lists;
        _actions = actions;
        _dialogs = dialogs;
        _navigation = navigation;

        NewListCommand = new AsyncRelayCommand(OnNewListAsync);
        ImportCommand = new AsyncRelayCommand(() => _actions.ImportAsync());

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

    /// <summary>
    /// Gets the command that asks for a name, creates the list and opens it.
    /// </summary>
    public AsyncRelayCommand NewListCommand { get; }

    /// <summary>
    /// Gets the command that imports lists from files picked in the system picker — exported by
    /// Enigma.Tasks or by another phone.
    /// </summary>
    public AsyncRelayCommand ImportCommand { get; }

    /// <summary>
    /// Opens a list's checklist.
    /// </summary>
    /// <param name="card">The list's card.</param>
    /// <returns>A task that completes once the checklist is shown.</returns>
    internal async Task OpenAsync(ListCardViewModel card)
    {
        _lists.Open(card.Entry);
        await _navigation.PushAsync(ShellPage.Checklist);
    }

    /// <summary>
    /// Asks for a new name and renames the list.
    /// </summary>
    /// <param name="card">The list's card.</param>
    /// <returns>A task that completes once the list is renamed, or the user cancelled.</returns>
    internal async Task RenameAsync(ListCardViewModel card)
    {
        string? name = await _dialogs.PromptAsync(
            Strings.RenameListTitle,
            card.Entry.List.Name,
            Strings.Rename,
            Strings.ListNamePlaceholder);

        if (name is not null)
        {
            await _actions.RenameAsync(card.Entry, name);
        }
    }

    /// <summary>
    /// Duplicates the list.
    /// </summary>
    /// <param name="card">The list's card.</param>
    /// <returns>A task that completes once the copy is written.</returns>
    internal Task DuplicateAsync(ListCardViewModel card) => _actions.DuplicateAsync(card.Entry);

    /// <summary>
    /// Exports the list as a JSON file, where the user picks.
    /// </summary>
    /// <param name="card">The list's card.</param>
    /// <returns>A task that completes once the file is written, or the user cancelled.</returns>
    internal Task ExportAsync(ListCardViewModel card) => _actions.ExportAsync(card.Entry);

    /// <summary>
    /// Asks, then deletes the list.
    /// </summary>
    /// <param name="card">The list's card.</param>
    /// <returns>A task that completes once the list is gone, or the user changed their mind.</returns>
    internal async Task DeleteAsync(ListCardViewModel card)
    {
        bool confirmed = await _dialogs.ConfirmAsync(
            string.Format(CultureInfo.CurrentCulture, Strings.DeleteListTitle, card.Entry.List.Name),
            Strings.DeleteListMessage,
            Strings.Delete,
            destructive: true);

        if (confirmed)
        {
            await _actions.DeleteAsync(card.Entry);
        }
    }

    private async Task OnNewListAsync()
    {
        string? name = await _dialogs.PromptAsync(Strings.NewListTitle, null, Strings.Create, Strings.ListNamePlaceholder);

        if (name is null || await _actions.CreateAsync(name) is not { } entry)
        {
            return;
        }

        if (Cards.FirstOrDefault(card => ReferenceEquals(card.Entry, entry)) is { } created)
        {
            await OpenAsync(created);
        }
    }

    private void Rebuild()
    {
        Cards.Clear();

        foreach (WorkspaceEntry entry in _lists.Entries)
        {
            Cards.Add(new ListCardViewModel(entry, this));
        }

        IsEmpty = Cards.Count == 0;
    }
}
