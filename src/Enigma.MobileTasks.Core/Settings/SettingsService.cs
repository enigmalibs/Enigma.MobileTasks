using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Workspace;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// Keeps the settings in a small JSON file beside the lists folder.
/// </summary>
/// <remarks>
/// Written the same way a list is — to a temporary file that then replaces the real one — so a
/// crash mid-write cannot leave the application without readable settings. A settings file that
/// cannot be read is not deleted either: the defaults are used, the caller is told, and the file
/// is only overwritten when something actually changes.
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    private readonly SettingsLocation _location;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="location">Where the settings file is.</param>
    public SettingsService(SettingsLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        _location = location;
        Current = new AppSettings();
    }

    /// <inheritdoc />
    public AppSettings Current { get; private set; }

    /// <inheritdoc />
    public string SettingsPath => _location.SettingsPath;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? Changed;

    /// <inheritdoc />
    public async Task<string?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_location.SettingsPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_location.SettingsPath, cancellationToken)
                .ConfigureAwait(false);

            AppSettings? settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings);

            if (settings is null)
            {
                return "The saved preferences are empty; the default values are used.";
            }

            Current = Normalize(settings);

            return null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return "The saved preferences could not be read back (" + exception.Message +
                "); the default values are used.";
        }
    }

    /// <summary>
    /// Puts back the defaults a partial file left out.
    /// </summary>
    /// <param name="settings">What the file held.</param>
    /// <returns>Settings with every member set.</returns>
    /// <remarks>
    /// The source-generated serializer creates the record with every <c>init</c> property assigned,
    /// so a member the file does not mention arrives as its type's default rather than as the
    /// initializer's value: <see langword="null"/> for the folded categories. (The theme's default
    /// value, <see cref="ThemePreference.System"/>, is also the enum's zero, so it needs nothing.)
    /// </remarks>
    private static AppSettings Normalize(AppSettings settings)
        => settings.CollapsedCategories is null
            ? settings with { CollapsedCategories = new Dictionary<string, IReadOnlyList<string>>() }
            : settings;

    /// <inheritdoc />
    public async Task UpdateAsync(
        Func<AppSettings, AppSettings> change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        AppSettings updated;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            updated = change(Current);

            if (updated == Current)
            {
                return;
            }

            Current = updated;

            await AtomicFile
                .WriteAsync(
                    _location.SettingsPath,
                    JsonSerializer.Serialize(updated, SettingsJsonContext.Default.AppSettings),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, new SettingsChangedEventArgs(updated));
    }
}
