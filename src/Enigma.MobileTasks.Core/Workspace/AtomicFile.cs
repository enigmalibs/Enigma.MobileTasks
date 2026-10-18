using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.Core.Workspace;

/// <summary>
/// Writes a text file in a way that cannot leave a half-written one behind.
/// </summary>
/// <remarks>
/// The content goes to a temporary file next to the target, which is then moved over it. A move
/// within a folder is a rename, which is atomic on the phone's file system, so a crash, a full
/// disk or the system killing a backgrounded application mid-save leaves either the previous list
/// or the new one — never a truncated file where a list used to be.
/// </remarks>
public static class AtomicFile
{
    /// <summary>
    /// The encoding every file this application writes uses: UTF-8 with no byte-order mark, as
    /// the list format requires.
    /// </summary>
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Writes text to a file, replacing whatever was there.
    /// </summary>
    /// <param name="path">The file to write.</param>
    /// <param name="content">What to write.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the file is in place.</returns>
    public static async Task WriteAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        string? folder = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string temporary = path + ".tmp";

        await File.WriteAllTextAsync(temporary, content, Utf8NoBom, cancellationToken).ConfigureAwait(false);

        File.Move(temporary, path, overwrite: true);
    }
}
