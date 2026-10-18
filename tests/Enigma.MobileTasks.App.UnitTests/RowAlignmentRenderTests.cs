using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
/// A checklist row's box and a theme choice's circle sit on the same line as their label, on the
/// real pages.
/// </summary>
/// <remarks>
/// Fluent pins the glyph to the top of a 32-pixel strip; the app's rows are 48 pixels tall, so it is
/// the app's own styles that bring the two back in line — and a template change in an Avalonia
/// upgrade that undoes them shows up here.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class RowAlignmentRenderTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ChecklistRows_CentreTheBoxOnTheLabel()
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
            CheckBox[] rows = [.. page.GetVisualDescendants().OfType<CheckBox>().Where(box => box.Classes.Contains("row") && box.IsEffectivelyVisible)];

            Assert.NotEmpty(rows);
            Assert.All(rows, row => AssertGlyphCentredOnLabel(row, "NormalRectangle"));

            window.Close();
        });
    }

    [Fact]
    public void ThemeChoices_CentreTheCircleOnTheLabel()
    {
        using TempApp app = new();

        fixture.RunAsync(async () =>
        {
            ShellView shell = App.CreateShell(app.Services);
            Window window = new() { Width = 400, Height = 800, Content = shell };
            window.Show();
            await app.Get<ShellViewModel>().InitializeAsync();
            await app.Get<ListsPageViewModel>().SettingsCommand.ExecuteAsync(null);
            window.UpdateLayout();

            SettingsPage page = Assert.IsType<SettingsPage>(((ShellNavigationPage)shell.NavigationHost.Page!).NavigationStack[^1]);
            RadioButton[] choices = [.. page.GetVisualDescendants().OfType<RadioButton>().Where(radio => radio.Classes.Contains("setting"))];

            Assert.Equal(3, choices.Length);
            Assert.All(choices, choice => AssertGlyphCentredOnLabel(choice, "OuterEllipse"));

            window.Close();
        });
    }

    /// <summary>
    /// Asserts that the template part drawing the glyph and the presenter holding the label share a
    /// vertical centre, and that the row kept its touch-target height.
    /// </summary>
    private static void AssertGlyphCentredOnLabel(ToggleButton toggle, string glyphPart)
    {
        Visual glyph = toggle.GetVisualDescendants().OfType<Control>().Single(part => part.Name == glyphPart);
        ContentPresenter label = toggle.GetVisualDescendants().OfType<ContentPresenter>().Single(part => part.Name == "PART_ContentPresenter");

        double glyphCentre = CentreY(glyph, toggle);
        double labelCentre = CentreY(label, toggle);

        Assert.True(
            Math.Abs(glyphCentre - labelCentre) <= 1,
            $"The glyph's centre is at {glyphCentre} and the label's at {labelCentre}.");
        Assert.True(toggle.Bounds.Height >= 48, $"The row is {toggle.Bounds.Height} pixels tall.");
    }

    private static double CentreY(Visual part, Visual relativeTo)
        => part.TranslatePoint(new Point(0, part.Bounds.Height / 2), relativeTo)!.Value.Y;
}
