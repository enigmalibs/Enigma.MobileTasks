using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// The folder of list files the application works out of.
/// </summary>
/// <remarks>
/// <para>
/// Every list is one ordinary <c>.json</c> file in the exact format Enigma.Tasks and the Listes web
/// app read and write. There is no container, no index and no database: each file <em>is</em> the
/// export, which is what makes moving a list to another device a copy rather than a conversion.
/// </para>
/// <para>
/// Import and export take streams rather than paths: on Android what the system file picker hands
/// back is a content URI, opened as a stream, never a path on disk.
/// </para>
/// </remarks>
public interface IWorkspaceStore
{
    /// <summary>
    /// Gets the folder the lists live in.
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// Reads every list in the folder.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The lists that could be read, and the files that could not.</returns>
    Task<WorkspaceLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an empty list and writes it out.
    /// </summary>
    /// <param name="name">Its name; blank falls back to the default.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The new entry.</returns>
    Task<WorkspaceEntry> CreateAsync(string? name = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an entry's list back to its file.
    /// </summary>
    /// <param name="entry">The entry to write.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the file is in place.</returns>
    Task SaveAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a list file from a stream and adds a copy of it to the workspace.
    /// </summary>
    /// <param name="source">The file's content; read to the end and never written.</param>
    /// <param name="cancellationToken">Cancels the import.</param>
    /// <returns>The new entry.</returns>
    /// <exception cref="Serialization.ListValidationException">
    /// The content is not a list this application can read, or is far too large to be one.
    /// Nothing is written.
    /// </exception>
    Task<WorkspaceEntry> ImportAsync(Stream source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a list given as text to the workspace.
    /// </summary>
    /// <param name="text">The list, in the JSON file format.</param>
    /// <param name="cancellationToken">Cancels the import.</param>
    /// <returns>The new entry.</returns>
    /// <exception cref="Serialization.ListValidationException">
    /// The text is not a list this application can read. Nothing is written.
    /// </exception>
    Task<WorkspaceEntry> ImportTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a list, in the file format, to a stream outside the workspace.
    /// </summary>
    /// <param name="list">The list to export.</param>
    /// <param name="destination">Where to write it; written from its current position and flushed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the content is written.</returns>
    Task ExportAsync(TaskList list, Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a list, identifiers included but renewed, into a file of its own.
    /// </summary>
    /// <param name="entry">The entry to copy.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The new entry.</returns>
    Task<WorkspaceEntry> DuplicateAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a list, and its file with it.
    /// </summary>
    /// <param name="entry">The entry to rename.</param>
    /// <param name="name">The new name; a blank one is refused.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns><see langword="true"/> when the name actually changed.</returns>
    Task<bool> RenameAsync(WorkspaceEntry entry, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a list's file.
    /// </summary>
    /// <param name="entry">The entry to delete.</param>
    /// <param name="cancellationToken">Cancels the deletion.</param>
    /// <returns><see langword="true"/> when a file was actually deleted.</returns>
    Task<bool> DeleteAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default);
}
