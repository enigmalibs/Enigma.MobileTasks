using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.Core.Identity;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The Checklist page: ticking, folding, and every structural edit, on the reference list.
/// </summary>
public sealed class ChecklistPageViewModelTests
{
    [Fact]
    public async Task Open_BuildsOneCardPerCategoryWithItsRowsAndTheListsProgress()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);

        Assert.Equal("Liste Judith", page.Title);
        Assert.Equal(8, page.Categories.Count);
        Assert.Equal("Habits", page.Categories[0].Name);
        Assert.Equal(18, page.Categories[0].Items.Count);
        Assert.Equal("80 / 94", page.ProgressText);
        Assert.False(page.IsEditing);
        Assert.False(page.IsEmpty);
    }

    [Fact]
    public async Task TickingARow_TicksTheItemRecountsAndSaves()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        (CategoryCardViewModel card, ItemRowViewModel row) = FirstUndone(page);
        string before = card.ProgressText;

        row.IsDone = true;
        await app.Get<IActiveListService>().FlushAsync(TestContext.Current.CancellationToken);

        Assert.True(row.Item.Done);
        Assert.Equal("81 / 94", page.ProgressText);
        Assert.NotEqual(before, card.ProgressText);
        TaskList saved = ReadBack(app, "liste-judith.json");
        Assert.True(saved.Categories.SelectMany(category => category.Items).Single(item => item.Id == row.Item.Id).Done);
    }

    [Fact]
    public async Task UntickAll_ResetsTheWholeListAndTheRows()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);

        page.UntickAllCommand.Execute(null);

        Assert.Equal("0 / 94", page.ProgressText);
        Assert.All(page.Categories.SelectMany(card => card.Items), row => Assert.False(row.IsDone));
    }

    [Fact]
    public async Task UntickACategory_ResetsThatCategoryOnly()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        CategoryCardViewModel card = page.Categories.First(category => category.Items.Any(row => row.IsDone));
        int ticked = card.Items.Count(row => row.IsDone);

        card.UntickCommand.Execute(null);

        Assert.All(card.Items, row => Assert.False(row.IsDone));
        Assert.Equal((80 - ticked).ToString(System.Globalization.CultureInfo.InvariantCulture) + " / 94", page.ProgressText);
    }

    [Fact]
    public async Task Folding_IsRememberedForThisList()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        IActiveListService lists = app.Get<IActiveListService>();
        WorkspaceEntry entry = lists.Current!;
        CategoryCardViewModel card = page.Categories[2];

        await card.ToggleCollapsedCommand.ExecuteAsync(null);

        Assert.True(card.IsCollapsed);
        Assert.Equal([card.Category.Id], app.Get<ISettingsService>().Current.CollapsedIn("liste-judith.json"));

        lists.Open(null);
        lists.Open(entry);

        Assert.True(page.Categories[2].IsCollapsed);
        Assert.False(page.Categories[0].IsCollapsed);
    }

    [Fact]
    public async Task AnEmptyList_OpensInEditMode()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = app.Get<ChecklistPageViewModel>();
        WorkspaceEntry entry = (await app.Get<IListActions>().CreateAsync("Vide", TestContext.Current.CancellationToken))!;

        app.Get<IActiveListService>().Open(entry);

        Assert.True(page.IsEmpty);
        Assert.True(page.IsEditing);
    }

    [Fact]
    public async Task AddCategory_AsksForANameAndAddsACardAtTheEnd()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();

        Task running = page.AddCategoryCommand.ExecuteAsync(null);
        dialog.InputText = "Plage";
        dialog.ConfirmCommand.Execute(null);
        await running;

        Assert.Equal(9, page.Categories.Count);
        Assert.Equal("Plage", page.Categories[^1].Name);
    }

    /// <summary>
    /// Adding an item keeps the very same card — and with it the quick-add box's focus — instead of
    /// rebuilding the page.
    /// </summary>
    [Fact]
    public async Task QuickAdd_AddsTheItemAndKeepsTheCard()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        CategoryCardViewModel card = page.Categories[0];

        Assert.False(card.AddItemCommand.CanExecute(null));
        card.QuickAddText = "  Bonnet  ";
        card.AddItemCommand.Execute(null);

        Assert.Same(card, page.Categories[0]);
        Assert.Equal("Bonnet", card.Items[^1].Text);
        Assert.Equal(19, card.Items.Count);
        Assert.Equal(string.Empty, card.QuickAddText);
        Assert.Equal("80 / 95", page.ProgressText);
    }

    [Fact]
    public async Task RenameItemAndCategory_StartFromTheCurrentText()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();
        CategoryCardViewModel card = page.Categories[0];
        ItemRowViewModel row = card.Items[0];

        Task renamingItem = row.RenameCommand.ExecuteAsync(null);
        Assert.Equal("Bodys", dialog.InputText);
        dialog.InputText = "Bodies";
        dialog.ConfirmCommand.Execute(null);
        await renamingItem;

        Task renamingCategory = card.RenameCommand.ExecuteAsync(null);
        Assert.Equal("Habits", dialog.InputText);
        dialog.InputText = "Vêtements";
        dialog.ConfirmCommand.Execute(null);
        await renamingCategory;

        Assert.Same(row, page.Categories[0].Items[0]);
        Assert.Equal("Bodies", row.Text);
        Assert.Equal("Vêtements", page.Categories[0].Name);
    }

    [Fact]
    public async Task DeletingAnItem_OffersUndo_AndUndoPutsItBackWhereItWas()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        NotificationService notifications = app.Get<NotificationService>();
        CategoryCardViewModel card = page.Categories[0];
        TaskItem item = card.Items[3].Item;

        card.Items[3].DeleteCommand.Execute(null);

        Assert.DoesNotContain(card.Items, row => row.Item == item);
        Assert.Equal("Undo", notifications.ActionText);
        Assert.Equal("\"" + item.Text + "\" deleted.", notifications.Message);

        notifications.ActionCommand.Execute(null);

        Assert.Same(item, card.Items[3].Item);
        Assert.Equal("\"" + item.Text + "\" restored.", notifications.Message);
    }

    [Fact]
    public async Task DeletingACategoryThatHoldsItems_AsksFirst()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();
        CategoryCardViewModel card = page.Categories[1];

        Task declined = card.DeleteCommand.ExecuteAsync(null);
        Assert.True(dialog.IsDestructive);
        Assert.Equal("It still holds 13 items. You can undo this right after.", dialog.Message);
        dialog.CancelCommand.Execute(null);
        await declined;
        Assert.Equal(8, page.Categories.Count);

        Task confirmed = card.DeleteCommand.ExecuteAsync(null);
        dialog.ConfirmCommand.Execute(null);
        await confirmed;

        Assert.Equal(7, page.Categories.Count);
        Assert.DoesNotContain(page.Categories, category => category.Category == card.Category);
    }

    [Fact]
    public async Task DeletingAnEmptyCategory_DoesNotAsk()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        app.Get<IListEditor>().AddCategory("Vide");

        await page.Categories[^1].DeleteCommand.ExecuteAsync(null);

        Assert.False(app.Get<DialogService>().IsOpen);
        Assert.Equal(8, page.Categories.Count);
    }

    [Fact]
    public async Task MovingTheLastItemDown_HopsIntoTheNextCategory()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        TaskItem last = page.Categories[0].Items[^1].Item;

        page.Categories[0].Items[^1].MoveDownCommand.Execute(null);

        Assert.Equal(17, page.Categories[0].Items.Count);
        Assert.Same(last, page.Categories[1].Items[0].Item);
    }

    [Fact]
    public async Task MovingACategoryUp_SwapsItWithThePreviousOne()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        CategoryCardViewModel second = page.Categories[1];

        second.MoveUpCommand.Execute(null);

        Assert.Same(second, page.Categories[0]);
        Assert.Equal(["Dormir", "Habits"], page.Categories.Take(2).Select(card => card.Name));
    }

    [Fact]
    public async Task RenameList_RenamesTheListAndItsFile()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);
        DialogService dialog = app.Get<DialogService>();

        Task running = page.RenameListCommand.ExecuteAsync(null);
        Assert.Equal("Liste Judith", dialog.InputText);
        dialog.InputText = "Valise";
        dialog.ConfirmCommand.Execute(null);
        await running;

        Assert.Equal("Valise", page.Title);
        Assert.True(File.Exists(Path.Combine(app.Root, "valise.json")));
    }

    [Fact]
    public async Task EditMode_TogglesWithTheCommand()
    {
        using TempApp app = new(RecordingNavigation.Register);
        ChecklistPageViewModel page = await OpenJudithAsync(app);

        page.EditCommand.Execute(null);
        Assert.True(page.IsEditing);

        page.EditCommand.Execute(null);
        Assert.False(page.IsEditing);
    }

    private static async Task<ChecklistPageViewModel> OpenJudithAsync(TempApp app)
    {
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        ChecklistPageViewModel page = app.Get<ChecklistPageViewModel>();
        IActiveListService lists = app.Get<IActiveListService>();
        await lists.ReloadAsync(TestContext.Current.CancellationToken);
        lists.Open(lists.Entries.Single());

        return page;
    }

    private static (CategoryCardViewModel Card, ItemRowViewModel Row) FirstUndone(ChecklistPageViewModel page)
    {
        CategoryCardViewModel card = page.Categories.First(category => category.Items.Any(row => !row.IsDone));

        return (card, card.Items.First(row => !row.IsDone));
    }

    private static TaskList ReadBack(TempApp app, string fileName)
        => new ListJsonSerializer(new ListIdGenerator()).Deserialize(File.ReadAllText(Path.Combine(app.Root, fileName)));
}
