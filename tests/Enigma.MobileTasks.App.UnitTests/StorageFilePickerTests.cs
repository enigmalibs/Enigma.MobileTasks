using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Enigma.MobileTasks.App.Services;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// What the system picker is asked — the part of the picker that is ours.
/// </summary>
public sealed class StorageFilePickerTests
{
    /// <summary>
    /// The suggested name carries its extension and no default extension is set: Android appends the
    /// default extension even to a name that already has it, and proposed "vacances.json.json".
    /// </summary>
    [Fact]
    public void SaveOptions_ProposeTheFileNameAsIs()
    {
        FilePickerSaveOptions options = StorageFilePicker.CreateSaveOptions("liste-judith.json");

        Assert.Equal("liste-judith.json", options.SuggestedFileName);
        Assert.Null(options.DefaultExtension);
        FilePickerFileType json = Assert.Single(options.FileTypeChoices!);
        Assert.Equal(["application/json"], json.MimeTypes);
        Assert.Equal(["*.json"], json.Patterns);
    }

    [Fact]
    public void OpenOptions_AllowSeveralFilesAndOfferAllFiles()
    {
        FilePickerOpenOptions options = StorageFilePicker.CreateOpenOptions();

        Assert.True(options.AllowMultiple);
        Assert.Equal(2, options.FileTypeFilter!.Count);
        Assert.Contains(FilePickerFileTypes.All, options.FileTypeFilter);
    }

    [Fact]
    public async Task Picking_BeforeAStorageProviderIsAttached_Throws()
    {
        StorageFilePicker picker = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() => picker.PickFilesToOpenAsync(TestContext.Current.CancellationToken));
    }
}
