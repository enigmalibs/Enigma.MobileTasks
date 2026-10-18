using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// Backs the Checklist page: the open list, one card per category, ticking, folding, and — in edit
/// mode — everything structural.
/// </summary>
/// <remarks>
/// <para>
/// A singleton bound to <see cref="IActiveListService.Current"/>: opening another list rebuilds it,
/// so every Checklist page Android builds shows the list that is open. A structural change syncs the
/// cards in place instead, so the text box being typed into keeps its focus and the keyboard stays up.
/// </para>
/// <para>
/// Five collaborators is the most this class should take: it coordinates a page, and each of them is
/// one of the page's concerns — the open list, its edits, the list as a file, questions, and the folded
/// cards remembered between runs.
/// </para>
/// </remarks>
public sealed class ChecklistPageViewModel : ObservableObject
{
    private readonly IActiveListService _lists;
    private readonly IListActions _actions;
    private readonly ISettingsService _settings;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">The lists the application knows about, and the open one.</param>
    /// <param name="editor">Changes the open list.</param>
    /// <param name="actions">Renames the list and its file.</param>
    /// <param name="dialogs">Asks for names and confirmations.</param>
    /// <param name="settings">Remembers which cards are folded.</param>
    public ChecklistPageViewModel(
        IActiveListService lists,
        IListEditor editor,
        IListActions actions,
        IDialogService dialogs,
        ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(settings);

        _lists = lists;
        Editor = editor;
        _actions = actions;
        Dialogs = dialogs;
        _settings = settings;

        EditCommand = new RelayCommand(() => IsEditing = !IsEditing);
        UntickAllCommand = new RelayCommand(() => Editor.SetAllDone(null, false));
        AddCategoryCommand = new AsyncRelayCommand(OnAddCategoryAsync);
        RenameListCommand = new AsyncRelayCommand(OnRenameListAsync);
        ExportCommand = new AsyncRelayCommand(
            () => _lists.Current is { } entry ? _actions.ExportAsync(entry) : Task.CompletedTask);

        _lists.CurrentChanged += (_, _) => Open();
        _lists.EntriesChanged += (_, _) => Title = _lists.Current?.List.Name ?? string.Empty;
        Editor.StructureChanged += (_, _) => Sync();

        Open();
    }

    /// <summary>
    /// Gets the open list's name, which is the page's header.
    /// </summary>
    public string Title { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the whole list's progress as text — "80 / 94".
    /// </summary>
    public string ProgressText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the ticked share as a percentage, for the progress bar.
    /// </summary>
    public double Percent { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether every item of the list is ticked.
    /// </summary>
    public bool IsComplete { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets one card per category of the open list.
    /// </summary>
    public ObservableCollection<CategoryCardViewModel> Categories { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the open list has no category yet.
    /// </summary>
    public bool IsEmpty
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasCategories));
            }
        }
    } = true;

    /// <summary>
    /// Gets a value indicating whether the open list has at least one category.
    /// </summary>
    public bool HasCategories => !IsEmpty;

    /// <summary>
    /// Gets or sets a value indicating whether the structural controls are showing. The everyday view
    /// stays a clean checklist.
    /// </summary>
    public bool IsEditing { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the command that turns edit mode on or off.
    /// </summary>
    public RelayCommand EditCommand { get; }

    /// <summary>
    /// Gets the command that unticks every item of the list, ready for the next time.
    /// </summary>
    public RelayCommand UntickAllCommand { get; }

    /// <summary>
    /// Gets the command that asks for a name and adds a category.
    /// </summary>
    public AsyncRelayCommand AddCategoryCommand { get; }

    /// <summary>
    /// Gets the command that renames the list, and its file with it.
    /// </summary>
    public AsyncRelayCommand RenameListCommand { get; }

    /// <summary>
    /// Gets the command that exports the open list as a JSON file.
    /// </summary>
    public AsyncRelayCommand ExportCommand { get; }

    /// <summary>
    /// Gets the editor the cards and rows change the list through.
    /// </summary>
    internal IListEditor Editor { get; }

    /// <summary>
    /// Gets the dialogs the cards and rows ask through.
    /// </summary>
    internal IDialogService Dialogs { get; }

    /// <summary>
    /// Re-counts the whole list after a tick.
    /// </summary>
    internal void RefreshProgress()
    {
        ListProgress progress = _lists.Current is { } entry ? ListProgress.Of(entry.List) : default;

        ProgressText = string.Create(CultureInfo.CurrentCulture, $"{progress.Done} / {progress.Total}");
        Percent = progress.Ratio * 100d;
        IsComplete = progress.IsComplete;
    }

    /// <summary>
    /// Folds or unfolds a card, and remembers it for this list.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <returns>A task that completes once the choice is written.</returns>
    internal async Task ToggleCollapsedAsync(CategoryCardViewModel card)
    {
        card.IsCollapsed = !card.IsCollapsed;

        if (_lists.Current is not { } entry)
        {
            return;
        }

        List<string> collapsed = [.. Categories.Where(category => category.IsCollapsed).Select(category => category.Category.Id)];
        string file = entry.FileName;

        await _settings.UpdateAsync(settings => settings.WithCollapsed(file, collapsed));
    }

    /// <summary>
    /// Shows a newly opened list from scratch. An empty list opens in edit mode: there is nothing to
    /// tick until something is added.
    /// </summary>
    private void Open()
    {
        WorkspaceEntry? entry = _lists.Current;
        HashSet<string> collapsed = entry is null ? [] : [.. _settings.Current.CollapsedIn(entry.FileName)];

        Categories.Clear();

        foreach (TaskCategory category in entry?.List.Categories ?? [])
        {
            Categories.Add(new CategoryCardViewModel(category, this, collapsed.Contains(category.Id)));
        }

        Title = entry?.List.Name ?? string.Empty;
        IsEmpty = Categories.Count == 0;
        IsEditing = entry is not null && IsEmpty;
        RefreshProgress();
    }

    /// <summary>
    /// Brings the cards in step with the list after a structural change, keeping the ones still there.
    /// </summary>
    private void Sync()
    {
        IReadOnlyList<TaskCategory> categories = _lists.Current?.List.Categories ?? [];

        Categories.SyncWith(categories, card => card.Category, category => new CategoryCardViewModel(category, this), card => card.Sync());

        IsEmpty = Categories.Count == 0;
        RefreshProgress();
    }

    private async Task OnAddCategoryAsync()
    {
        string? name = await Dialogs.PromptAsync(Strings.AddCategoryTitle, null, Strings.Create, Strings.CategoryNamePlaceholder);

        if (name is not null)
        {
            Editor.AddCategory(name);
        }
    }

    private async Task OnRenameListAsync()
    {
        if (_lists.Current is not { } entry)
        {
            return;
        }

        string? name = await Dialogs.PromptAsync(Strings.RenameListTitle, entry.List.Name, Strings.Rename, Strings.ListNamePlaceholder);

        if (name is not null)
        {
            await _actions.RenameAsync(entry, name);
        }
    }
}
