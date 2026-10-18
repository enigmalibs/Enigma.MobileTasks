using System;
using System.IO;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Where the application keeps its data on the device.
/// </summary>
/// <remarks>
/// <c>LocalApplicationData</c> is the application's private storage on Android — readable by this
/// application only, removed with it, and needing no permission — and <c>~/.local/share</c> on a
/// Linux development machine. The lists sit in a plain sub-folder, one file each, in the very format
/// an export writes: the folder is private, so the system file picker is how a list leaves the
/// phone.
/// </remarks>
public static class AppPaths
{
    /// <summary>
    /// The folder name everything of this application's goes under.
    /// </summary>
    public const string FolderName = "Enigma.MobileTasks";

    /// <summary>
    /// Gets the application's data folder.
    /// </summary>
    public static string DataFolder
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    /// <summary>
    /// Gets the folder the lists live in.
    /// </summary>
    public static string WorkspaceFolder => Path.Combine(DataFolder, "lists");

    /// <summary>
    /// Gets the settings file.
    /// </summary>
    public static string SettingsFile => Path.Combine(DataFolder, "settings.json");

    /// <summary>
    /// Gets the locations as one value, which is what the container hands the settings service and
    /// the workspace store.
    /// </summary>
    /// <returns>The locations.</returns>
    public static SettingsLocation Defaults() => new(SettingsFile, WorkspaceFolder);
}
