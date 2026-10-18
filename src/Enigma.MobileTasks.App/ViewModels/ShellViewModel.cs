using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.ViewModels;

/// <summary>
/// Backs the shell around the navigation stack: the dialog overlay, the snackbar, and the one-time
/// start-up work that has somewhere to report a problem only once the shell is on screen.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly IActiveListService _lists;
    private Task? _initialization;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="lists">The lists the application knows about.</param>
    /// <param name="dialog">The dialog overlay's state.</param>
    /// <param name="notifications">The snackbar's state.</param>
    public ShellViewModel(IActiveListService lists, DialogService dialog, NotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(notifications);

        _lists = lists;
        Dialog = dialog;
        Notifications = notifications;
    }

    /// <summary>
    /// Gets the dialog overlay's state.
    /// </summary>
    public DialogService Dialog { get; }

    /// <summary>
    /// Gets the snackbar's state.
    /// </summary>
    public NotificationService Notifications { get; }

    /// <summary>
    /// Reads the lists for the first time and reports whatever went wrong on the way. Runs once per
    /// process, however many times the activity — and so the shell — is re-created: every later call
    /// returns the first call's task, so whoever awaits it waits for the lists to be there.
    /// </summary>
    /// <param name="settingsWarning">What reading the settings reported, if anything.</param>
    /// <returns>A task that completes once the lists are loaded.</returns>
    public Task InitializeAsync(string? settingsWarning = null)
        => _initialization ??= InitializeCoreAsync(settingsWarning);

    private async Task InitializeCoreAsync(string? settingsWarning)
    {
        _lists.SaveFailed += OnSaveFailed;

        if (settingsWarning is not null)
        {
            Notifications.Show(settingsWarning);
        }

        try
        {
            Report(await _lists.ReloadAsync());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Notifications.Show(Format(Strings.LoadFailed, exception.Message));
        }
    }

    private void Report(IReadOnlyList<WorkspaceProblem> problems)
    {
        switch (problems.Count)
        {
            case 0:
                return;

            case 1:
                Notifications.Show(Format(Strings.OneFileUnreadable, Path.GetFileName(problems[0].Path), problems[0].Message));
                return;

            default:
                Notifications.Show(Format(Strings.SomeFilesUnreadable, problems.Count));
                return;
        }
    }

    /// <summary>
    /// Reports a save that did not happen. The scheduler raises this on the thread the write ran on,
    /// so it is marshalled; <c>Post</c> rather than <c>Invoke</c>, because the flush on the way to the
    /// background can be waiting on that very thread.
    /// </summary>
    private void OnSaveFailed(object? sender, WorkspaceSaveFailedEventArgs e)
        => Dispatcher.UIThread.Post(() => Notifications.Show(
            Format(Strings.SaveFailed, e.Entry.List.Name, e.Exception.Message)));

    private static string Format(string format, params object[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
