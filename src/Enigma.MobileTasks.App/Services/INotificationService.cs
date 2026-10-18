using System;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Says what just happened, briefly, at the bottom of the screen — and offers to take it back when
/// it can be taken back.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a message, replacing whatever message was showing.
    /// </summary>
    /// <param name="message">What happened.</param>
    /// <param name="actionText">The wording of the action button, e.g. "Undo"; none when <see langword="null"/>.</param>
    /// <param name="action">What the action button does; it also dismisses the message.</param>
    void Show(string message, string? actionText = null, Action? action = null);

    /// <summary>
    /// Hides the message now.
    /// </summary>
    void Dismiss();
}
