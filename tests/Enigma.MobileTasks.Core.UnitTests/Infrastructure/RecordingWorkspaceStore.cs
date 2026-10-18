using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.Core.UnitTests.Infrastructure;

/// <summary>
/// A store that only counts what it was asked to write, so a test can watch the save scheduler
/// without a disk.
/// </summary>
public sealed class RecordingWorkspaceStore : IWorkspaceStore
{
    private readonly Lock _gate = new();
    private readonly List<string> _saved = [];

    /// <summary>
    /// Gets or sets what <see cref="SaveAsync"/> should throw, if anything.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <inheritdoc />
    public string RootPath => "memory";

    /// <summary>
    /// Gets the paths written so far, in order.
    /// </summary>
    public IReadOnlyList<string> Saved
    {
        get
        {
            lock (_gate)
            {
                return [.. _saved];
            }
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        if (Failure is { } failure)
        {
            return Task.FromException(failure);
        }

        lock (_gate)
        {
            _saved.Add(entry.Path);
        }

        return Task.CompletedTask;
    }

    /* The scheduler uses SaveAsync and nothing else; the rest of the interface is deliberately
       not implemented so that a test reaching for it fails loudly instead of quietly passing. */

    /// <inheritdoc />
    public Task<WorkspaceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<WorkspaceEntry> CreateAsync(string? name = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<WorkspaceEntry> ImportAsync(Stream source, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<WorkspaceEntry> ImportTextAsync(string text, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task ExportAsync(TaskList list, Stream destination, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<WorkspaceEntry> DuplicateAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> RenameAsync(WorkspaceEntry entry, string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> DeleteAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
