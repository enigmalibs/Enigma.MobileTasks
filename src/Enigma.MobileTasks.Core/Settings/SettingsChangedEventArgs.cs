using System;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Announces that the settings changed.
/// </summary>
public sealed class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="settings">The settings as they now are.</param>
    public SettingsChangedEventArgs(AppSettings settings) => Settings = settings;

    /// <summary>
    /// Gets the settings as they now are.
    /// </summary>
    public AppSettings Settings { get; }
}
