using System.Threading;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Asks the user something, modally, over whatever page is showing.
/// </summary>
/// <remarks>
/// One dialog at a time: asking while a dialog is open cancels the open one. The system back button
/// cancels too, as on any Android dialog.
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Gets a value indicating whether a dialog is showing.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Asks for a yes or a no.
    /// </summary>
    /// <param name="title">What is being asked.</param>
    /// <param name="message">What saying yes will do.</param>
    /// <param name="confirmText">The wording of the yes button — the action, not "OK".</param>
    /// <param name="destructive"><see langword="true"/> to paint the yes button as the loss it causes.</param>
    /// <param name="cancellationToken">Closes the dialog as cancelled.</param>
    /// <returns><see langword="true"/> when the user confirmed.</returns>
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        bool destructive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks for a line of text — a name, a label.
    /// </summary>
    /// <param name="title">What is being asked for.</param>
    /// <param name="initialText">The text the box starts with.</param>
    /// <param name="confirmText">The wording of the confirm button.</param>
    /// <param name="placeholder">The hint shown while the box is empty.</param>
    /// <param name="cancellationToken">Closes the dialog as cancelled.</param>
    /// <returns>The trimmed text, or <see langword="null"/> when the user cancelled.</returns>
    /// <remarks>The confirm button stays disabled while the box is blank, so a blank answer never comes back.</remarks>
    Task<string?> PromptAsync(
        string title,
        string? initialText,
        string confirmText,
        string? placeholder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the open dialog, if there is one — what the system back button does.
    /// </summary>
    /// <returns><see langword="true"/> when a dialog was closed.</returns>
    bool TryCancel();
}
