using System;
using Avalonia.Controls;

namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// The application's navigation stack: a <see cref="NavigationPage"/> that lets an open dialog take
/// the system back button first.
/// </summary>
/// <remarks>
/// Back on Android closes whatever is on top. A dialog is drawn over the stack rather than pushed on
/// it, so without this the back button would pop the page from under the dialog.
/// </remarks>
public sealed class ShellNavigationPage : NavigationPage
{
    private readonly Func<bool> _closeOverlay;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="closeOverlay">Closes an open dialog and says whether there was one.</param>
    public ShellNavigationPage(Func<bool> closeOverlay)
    {
        ArgumentNullException.ThrowIfNull(closeOverlay);
        _closeOverlay = closeOverlay;
    }

    /// <summary>
    /// Keeps <see cref="NavigationPage"/>'s template: a subclass is otherwise looked up by its own
    /// type, finds no theme, and draws nothing.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(NavigationPage);

    /// <inheritdoc />
    protected override bool OnSystemBackButtonPressed()
        => _closeOverlay() || base.OnSystemBackButtonPressed();
}
