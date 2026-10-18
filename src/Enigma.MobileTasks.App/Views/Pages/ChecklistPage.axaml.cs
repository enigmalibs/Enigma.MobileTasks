using System;
using Avalonia.Controls;

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
}
