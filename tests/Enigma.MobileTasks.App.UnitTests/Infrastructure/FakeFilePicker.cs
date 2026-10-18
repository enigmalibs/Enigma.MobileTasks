using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.MobileTasks.App.UnitTests.Infrastructure;

/// <summary>
/// A system picker that answers from memory: the file an export lands in, and the files an import
/// reads — or nothing, which is the user cancelling.
/// </summary>
public sealed class FakeFilePicker : IFilePicker
{
    /// <summary>
    /// Gets or sets the file an export is written to; <see langword="null"/> cancels the export.
    /// </summary>
    public InMemoryFile? SaveTarget { get; set; }

    /// <summary>
    /// Gets the files an import is handed; empty cancels the import.
    /// </summary>
    public List<InMemoryFile> ToOpen { get; } = [];

    /// <summary>
    /// Gets the name the last export proposed.
    /// </summary>
    public string? SuggestedName { get; private set; }

    /// <summary>
    /// Builds a registration that puts this picker in place of the real one, with a recording
    /// navigation beside it.
    /// </summary>
    /// <param name="services">The collection.</param>
    public void Register(IServiceCollection services)
    {
        RecordingNavigation.Register(services);
        services.AddSingleton<IFilePicker>(this);
    }

    /// <inheritdoc />
    public Task<IPickedFile?> PickSaveTargetAsync(string suggestedName, CancellationToken cancellationToken = default)
    {
        SuggestedName = suggestedName;

        return Task.FromResult<IPickedFile?>(SaveTarget);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IPickedFile>> PickFilesToOpenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<IPickedFile>>([.. ToOpen]);
}

/// <summary>
/// A picked file held in memory.
/// </summary>
/// <param name="name">The file's name.</param>
/// <param name="content">What it holds before anything is written to it.</param>
public sealed class InMemoryFile(string name, byte[]? content = null) : IPickedFile
{
    /// <inheritdoc />
    public string Name { get; } = name;

    /// <summary>
    /// Gets what the file holds — after an export, the bytes written.
    /// </summary>
    public byte[] Content { get; private set; } = content ?? [];

    /// <summary>
    /// Gets a value indicating whether the file was disposed, as the picker's files must be.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(new MemoryStream(Content, writable: false));

    /// <inheritdoc />
    public Task<Stream> OpenWriteAsync() => Task.FromResult<Stream>(new CapturingStream(bytes => Content = bytes));

    /// <inheritdoc />
    public void Dispose() => IsDisposed = true;

    private sealed class CapturingStream(System.Action<byte[]> onClose) : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onClose(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}
