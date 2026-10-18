using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Everything that adds, copies, renames or removes a list — with the message that says what
/// happened, or why it did not.
/// </summary>
/// <remarks>
/// One place for these rather than a copy in every ViewModel: each of them has to keep the files on
/// the phone and <see cref="IActiveListService"/> in step. None of them asks anything — asking is
/// the caller's job — and none of them throws for a failure the user can be told about.
/// </remarks>
public interface IListActions
{
    /// <summary>
    /// Creates an empty list.
    /// </summary>
    /// <param name="name">Its name; blank falls back to the default.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The new list, or <see langword="null"/> when it could not be created.</returns>
    Task<WorkspaceEntry?> CreateAsync(string? name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a list, with fresh identifiers, into a file of its own.
    /// </summary>
    /// <param name="entry">The list to copy.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The copy, or <see langword="null"/> when it could not be made.</returns>
    Task<WorkspaceEntry?> DuplicateAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a list, and its file with it.
    /// </summary>
    /// <param name="entry">The list to rename.</param>
    /// <param name="name">The new name.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns><see langword="true"/> when the name actually changed.</returns>
    Task<bool> RenameAsync(WorkspaceEntry entry, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a list's file. The caller confirms first; this does not ask.
    /// </summary>
    /// <param name="entry">The list to delete.</param>
    /// <param name="cancellationToken">Cancels the deletion.</param>
    /// <returns><see langword="true"/> when it is gone.</returns>
    Task<bool> DeleteAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default);
}
