using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.App.Views.Pages;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// A category's quick-add "+" on the real Checklist page, tapped the way a finger taps it: the item
/// goes in and the cursor is in the box again, ready for the next one.
/// </summary>
/// <remarks>
/// Driven through the headless pointer, not the command, so Avalonia's own focus-on-press runs — that
/// is what used to pull the focus, and on a phone the keyboard, out of the box.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class QuickAddFocusRenderTests(HeadlessAvaloniaFixture fixture)
{
    private const string Courses = """
        {
          "schemaVersion": 1,
          "name": "Courses",
          "categories": [
            { "name": "Fruits", "items": [ { "text": "Pommes" } ] },
            { "name": "Boulangerie", "items": [ { "text": "Pain" } ] }
          ]
        }
        """;

    [Fact]
    public void TappingPlus_AddsTheItemWithoutTheFocusEverLeavingTheBox()
    {
        using TempApp app = new();
        app.WriteRaw("courses.json", Courses);

        fixture.RunAsync(async () =>
        {
            Window window = await OpenInEditModeAsync(app);
            CategoryCardViewModel card = app.Get<ChecklistPageViewModel>().Categories[0];
            (TextBox box, Button plus) = QuickAdd(window, 0);

            box.Focus();
            window.KeyTextInput("Bananes");
            Point centre = CentreOf(plus, window);

            window.MouseDown(centre, MouseButton.Left);
            Assert.Same(box, window.FocusManager!.GetFocusedElement());

            window.MouseUp(centre, MouseButton.Left);
            window.UpdateLayout();

            Assert.Equal("Bananes", card.Items[^1].Text);
            Assert.Equal(string.Empty, box.Text);
            Assert.Same(box, window.FocusManager!.GetFocusedElement());

            window.Close();
        });
    }

    [Fact]
    public void TappingPlus_PutsTheFocusBackInTheBoxItAddedFrom()
    {
        using TempApp app = new();
        app.WriteRaw("courses.json", Courses);

        fixture.RunAsync(async () =>
        {
            Window window = await OpenInEditModeAsync(app);
            CategoryCardViewModel card = app.Get<ChecklistPageViewModel>().Categories[1];
            (TextBox elsewhere, _) = QuickAdd(window, 0);
            (TextBox box, Button plus) = QuickAdd(window, 1);

            card.QuickAddText = "Croissants";
            elsewhere.Focus();
            Point centre = CentreOf(plus, window);

            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            window.UpdateLayout();

            Assert.Equal("Croissants", card.Items[^1].Text);
            Assert.Same(box, window.FocusManager!.GetFocusedElement());

            window.Close();
        });
    }

    /// <summary>
    /// Shows the shell, opens the only list and turns edit mode on, as the pencil does.
    /// </summary>
    private static async Task<Window> OpenInEditModeAsync(TempApp app)
    {
        ShellView shell = App.CreateShell(app.Services);
        Window window = new() { Width = 400, Height = 800, Content = shell };
        window.Show();
        await app.Get<ShellViewModel>().InitializeAsync();
        await app.Get<ListsPageViewModel>().Cards[0].OpenCommand.ExecuteAsync(null);
        app.Get<ChecklistPageViewModel>().IsEditing = true;
        window.UpdateLayout();

        Assert.IsType<ChecklistPage>(((ShellNavigationPage)shell.NavigationHost.Page!).NavigationStack[^1]);

        return window;
    }

    /// <summary>
    /// Finds the quick-add box of the nth card, and the "+" on the same row.
    /// </summary>
    private static (TextBox Box, Button Plus) QuickAdd(Window window, int card)
    {
        TextBox box = window.GetVisualDescendants().OfType<TextBox>().Where(text => text.Classes.Contains("quickadd") && text.IsEffectivelyVisible).ElementAt(card);
        Button plus = ((Panel)box.Parent!).Children.OfType<Button>().Single();

        return (box, plus);
    }

    private static Point CentreOf(Control control, Visual relativeTo)
        => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), relativeTo)!.Value;
}
