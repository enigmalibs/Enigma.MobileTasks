using Avalonia.Controls;

namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// Builds a page, bound to its ViewModel, ready to be pushed.
/// </summary>
public interface IPageFactory
{
    /// <summary>
    /// Builds a page.
    /// </summary>
    /// <param name="page">Which page.</param>
    /// <returns>A fresh view whose <c>DataContext</c> is the page's ViewModel.</returns>
    Page Create(ShellPage page);
}
