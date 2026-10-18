using System.Threading.Tasks;

namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// Moves between pages, from a ViewModel that never sees one.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Builds a fresh navigation stack rooted at the Lists page, and makes it the one navigated.
    /// </summary>
    /// <returns>The stack, for the shell to host.</returns>
    /// <remarks>Called once per activity: Android can re-create the activity, and each gets its own tree.</remarks>
    ShellNavigationPage CreateRoot();

    /// <summary>
    /// Shows a page on top of the current one.
    /// </summary>
    /// <param name="page">Which page.</param>
    /// <returns>A task that completes once the page is shown.</returns>
    Task PushAsync(ShellPage page);

    /// <summary>
    /// Goes back to the previous page.
    /// </summary>
    /// <returns>A task that completes once the page is gone.</returns>
    Task GoBackAsync();
}
