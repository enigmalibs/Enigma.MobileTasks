using System;
using System.IO;
using Enigma.MobileTasks.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.MobileTasks.App.UnitTests.Infrastructure;

/// <summary>
/// The application's real container, with its data folder moved into a temporary directory that goes
/// away with the test.
/// </summary>
/// <remarks>
/// Everything is the production registration except <see cref="SettingsLocation"/>, registered again
/// last so it wins: a test must never read or write the developer's own lists.
/// </remarks>
public sealed class TempApp : IDisposable
{
    private readonly string _installation;
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="configure">Replaces registrations — a test double — after the production ones.</param>
    public TempApp(Action<IServiceCollection>? configure = null)
    {
        _installation = Path.Combine(Path.GetTempPath(), "enigma-mobiletasks-app-tests", Guid.NewGuid().ToString("N"));
        Root = Path.Combine(_installation, "lists");
        Directory.CreateDirectory(Root);

        ServiceCollection services = new();
        App.ConfigureServices(services);
        services.AddSingleton(new SettingsLocation(Path.Combine(_installation, "settings.json"), Root));
        configure?.Invoke(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    /// <summary>
    /// Gets the folder the lists live in.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Gets the container.
    /// </summary>
    public IServiceProvider Services => _provider;

    /// <summary>
    /// Resolves a service.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service.</returns>
    public T Get<T>()
        where T : notnull
        => _provider.GetRequiredService<T>();

    /// <summary>
    /// Writes a file straight into the lists folder, as if it had been there before the application
    /// started.
    /// </summary>
    /// <param name="fileName">The file's name.</param>
    /// <param name="content">Its content.</param>
    /// <returns>The full path.</returns>
    public string WriteRaw(string fileName, string content)
    {
        string path = Path.Combine(Root, fileName);
        File.WriteAllText(path, content);

        return path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _provider.Dispose();

        try
        {
            if (Directory.Exists(_installation))
            {
                Directory.Delete(_installation, recursive: true);
            }
        }
        catch (IOException)
        {
            // A temporary directory that survives a test run is the operating system's problem.
        }
    }
}
