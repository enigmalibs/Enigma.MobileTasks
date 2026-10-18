using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Settings;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The saved theme preference, turned into the variant the whole application paints in.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ThemeTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(ThemePreference.Dark, "Dark")]
    [InlineData(ThemePreference.Light, "Light")]
    public void ApplyTheme_SetsTheRequestedVariant(ThemePreference preference, string expected)
    {
        fixture.Run(() =>
        {
            App.ApplyTheme(preference);

            Assert.Equal(expected, Application.Current!.RequestedThemeVariant?.Key.ToString());

            App.ApplyTheme(ThemePreference.System);
        });
    }

    /// <summary>
    /// System always ends on one of the palette's two variants — Avalonia's Default where the platform
    /// resolves it (so a phone switching to dark mode is followed), the platform's variant requested
    /// explicitly where it does not. Never no variant at all, which paints every Enigma brush
    /// transparent.
    /// </summary>
    [Fact]
    public void ApplyTheme_System_AlwaysEndsOnLightOrDark()
    {
        fixture.Run(() =>
        {
            App.ApplyTheme(ThemePreference.Dark);
            App.ApplyTheme(ThemePreference.System);

            Application application = Application.Current!;
            Assert.True(
                application.ActualThemeVariant == ThemeVariant.Light || application.ActualThemeVariant == ThemeVariant.Dark,
                "Actual variant: " + application.ActualThemeVariant);
            Assert.True(
                application.RequestedThemeVariant == ThemeVariant.Default ||
                application.RequestedThemeVariant == App.SystemVariant(application));
        });
    }

    /// <summary>
    /// A ticked row is painted with the Enigma accent, not the phone's system accent Fluent would use.
    /// </summary>
    [Fact]
    public void ATickedRow_UsesTheEnigmaAccent()
    {
        fixture.Run(() =>
        {
            App.ApplyTheme(ThemePreference.Light);

            CheckBox box = new() { IsChecked = true };
            Window window = new() { Content = box };
            window.Show();

            Assert.True(box.TryFindResource("CheckBoxCheckBackgroundFillChecked", box.ActualThemeVariant, out object? fill));
            Assert.True(box.TryFindResource("EnigmaAccentColor", box.ActualThemeVariant, out object? accent));
            window.UpdateLayout();
            Assert.Equal(accent, Assert.IsAssignableFrom<ISolidColorBrush>(fill).Color);

            window.Close();
            App.ApplyTheme(ThemePreference.System);
        });
    }
}
