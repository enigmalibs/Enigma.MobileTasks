using System;
using System.IO;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// A file the user picked in the system picker — on Android a content URI, never a path.
/// </summary>
public interface IPickedFile : IDisposable
{
    /// <summary>
    /// Gets the file's name, as the picker shows it.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Opens the file for reading.
    /// </summary>
    /// <returns>The stream; the caller disposes it.</returns>
    Task<Stream> OpenReadAsync();

    /// <summary>
    /// Opens the file for writing, from the start.
    /// </summary>
    /// <returns>The stream; the caller disposes it.</returns>
    Task<Stream> OpenWriteAsync();
}
