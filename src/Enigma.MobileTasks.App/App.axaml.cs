using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Enigma.MobileTasks.App.DependencyInjection;
using Enigma.MobileTasks.App.Navigation;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.ViewModels;
using Enigma.MobileTasks.App.Views;
using Enigma.MobileTasks.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Enigma.MobileTasks.App;

/// <summary>
/// The Avalonia <see cref="Application"/>, and the composition root.
/// </summary>
/// <remarks>
/// <para>
/// The generic host is built and <b>started</b>, never run: the platform's activity lifetime runs the
/// application, and the host only supplies services. It comes from
/// <see cref="Host.CreateEmptyApplicationBuilder"/>, because <c>Host.CreateApplicationBuilder</c>'s
/// defaults are desktop and server assumptions with no place on a phone — <c>appsettings.json</c>
/// files watched on the content root, and a console lifetime.
/// </para>
/// <para>
/// The host is never stopped either: Android ends an application by ending its process, with no
/// exit event to hang anything on. What must not be lost — a list ticked a moment ago — is flushed
/// when the application goes to the background instead.
/// </para>
/// </remarks>
public partial class App : Application
{
    private IHost? _host;
    private string? _settingsWarning;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            // Android: the factory runs once per activity, and an activity can be re-created during a
            // session, so each call builds a fresh visual tree around the same singleton ViewModels.
            case IActivityApplicationLifetime activity:
            {
                IServiceProvider services = Start();
                activity.MainViewFactory = () => CreateShell(services);
                break;
            }

            // The single-view fallback, as Avalonia's own template keeps it — the shape iOS uses.
            case ISingleViewApplicationLifetime singleView:
            {
                IServiceProvider services = Start();
                singleView.MainView = CreateShell(services);
                break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the application host. Exposed so the tests can assert the container resolves
    /// everything the shell needs.
    /// </summary>
    /// <returns>The built, unstarted host.</returns>
    internal static IHost BuildHost()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        ConfigureServices(builder.Services);

        return builder.Build();
    }

    /// <summary>
    /// Registers everything the application needs.
    /// </summary>
    /// <param name="services">The collection to register into.</param>
    internal static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMobileTasksCore();
        services.AddMobileTasksApp();
    }

    /// <summary>
    /// Builds one activity's shell: the view, its ViewModel, and a fresh navigation stack.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="settingsWarning">What reading the settings reported, shown once the lists load.</param>
    /// <returns>The shell, ready to be hosted.</returns>
    internal static ShellView CreateShell(IServiceProvider services, string? settingsWarning = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ShellViewModel viewModel = services.GetRequiredService<ShellViewModel>();
        ShellView shell = services.GetRequiredService<ShellView>();

        shell.DataContext = viewModel;
        shell.NavigationHost.Page = services.GetRequiredService<INavigationService>().CreateRoot();

        // Reading the lists is I/O, so it starts once there is a shell to report a problem in. It is
        // a no-op for every shell after the first.
        _ = viewModel.InitializeAsync(settingsWarning);

        return shell;
    }

    /// <summary>
    /// Paints the application in the preferred theme, now, whenever the preference changes, and —
    /// while the preference is System — whenever the phone switches between light and dark mode.
    /// </summary>
    /// <param name="settings">What the application remembers.</param>
    /// <remarks>
    /// <see cref="ISettingsService.Changed"/> is raised on whichever thread finished writing the file,
    /// and the theme may only be set on the UI thread, so the handlers marshal — with <c>Post</c>, so a
    /// UI thread blocked on a flush can never deadlock against them.
    /// </remarks>
    internal static void ObserveTheme(ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ApplyTheme(settings.Current.Theme);
        settings.Changed += (_, e) => Dispatcher.UIThread.Post(() => ApplyTheme(e.Settings.Theme));

        if (Current?.PlatformSettings is { } platform)
        {
            platform.ColorValuesChanged += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                if (settings.Current.Theme == ThemePreference.System)
                {
                    ApplyTheme(ThemePreference.System);
                }
            });
        }
    }

    /// <summary>
    /// Puts the chosen theme on the application. The caller is on the UI thread.
    /// </summary>
    /// <param name="preference">What the user chose.</param>
    /// <remarks>
    /// System is resolved here, to Light or Dark, rather than handed to Avalonia as
    /// <c>ThemeVariant.Default</c>: the Enigma palette defines exactly those two dictionaries, and an
    /// application switched back to Default can be left with no actual variant at all — every Enigma
    /// brush then resolves to nothing and the screen paints blank. The phone's own switch is followed
    /// through <see cref="ObserveTheme"/> instead.
    /// </remarks>
    internal static void ApplyTheme(ThemePreference preference)
    {
        if (Current is not { } application)
        {
            return;
        }

        application.RequestedThemeVariant = preference switch
        {
            ThemePreference.Dark => ThemeVariant.Dark,
            ThemePreference.Light => ThemeVariant.Light,
            _ => SystemVariant(application),
        };
    }

    /// <summary>
    /// Says which of the two variants the phone is in.
    /// </summary>
    /// <param name="application">The application whose platform is asked.</param>
    /// <returns>Dark when the platform says so, Light otherwise.</returns>
    internal static ThemeVariant SystemVariant(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    private IServiceProvider Start()
    {
        IHost host = BuildHost();
        host.Start();
        _host = host;

        IServiceProvider services = host.Services;

        // Before the first view: the theme is a preference, and a screen that paints dark and then
        // flips to light is a worse first impression than one that starts right. The file is a few
        // hundred bytes, and the service never resumes on this thread, so blocking cannot deadlock.
        ISettingsService settings = services.GetRequiredService<ISettingsService>();
        _settingsWarning = settings.LoadAsync().GetAwaiter().GetResult();
        ObserveTheme(settings);

        if (this.TryGetFeature<IActivatableLifetime>() is { } activatable)
        {
            activatable.Deactivated += OnDeactivated;
        }

        return services;
    }

    private ShellView CreateShell(IServiceProvider services)
    {
        string? warning = _settingsWarning;
        _settingsWarning = null;

        return CreateShell(services, warning);
    }

    /// <summary>
    /// Writes whatever is pending as the application goes to the background, where Android may end
    /// the process without another word.
    /// </summary>
    private async void OnDeactivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind == ActivationKind.Background && _host?.Services.GetService<IActiveListService>() is { } lists)
        {
            await FlushAsync(lists);
        }
    }

    private static Task FlushAsync(IActiveListService lists) => lists.FlushAsync();
}
