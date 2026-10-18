using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The one truth about which lists exist and which one is open.
/// </summary>
public sealed class ActiveListServiceTests
{
    [Fact]
    public async Task ReloadAsync_ReadsTheFolderAndReportsWhatItCouldNotRead()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        app.WriteRaw("cassee.json", "{ \"categories\": ");
        IActiveListService lists = app.Get<IActiveListService>();
        int changes = 0;
        lists.EntriesChanged += (_, _) => changes++;

        IReadOnlyList<WorkspaceProblem> problems = await lists.ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Liste Judith", Assert.Single(lists.Entries).List.Name);
        Assert.Equal("cassee.json", Path.GetFileName(Assert.Single(problems).Path));
        Assert.Equal(1, changes);
        Assert.Null(lists.Current);
    }

    [Fact]
    public async Task TrackAndForget_KeepTheListsSortedAndCloseAForgottenOpenList()
    {
        using TempApp app = new();
        IActiveListService lists = app.Get<IActiveListService>();
        IWorkspaceStore store = app.Get<IWorkspaceStore>();

        WorkspaceEntry zebra = await store.CreateAsync("Zèbre", TestContext.Current.CancellationToken);
        WorkspaceEntry apple = await store.CreateAsync("Abricot", TestContext.Current.CancellationToken);
        lists.Track(zebra);
        lists.Track(apple);
        lists.Open(zebra);

        Assert.Equal(["Abricot", "Zèbre"], lists.Entries.Select(entry => entry.List.Name));

        lists.Forget(zebra);

        Assert.Equal(["Abricot"], lists.Entries.Select(entry => entry.List.Name));
        Assert.Null(lists.Current);
    }

    [Fact]
    public async Task Touch_SchedulesASaveAndAnnouncesTheList()
    {
        using TempApp app = new();
        IActiveListService lists = app.Get<IActiveListService>();
        WorkspaceEntry entry = await app.Get<IWorkspaceStore>().CreateAsync("Essai", TestContext.Current.CancellationToken);
        lists.Track(entry);
        lists.Open(entry);
        WorkspaceEntry? touched = null;
        lists.Touched += (_, e) => touched = e;

        entry.List.Name = "Renommée dans le fichier";
        lists.Touch();
        await lists.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Same(entry, touched);
        Assert.Contains(
            "Renommée dans le fichier",
            await File.ReadAllTextAsync(entry.Path, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Open_RaisesCurrentChangedOnlyForADifferentList()
    {
        using TempApp app = new();
        IActiveListService lists = app.Get<IActiveListService>();
        int changes = 0;
        lists.CurrentChanged += (_, _) => changes++;

        lists.Open(null);

        Assert.Equal(0, changes);
    }
}
