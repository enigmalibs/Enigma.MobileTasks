using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.MobileTasks.App.Resources;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="IDialogService"/>, and the ViewModel of the dialog overlay the shell draws.
/// </summary>
/// <remarks>
/// The service is its own ViewModel on purpose: the overlay is a view of exactly this state, and a
/// ViewModel that asks a question can be tested by answering it through <see cref="ConfirmCommand"/>
/// and <see cref="CancelCommand"/>, with no window anywhere.
/// </remarks>
public sealed class DialogService : ObservableObject, IDialogService
{
    private TaskCompletionSource<bool>? _pending;
    private CancellationTokenRegistration _cancellation;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public DialogService()
    {
        ConfirmCommand = new RelayCommand(OnConfirm, CanConfirm);
        CancelCommand = new RelayCommand(OnCancel);
    }

    /// <inheritdoc />
    public bool IsOpen { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets what is being asked.
    /// </summary>
    public string Title { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the explanation under the title, if any.
    /// </summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is an explanation to show.
    /// </summary>
    public bool HasMessage => !string.IsNullOrEmpty(Message);

    /// <summary>
    /// Gets a value indicating whether the dialog asks for text.
    /// </summary>
    public bool IsPrompt { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets the text being typed into a prompt.
    /// </summary>
    public string InputText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ConfirmCommand.NotifyCanExecuteChanged();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the hint shown while the prompt's box is empty.
    /// </summary>
    public string? Placeholder { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the wording of the confirm button.
    /// </summary>
    public string ConfirmText { get; private set => SetProperty(ref field, value); } = Strings.Ok;

    /// <summary>
    /// Gets the wording of the cancel button.
    /// </summary>
    public string CancelText => Strings.Cancel;

    /// <summary>
    /// Gets a value indicating whether confirming destroys something.
    /// </summary>
    public bool IsDestructive { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the command that answers yes.
    /// </summary>
    public RelayCommand ConfirmCommand { get; }

    /// <summary>
    /// Gets the command that answers no.
    /// </summary>
    public RelayCommand CancelCommand { get; }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        bool destructive = false,
        CancellationToken cancellationToken = default)
        => ShowAsync(title, message, confirmText, destructive, isPrompt: false, null, null, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> PromptAsync(
        string title,
        string? initialText,
        string confirmText,
        string? placeholder = null,
        CancellationToken cancellationToken = default)
    {
        bool confirmed = await ShowAsync(
            title,
            null,
            confirmText,
            destructive: false,
            isPrompt: true,
            initialText,
            placeholder,
            cancellationToken);

        return confirmed ? InputText.Trim() : null;
    }

    /// <inheritdoc />
    public bool TryCancel()
    {
        if (!IsOpen)
        {
            return false;
        }

        Close(false);

        return true;
    }

    private Task<bool> ShowAsync(
        string title,
        string? message,
        string confirmText,
        bool destructive,
        bool isPrompt,
        string? initialText,
        string? placeholder,
        CancellationToken cancellationToken)
    {
        // A dialog already open is answered "no" rather than left waiting forever.
        TryCancel();

        TaskCompletionSource<bool> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;

        Title = title;
        Message = message;
        ConfirmText = confirmText;
        IsDestructive = destructive;
        IsPrompt = isPrompt;
        Placeholder = placeholder;
        InputText = initialText ?? string.Empty;
        ConfirmCommand.NotifyCanExecuteChanged();
        IsOpen = true;

        if (cancellationToken.CanBeCanceled)
        {
            _cancellation = cancellationToken.Register(() =>
            {
                if (ReferenceEquals(_pending, pending))
                {
                    Close(false);
                }
            });
        }

        return pending.Task;
    }

    private bool CanConfirm() => !IsPrompt || !string.IsNullOrWhiteSpace(InputText);

    private void OnConfirm()
    {
        if (IsOpen && CanConfirm())
        {
            Close(true);
        }
    }

    private void OnCancel() => TryCancel();

    private void Close(bool result)
    {
        TaskCompletionSource<bool>? pending = _pending;
        _pending = null;
        _cancellation.Dispose();
        _cancellation = default;
        IsOpen = false;

        // The answer is read after this returns — InputText is left as typed until the next dialog.
        pending?.TrySetResult(result);
    }
}
