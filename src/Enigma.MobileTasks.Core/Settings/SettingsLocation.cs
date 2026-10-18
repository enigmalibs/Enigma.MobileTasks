namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Where the settings file is, and where the lists go.
/// </summary>
/// <param name="SettingsPath">The settings file.</param>
/// <param name="WorkspacePath">The folder the lists live in.</param>
/// <remarks>
/// Injected rather than read from <see cref="AppPaths"/> inside the services, so a test can put a
/// whole installation in a temporary folder. The workspace folder is fixed on the phone — the
/// private storage cannot be pointed anywhere else — so it lives here rather than among the
/// settings a user can change.
/// </remarks>
public sealed record SettingsLocation(string SettingsPath, string WorkspacePath);
