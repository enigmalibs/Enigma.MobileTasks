using System;
using Avalonia.Controls;
using Enigma.MobileTasks.App.ViewModels.Pages;
using Enigma.MobileTasks.App.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// Default <see cref="IPageFactory"/>: resolves the view and its ViewModel from the container.
/// </summary>
/// <remarks>
/// Views are transient and ViewModels singletons, deliberately: Android can re-create the activity
/// during a session, and each re-creation builds a fresh visual tree that re-attaches the ViewModels
/// it had before, so the pages' state survives while the old tree does not leak. Taking the
/// provider is the one place the container is asked for something by name — the price of
/// navigating by page rather than wiring every view into every ViewModel.
/// </remarks>
public sealed class ContainerPageFactory : IPageFactory
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="services">The container the pages and their ViewModels come from.</param>
    public ContainerPageFactory(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <inheritdoc />
    public Page Create(ShellPage page) => page switch
    {
        ShellPage.Lists => Bind<ListsPage, ListsPageViewModel>(),
        ShellPage.Checklist => Bind<ChecklistPage, ChecklistPageViewModel>(),
        ShellPage.Settings => Bind<SettingsPage, SettingsPageViewModel>(),
        _ => throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown page."),
    };

    private TView Bind<TView, TViewModel>()
        where TView : Page
        where TViewModel : class
    {
        TView view = _services.GetRequiredService<TView>();
        view.DataContext = _services.GetRequiredService<TViewModel>();

        return view;
    }
}
