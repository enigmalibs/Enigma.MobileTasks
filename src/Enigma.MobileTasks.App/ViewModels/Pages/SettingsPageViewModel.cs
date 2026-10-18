using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.App.Resources;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.Core.Settings;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// Backs the Settings page: the theme, where the lists live, and which version this is.
/// </summary>
/// <remarks>
/// <see cref="Theme"/> is the single source of truth; the three <c>Is…Theme</c> properties are its
/// projections for the three radio buttons. Choosing one writes the settings file, and the application
/// re-themes on the settings' <c>Changed</c> event — so the switch is live and survives a restart
/// through one path.
/// </remarks>
public sealed class SettingsPageViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IActiveListService _lists;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="settings">Where the theme is remembered.</param>
    /// <param name="lists">The lists on the phone, for the storage summary.</param>
    public SettingsPageViewModel(ISettingsService settings, IActiveListService lists)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(lists);

        _settings = settings;
        _lists = lists;
        _lists.EntriesChanged += (_, _) => OnPropertyChanged(nameof(StorageSummary));
    }

    /// <summary>
    /// Gets the theme the application paints in, as saved.
    /// </summary>
    public ThemePreference Theme => _settings.Current.Theme;

    /// <summary>
    /// Gets or sets a value indicating whether the application follows the phone's dark mode.
    /// </summary>
    /// <remarks>
    /// Setting <see langword="false"/> does nothing: that is what a radio button reports when another one
    /// in its group is checked, and that other button's own property makes the change.
    /// </remarks>
    public bool IsSystemTheme
    {
        get => Theme == ThemePreference.System;
        set => Choose(value, ThemePreference.System);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application is always light.
    /// </summary>
    public bool IsLightTheme
    {
        get => Theme == ThemePreference.Light;
        set => Choose(value, ThemePreference.Light);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application is always dark.
    /// </summary>
    public bool IsDarkTheme
    {
        get => Theme == ThemePreference.Dark;
        set => Choose(value, ThemePreference.Dark);
    }

    /// <summary>
    /// Gets how many lists the phone holds, and where.
    /// </summary>
    public string StorageSummary
        => string.Format(CultureInfo.CurrentCulture, Strings.StorageSummary, _lists.Entries.Count);

    /// <summary>
    /// Gets the version line.
    /// </summary>
    public string VersionText { get; } = string.Format(
        CultureInfo.CurrentCulture,
        Strings.VersionLine,
        typeof(SettingsPageViewModel).Assembly.GetName().Version?.ToString(3) ?? "?");

    /// <summary>
    /// Gets the task of the last theme change, so a caller — a test — can wait for the file to be
    /// written.
    /// </summary>
    internal Task LastChange { get; private set; } = Task.CompletedTask;

    private void Choose(bool selected, ThemePreference theme)
    {
        if (!selected || Theme == theme)
        {
            return;
        }

        LastChange = ChooseAsync(theme);
    }

    private async Task ChooseAsync(ThemePreference theme)
    {
        await _settings.UpdateAsync(settings => settings with { Theme = theme });

        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
    }
}
