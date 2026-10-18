using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// Writes a list out shortly after it stops changing.
/// </summary>
/// <remarks>
/// <para>
/// Ticking a box is a save, and so is every keystroke in a rename box. Writing on each of them
/// would put the disk in the middle of the interaction; not writing until the user asks would
/// reintroduce the one thing a checklist must never have — a way to lose what you just ticked.
/// The middle is this: a change schedules a write, a further change within the delay replaces
/// the pending one, and closing the application flushes whatever is still waiting.
/// </para>
/// <para>
/// Pending writes are keyed by file path, so several lists can be waiting at once without
/// stepping on each other.
/// </para>
/// </remarks>
public sealed class SaveScheduler : IAsyncDisposable, IDisposable
{
    private readonly IWorkspaceStore _store;
    private readonly TimeSpan _delay;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Pending> _pending = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="store">Where the lists are written.</param>
    /// <param name="delay">
    /// How long to wait for the changes to stop. The default coalesces a burst of typing without
    /// being long enough to lose anything that matters.
    /// </param>
    public SaveScheduler(IWorkspaceStore store, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _delay = delay ?? TimeSpan.FromMilliseconds(400);
    }

    /// <summary>
    /// Raised when a scheduled write failed.
    /// </summary>
    /// <remarks>
    /// <strong>Raised on the thread the write ran on</strong>, which is never the one that
    /// scheduled it: the whole point of the scheduler is that the write happens later. A
    /// subscriber that touches a user interface must marshal onto its own thread first.
    /// </remarks>
    public event EventHandler<WorkspaceSaveFailedEventArgs>? SaveFailed;

    /// <summary>
    /// Gets a value indicating whether a write is waiting to happen.
    /// </summary>
    public bool HasPending
    {
        get
        {
            lock (_gate)
            {
                return _pending.Count > 0;
            }
        }
    }

    /// <summary>
    /// Notes that a list changed and should be written shortly.
    /// </summary>
    /// <param name="entry">The list that changed.</param>
    public void Schedule(WorkspaceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Pending pending;

        lock (_gate)
        {
            if (_pending.TryGetValue(entry.Path, out Pending? existing))
            {
                existing.Cancellation.Cancel();
            }

            pending = new Pending(entry);
            _pending[entry.Path] = pending;
        }

        _ = WaitThenWriteAsync(pending);
    }

    /// <summary>
    /// Writes everything that is still waiting, right now.
    /// </summary>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>A task that completes once every pending list is on disk.</returns>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        List<Pending> waiting;

        lock (_gate)
        {
            waiting = [.. _pending.Values];
            _pending.Clear();
        }

        foreach (Pending pending in waiting)
        {
            pending.Cancellation.Cancel();
            await WriteAsync(pending.Entry, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await FlushAsync().ConfigureAwait(false);
        Discard();
    }

    /// <summary>
    /// Drops whatever is still waiting, without writing it.
    /// </summary>
    /// <remarks>
    /// The container disposes its singletons synchronously, and a <c>Dispose</c> that blocked on
    /// a write would be a deadlock waiting to happen on the way out. Durability is not left to
    /// chance for it: the application flushes explicitly before the host is disposed, and
    /// <see cref="DisposeAsync"/> flushes for anyone who can await. This is the last-resort
    /// cleanup of the timers themselves.
    /// </remarks>
    public void Dispose() => Discard();

    private void Discard()
    {
        lock (_gate)
        {
            foreach (Pending pending in _pending.Values)
            {
                pending.Cancellation.Cancel();
                pending.Cancellation.Dispose();
            }

            _pending.Clear();
        }
    }

    private async Task WaitThenWriteAsync(Pending pending)
    {
        try
        {
            await Task.Delay(_delay, pending.Cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Replaced by a later change, or flushed already.
            return;
        }

        lock (_gate)
        {
            if (!_pending.TryGetValue(pending.Entry.Path, out Pending? current) || current != pending)
            {
                return;
            }

            _pending.Remove(pending.Entry.Path);
        }

        await WriteAsync(pending.Entry, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task WriteAsync(WorkspaceEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _store.SaveAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SaveFailed?.Invoke(this, new WorkspaceSaveFailedEventArgs(entry, exception));
        }
    }

    private sealed class Pending
    {
        public Pending(WorkspaceEntry entry) => Entry = entry;

        public WorkspaceEntry Entry { get; }

        public CancellationTokenSource Cancellation { get; } = new();
    }
}
