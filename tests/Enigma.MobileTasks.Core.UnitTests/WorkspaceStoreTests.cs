using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The folder of list files the application works out of: what it writes, what it refuses to
/// touch, and what it hands back.
/// </summary>
public sealed class WorkspaceStoreTests
{
    [Fact]
    public async Task CreateAsync_WritesAFileNamedAfterTheList()
    {
        using TempWorkspace workspace = new();

        WorkspaceEntry entry = await workspace.Store.CreateAsync("Liste Judith", TestContext.Current.CancellationToken);

        Assert.Equal("liste-judith.json", entry.FileName);
        Assert.True(File.Exists(entry.Path));
        Assert.Equal(
            ListJsonSerializer.Serialize(entry.List),
            await File.ReadAllTextAsync(entry.Path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_NumbersASecondListOfTheSameName()
    {
        using TempWorkspace workspace = new();

        WorkspaceEntry first = await workspace.Store.CreateAsync("Vacances", TestContext.Current.CancellationToken);
        WorkspaceEntry second = await workspace.Store.CreateAsync("Vacances", TestContext.Current.CancellationToken);

        Assert.Equal("vacances.json", first.FileName);
        Assert.Equal("vacances-2.json", second.FileName);
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFileBehind()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Essai", TestContext.Current.CancellationToken);

        workspace.Operations.AddCategory(entry.List, "Habits");
        await workspace.Store.SaveAsync(entry, TestContext.Current.CancellationToken);

        Assert.Empty(Directory.EnumerateFiles(workspace.Root, "*.tmp"));
        Assert.Contains("Habits", await File.ReadAllTextAsync(entry.Path, TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_ReadsEveryListOrderedByName()
    {
        using TempWorkspace workspace = new();
        await workspace.Store.CreateAsync("Zanzibar", TestContext.Current.CancellationToken);
        await workspace.Store.CreateAsync("Avignon", TestContext.Current.CancellationToken);
        await workspace.Store.CreateAsync("montréal", TestContext.Current.CancellationToken);

        WorkspaceLoadResult result = await workspace.Store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Avignon", "montréal", "Zanzibar"], result.Entries.Select(entry => entry.List.Name));
        Assert.Empty(result.Problems);
    }

    [Fact]
    public async Task LoadAsync_IgnoresFilesThatAreNotLists()
    {
        using TempWorkspace workspace = new();
        await workspace.Store.CreateAsync("Essai", TestContext.Current.CancellationToken);
        workspace.WriteRaw("notes.md", "# Pas une liste enregistrée");
        workspace.WriteRaw("photo.png", "not json either");

        WorkspaceLoadResult result = await workspace.Store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Single(result.Entries);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public async Task LoadAsync_ReportsABrokenFileAndLeavesItExactlyWhereItIs()
    {
        using TempWorkspace workspace = new();
        await workspace.Store.CreateAsync("Bonne", TestContext.Current.CancellationToken);
        const string Broken = "{ \"categories\": [ { \"name\": ";
        string brokenPath = workspace.WriteRaw("cassee.json", Broken);

        WorkspaceLoadResult result = await workspace.Store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Single(result.Entries);
        WorkspaceProblem problem = Assert.Single(result.Problems);
        Assert.Equal(brokenPath, problem.Path);
        Assert.StartsWith("Unreadable JSON file", problem.Message, StringComparison.Ordinal);
        Assert.Equal(Broken, await File.ReadAllTextAsync(brokenPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadAsync_ReportsAFileFromANewerVersionWithoutTouchingIt()
    {
        using TempWorkspace workspace = new();
        workspace.WriteRaw("future.json", """{ "schemaVersion": 9, "name": "Demain", "categories": [] }""");

        WorkspaceLoadResult result = await workspace.Store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result.Entries);
        Assert.Contains("newer version", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAsync_CopiesAListInFromAStream()
    {
        using TempWorkspace workspace = new();
        await using FileStream source = File.OpenRead(TestLists.JudithJsonPath);

        WorkspaceEntry entry = await workspace.Store.ImportAsync(source, TestContext.Current.CancellationToken);

        Assert.Equal("Liste Judith", entry.List.Name);
        Assert.Equal("liste-judith.json", entry.FileName);
        Assert.Equal(94, ListProgress.Of(entry.List).Total);
        Assert.Equal(workspace.Root, Path.GetDirectoryName(entry.Path));
    }

    /// <summary>
    /// The compatibility claim, end to end: a file Enigma.Tasks (or the web app) wrote goes into the
    /// phone's workspace and comes back out of it byte for byte.
    /// </summary>
    [Fact]
    public async Task ImportAsync_OfADesktopFile_WritesItBackUnchanged()
    {
        using TempWorkspace workspace = new();
        await using FileStream source = File.OpenRead(TestLists.JudithJsonPath);

        WorkspaceEntry entry = await workspace.Store.ImportAsync(source, TestContext.Current.CancellationToken);

        Assert.Equal(
            TestLists.ReadJudithJson().TrimEnd('\n'),
            await File.ReadAllTextAsync(entry.Path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ImportAsync_DropsAByteOrderMark()
    {
        using TempWorkspace workspace = new();
        byte[] content = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("""{ "name": "Été", "categories": [] }""")];
        using MemoryStream source = new(content);

        WorkspaceEntry entry = await workspace.Store.ImportAsync(source, TestContext.Current.CancellationToken);

        Assert.Equal("Été", entry.List.Name);
        Assert.Equal("ete.json", entry.FileName);
    }

    [Fact]
    public async Task ImportAsync_OfAFileFarTooLargeToBeAList_RefusesItAndWritesNothing()
    {
        using TempWorkspace workspace = new();
        using MemoryStream source = new(new byte[WorkspaceStore.MaxImportBytes + 1]);

        ListValidationException exception = await Assert.ThrowsAsync<ListValidationException>(
            () => workspace.Store.ImportAsync(source, TestContext.Current.CancellationToken));

        Assert.Equal("file-too-large", exception.Errors[0].Code);
        Assert.Empty(Directory.EnumerateFiles(workspace.Root));
    }

    [Fact]
    public async Task ImportAsync_TwiceTheSameList_KeepsBothCopies()
    {
        using TempWorkspace workspace = new();
        string json = TestLists.ReadJudithJson();

        WorkspaceEntry first = await workspace.Store.ImportTextAsync(json, TestContext.Current.CancellationToken);
        WorkspaceEntry second = await workspace.Store.ImportTextAsync(json, TestContext.Current.CancellationToken);

        Assert.Equal("liste-judith.json", first.FileName);
        Assert.Equal("liste-judith-2.json", second.FileName);
    }

    [Fact]
    public async Task ImportTextAsync_OfSomethingThatIsNotAList_ThrowsAndWritesNothing()
    {
        using TempWorkspace workspace = new();

        await Assert.ThrowsAsync<ListValidationException>(
            () => workspace.Store.ImportTextAsync("# Liste Judith", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.EnumerateFiles(workspace.Root));
    }

    /// <summary>
    /// What leaves the phone is exactly what Enigma.Tasks writes: UTF-8 without a byte-order mark,
    /// byte for byte the serializer's output.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WritesTheFileFormatToTheStream()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.ImportTextAsync(
            TestLists.ReadJudithJson(),
            TestContext.Current.CancellationToken);
        using MemoryStream destination = new();

        await workspace.Store.ExportAsync(entry.List, destination, TestContext.Current.CancellationToken);

        byte[] expected = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            .GetBytes(TestLists.ReadJudithJson().TrimEnd('\n'));
        Assert.Equal(expected, destination.ToArray());
    }

    [Fact]
    public async Task DuplicateAsync_CopiesTheListIntoItsOwnFileWithFreshIdentifiers()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Voyage", TestContext.Current.CancellationToken);
        TaskCategory category = workspace.Operations.AddCategory(entry.List, "Habits");
        workspace.Operations.AddItem(entry.List, category.Id, "Bodys");
        await workspace.Store.SaveAsync(entry, TestContext.Current.CancellationToken);

        WorkspaceEntry copy = await workspace.Store.DuplicateAsync(entry, TestContext.Current.CancellationToken);

        Assert.Equal("Voyage (copy)", copy.List.Name);
        Assert.Equal("voyage-copy.json", copy.FileName);
        Assert.Equal("Bodys", copy.List.Categories[0].Items[0].Text);
        Assert.NotEqual(entry.List.Categories[0].Id, copy.List.Categories[0].Id);
        Assert.True(File.Exists(entry.Path));
    }

    [Fact]
    public async Task RenameAsync_MovesTheFileWithTheList()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Avant", TestContext.Current.CancellationToken);
        string oldPath = entry.Path;

        Assert.True(await workspace.Store.RenameAsync(entry, "Après le voyage", TestContext.Current.CancellationToken));

        Assert.Equal("apres-le-voyage.json", entry.FileName);
        Assert.False(File.Exists(oldPath));
        Assert.Contains(
            "\"name\": \"Après le voyage\"",
            await File.ReadAllTextAsync(entry.Path, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameAsync_RefusesANameThatChangesNothing()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Essai", TestContext.Current.CancellationToken);

        Assert.False(await workspace.Store.RenameAsync(entry, "  Essai  ", TestContext.Current.CancellationToken));
        Assert.False(await workspace.Store.RenameAsync(entry, "   ", TestContext.Current.CancellationToken));
        Assert.Equal("essai.json", entry.FileName);
    }

    [Fact]
    public async Task RenameAsync_DoesNotCollideWithItsOwnFile()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Essai", TestContext.Current.CancellationToken);

        Assert.True(await workspace.Store.RenameAsync(entry, "ESSAI !", TestContext.Current.CancellationToken));

        Assert.Equal("essai.json", entry.FileName);
        Assert.Single(Directory.EnumerateFiles(workspace.Root, "*.json"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFileOnceAndSaysSo()
    {
        using TempWorkspace workspace = new();
        WorkspaceEntry entry = await workspace.Store.CreateAsync("Essai", TestContext.Current.CancellationToken);

        Assert.True(await workspace.Store.DeleteAsync(entry, TestContext.Current.CancellationToken));
        Assert.False(await workspace.Store.DeleteAsync(entry, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(entry.Path));
    }

    [Fact]
    public async Task LoadAsync_CreatesTheWorkspaceFolderWhenItIsNotThereYet()
    {
        using TempWorkspace workspace = new();
        Directory.Delete(workspace.Root, recursive: true);

        WorkspaceLoadResult result = await workspace.Store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(Directory.Exists(workspace.Root));
        Assert.Empty(result.Entries);
    }
}
