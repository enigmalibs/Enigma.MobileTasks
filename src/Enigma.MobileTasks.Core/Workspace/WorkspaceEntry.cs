using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// One list in the workspace: the file it lives in, and what is in it.
/// </summary>
/// <remarks>
/// The path is the identity. A workspace is a folder of ordinary files rather than a database,
/// which is the whole point — the file is what gets synced, attached to a message or copied to
/// a phone — so the file is also what the application points at.
/// </remarks>
public sealed class WorkspaceEntry
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="path">The file the list lives in.</param>
    /// <param name="list">The list itself.</param>
    public WorkspaceEntry(string path, TaskList list)
    {
        Path = path;
        List = list;
    }

    /// <summary>
    /// Gets or sets the file the list lives in. Renaming a list renames its file, which is the
    /// only time this changes.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets the list.
    /// </summary>
    public TaskList List { get; }

    /// <summary>
    /// Gets the file's name, which is what the user sees when they go looking for it.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Path);
}
