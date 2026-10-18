using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// What reading the workspace folder found.
/// </summary>
/// <param name="Entries">The lists that could be read, ordered by name.</param>
/// <param name="Problems">The files that could not, left untouched on disk.</param>
public sealed record WorkspaceLoadResult(
    IReadOnlyList<WorkspaceEntry> Entries,
    IReadOnlyList<WorkspaceProblem> Problems);
