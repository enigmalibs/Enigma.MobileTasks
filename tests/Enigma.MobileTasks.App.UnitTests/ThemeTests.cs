using Avalonia;
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
    /// System is resolved to one of the palette's two variants, never left to Avalonia's Default —
    /// which, set again after Dark, can leave the application with no actual variant and every Enigma
    /// brush transparent.
    /// </summary>
    [Fact]
    public void ApplyTheme_System_ResolvesToThePlatformsVariant()
    {
        fixture.Run(() =>
        {
            App.ApplyTheme(ThemePreference.Dark);
            App.ApplyTheme(ThemePreference.System);

            Application application = Application.Current!;
            Assert.NotEqual(ThemeVariant.Default, application.RequestedThemeVariant);
            Assert.Equal(App.SystemVariant(application), application.RequestedThemeVariant);
            Assert.Equal(application.RequestedThemeVariant, application.ActualThemeVariant);
        });
    }
}
