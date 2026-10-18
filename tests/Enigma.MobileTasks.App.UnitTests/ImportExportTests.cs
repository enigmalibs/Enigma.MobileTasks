using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The point of the feature: a list leaves the phone in exactly the file Enigma.Tasks writes, and
/// comes in from the desktop or from another phone unchanged.
/// </summary>
public sealed class ImportExportTests
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// The desktop compatibility claim: the reference file — the one Enigma.Tasks proves its own output
    /// against — goes in, and the export is that file, byte for byte.
    /// </summary>
    [Fact]
    public async Task Export_OfTheReferenceList_IsByteForByteWhatEnigmaTasksWrites()
    {
        FakeFilePicker picker = new() { SaveTarget = new InMemoryFile("liste-judith.json") };
        using TempApp app = new(picker.Register);
        WorkspaceEntry entry = await app.Get<IWorkspaceStore>().ImportTextAsync(TestLists.ReadJudithJson(), TestContext.Current.CancellationToken);

        Assert.True(await app.Get<IListActions>().ExportAsync(entry, TestContext.Current.CancellationToken));

        Assert.Equal(Utf8NoBom.GetBytes(TestLists.ReadJudithJson().TrimEnd('\n')), picker.SaveTarget.Content);
        Assert.Equal("liste-judith.json", picker.SuggestedName);
        Assert.True(picker.SaveTarget.IsDisposed);
        Assert.Equal("\"Liste Judith\" exported as liste-judith.json.", app.Get<NotificationService>().Message);
    }

    [Fact]
    public async Task Export_Cancelled_WritesNothingAndSaysNothing()
    {
        FakeFilePicker picker = new();
        using TempApp app = new(picker.Register);
        WorkspaceEntry entry = (await app.Get<IListActions>().CreateAsync("Essai", TestContext.Current.CancellationToken))!;

        Assert.False(await app.Get<IListActions>().ExportAsync(entry, TestContext.Current.CancellationToken));

        Assert.False(app.Get<NotificationService>().IsVisible);
    }

    [Fact]
    public async Task Import_OfTheDesktopReferenceFile_AddsTheListUnchanged()
    {
        FakeFilePicker picker = new();
        picker.ToOpen.Add(new InMemoryFile("liste-judith.json", Encoding.UTF8.GetBytes(TestLists.ReadJudithJson())));
        using TempApp app = new(picker.Register);
        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        await page.ImportCommand.ExecuteAsync(null);

        ListCardViewModel card = Assert.Single(page.Cards);
        Assert.Equal("80 / 94", card.ProgressText);
        Assert.Equal(
            TestLists.ReadJudithJson().TrimEnd('\n'),
            await File.ReadAllTextAsync(card.Entry.Path, TestContext.Current.CancellationToken));
        Assert.Equal("\"Liste Judith\" imported.", app.Get<NotificationService>().Message);
        Assert.True(picker.ToOpen[0].IsDisposed);
    }

    [Fact]
    public async Task Import_OfAFileThatIsNotAList_SaysWhyAndAddsNothing()
    {
        FakeFilePicker picker = new();
        picker.ToOpen.Add(new InMemoryFile("notes.json", Encoding.UTF8.GetBytes("""{ "name": "Pas une liste" }""")));
        using TempApp app = new(picker.Register);

        Assert.Empty(await app.Get<IListActions>().ImportAsync(TestContext.Current.CancellationToken));

        Assert.Empty(Directory.EnumerateFiles(app.Root));
        Assert.Equal(
            "notes.json could not be imported: The file has no \"categories\" array.",
            app.Get<NotificationService>().Message);
    }

    [Fact]
    public async Task Import_OfSeveralFiles_AddsTheReadableOnesAndCountsTheRest()
    {
        FakeFilePicker picker = new();
        picker.ToOpen.Add(new InMemoryFile("liste-judith.json", Encoding.UTF8.GetBytes(TestLists.ReadJudithJson())));
        picker.ToOpen.Add(new InMemoryFile("photo.jpg", [0xFF, 0xD8, 0xFF, 0xE0]));
        picker.ToOpen.Add(new InMemoryFile("future.json", Encoding.UTF8.GetBytes("""{ "schemaVersion": 2, "categories": [] }""")));
        using TempApp app = new(picker.Register);

        Assert.Single(await app.Get<IListActions>().ImportAsync(TestContext.Current.CancellationToken));

        Assert.Single(Directory.EnumerateFiles(app.Root));
        Assert.Equal(
            "\"Liste Judith\" imported." + Environment.NewLine +
            "2 files could not be imported — they are not lists this app can read.",
            app.Get<NotificationService>().Message);
        Assert.All(picker.ToOpen, file => Assert.True(file.IsDisposed));
    }

    [Fact]
    public async Task Import_Cancelled_AddsNothingAndSaysNothing()
    {
        FakeFilePicker picker = new();
        using TempApp app = new(picker.Register);

        Assert.Empty(await app.Get<IListActions>().ImportAsync(TestContext.Current.CancellationToken));

        Assert.False(app.Get<NotificationService>().IsVisible);
    }

    /// <summary>
    /// The phone-to-phone claim: a list made on one phone, exported, and imported on another, arrives
    /// as the same file — identifiers, ticks and order included.
    /// </summary>
    [Fact]
    public async Task ExportOnOnePhone_ImportOnAnother_ArrivesAsTheSameFile()
    {
        FakeFilePicker first = new() { SaveTarget = new InMemoryFile("vacances.json") };
        using TempApp phoneA = new(first.Register);
        WorkspaceEntry made = (await phoneA.Get<IListActions>().CreateAsync("Vacances d'été", TestContext.Current.CancellationToken))!;
        phoneA.Get<IActiveListService>().Open(made);
        IListEditor editor = phoneA.Get<IListEditor>();
        TaskCategory clothes = editor.AddCategory("Habits")!;
        editor.AddItem(clothes, "Maillot");
        editor.Toggle(editor.AddItem(clothes, "Crème solaire")!, true);
        editor.AddCategory("Plage — « sable »");
        await phoneA.Get<IActiveListService>().FlushAsync(TestContext.Current.CancellationToken);

        Assert.True(await phoneA.Get<IListActions>().ExportAsync(made, TestContext.Current.CancellationToken));

        FakeFilePicker second = new();
        second.ToOpen.Add(new InMemoryFile("vacances.json", first.SaveTarget!.Content));
        using TempApp phoneB = new(second.Register);
        WorkspaceEntry arrived = Assert.Single(await phoneB.Get<IListActions>().ImportAsync(TestContext.Current.CancellationToken));

        Assert.Equal(
            await File.ReadAllBytesAsync(made.Path, TestContext.Current.CancellationToken),
            await File.ReadAllBytesAsync(arrived.Path, TestContext.Current.CancellationToken));
        Assert.Equal(made.FileName, arrived.FileName);
    }

    [Fact]
    public async Task ExportCommands_OnTheCardAndOnTheChecklist_ExportTheirList()
    {
        FakeFilePicker picker = new() { SaveTarget = new InMemoryFile("liste-judith.json") };
        using TempApp app = new(picker.Register);
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        IActiveListService lists = app.Get<IActiveListService>();
        ListsPageViewModel page = app.Get<ListsPageViewModel>();
        ChecklistPageViewModel checklist = app.Get<ChecklistPageViewModel>();
        await lists.ReloadAsync(TestContext.Current.CancellationToken);

        await page.Cards[0].ExportCommand.ExecuteAsync(null);
        Assert.Equal("liste-judith.json", picker.SuggestedName);

        picker.SaveTarget = new InMemoryFile("again.json");
        lists.Open(lists.Entries.Single());
        await checklist.ExportCommand.ExecuteAsync(null);

        Assert.Equal(Utf8NoBom.GetBytes(TestLists.ReadJudithJson().TrimEnd('\n')), picker.SaveTarget.Content);
    }
}
