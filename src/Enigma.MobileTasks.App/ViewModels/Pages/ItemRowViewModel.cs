using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// One item of a category: a row to tick, a trash button to delete it, and — in edit mode — a menu to
/// rename or move it.
/// </summary>
public sealed class ItemRowViewModel : ObservableObject
{
    private bool _syncing;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="item">The item this row shows.</param>
    /// <param name="card">The category card the row sits in.</param>
    public ItemRowViewModel(TaskItem item, CategoryCardViewModel card)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(card);

        Item = item;
        Card = card;

        RenameCommand = new AsyncRelayCommand(OnRenameAsync);
        DeleteCommand = new RelayCommand(() => card.Page.Editor.RemoveItem(Item));
        MoveUpCommand = new RelayCommand(() => card.Page.Editor.MoveItem(Item, -1));
        MoveDownCommand = new RelayCommand(() => card.Page.Editor.MoveItem(Item, 1));

        Sync();
    }

    /// <summary>
    /// Gets the item this row shows.
    /// </summary>
    public TaskItem Item { get; }

    /// <summary>
    /// Gets the card the row sits in — and through it the page, whose edit mode the row follows.
    /// </summary>
    public CategoryCardViewModel Card { get; }

    /// <summary>
    /// Gets the item's label.
    /// </summary>
    public string Text
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ActionsLabel));
                OnPropertyChanged(nameof(DeleteLabel));
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the item is ticked. Setting it ticks the item in the
    /// list and schedules the save.
    /// </summary>
    public bool IsDone
    {
        get;
        set
        {
            if (!SetProperty(ref field, value) || _syncing)
            {
                return;
            }

            if (Card.Page.Editor.Toggle(Item, value))
            {
                Card.Refresh();
                Card.Page.RefreshProgress();
            }
        }
    }

    /// <summary>
    /// Gets the accessible name of the row's menu button.
    /// </summary>
    public string ActionsLabel => string.Format(CultureInfo.CurrentCulture, Strings.ItemActionsFor, Text);

    /// <summary>
    /// Gets the accessible name of the row's trash button.
    /// </summary>
    public string DeleteLabel => string.Format(CultureInfo.CurrentCulture, Strings.DeleteItemFor, Text);

    /// <summary>
    /// Gets the command that renames the item.
    /// </summary>
    public AsyncRelayCommand RenameCommand { get; }

    /// <summary>
    /// Gets the command that deletes the item, offering to undo it.
    /// </summary>
    public RelayCommand DeleteCommand { get; }

    /// <summary>
    /// Gets the command that moves the item up, into the previous category at the top.
    /// </summary>
    public RelayCommand MoveUpCommand { get; }

    /// <summary>
    /// Gets the command that moves the item down, into the next category at the bottom.
    /// </summary>
    public RelayCommand MoveDownCommand { get; }

    /// <summary>
    /// Re-reads the item — after a rename, an "untick everything" or an undo — without writing
    /// anything back: the model already holds the state, so the tick-and-save must not run.
    /// </summary>
    public void Sync()
    {
        _syncing = true;

        try
        {
            Text = Item.Text;
            IsDone = Item.Done;
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task OnRenameAsync()
    {
        string? text = await Card.Page.Dialogs.PromptAsync(Strings.RenameItemTitle, Text, Strings.Rename, Strings.ItemPlaceholder);

        if (text is not null)
        {
            Card.Page.Editor.RenameItem(Item, text);
        }
    }
}
