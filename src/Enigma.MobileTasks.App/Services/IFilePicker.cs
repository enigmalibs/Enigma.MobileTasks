using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// The system's own file picker — Android's documents UI: Downloads, Drive, a USB stick, whatever
/// the phone offers. It is how a list leaves the phone and how one arrives, without the application
/// asking for any storage permission.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Asks where to save a file.
    /// </summary>
    /// <param name="suggestedName">The name the picker proposes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The file to write, or <see langword="null"/> when the user cancelled.</returns>
    Task<IPickedFile?> PickSaveTargetAsync(string suggestedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks for one or more files to open.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The files picked; empty when the user cancelled.</returns>
    Task<IReadOnlyList<IPickedFile>> PickFilesToOpenAsync(CancellationToken cancellationToken = default);
}
