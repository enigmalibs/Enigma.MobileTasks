using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.Core.Settings;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The Settings page: the theme choice, saved and applied live, and what the page says about the lists
/// and the version.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SettingsPageViewModelTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ByDefault_FollowsThePhone()
    {
        using TempApp app = new(RecordingNavigation.Register);

        SettingsPageViewModel page = app.Get<SettingsPageViewModel>();

        Assert.True(page.IsSystemTheme);
        Assert.False(page.IsLightTheme);
        Assert.False(page.IsDarkTheme);
    }

    [Fact]
    public async Task ChoosingDark_SavesItAndMovesTheRadioButtons()
    {
        using TempApp app = new(RecordingNavigation.Register);
        SettingsPageViewModel page = app.Get<SettingsPageViewModel>();

        page.IsDarkTheme = true;
        await page.LastChange;

        Assert.Equal(ThemePreference.Dark, app.Get<ISettingsService>().Current.Theme);
        Assert.True(page.IsDarkTheme);
        Assert.False(page.IsSystemTheme);
        Assert.Contains(
            "\"theme\": \"Dark\"",
            await File.ReadAllTextAsync(app.Get<ISettingsService>().SettingsPath, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A radio button reports false when another one in its group is checked; that must not undo the
    /// other button's choice.
    /// </summary>
    [Fact]
    public async Task UncheckingAButton_ChangesNothing()
    {
        using TempApp app = new(RecordingNavigation.Register);
        SettingsPageViewModel page = app.Get<SettingsPageViewModel>();

        page.IsSystemTheme = false;
        await page.LastChange;

        Assert.Equal(ThemePreference.System, app.Get<ISettingsService>().Current.Theme);
        Assert.False(File.Exists(app.Get<ISettingsService>().SettingsPath));
    }

    /// <summary>
    /// The live path: the page writes the settings, and the application — observing them — repaints in
    /// the chosen variant without a restart.
    /// </summary>
    [Fact]
    public void ChoosingATheme_RepaintsTheApplication()
    {
        using TempApp app = new(RecordingNavigation.Register);
        SettingsPageViewModel page = app.Get<SettingsPageViewModel>();

        fixture.RunAsync(async () =>
        {
            App.ObserveTheme(app.Get<ISettingsService>());

            // The settings raise Changed before the update completes, and the application posts the
            // repaint to the UI thread from there: running the queued jobs applies it.
            page.IsDarkTheme = true;
            await page.LastChange;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Dark, Application.Current!.ActualThemeVariant);

            page.IsLightTheme = true;
            await page.LastChange;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Light, Application.Current!.ActualThemeVariant);

            page.IsSystemTheme = true;
            await page.LastChange;
            Dispatcher.UIThread.RunJobs();
            Assert.True(
                Application.Current!.ActualThemeVariant == ThemeVariant.Light ||
                Application.Current!.ActualThemeVariant == ThemeVariant.Dark);
            Assert.True(
                Application.Current!.RequestedThemeVariant == ThemeVariant.Default ||
                Application.Current!.RequestedThemeVariant == App.SystemVariant(Application.Current!));
        });
    }

    [Fact]
    public async Task TheStorageSummary_CountsTheLists()
    {
        using TempApp app = new(RecordingNavigation.Register);
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());
        SettingsPageViewModel page = app.Get<SettingsPageViewModel>();

        await app.Get<IActiveListService>().ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("1 lists, kept in this app's private storage on the phone.", page.StorageSummary);
    }

    /// <summary>
    /// The About line names the app as the launcher does, with the solution's version — read from
    /// this test assembly, which <c>Directory.Build.props</c> versions like every other, so a release
    /// never has to edit the test.
    /// </summary>
    [Fact]
    public void TheVersionLine_IsTheAppsNameAndTheSolutionsVersion()
    {
        using TempApp app = new(RecordingNavigation.Register);
        string version = typeof(SettingsPageViewModelTests).Assembly.GetName().Version!.ToString(3);

        Assert.Equal("Enigma tasks " + version, app.Get<SettingsPageViewModel>().VersionText);
    }

    [Fact]
    public async Task TheGear_OpensTheSettingsPage()
    {
        using TempApp app = new(RecordingNavigation.Register);

        await app.Get<ListsPageViewModel>().SettingsCommand.ExecuteAsync(null);

        Assert.Equal([ShellPage.Settings], ((RecordingNavigation)app.Get<INavigationService>()).Pushed);
    }
}
