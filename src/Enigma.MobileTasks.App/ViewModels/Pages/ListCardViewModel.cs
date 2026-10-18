using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// One list on the Lists page: its name and how far through it the user is.
/// </summary>
public sealed class ListCardViewModel : ObservableObject
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="entry">The list this card shows.</param>
    public ListCardViewModel(WorkspaceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entry = entry;
        Refresh();
    }

    /// <summary>
    /// Gets the list this card shows.
    /// </summary>
    public WorkspaceEntry Entry { get; }

    /// <summary>
    /// Gets the list's name.
    /// </summary>
    public string Name { get; private set => SetProperty(ref field, value); } = string.Empty;

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
