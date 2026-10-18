using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// One category of the open list: its card, its rows, and what can be done to it.
/// </summary>
public sealed class CategoryCardViewModel : ObservableObject
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="category">The category this card shows.</param>
    /// <param name="page">The page the card sits on.</param>
    /// <param name="collapsed">Whether the card starts folded.</param>
    public CategoryCardViewModel(TaskCategory category, ChecklistPageViewModel page, bool collapsed = false)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(page);

        Category = category;
        Page = page;
        IsCollapsed = collapsed;

        ToggleCollapsedCommand = new AsyncRelayCommand(() => page.ToggleCollapsedAsync(this));
        AddItemCommand = new RelayCommand(OnAddItem, () => !string.IsNullOrWhiteSpace(QuickAddText));
        RenameCommand = new AsyncRelayCommand(OnRenameAsync);
        DeleteCommand = new AsyncRelayCommand(OnDeleteAsync);
        MoveUpCommand = new RelayCommand(() => page.Editor.MoveCategory(Category, -1));
        MoveDownCommand = new RelayCommand(() => page.Editor.MoveCategory(Category, 1));
        UntickCommand = new RelayCommand(() => page.Editor.SetAllDone(Category, false));

        Sync();
    }

    /// <summary>
    /// Gets the category this card shows.
    /// </summary>
    public TaskCategory Category { get; }

    /// <summary>
    /// Gets the page the card sits on, whose edit mode the card follows.
    /// </summary>
    public ChecklistPageViewModel Page { get; }

    /// <summary>
    /// Gets the category's name.
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
    /// Gets one row per item.
    /// </summary>
    public ObservableCollection<ItemRowViewModel> Items { get; } = [];

    /// <summary>
    /// Gets the category's progress as text — "3 / 18".
    /// </summary>
    public string ProgressText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether every item of the category is ticked.
    /// </summary>
    public bool IsComplete { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether the card is folded down to its heading.
    /// </summary>
    public bool IsCollapsed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the card shows its rows.
    /// </summary>
    public bool IsExpanded => !IsCollapsed;

    /// <summary>
    /// Gets or sets the text typed into the card's quick-add box.
    /// </summary>
    public string QuickAddText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AddItemCommand.NotifyCanExecuteChanged();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the accessible name of the card's menu button.
    /// </summary>
    public string ActionsLabel => string.Format(CultureInfo.CurrentCulture, Strings.CategoryActionsFor, Name);

    /// <summary>
    /// Gets the command that folds or unfolds the card.
    /// </summary>
    public AsyncRelayCommand ToggleCollapsedCommand { get; }

    /// <summary>
    /// Gets the command that adds the quick-add box's text as an item.
    /// </summary>
    public RelayCommand AddItemCommand { get; }

    /// <summary>
    /// Gets the command that renames the category.
    /// </summary>
    public AsyncRelayCommand RenameCommand { get; }

    /// <summary>
    /// Gets the command that deletes the category — asking first when it still holds items.
    /// </summary>
    public AsyncRelayCommand DeleteCommand { get; }

    /// <summary>
    /// Gets the command that moves the category up.
    /// </summary>
    public RelayCommand MoveUpCommand { get; }

    /// <summary>
    /// Gets the command that moves the category down.
    /// </summary>
    public RelayCommand MoveDownCommand { get; }

    /// <summary>
    /// Gets the command that unticks every item of the category.
    /// </summary>
    public RelayCommand UntickCommand { get; }

    /// <summary>
    /// Re-reads the category after a structural change, keeping the rows of items still there.
    /// </summary>
    public void Sync()
    {
        Name = Category.Name;
        Items.SyncWith(Category.Items, row => row.Item, item => new ItemRowViewModel(item, this), row => row.Sync());
        Refresh();
    }

    /// <summary>
    /// Re-counts the category after a tick.
    /// </summary>
    public void Refresh()
    {
        ListProgress progress = ListProgress.Of(Category);

        ProgressText = string.Create(CultureInfo.CurrentCulture, $"{progress.Done} / {progress.Total}");
        IsComplete = progress.IsComplete;
    }

    private void OnAddItem()
    {
        string text = QuickAddText;

        QuickAddText = string.Empty;
        Page.Editor.AddItem(Category, text);
    }

    private async Task OnRenameAsync()
    {
        string? name = await Page.Dialogs.PromptAsync(Strings.RenameCategoryTitle, Name, Strings.Rename, Strings.CategoryNamePlaceholder);

        if (name is not null)
        {
            Page.Editor.RenameCategory(Category, name);
        }
    }

    private async Task OnDeleteAsync()
    {
        // An empty category goes at once; one with items asks, because "undo" is easy to miss.
        if (Category.Items.Count > 0)
        {
            bool confirmed = await Page.Dialogs.ConfirmAsync(
                string.Format(CultureInfo.CurrentCulture, Strings.DeleteCategoryTitle, Name),
                string.Format(CultureInfo.CurrentCulture, Strings.DeleteCategoryMessage, Category.Items.Count),
                Strings.Delete,
                destructive: true);

            if (!confirmed)
            {
                return;
            }
        }

        Page.Editor.RemoveCategory(Category);
    }
}
