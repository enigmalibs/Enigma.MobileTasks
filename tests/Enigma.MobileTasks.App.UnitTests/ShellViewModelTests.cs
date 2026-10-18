using System;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The start-up work the shell does once, and what it tells the user about it.
/// </summary>
public sealed class ShellViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsTheLists()
    {
        using TempApp app = new();
        app.WriteRaw("liste-judith.json", TestLists.ReadJudithJson());

        await app.Get<ShellViewModel>().InitializeAsync();

        Assert.Single(app.Get<IActiveListService>().Entries);
        Assert.False(app.Get<NotificationService>().IsVisible);
    }

    [Fact]
    public async Task InitializeAsync_ReportsAnUnreadableFileByName()
    {
        using TempApp app = new();
        app.WriteRaw("cassee.json", "{ \"categories\": ");

        await app.Get<ShellViewModel>().InitializeAsync();

        NotificationService notifications = app.Get<NotificationService>();
        Assert.True(notifications.IsVisible);
        Assert.StartsWith("cassee.json could not be read and was left untouched", notifications.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitializeAsync_CountsSeveralUnreadableFiles()
    {
        using TempApp app = new();
        app.WriteRaw("une.json", "nope");
        app.WriteRaw("deux.json", "nope");

        await app.Get<ShellViewModel>().InitializeAsync();

        Assert.Equal("2 list files could not be read and were left untouched.", app.Get<NotificationService>().Message);
    }

    [Fact]
    public async Task InitializeAsync_ShowsTheSettingsWarning()
    {
        using TempApp app = new();

        await app.Get<ShellViewModel>().InitializeAsync("The saved preferences are empty; the default values are used.");

        Assert.Equal("The saved preferences are empty; the default values are used.", app.Get<NotificationService>().Message);
    }

    /// <summary>
    /// An activity re-created by Android builds a new shell around the same ViewModel; the lists are
    /// not read a second time.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_RunsOnce()
    {
        using TempApp app = new();
        ShellViewModel shell = app.Get<ShellViewModel>();
        int reloads = 0;
        app.Get<IActiveListService>().EntriesChanged += (_, _) => reloads++;

        await shell.InitializeAsync();
        await shell.InitializeAsync();

        Assert.Equal(1, reloads);
    }
}
