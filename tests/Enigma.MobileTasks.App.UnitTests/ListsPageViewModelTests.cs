using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The Lists page: one card per list, its progress, the empty state, and every command a card or the
/// page carries — answered through the dialog the way the overlay answers it.
/// </summary>
public sealed class ListsPageViewModelTests
{
    [Fact]
    public void WithNoList_ShowsTheEmptyState()
    {
        using TempApp app = new(RecordingNavigation.Register);

        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        Assert.True(page.IsEmpty);
        Assert.Empty(page.Cards);
    }

    [Fact]
    public async Task AfterAReload_ShowsOneCardPerListWithItsProgress()
    {
        using TempApp app = new(RecordingNavigation.Register);
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        await app.Get<IActiveListService>().ReloadAsync(TestContext.Current.CancellationToken);

        ListCardViewModel card = Assert.Single(page.Cards);
        Assert.False(page.IsEmpty);
        Assert.Equal("Liste Judith", card.Name);
        Assert.Equal("80 / 94", card.ProgressText);
        Assert.Equal(80d / 94d * 100d, card.Percent, 6);
        Assert.False(card.IsComplete);
        Assert.Equal("Actions for Liste Judith", card.ActionsLabel);
    }

    [Fact]
    public async Task ATouchedList_RefreshesItsCard()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);
        IActiveListService lists = app.Get<IActiveListService>();
        WorkspaceEntry entry = lists.Entries.Single();

        ListOperations.SetAllDone(entry.List, true);
        lists.Touch(entry);
        await lists.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal("94 / 94", page.Cards[0].ProgressText);
        Assert.True(page.Cards[0].IsComplete);
    }

    [Fact]
    public async Task NewList_AsksForANameCreatesTheFileAndOpensIt()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = app.Get<ListsPageViewModel>();
        DialogService dialog = app.Get<DialogService>();

        Task running = page.NewListCommand.ExecuteAsync(null);
        Assert.True(dialog.IsPrompt);
        dialog.InputText = "Vacances d'été";
        dialog.ConfirmCommand.Execute(null);
        await running;

        Assert.Equal("Vacances d'été", Assert.Single(page.Cards).Name);
        Assert.True(File.Exists(Path.Combine(app.Root, "vacances-d-ete.json")));
        Assert.Equal("Vacances d'été", app.Get<IActiveListService>().Current?.List.Name);
        Assert.Equal([ShellPage.Checklist], ((RecordingNavigation)app.Get<INavigationService>()).Pushed);
    }

    [Fact]
    public async Task NewList_Cancelled_CreatesNothing()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = app.Get<ListsPageViewModel>();

        Task running = page.NewListCommand.ExecuteAsync(null);
        app.Get<DialogService>().CancelCommand.Execute(null);
        await running;

        Assert.Empty(page.Cards);
        Assert.Empty(Directory.EnumerateFiles(app.Root));
        Assert.Empty(((RecordingNavigation)app.Get<INavigationService>()).Pushed);
    }

    [Fact]
    public async Task Open_OpensTheListAndShowsItsChecklist()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);

        await page.Cards[0].OpenCommand.ExecuteAsync(null);

        Assert.Same(page.Cards[0].Entry, app.Get<IActiveListService>().Current);
        Assert.Equal([ShellPage.Checklist], ((RecordingNavigation)app.Get<INavigationService>()).Pushed);
    }

    [Fact]
    public async Task Rename_StartsFromTheCurrentNameAndMovesTheFile()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();

        Task running = page.Cards[0].RenameCommand.ExecuteAsync(null);
        Assert.Equal("Liste Judith", dialog.InputText);
        dialog.InputText = "Valise de Judith";
        dialog.ConfirmCommand.Execute(null);
        await running;

        Assert.Equal("Valise de Judith", Assert.Single(page.Cards).Name);
        Assert.True(File.Exists(Path.Combine(app.Root, "valise-de-judith.json")));
        Assert.False(File.Exists(Path.Combine(app.Root, "liste-judith.json")));
    }

    [Fact]
    public async Task Rename_Cancelled_ChangesNothing()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);

        Task running = page.Cards[0].RenameCommand.ExecuteAsync(null);
        app.Get<DialogService>().TryCancel();
        await running;

        Assert.Equal("Liste Judith", page.Cards[0].Name);
        Assert.True(File.Exists(Path.Combine(app.Root, "liste-judith.json")));
    }

    [Fact]
    public async Task Duplicate_AddsACopyAndSaysSo()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);

        await page.Cards[0].DuplicateCommand.ExecuteAsync(null);

        Assert.Equal(["Liste Judith", "Liste Judith (copy)"], page.Cards.Select(card => card.Name));
        Assert.Equal("\"Liste Judith (copy)\" created.", app.Get<NotificationService>().Message);
        Assert.True(File.Exists(Path.Combine(app.Root, "liste-judith-copy.json")));
    }

    [Fact]
    public async Task Delete_AsksFirstAndRemovesTheFileOnlyWhenConfirmed()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ListsPageViewModel page = await LoadJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();

        Task declined = page.Cards[0].DeleteCommand.ExecuteAsync(null);
        Assert.True(dialog.IsDestructive);
        Assert.Equal("Delete \"Liste Judith\"?", dialog.Title);
        dialog.CancelCommand.Execute(null);
        await declined;
        Assert.Single(page.Cards);

        Task confirmed = page.Cards[0].DeleteCommand.ExecuteAsync(null);
        dialog.ConfirmCommand.Execute(null);
        await confirmed;

        Assert.Empty(page.Cards);
        Assert.True(page.IsEmpty);
        Assert.False(File.Exists(Path.Combine(app.Root, "liste-judith.json")));
        Assert.Equal("\"Liste Judith\" deleted.", app.Get<NotificationService>().Message);
    }

    private static async Task<ListsPageViewModel> LoadJudithAsync(TempApp app)
    {
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        ListsPageViewModel page = app.Get<ListsPageViewModel>();
        await app.Get<IActiveListService>().ReloadAsync(TestContext.Current.CancellationToken);

        return page;
    }
}
