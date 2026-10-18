using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Serialization;
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
    private readonly IFilePicker _picker;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="store">The files on the phone.</param>
    /// <param name="lists">What the application knows about them.</param>
    /// <param name="notifications">Where outcomes are reported.</param>
    /// <param name="picker">The system picker an export or an import goes through.</param>
    public ListActions(
        IWorkspaceStore store,
        IActiveListService lists,
        INotificationService notifications,
        IFilePicker picker)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(picker);

        _store = store;
        _lists = lists;
        _notifications = notifications;
        _picker = picker;
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

    /// <inheritdoc />
    public async Task<bool> ExportAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            using IPickedFile? file = await _picker.PickSaveTargetAsync(
                ListFileName.Suggest(entry.List.Name),
                cancellationToken);

            if (file is null)
            {
                return false;
            }

            await using (Stream destination = await file.OpenWriteAsync())
            {
                await _store.ExportAsync(entry.List, destination, cancellationToken);
            }

            Report(Strings.Exported, entry.List.Name, file.Name);

            return true;
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.ExportFailed, exception.Message);

            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkspaceEntry>> ImportAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IPickedFile> files;

        try
        {
            files = await _picker.PickFilesToOpenAsync(cancellationToken);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Report(Strings.ImportRefused, Strings.ImportPickerTitle, exception.Message);

            return [];
        }

        List<WorkspaceEntry> imported = [];
        List<(string File, string Reason)> refused = [];

        foreach (IPickedFile file in files)
        {
            using (file)
            {
                try
                {
                    await using Stream source = await file.OpenReadAsync();
                    WorkspaceEntry entry = await _store.ImportAsync(source, cancellationToken);

                    _lists.Track(entry);
                    imported.Add(entry);
                }
                catch (Exception exception) when (exception is ListValidationException || IsExpected(exception))
                {
                    refused.Add((file.Name, exception.Message));
                }
            }
        }

        ReportImport(imported, refused);

        return imported;
    }

    /// <summary>
    /// Says, in one message, what came in and what did not.
    /// </summary>
    private void ReportImport(List<WorkspaceEntry> imported, List<(string File, string Reason)> refused)
    {
        List<string> lines = [];

        if (imported.Count == 1)
        {
            lines.Add(Format(Strings.ImportedOne, imported[0].List.Name));
        }
        else if (imported.Count > 1)
        {
            lines.Add(Format(Strings.ImportedMany, imported.Count.ToString(CultureInfo.CurrentCulture)));
        }

        if (refused.Count == 1)
        {
            lines.Add(Format(Strings.ImportRefused, refused[0].File, refused[0].Reason));
        }
        else if (refused.Count > 1)
        {
            lines.Add(Format(Strings.ImportRefusedMany, refused.Count.ToString(CultureInfo.CurrentCulture)));
        }

        if (lines.Count > 0)
        {
            _notifications.Show(string.Join(Environment.NewLine, lines));
        }
    }

    private static bool IsExpected(Exception exception)
        => exception is IOException or UnauthorizedAccessException;

    private void Report(string format, params string[] arguments)
        => _notifications.Show(Format(format, arguments));

    private static string Format(string format, params string[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
