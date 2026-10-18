using System;
using Avalonia.Controls;

namespace Enigma.MobileTasks.App.Views.Pages;

/// <summary>
/// Every list on the phone — the root of the navigation stack.
/// </summary>
public partial class ListsPage : ContentPage
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public ListsPage() => InitializeComponent();

    /// <summary>
    /// Hands the page's ViewModel to its command bar. The bar is shown inside the navigation bar's
    /// template, outside this page's own tree, so it does not inherit the page's DataContext: left
    /// alone, every command in it binds to nothing.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        CommandBar.DataContext = DataContext;
    }
}
