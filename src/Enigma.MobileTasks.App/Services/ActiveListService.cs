using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="IActiveListService"/>, over the workspace store and the save scheduler.
/// </summary>
public sealed class ActiveListService : IActiveListService
{
    private readonly IWorkspaceStore _store;
    private readonly SaveScheduler _scheduler;
    private readonly List<WorkspaceEntry> _entries = [];

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="store">The workspace folder.</param>
    /// <param name="scheduler">Writes a changed list shortly after it stops changing.</param>
    public ActiveListService(IWorkspaceStore store, SaveScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(scheduler);

        _store = store;
        _scheduler = scheduler;
        _scheduler.SaveFailed += (_, e) => SaveFailed?.Invoke(this, e);
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkspaceEntry> Entries => _entries;

    /// <inheritdoc />
    public WorkspaceEntry? Current { get; private set; }

    /// <inheritdoc />
    public event EventHandler? EntriesChanged;

    /// <inheritdoc />
    public event EventHandler? CurrentChanged;

    /// <inheritdoc />
    public event EventHandler<WorkspaceEntry>? Touched;

    /// <inheritdoc />
    public event EventHandler<WorkspaceSaveFailedEventArgs>? SaveFailed;

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkspaceProblem>> ReloadAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceLoadResult result = await _store.LoadAsync(cancellationToken);

        _entries.Clear();
        _entries.AddRange(result.Entries);
        EntriesChanged?.Invoke(this, EventArgs.Empty);

        // The entries are new objects: whatever was open is not among them any more.
        Open(null);

        return result.Problems;
    }

    /// <inheritdoc />
    public void Open(WorkspaceEntry? entry)
    {
        if (ReferenceEquals(entry, Current))
        {
            return;
        }

        Current = entry;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Track(WorkspaceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!_entries.Contains(entry))
        {
            _entries.Add(entry);
        }

        Resort();
    }

    /// <inheritdoc />
    public void Forget(WorkspaceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_entries.Remove(entry))
        {
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        if (ReferenceEquals(entry, Current))
        {
            Open(null);
        }
    }

    /// <inheritdoc />
    public void Resort()
    {
        _entries.Sort(static (left, right) =>
        {
            int byName = string.Compare(
                left.List.Name,
                right.List.Name,
                StringComparison.InvariantCultureIgnoreCase);

            return byName != 0
                ? byName
                : string.Compare(left.FileName, right.FileName, StringComparison.OrdinalIgnoreCase);
        });

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Touch(WorkspaceEntry? entry = null)
    {
        if ((entry ?? Current) is { } target)
        {
            _scheduler.Schedule(target);
            Touched?.Invoke(this, target);
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default)
        => _scheduler.FlushAsync(cancellationToken);
}
