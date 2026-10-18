using Avalonia.Controls;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.Workspace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The container the application starts from: everything resolves, and the lifetimes are the ones
/// the activity re-creation story depends on.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class CompositionRootTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void BuildHost_ResolvesEveryServiceAndViewModel()
    {
        using IHost host = App.BuildHost();

        Assert.NotNull(host.Services.GetRequiredService<ISettingsService>());
        Assert.NotNull(host.Services.GetRequiredService<IWorkspaceStore>());
        Assert.NotNull(host.Services.GetRequiredService<IActiveListService>());
        Assert.NotNull(host.Services.GetRequiredService<INavigationService>());
        Assert.NotNull(host.Services.GetRequiredService<ShellViewModel>());
        Assert.NotNull(host.Services.GetRequiredService<ListsPageViewModel>());
    }

    [Fact]
    public void Defaults_KeepTheListsInThePrivateDataFolder()
    {
        using IHost host = App.BuildHost();

        Assert.Equal(AppPaths.WorkspaceFolder, host.Services.GetRequiredService<IWorkspaceStore>().RootPath);
    }

    [Fact]
    public void DialogAndNotificationServices_AreOneInstanceBehindTwoTypes()
    {
        using TempApp app = new();

        Assert.Same(app.Get<DialogService>(), app.Get<IDialogService>());
        Assert.Same(app.Get<NotificationService>(), app.Get<INotificationService>());
    }

    /// <summary>
    /// Views are rebuilt for every activity; ViewModels outlive them.
    /// </summary>
    [Fact]
    public void Views_AreTransientAndViewModelsSingletons()
    {
        using TempApp app = new();

        fixture.Run(() =>
        {
            Assert.NotSame(app.Get<ShellView>(), app.Get<ShellView>());
            Assert.Same(app.Get<ShellViewModel>(), app.Get<ShellViewModel>());

            Page first = app.Get<IPageFactory>().Create(ShellPage.Lists);
            Page second = app.Get<IPageFactory>().Create(ShellPage.Lists);

            Assert.NotSame(first, second);
            Assert.Same(first.DataContext, second.DataContext);
            Assert.IsType<ListsPageViewModel>(first.DataContext);
        });
    }
}
