using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// What the application remembers between runs, and what it does when it cannot read it.
/// </summary>
public sealed class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WithNoFile_LeavesTheDefaultsInPlace()
    {
        using TempWorkspace workspace = new();

        string? warning = await workspace.Settings.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Null(warning);
        Assert.Equal(ThemePreference.System, workspace.Settings.Current.Theme);
        Assert.Empty(workspace.Settings.Current.CollapsedCategories);
    }

    [Fact]
    public async Task UpdateAsync_WritesTheFileAndAnnouncesTheChange()
    {
        using TempWorkspace workspace = new();
        AppSettings? announced = null;
        workspace.Settings.Changed += (_, e) => announced = e.Settings;

        await workspace.Settings.UpdateAsync(
            settings => settings with { Theme = ThemePreference.Light },
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(workspace.Settings.SettingsPath));
        Assert.Equal(ThemePreference.Light, announced?.Theme);
        Assert.Contains(
            "\"theme\": \"Light\"",
            await File.ReadAllTextAsync(workspace.Settings.SettingsPath, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAsync_ThenLoadAsync_RoundTripsEverything()
    {
        using TempWorkspace workspace = new();

        await workspace.Settings.UpdateAsync(
            settings => (settings with { Theme = ThemePreference.Dark })
                .WithCollapsed("liste-judith.json", ["c-1", "c-2"]),
            TestContext.Current.CancellationToken);

        SettingsService reopened = new(workspace.Location);
        Assert.Null(await reopened.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ThemePreference.Dark, reopened.Current.Theme);
        Assert.Equal(["c-1", "c-2"], reopened.Current.CollapsedIn("liste-judith.json"));
        Assert.Empty(reopened.Current.CollapsedIn("autre.json"));
    }

    [Fact]
    public async Task UpdateAsync_ThatChangesNothing_DoesNotWriteAndDoesNotAnnounce()
    {
        using TempWorkspace workspace = new();
        bool announced = false;
        workspace.Settings.Changed += (_, _) => announced = true;

        await workspace.Settings.UpdateAsync(settings => settings, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(workspace.Settings.SettingsPath));
        Assert.False(announced);
    }

    [Fact]
    public async Task LoadAsync_WithAnUnreadableFile_FallsBackAndSaysSoWithoutDeletingIt()
    {
        using TempWorkspace workspace = new();
        const string Broken = "{ \"theme\": ";
        await File.WriteAllTextAsync(workspace.Settings.SettingsPath, Broken, TestContext.Current.CancellationToken);

        string? warning = await workspace.Settings.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(warning);
        Assert.StartsWith("The saved preferences could not be read back", warning, StringComparison.Ordinal);
        Assert.Equal(ThemePreference.System, workspace.Settings.Current.Theme);
        Assert.Equal(
            Broken,
            await File.ReadAllTextAsync(workspace.Settings.SettingsPath, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// A settings file written by an older build, or one that only ever recorded the theme, reads
    /// fine and keeps the defaults for everything it does not mention.
    /// </summary>
    [Fact]
    public async Task LoadAsync_WithAPartialFile_KeepsTheDefaultsForTheRest()
    {
        using TempWorkspace workspace = new();
        await File.WriteAllTextAsync(
            workspace.Settings.SettingsPath,
            """{ "theme": "Light", "somethingElse": true }""",
            TestContext.Current.CancellationToken);

        Assert.Null(await workspace.Settings.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ThemePreference.Light, workspace.Settings.Current.Theme);
        Assert.Empty(workspace.Settings.Current.CollapsedCategories);
    }

    [Fact]
    public void WithCollapsed_RemovesTheEntryWhenNothingIsFolded()
    {
        AppSettings settings = new AppSettings().WithCollapsed("a.json", ["c-1"]);

        Assert.Single(settings.CollapsedCategories);
        Assert.Empty(settings.WithCollapsed("a.json", []).CollapsedCategories);
    }

    [Fact]
    public void Defaults_PutTheListsAndTheSettingsInThePrivateDataFolder()
    {
        SettingsLocation location = AppPaths.Defaults();

        Assert.Equal(Path.Combine(AppPaths.DataFolder, "lists"), location.WorkspacePath);
        Assert.Equal(Path.Combine(AppPaths.DataFolder, "settings.json"), location.SettingsPath);
        Assert.EndsWith(AppPaths.FolderName, AppPaths.DataFolder, StringComparison.Ordinal);
    }
}
