using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.App.Views.Pages;
using Enigma.MobileTasks.Core.Settings;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The shell as the activity hosts it, rendered off-screen.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ShellRenderTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void CreateShell_HostsANavigationStackRootedAtTheListsPage()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());

        fixture.RunAsync(async () =>
        {
            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();

            await app.Get<ShellViewModel>().InitializeAsync();

            ShellNavigationPage root = Assert.IsType<ShellNavigationPage>(shell.NavigationHost.Page);
            Assert.IsType<ListsPage>(root.Content);
            Assert.Equal(1, root.StackDepth);
            Assert.Same(app.Get<ShellViewModel>(), shell.DataContext);

            window.UpdateLayout();
            Assert.Contains(
                shell.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "Liste Judith");

            window.Close();
        });
    }

    /// <summary>
    /// The page's empty margin is painted with the Enigma background colour of whichever variant is
    /// active — the proof that the vendored palette is merged and reaches the pages.
    /// </summary>
    [Fact]
    public void Shell_PaintsTheEnigmaBackground()
    {
        using TempApp app = new();

        fixture.Run(() =>
        {
            Window window = new() { Width = 400, Height = 800, Content = App.CreateShell(app.Services) };
            window.Show();

            using WriteableBitmap? frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);

            Color pixel = ReadPixel(frame, 4, 700);
            Assert.True(
                pixel == Color.Parse("#F7F8FA") || pixel == Color.Parse("#1E1F22"),
                "Expected the Enigma background, found " + pixel);

            window.Close();
        });
    }

    /// <summary>
    /// The real path a tap takes: the card's command opens the list and pushes its checklist on the
    /// stack, and the page's "+" sits in the navigation bar, bound to the page's command.
    /// </summary>
    [Fact]
    public void OpeningACard_PushesTheChecklistPage()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());

        fixture.RunAsync(async () =>
        {
            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();
            await app.Get<ShellViewModel>().InitializeAsync();
            window.UpdateLayout();

            ShellNavigationPage root = Assert.IsType<ShellNavigationPage>(shell.NavigationHost.Page);
            Button add = shell.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NewListButton");
            Assert.Same(app.Get<ListsPageViewModel>().NewListCommand, add.Command);
            Assert.Equal("New list", AutomationProperties.GetName(add));

            await app.Get<ListsPageViewModel>().Cards[0].OpenCommand.ExecuteAsync(null);
            window.UpdateLayout();

            Assert.Equal(2, root.StackDepth);
            ChecklistPage checklist = Assert.IsType<ChecklistPage>(root.NavigationStack[^1]);
            Assert.Equal("Liste Judith", checklist.Header);

            window.Close();
        });
    }

    /// <summary>
    /// The checklist as a phone shows it: rows to tick, and in edit mode the menus and the quick-add
    /// boxes, with the navigation bar's toggle bound to the page's edit mode.
    /// </summary>
    [Fact]
    public void ChecklistPage_ShowsItsEditControlsOnlyInEditMode()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());

        fixture.RunAsync(async () =>
        {
            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();
            await app.Get<ShellViewModel>().InitializeAsync();
            await app.Get<ListsPageViewModel>().Cards[0].OpenCommand.ExecuteAsync(null);
            window.UpdateLayout();

            ChecklistPage page = Assert.IsType<ChecklistPage>(((ShellNavigationPage)shell.NavigationHost.Page!).NavigationStack[^1]);
            ToggleButton toggle = shell.GetVisualDescendants().OfType<ToggleButton>().Single(button => button.Name == "EditToggle");

            Assert.Contains(page.GetVisualDescendants().OfType<CheckBox>(), box => box.IsChecked == true);
            Assert.DoesNotContain(page.GetVisualDescendants().OfType<TextBox>(), box => box.IsEffectivelyVisible);

            toggle.IsChecked = true;
            window.UpdateLayout();

            Assert.True(app.Get<ChecklistPageViewModel>().IsEditing);
            Assert.Contains(page.GetVisualDescendants().OfType<TextBox>(), box => box.IsEffectivelyVisible);

            window.Close();
        });
    }

    /// <summary>
    /// Fluent paints the navigation bar with a fixed grey; the app re-colours it with the Enigma
    /// surface, and its title with the Enigma foreground, in whichever variant is active. The variant
    /// is set on the application, as the app sets it: the Enigma brushes are shared instances whose
    /// colour follows the application's variant.
    /// </summary>
    [Theory]
    [InlineData(ThemePreference.Light, "#EBEDF0", "#1E1F22")]
    [InlineData(ThemePreference.Dark, "#2B2D30", "#BCBEC4")]
    public void NavigationBar_UsesTheEnigmaColours(ThemePreference variant, string surface, string foreground)
    {
        using TempApp app = new();

        fixture.Run(() =>
        {
            App.ApplyTheme(variant);

            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();
            window.UpdateLayout();

            Border bar = shell.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_NavigationBar");
            TextBlock title = bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Lists");

            Assert.Equal(Color.Parse(surface), Assert.IsAssignableFrom<ISolidColorBrush>(bar.Background).Color);
            Assert.Equal(Color.Parse(foreground), Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color);

            window.Close();
            App.ApplyTheme(ThemePreference.System);
        });
    }

    private static Color ReadPixel(WriteableBitmap bitmap, int x, int y)
    {
        using ILockedFramebuffer buffer = bitmap.Lock();
        byte[] row = new byte[buffer.RowBytes];
        Marshal.Copy(buffer.Address + (y * buffer.RowBytes), row, 0, row.Length);

        int offset = x * 4;

        // The frame's byte order is whatever the platform's Skia surface uses; read it, never assume it.
        return buffer.Format == PixelFormat.Rgba8888
            ? Color.FromArgb(row[offset + 3], row[offset], row[offset + 1], row[offset + 2])
            : Color.FromArgb(row[offset + 3], row[offset + 2], row[offset + 1], row[offset]);
    }

    [Fact]
    public void OpenDialog_ShowsTheScrimOverThePages()
    {
        using TempApp app = new();

        fixture.Run(() =>
        {
            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();

            Border scrim = shell.GetLogicalDescendants().OfType<Border>().Single(border => border.Name == "DialogScrim");
            Assert.False(scrim.IsVisible);

            _ = app.Get<IDialogService>().PromptAsync("New list", null, "Create");
            window.UpdateLayout();

            Assert.True(scrim.IsVisible);

            app.Get<IDialogService>().TryCancel();
            window.UpdateLayout();
            Assert.False(scrim.IsVisible);

            window.Close();
        });
    }
}
