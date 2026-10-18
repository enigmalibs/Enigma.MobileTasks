using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// The lists the application knows about, and the one being edited.
/// </summary>
/// <remarks>
/// Every page observes this rather than holding its own copy: the Lists page changes which list is
/// open, the Checklist page edits it, and the Lists page's progress follows the edits. One service
/// means one truth, and one place where a change turns into a scheduled save.
/// </remarks>
public interface IActiveListService
{
    /// <summary>
    /// Gets the lists in the workspace, ordered by name.
    /// </summary>
    IReadOnlyList<WorkspaceEntry> Entries { get; }

    /// <summary>
    /// Gets the list being edited, if any.
    /// </summary>
    WorkspaceEntry? Current { get; }

    /// <summary>
    /// Raised when a list is added, removed or renamed.
    /// </summary>
    event EventHandler? EntriesChanged;

    /// <summary>
    /// Raised when a different list is opened.
    /// </summary>
    event EventHandler? CurrentChanged;

    /// <summary>
    /// Raised when a list's contents changed, with the list that did.
    /// </summary>
    event EventHandler<WorkspaceEntry>? Touched;

    /// <summary>
    /// Raised when a scheduled save failed.
    /// </summary>
    /// <remarks>
    /// <strong>Raised on the thread the write ran on</strong>, never the UI thread: a subscriber that
    /// touches the interface marshals first.
    /// </remarks>
    event EventHandler<WorkspaceSaveFailedEventArgs>? SaveFailed;

    /// <summary>
    /// Re-reads the workspace folder.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The files that could not be read, for the shell to report.</returns>
    Task<IReadOnlyList<WorkspaceProblem>> ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a list.
    /// </summary>
    /// <param name="entry">The list to open, or <see langword="null"/> to close the current one.</param>
    void Open(WorkspaceEntry? entry);

    /// <summary>
    /// Adds a list the store has just written to what the application knows about.
    /// </summary>
    /// <param name="entry">The new list.</param>
    void Track(WorkspaceEntry entry);

    /// <summary>
    /// Drops a list the store has just deleted, closing it if it was open.
    /// </summary>
    /// <param name="entry">The list that is gone.</param>
    void Forget(WorkspaceEntry entry);

    /// <summary>
    /// Re-sorts the lists and announces it — after a rename, which is the one change that moves a
    /// list without adding or removing one.
    /// </summary>
    void Resort();

    /// <summary>
    /// Notes that a list's contents changed, so it is written shortly.
    /// </summary>
    /// <param name="entry">The list that changed; defaults to the one being edited.</param>
    void Touch(WorkspaceEntry? entry = null);

    /// <summary>
    /// Writes every pending change now.
    /// </summary>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>A task that completes once everything is on disk.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
