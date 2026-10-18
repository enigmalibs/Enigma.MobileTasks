using System;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;

namespace Enigma.MobileTasks.App.Navigation;

/// <summary>
/// Default <see cref="INavigationService"/>, over the <see cref="ShellNavigationPage"/> the current
/// activity hosts.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IPageFactory _pages;
    private readonly IDialogService _dialogs;
    private ShellNavigationPage? _root;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="pages">Builds the pages.</param>
    /// <param name="dialogs">Takes the back button first while a dialog is open.</param>
    public NavigationService(IPageFactory pages, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(dialogs);

        _pages = pages;
        _dialogs = dialogs;
    }

    /// <inheritdoc />
    public ShellNavigationPage CreateRoot()
    {
        ShellNavigationPage root = new(_dialogs.TryCancel)
        {
            Content = _pages.Create(ShellPage.Lists),
        };

        _root = root;

        return root;
    }

    /// <inheritdoc />
    public Task PushAsync(ShellPage page)
        => Root.PushAsync(_pages.Create(page));

    /// <inheritdoc />
    public Task GoBackAsync()
        => Root.StackDepth > 1 ? Root.PopAsync() : Task.CompletedTask;

    private ShellNavigationPage Root
        => _root ?? throw new InvalidOperationException("The navigation stack has not been created yet.");
}
