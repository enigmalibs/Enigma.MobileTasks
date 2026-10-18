using System;
using System.IO;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.Serialization;
using Enigma.MobileTasks.Core.Settings;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.Core.UnitTests.Infrastructure;

/// <summary>
/// A whole installation — settings file, workspace folder and the services over them — inside a
/// temporary directory that goes away with the test.
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    private readonly string _installation;

    /// <summary>
    /// Initialises a new instance and creates the folders.
    /// </summary>
    public TempWorkspace()
    {
        _installation = Path.Combine(
            Path.GetTempPath(),
            "enigma-mobiletasks-tests",
            Guid.NewGuid().ToString("N"));

        Root = Path.Combine(_installation, "lists");
        Directory.CreateDirectory(Root);

        Location = new SettingsLocation(Path.Combine(_installation, "settings.json"), Root);
        Ids = new SequentialIdGenerator();
        Operations = new ListOperations(Ids);
        Json = new ListJsonSerializer(Ids);
        Settings = new SettingsService(Location);
        Store = new WorkspaceStore(Location, Operations, Json);
    }

    /// <summary>
    /// Gets the folder the lists live in.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Gets the folder the whole installation lives in.
    /// </summary>
    public string Installation => _installation;

    /// <summary>
    /// Gets the locations the services were built over.
    /// </summary>
    public SettingsLocation Location { get; }

    /// <summary>
    /// Gets the identifier generator the services share.
    /// </summary>
    public SequentialIdGenerator Ids { get; }

    /// <summary>
    /// Gets the model operations.
    /// </summary>
    public ListOperations Operations { get; }

    /// <summary>
    /// Gets the JSON serializer.
    /// </summary>
    public ListJsonSerializer Json { get; }

    /// <summary>
    /// Gets the settings service, pointed at the temporary installation.
    /// </summary>
    public ISettingsService Settings { get; }

    /// <summary>
    /// Gets the store over the temporary workspace.
    /// </summary>
    public IWorkspaceStore Store { get; }

    /// <summary>
    /// Writes a file straight into the workspace folder, bypassing the store — how a test puts a
    /// broken file, or a file written elsewhere, in front of it.
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
        try
        {
            if (Directory.Exists(_installation))
            {
                Directory.Delete(_installation, recursive: true);
            }
        }
        catch (IOException)
        {
            // A temporary directory that survives a test run is the operating system's problem,
            // not a test failure.
        }
    }
}
