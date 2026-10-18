using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.App.Services;

/// <summary>
/// Default <see cref="IFilePicker"/>, over Avalonia's <see cref="IStorageProvider"/> — on Android, the
/// Storage Access Framework.
/// </summary>
/// <remarks>
/// The storage provider belongs to the activity's top level, so the shell hands it over with
/// <see cref="Attach"/> each time Android builds one. Using the picker before that throws.
/// </remarks>
public sealed class StorageFilePicker : IFilePicker
{
    /// <summary>
    /// The list file type: what export proposes and what import shows first.
    /// </summary>
    private static readonly FilePickerFileType JsonList = new(Strings.JsonFileType)
    {
        Patterns = ["*" + ListFileName.JsonExtension],
        MimeTypes = ["application/json"],
        AppleUniformTypeIdentifiers = ["public.json"],
    };

    private IStorageProvider? _provider;

    /// <summary>
    /// Hands over the storage provider of the activity now on screen.
    /// </summary>
    /// <param name="provider">The top level's storage provider.</param>
    public void Attach(IStorageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<IPickedFile?> PickSaveTargetAsync(string suggestedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IStorageFile? file = await Provider.SaveFilePickerAsync(CreateSaveOptions(suggestedName));

        return file is null ? null : new StorageFile(file);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IPickedFile>> PickFilesToOpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<IStorageFile> files = await Provider.OpenFilePickerAsync(CreateOpenOptions());

        return [.. files.Select(file => (IPickedFile)new StorageFile(file))];
    }

    /// <summary>
    /// Builds what the save picker is asked.
    /// </summary>
    /// <param name="suggestedName">The proposed file name, extension included.</param>
    /// <returns>The options.</returns>
    /// <remarks>
    /// No <c>DefaultExtension</c>: the suggested name already ends in <c>.json</c>, and Android appends
    /// the default extension regardless — the picker proposed <c>vacances.json.json</c>. The MIME type
    /// of the file-type choice is what tells Android's documents UI the file is JSON.
    /// </remarks>
    internal static FilePickerSaveOptions CreateSaveOptions(string suggestedName) => new()
    {
        Title = Strings.ExportPickerTitle,
        SuggestedFileName = suggestedName,
        FileTypeChoices = [JsonList],
        ShowOverwritePrompt = true,
    };

    /// <summary>
    /// Builds what the open picker is asked.
    /// </summary>
    /// <returns>The options.</returns>
    /// <remarks>
    /// "All files" beside the JSON type: cloud providers label a <c>.json</c> file as text or as a
    /// binary blob often enough that a JSON-only filter would grey out the very file the user wants.
    /// The content is validated anyway.
    /// </remarks>
    internal static FilePickerOpenOptions CreateOpenOptions() => new()
    {
        Title = Strings.ImportPickerTitle,
        AllowMultiple = true,
        FileTypeFilter = [JsonList, FilePickerFileTypes.All],
    };

    private IStorageProvider Provider
        => _provider ?? throw new InvalidOperationException("No storage provider has been attached yet.");

    private sealed class StorageFile(IStorageFile file) : IPickedFile
    {
        public string Name => file.Name;

        public Task<Stream> OpenReadAsync() => file.OpenReadAsync();

        public Task<Stream> OpenWriteAsync() => file.OpenWriteAsync();

        public void Dispose() => file.Dispose();
    }
}
