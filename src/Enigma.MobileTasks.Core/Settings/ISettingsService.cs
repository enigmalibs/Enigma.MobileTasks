using System;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Reads and writes what the application remembers between runs.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the settings as they are now. Never <see langword="null"/>: before
    /// <see cref="LoadAsync"/> runs, this is the defaults.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Gets the file the settings are kept in.
    /// </summary>
    string SettingsPath { get; }

    /// <summary>
    /// Raised after <see cref="UpdateAsync"/> has changed something.
    /// </summary>
    /// <remarks>
    /// <strong>Raised on whichever thread finished writing the file</strong> — a thread-pool
    /// thread, in practice, not the one that asked for the change. A subscriber that touches a
    /// user interface must marshal onto its own thread before doing anything.
    /// </remarks>
    event EventHandler<SettingsChangedEventArgs>? Changed;

    /// <summary>
    /// Reads the settings file.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>
    /// <see langword="null"/> when all went well, or a message to show when the file was there
    /// but unreadable and the defaults were used instead.
    /// </returns>
    Task<string?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the settings and writes them out.
    /// </summary>
    /// <param name="change">Produces the new settings from the current ones.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the file has been written.</returns>
    Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken = default);
}
