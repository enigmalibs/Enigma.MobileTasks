using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="IListActions"/>, over the workspace store.
/// </summary>
public sealed class ListActions : IListActions
{
    private readonly IWorkspaceStore _store;
    private readonly IActiveListService _lists;
    private readonly INotificationService _notifications;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="store">The files on the phone.</param>
    /// <param name="lists">What the application knows about them.</param>
    /// <param name="notifications">Where outcomes are reported.</param>
    public ListActions(IWorkspaceStore store, IActiveListService lists, INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(notifications);

        _store = store;
        _lists = lists;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry?> CreateAsync(string? name, CancellationToken cancellationToken = default)
    {
        try
        {
            WorkspaceEntry entry = await _store.CreateAsync(name, cancellationToken);
            _lists.Track(entry);

            return entry;
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.CreateFailed, exception.Message);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry?> DuplicateAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            WorkspaceEntry copy = await _store.DuplicateAsync(entry, cancellationToken);
            _lists.Track(copy);
            Report(Strings.ListDuplicated, copy.List.Name);

            return copy;
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.DuplicateFailed, exception.Message);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RenameAsync(WorkspaceEntry entry, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            // Anything still waiting goes to the old file first; the rename then moves the list,
            // its latest state included.
            await _lists.FlushAsync(cancellationToken);

            if (!await _store.RenameAsync(entry, name, cancellationToken))
            {
                return false;
            }

            _lists.Resort();

            return true;
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.RenameFailed, exception.Message);

            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string name = entry.List.Name;

        try
        {
            // A save still waiting for this list would otherwise land after the deletion and bring
            // the file back.
            await _lists.FlushAsync(cancellationToken);
            await _store.DeleteAsync(entry, cancellationToken);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.DeleteFailed, exception.Message);

            return false;
        }

        _lists.Forget(entry);
        Report(Strings.ListDeleted, name);

        return true;
    }

    private static bool IsExpected(Exception exception)
        => exception is IOException or UnauthorizedAccessException;

    private void Report(string format, string argument)
        => _notifications.Show(string.Format(CultureInfo.CurrentCulture, format, argument));
}
