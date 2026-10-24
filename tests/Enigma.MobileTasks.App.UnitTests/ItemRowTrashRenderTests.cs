using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.App.Views.Pages;
using Enigma.MobileTasks.Core.Model;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The trash button at the right end of every item row, on the real Checklist page: there in the
/// everyday view and in edit mode, in the same place, and one tap from removing the item.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ItemRowTrashRenderTests(HeadlessAvaloniaFixture fixture)
{
    private const string Courses = """
        {
          "schemaVersion": 1,
          "name": "Courses",
          "categories": [
            { "name": "Fruits", "items": [ { "text": "Pommes" }, { "text": "Poires" } ] }
          ]
        }
        """;

    [Fact]
    public void EveryRow_EndsWithItsTrash_InTheEverydayViewAndInEditMode()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());

        fixture.RunAsync(async () =>
        {
            Window window = await OpenAsync(app);
            ChecklistPage page = Page(window);

            ItemRow[] rows = VisibleRows(page);
            Assert.NotEmpty(rows);
            Assert.All(rows, row =>
            {
                Assert.True(row.Trash.IsEffectivelyVisible);
                Assert.False(row.Menu.IsEffectivelyVisible);
                AssertTrashIsTheRowsLastTarget(row);
            });

            app.Get<ChecklistPageViewModel>().IsEditing = true;
            window.UpdateLayout();

            Assert.All(VisibleRows(page), row =>
            {
                Assert.True(row.Trash.IsEffectivelyVisible);
                Assert.True(row.Menu.IsEffectivelyVisible);
                Assert.True(
                    RightOf(row.Menu, row.Grid) <= LeftOf(row.Trash, row.Grid),
                    "The menu should sit to the left of the trash.");
                AssertTrashIsTheRowsLastTarget(row);
            });

            window.Close();
        });
    }

    [Fact]
    public void TheItemMenu_NoLongerOffersDelete()
    {
        using TempApp app = new();
        app.WriteRaw("courses.json", Courses);

        fixture.RunAsync(async () =>
        {
            Window window = await OpenAsync(app);
            ItemRow row = VisibleRows(Page(window))[0];

            MenuFlyout menu = Assert.IsType<MenuFlyout>(row.Menu.Flyout);

            Assert.Equal(["Rename", "Move up", "Move down"], menu.Items.Select(item => Assert.IsType<MenuItem>(item).Header));

            window.Close();
        });
    }

    [Fact]
    public void TappingTheTrash_RemovesTheItemWithoutTickingIt_AndUndoPutsItBack()
    {
        using TempApp app = new();
        app.WriteRaw("courses.json", Courses);

        fixture.RunAsync(async () =>
        {
            Window window = await OpenAsync(app);
            ChecklistPageViewModel viewModel = app.Get<ChecklistPageViewModel>();
            NotificationService notifications = app.Get<NotificationService>();
            CategoryCardViewModel card = viewModel.Categories[0];
            TaskItem pommes = card.Items[0].Item;
            ItemRow row = VisibleRows(Page(window))[0];
            Point centre = row.Trash.TranslatePoint(new Point(row.Trash.Bounds.Width / 2, row.Trash.Bounds.Height / 2), window)!.Value;

            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            window.UpdateLayout();

            Assert.DoesNotContain(card.Items, item => item.Item == pommes);
            Assert.Equal("0 / 1", viewModel.ProgressText);
            Assert.Equal("\"Pommes\" deleted.", notifications.Message);
            Assert.Equal("Undo", notifications.ActionText);

            notifications.ActionCommand.Execute(null);

            Assert.Same(pommes, card.Items[0].Item);
            Assert.False(pommes.Done);
            Assert.Equal("0 / 2", viewModel.ProgressText);

            window.Close();
        });
    }

    /// <summary>
    /// The trash is the row's rightmost control, a full touch target, named after the item.
    /// </summary>
    private static void AssertTrashIsTheRowsLastTarget(ItemRow row)
    {
        Assert.Equal(row.Grid.Bounds.Width, RightOf(row.Trash, row.Grid), 1);
        Assert.True(row.Trash.Bounds.Width >= 48 && row.Trash.Bounds.Height >= 48, $"The trash is {row.Trash.Bounds.Size}.");
        Assert.Equal("Delete " + row.ViewModel.Text, AutomationProperties.GetName(row.Trash));
    }

    /// <summary>
    /// Shows the shell and opens the only list, in its everyday view.
    /// </summary>
    private static async Task<Window> OpenAsync(TempApp app)
    {
        ShellView shell = App.CreateShell(app.Services);
        Window window = new() { Width = 400, Height = 800, Content = shell };
        window.Show();
        await app.Get<ShellViewModel>().InitializeAsync();
        await app.Get<ListsPageViewModel>().Cards[0].OpenCommand.ExecuteAsync(null);
        window.UpdateLayout();

        return window;
    }

    private static ChecklistPage Page(Window window)
        => Assert.IsType<ChecklistPage>(((ShellNavigationPage)((ShellView)window.Content!).NavigationHost.Page!).NavigationStack[^1]);

    /// <summary>
    /// Every item row on screen — folded cards' rows are not — with its two buttons.
    /// </summary>
    private static ItemRow[] VisibleRows(ChecklistPage page)
        => [.. page.GetVisualDescendants()
            .OfType<CheckBox>()
            .Where(box => box.Classes.Contains("row") && box.IsEffectivelyVisible)
            .Select(box =>
            {
                Grid grid = (Grid)box.Parent!;
                ItemRowViewModel viewModel = (ItemRowViewModel)grid.DataContext!;
                Button[] buttons = [.. grid.Children.OfType<Button>()];

                return new ItemRow(
                    grid,
                    viewModel,
                    buttons.Single(button => button.Flyout is not null),
                    buttons.Single(button => button.Command == viewModel.DeleteCommand));
            })];

    private static double LeftOf(Control control, Visual relativeTo)
        => control.TranslatePoint(default, relativeTo)!.Value.X;

    private static double RightOf(Control control, Visual relativeTo)
        => LeftOf(control, relativeTo) + control.Bounds.Width;

    private sealed record ItemRow(Grid Grid, ItemRowViewModel ViewModel, Button Menu, Button Trash);
}
