using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// One list on the Lists page: its name, how far through it the user is, and what can be done to it.
/// </summary>
/// <remarks>
/// The card carries its own commands, delegating to the page: a card's menu is a flyout, which lives
/// outside the visual tree, where a binding cannot climb to the page's DataContext.
/// </remarks>
public sealed class ListCardViewModel : ObservableObject
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="entry">The list this card shows.</param>
    /// <param name="page">The page whose commands the card's own commands run.</param>
    public ListCardViewModel(WorkspaceEntry entry, ListsPageViewModel page)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(page);

        Entry = entry;
        OpenCommand = new AsyncRelayCommand(() => page.OpenAsync(this));
        RenameCommand = new AsyncRelayCommand(() => page.RenameAsync(this));
        DuplicateCommand = new AsyncRelayCommand(() => page.DuplicateAsync(this));
        ExportCommand = new AsyncRelayCommand(() => page.ExportAsync(this));
        DeleteCommand = new AsyncRelayCommand(() => page.DeleteAsync(this));

        Refresh();
    }

    /// <summary>
    /// Gets the list this card shows.
    /// </summary>
    public WorkspaceEntry Entry { get; }

    /// <summary>
    /// Gets the list's name.
    /// </summary>
    public string Name
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ActionsLabel));
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the accessible name of the card's menu button.
    /// </summary>
    public string ActionsLabel => string.Format(CultureInfo.CurrentCulture, Strings.ListActionsFor, Name);

    /// <summary>
    /// Gets the progress as text — "80 / 94".
    /// </summary>
    public string ProgressText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the ticked share as a percentage, for the progress bar.
    /// </summary>
    public double Percent { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether every item is ticked.
    /// </summary>
    public bool IsComplete { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the command that opens the list.
    /// </summary>
    public AsyncRelayCommand OpenCommand { get; }

    /// <summary>
    /// Gets the command that renames the list.
    /// </summary>
    public AsyncRelayCommand RenameCommand { get; }

    /// <summary>
    /// Gets the command that duplicates the list.
    /// </summary>
    public AsyncRelayCommand DuplicateCommand { get; }

    /// <summary>
    /// Gets the command that exports the list as a JSON file.
    /// </summary>
    public AsyncRelayCommand ExportCommand { get; }

    /// <summary>
    /// Gets the command that deletes the list, after asking.
    /// </summary>
    public AsyncRelayCommand DeleteCommand { get; }

    /// <summary>
    /// Re-reads the list — after it was renamed, ticked or edited.
    /// </summary>
    public void Refresh()
    {
        ListProgress progress = ListProgress.Of(Entry.List);

        Name = Entry.List.Name;
        ProgressText = string.Create(CultureInfo.CurrentCulture, $"{progress.Done} / {progress.Total}");
        Percent = progress.Ratio * 100d;
        IsComplete = progress.IsComplete;
    }
}
