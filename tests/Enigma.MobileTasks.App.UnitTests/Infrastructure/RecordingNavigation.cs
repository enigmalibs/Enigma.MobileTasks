using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.MobileTasks.App.UnitTests.Infrastructure;

/// <summary>
/// A navigation service that only remembers where it was asked to go, so a ViewModel test needs no
/// navigation stack and no UI thread.
/// </summary>
public sealed class RecordingNavigation : INavigationService
{
    /// <summary>
    /// Gets the pages pushed, in order.
    /// </summary>
    public List<ShellPage> Pushed { get; } = [];

    /// <summary>
    /// Gets how many times the user went back.
    /// </summary>
    public int Backs { get; private set; }

    /// <summary>
    /// Registers a recording navigation in place of the real one.
    /// </summary>
    /// <param name="services">The collection.</param>
    public static void Register(IServiceCollection services)
        => services.AddSingleton<INavigationService, RecordingNavigation>();

    /// <inheritdoc />
    public ShellNavigationPage CreateRoot() => throw new NotSupportedException("No navigation stack in a ViewModel test.");

    /// <inheritdoc />
    public Task PushAsync(ShellPage page)
    {
        Pushed.Add(page);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        Backs++;

        return Task.CompletedTask;
    }
}
