using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The list actions' edges: pending saves, the open list, and failures reported rather than thrown.
/// </summary>
public sealed class ListActionsTests
{
    /// <summary>
    /// A tick a moment before the deletion leaves a save waiting; it must not bring the file back.
    /// </summary>
    [Fact]
    public async Task Delete_WithASaveStillPending_DoesNotResurrectTheFile()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IActiveListService lists = app.Get<IActiveListService>();
        WorkspaceEntry entry = (await app.Get<IListActions>().CreateAsync("Essai", TestContext.Current.CancellationToken))!;
        lists.Open(entry);
        lists.Touch();

        Assert.True(await app.Get<IListActions>().DeleteAsync(entry, TestContext.Current.CancellationToken));
        await Task.Delay(600, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(entry.Path));
        Assert.Null(lists.Current);
        Assert.DoesNotContain(entry, lists.Entries);
    }

    [Fact]
    public async Task Rename_WithASaveStillPending_KeepsTheLatestStateInTheRenamedFile()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IActiveListService lists = app.Get<IActiveListService>();
        IListActions actions = app.Get<IListActions>();
        WorkspaceEntry entry = (await actions.CreateAsync("Avant", TestContext.Current.CancellationToken))!;
        lists.Open(entry);
        entry.List.Categories.Add(new Core.Model.TaskCategory("c-x", "Ajoutée juste avant"));
        lists.Touch();

        Assert.True(await actions.RenameAsync(entry, "Après", TestContext.Current.CancellationToken));
        await Task.Delay(600, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(Path.Combine(app.Root, "avant.json")));
        Assert.Contains(
            "Ajoutée juste avant",
            await File.ReadAllTextAsync(Path.Combine(app.Root, "apres.json"), TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenTheFolderCannotBeWritten_ReportsInsteadOfThrowing()
    {
        using TempApp app = new(RecordingNavigation.Register);
        Directory.Delete(app.Root);
        await File.WriteAllTextAsync(app.Root, "a file where the folder should be", TestContext.Current.CancellationToken);

        WorkspaceEntry? entry = await app.Get<IListActions>().CreateAsync("Essai", TestContext.Current.CancellationToken);

        Assert.Null(entry);
        Assert.StartsWith("The list could not be created", app.Get<NotificationService>().Message, StringComparison.Ordinal);
        Assert.Empty(app.Get<IActiveListService>().Entries);
    }
}
