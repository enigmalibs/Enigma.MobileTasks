using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.Settings;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// The default <see cref="IWorkspaceStore"/>: a folder of <c>.json</c> list files.
/// </summary>
public sealed class WorkspaceStore : IWorkspaceStore
{
    /// <summary>
    /// The largest file an import reads. A list of a few hundred items is a few tens of kilobytes;
    /// the bound is there so a video picked by mistake under "All files" is refused instead of being
    /// read into memory whole.
    /// </summary>
    public const int MaxImportBytes = 4 * 1024 * 1024;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly SettingsLocation _location;
    private readonly ListOperations _operations;
    private readonly ListJsonSerializer _json;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="location">Says which folder the lists live in.</param>
    /// <param name="operations">Creates and copies lists.</param>
    /// <param name="json">Reads and writes the list format.</param>
    public WorkspaceStore(SettingsLocation location, ListOperations operations, ListJsonSerializer json)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(json);

        _location = location;
        _operations = operations;
        _json = json;
    }

    /// <inheritdoc />
    public string RootPath => _location.WorkspacePath;

    /// <inheritdoc />
    public async Task<WorkspaceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        string root = EnsureRoot();

        List<WorkspaceEntry> entries = [];
        List<WorkspaceProblem> problems = [];

        foreach (string path in Directory
            .EnumerateFiles(root, "*" + ListFileName.JsonExtension, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                entries.Add(new WorkspaceEntry(path, _json.Deserialize(text)));
            }
            catch (Exception exception) when (exception is ListValidationException or IOException or UnauthorizedAccessException)
            {
                // Left exactly where it is: it is the user's file, and the next save must not
                // land on top of something this application merely failed to understand.
                problems.Add(new WorkspaceProblem(path, exception.Message));
            }
        }

        return new WorkspaceLoadResult(
            [.. entries.OrderBy(entry => entry.List.Name, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase)],
            problems);
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry> CreateAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        TaskList list = _operations.CreateList(name);

        return await AddAsync(list, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EnsureRoot();

        await AtomicFile.WriteAsync(entry.Path, ListJsonSerializer.Serialize(entry.List), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry> ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        string text = await ReadBoundedAsync(source, cancellationToken).ConfigureAwait(false);

        return await ImportTextAsync(text, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry> ImportTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        return await AddAsync(_json.Deserialize(text), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportAsync(TaskList list, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(destination);

        byte[] bytes = Utf8NoBom.GetBytes(ListJsonSerializer.Serialize(list));

        await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkspaceEntry> DuplicateAsync(
        WorkspaceEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        TaskList copy = _operations.Clone(entry.List, withNewIds: true);
        copy.Name = entry.List.Name + " (copy)";

        return await AddAsync(copy, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RenameAsync(
        WorkspaceEntry entry,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (ListOperations.RenameList(entry.List, name) is null)
        {
            return false;
        }

        string root = EnsureRoot();
        string wanted = ListFileName.Unique(root, entry.List.Name, ListFileName.JsonExtension, entry.Path);

        await AtomicFile.WriteAsync(wanted, ListJsonSerializer.Serialize(entry.List), cancellationToken)
            .ConfigureAwait(false);

        // The new file is in place before the old one goes, so a failure in between leaves two
        // readable lists rather than none.
        if (!PathsMatch(wanted, entry.Path) && File.Exists(entry.Path))
        {
            File.Delete(entry.Path);
        }

        entry.Path = wanted;

        return true;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(WorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(entry.Path))
        {
            return Task.FromResult(false);
        }

        File.Delete(entry.Path);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Reads a stream as text, refusing it once it passes <see cref="MaxImportBytes"/>.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The text, a UTF-8 byte-order mark dropped if there was one.</returns>
    /// <exception cref="ListValidationException">The stream is larger than the bound.</exception>
    private static async Task<string> ReadBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        int read;

        while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxImportBytes)
            {
                throw new ListValidationException(new ListValidationError(
                    "file-too-large",
                    string.Empty,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"This file is too large to be a list (more than {MaxImportBytes / (1024 * 1024)} MB).")));
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;

        using StreamReader reader = new(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<WorkspaceEntry> AddAsync(TaskList list, CancellationToken cancellationToken)
    {
        string root = EnsureRoot();
        WorkspaceEntry entry = new(ListFileName.Unique(root, list.Name), list);

        await SaveAsync(entry, cancellationToken).ConfigureAwait(false);

        return entry;
    }

    private string EnsureRoot()
    {
        string root = RootPath;

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("No list folder is configured.");
        }

        Directory.CreateDirectory(root);

        return root;
    }

    private static bool PathsMatch(string left, string right)
        => string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
