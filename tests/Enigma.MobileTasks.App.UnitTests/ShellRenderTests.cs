using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
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
