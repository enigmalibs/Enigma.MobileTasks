using System;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// Announces that a scheduled save did not happen.
/// </summary>
/// <remarks>
/// A save the user did not ask for must not throw into whatever they were doing, but it must
/// not be swallowed either: a list that is quietly not being written is the worst possible
/// failure for this application. The scheduler reports it here and the shell shows it.
/// </remarks>
public sealed class WorkspaceSaveFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="entry">The list that could not be written.</param>
    /// <param name="exception">What went wrong.</param>
    public WorkspaceSaveFailedEventArgs(WorkspaceEntry entry, Exception exception)
    {
        Entry = entry;
        Exception = exception;
    }

    /// <summary>
    /// Gets the list that could not be written.
    /// </summary>
    public WorkspaceEntry Entry { get; }

    /// <summary>
    /// Gets what went wrong.
    /// </summary>
    public Exception Exception { get; }
}
