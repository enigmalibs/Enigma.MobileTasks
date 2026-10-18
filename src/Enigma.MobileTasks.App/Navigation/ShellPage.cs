namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// The pages the application navigates between.
/// </summary>
public enum ShellPage
{
    /// <summary>
    /// Every list on the phone — the root of the navigation stack.
    /// </summary>
    Lists,

    /// <summary>
    /// The open list's categories and items.
    /// </summary>
    Checklist,

    /// <summary>
    /// The theme, the storage summary and the version.
    /// </summary>
    Settings,
}
