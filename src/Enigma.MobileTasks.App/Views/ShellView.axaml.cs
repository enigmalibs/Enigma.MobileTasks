using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.ViewModels;

namespace Enigma.MobileTasks.App.Views;

/// <summary>
/// The root of every activity: the navigation stack, the snackbar and the dialog overlay.
/// </summary>
public partial class ShellView : UserControl
{
    private IInsetsManager? _insets;
    private DialogService? _dialog;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public ShellView() => InitializeComponent();

    /// <summary>
    /// Gets the control the navigation stack is hosted in.
    /// </summary>
    public PageNavigationHost NavigationHost => Host;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _insets = TopLevel.GetTopLevel(this)?.InsetsManager;

        if (_insets is not null)
        {
            // Edge to edge, as Android 15 expects of an application targeting it: the navigation bar
            // paints behind the status bar and makes room for it itself. Left to the default, the
            // system reserves the strip *and* the bar adds the inset again — a bar twice as tall.
            _insets.DisplayEdgeToEdgePreference = true;
            _insets.SafeAreaChanged += OnSafeAreaChanged;
            OverlayLayer.Margin = _insets.SafeAreaPadding;
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(System.EventArgs e)
    {
        if (_dialog is not null)
        {
            _dialog.PropertyChanged -= OnDialogPropertyChanged;
        }

        _dialog = (DataContext as ShellViewModel)?.Dialog;

        if (_dialog is not null)
        {
            _dialog.PropertyChanged += OnDialogPropertyChanged;
        }

        base.OnDataContextChanged(e);
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        => OverlayLayer.Margin = e.SafeAreaPadding;

    /// <summary>
    /// Puts the cursor in a prompt's box as it opens, text selected, so the keyboard comes up and the
    /// first keystroke replaces the old name — the one bit of focus handling a ViewModel cannot do.
    /// </summary>
    private void OnDialogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DialogService.IsOpen) && _dialog is { IsOpen: true, IsPrompt: true })
        {
            Dispatcher.UIThread.Post(() =>
            {
                PromptInput.Focus();
                PromptInput.SelectAll();
            });
        }
    }
}
