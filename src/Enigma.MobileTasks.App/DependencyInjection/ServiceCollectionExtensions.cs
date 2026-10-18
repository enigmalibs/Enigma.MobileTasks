using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.App.Views.Pages;
using Enigma.MobileTasks.Core.Identity;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.MobileTasks.App.DependencyInjection;

/// <summary>
/// Registers the application's services, in two layers: the model and its storage, then the
/// interface over them.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the model, the file format, the workspace on the device and the settings.
        /// </summary>
        /// <returns>The same collection.</returns>
        public IServiceCollection AddMobileTasksCore()
        {
            services.AddSingleton(AppPaths.Defaults());
            services.AddSingleton<IListIdGenerator, ListIdGenerator>();
            services.AddSingleton<ListOperations>();
            services.AddSingleton<ListJsonSerializer>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWorkspaceStore, WorkspaceStore>();
            services.AddSingleton(provider => new SaveScheduler(provider.GetRequiredService<IWorkspaceStore>()));

            return services;
        }

        /// <summary>
        /// Registers the shell, its services, the pages and their ViewModels.
        /// </summary>
        /// <returns>The same collection.</returns>
        /// <remarks>
        /// ViewModels are singletons and views transient, so an activity re-created by Android builds
        /// a fresh tree around the state it had. The dialog and notification services are registered
        /// once and exposed twice — as their interface to the ViewModels that ask, and as themselves
        /// to the shell that draws them.
        /// </remarks>
        public IServiceCollection AddMobileTasksApp()
        {
            services.AddSingleton<IActiveListService, ActiveListService>();

            services.AddSingleton<DialogService>();
            services.AddSingleton<IDialogService>(provider => provider.GetRequiredService<DialogService>());
            services.AddSingleton<NotificationService>();
            services.AddSingleton<INotificationService>(provider => provider.GetRequiredService<NotificationService>());

            services.AddSingleton<IPageFactory, ContainerPageFactory>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<ListsPageViewModel>();

            services.AddTransient<ShellView>();
            services.AddTransient<ListsPage>();

            return services;
        }
    }
}
