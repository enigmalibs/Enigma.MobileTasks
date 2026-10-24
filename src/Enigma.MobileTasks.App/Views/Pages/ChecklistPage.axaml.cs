using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Enigma.MobileTasks.App.Views.Pages;

/// <summary>
/// The open list: its categories and their items.
/// </summary>
public partial class ChecklistPage : ContentPage
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public ChecklistPage() => InitializeComponent();

    /// <summary>
    /// Hands the page's ViewModel to its command bar, which is shown inside the navigation bar's
    /// template and so does not inherit the page's DataContext.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        CommandBar.DataContext = DataContext;
    }

    /// <summary>
    /// Puts the cursor back in the quick-add box whose "+" was tapped, so the next item can be typed
    /// at once — the one bit of the quick add a ViewModel cannot do. The "+" never takes the focus,
    /// but the box may have lost it before the tap.
    /// </summary>
    /// <param name="sender">The "+" button.</param>
    /// <param name="e">The event data.</param>
    private void OnQuickAddClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Parent: Panel row } && row.Children.OfType<TextBox>().FirstOrDefault() is { } box)
        {
            box.Focus();
        }
    }
}
