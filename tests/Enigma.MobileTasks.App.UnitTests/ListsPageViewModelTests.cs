using System.Linq;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The Lists page: one card per list, its progress, and the empty state.
/// </summary>
public sealed class ListsPageViewModelTests
{
    [Fact]
    public void WithNoList_ShowsTheEmptyState()
    {
        using TempApp app = new();

        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        Assert.True(page.IsEmpty);
        Assert.Empty(page.Cards);
    }

    [Fact]
    public async Task AfterAReload_ShowsOneCardPerListWithItsProgress()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        await app.Get<IActiveListService>().ReloadAsync(TestContext.Current.CancellationToken);

        ListCardViewModel card = Assert.Single(page.Cards);
        Assert.False(page.IsEmpty);
        Assert.Equal("Liste Judith", card.Name);
        Assert.Equal("80 / 94", card.ProgressText);
        Assert.Equal(80d / 94d * 100d, card.Percent, 6);
        Assert.False(card.IsComplete);
    }

    [Fact]
    public async Task ATouchedList_RefreshesItsCard()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        IActiveListService lists = app.Get<IActiveListService>();
        ListsPageViewModel page = app.Get<ListsPageViewModel>();
        await lists.ReloadAsync(TestContext.Current.CancellationToken);
        WorkspaceEntry entry = lists.Entries.Single();

        ListOperations.SetAllDone(entry.List, true);
        lists.Touch(entry);
        await lists.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal("94 / 94", page.Cards[0].ProgressText);
        Assert.True(page.Cards[0].IsComplete);
    }
}
