using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="INotificationService"/>, and the ViewModel of the snackbar the shell draws.
/// </summary>
/// <remarks>
/// A message goes away by itself after a few seconds — longer when it carries an action, so there is
/// time to reach for it. Called on the UI thread; the timer's continuation comes back to it.
/// </remarks>
public sealed class NotificationService : ObservableObject, INotificationService
{
    private CancellationTokenSource? _timer;
    private Action? _action;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public NotificationService()
    {
        ActionCommand = new RelayCommand(OnAction);
        DismissCommand = new RelayCommand(Dismiss);
    }

    /// <summary>
    /// Gets a value indicating whether a message is showing.
    /// </summary>
    public bool IsVisible { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public string Message { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the wording of the action button, if there is one.
    /// </summary>
    public string? ActionText
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasAction));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the message carries an action.
    /// </summary>
    public bool HasAction => ActionText is not null;

    /// <summary>
    /// Gets the command the action button runs.
    /// </summary>
    public RelayCommand ActionCommand { get; }

    /// <summary>
    /// Gets the command that hides the message.
    /// </summary>
    public RelayCommand DismissCommand { get; }

    /// <summary>
    /// Gets or sets how long a plain message stays.
    /// </summary>
    internal TimeSpan MessageDuration { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Gets or sets how long a message with an action stays.
    /// </summary>
    internal TimeSpan ActionDuration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public void Show(string message, string? actionText = null, Action? action = null)
    {
        StopTimer();

        Message = message;
        _action = action;
        ActionText = action is null ? null : actionText;
        IsVisible = true;

        CancellationTokenSource timer = new();
        _timer = timer;
        _ = DismissLaterAsync(HasAction ? ActionDuration : MessageDuration, timer);
    }

    /// <inheritdoc />
    public void Dismiss()
    {
        StopTimer();
        _action = null;
        IsVisible = false;
    }

    private async Task DismissLaterAsync(TimeSpan delay, CancellationTokenSource timer)
    {
        try
        {
            await Task.Delay(delay, timer.Token);
        }
        catch (OperationCanceledException)
        {
            // Replaced by a newer message, or dismissed already.
            return;
        }

        if (ReferenceEquals(_timer, timer))
        {
            Dismiss();
        }
    }

    private void OnAction()
    {
        Action? action = _action;
        Dismiss();
        action?.Invoke();
    }

    private void StopTimer()
    {
        CancellationTokenSource? timer = _timer;
        _timer = null;

        if (timer is not null)
        {
            timer.Cancel();
            timer.Dispose();
        }
    }
}
