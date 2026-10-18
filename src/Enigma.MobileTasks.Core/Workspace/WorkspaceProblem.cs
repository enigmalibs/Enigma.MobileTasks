namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// A file in the workspace folder that could not be read.
/// </summary>
/// <param name="Path">The file.</param>
/// <param name="Message">Why it could not be read, in English, ready to show.</param>
/// <remarks>
/// A file that cannot be read is reported and left exactly where it is. It is the user's data:
/// a bug in this application must never be the reason it is gone, and the next save must never
/// land on top of it.
/// </remarks>
public sealed record WorkspaceProblem(string Path, string Message);
