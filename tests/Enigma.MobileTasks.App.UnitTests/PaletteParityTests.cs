using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The vendored palette against the published one: every colour and brush key of
/// Enigma.Avalonia.Desktop 1.2.0 resolves to the same value in this application, in both variants.
/// </summary>
/// <remarks>
/// The desktop package is referenced by this test project only. When it is bumped, these tests fail
/// until Themes/Colors.axaml and Themes/Brushes.axaml are copied from the same release — which is
/// the whole point.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class PaletteParityTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly Uri DesktopColors = new("avares://Enigma.Avalonia.Desktop/Themes/Colors.axaml");
    private static readonly Uri DesktopBrushes = new("avares://Enigma.Avalonia.Desktop/Themes/Brushes.axaml");
    private static readonly Uri AppColors = new("avares://Enigma.MobileTasks.App/Themes/Colors.axaml");
    private static readonly Uri AppBrushes = new("avares://Enigma.MobileTasks.App/Themes/Brushes.axaml");

    public static TheoryData<string> Variants => ["Dark", "Light"];

    [Theory]
    [MemberData(nameof(Variants))]
    public void EveryColour_IsTheDesktopLibrarys(string variantName)
    {
        ThemeVariant variant = Variant(variantName);

        fixture.Run(() =>
        {
            ResourceDictionary desktop = Load(DesktopColors);
            ResourceDictionary app = Load(AppColors);

            List<string> keys = KeysOf(desktop, variant);
            Assert.True(keys.Count >= 30, "The desktop palette should define its ~31 colour keys.");
            Assert.Equal(keys.Order(StringComparer.Ordinal), KeysOf(app, variant).Order(StringComparer.Ordinal));

            Assert.All(keys, key =>
            {
                Assert.True(desktop.TryGetResource(key, variant, out object? expected));
                Assert.True(app.TryGetResource(key, variant, out object? actual), key);
                Assert.Equal(expected, actual);
            });
        });
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void EveryBrush_PaintsTheDesktopLibrarysColour(string variantName)
    {
        ThemeVariant variant = Variant(variantName);

        fixture.Run(() =>
        {
            ResourceDictionary desktopBrushes = Load(DesktopBrushes);
            List<string> keys = [.. desktopBrushes.Keys.OfType<string>()];
            Assert.True(keys.Count >= 25, "The desktop library should define its brush keys.");
            Assert.Equal(
                keys.Order(StringComparer.Ordinal),
                Load(AppBrushes).Keys.OfType<string>().Order(StringComparer.Ordinal));

            IReadOnlyDictionary<string, Color> expected = Paint(DesktopColors, DesktopBrushes, variant, keys);
            IReadOnlyDictionary<string, Color> actual = Paint(AppColors, AppBrushes, variant, keys);

            Assert.All(keys, key => Assert.Equal(expected[key], actual[key]));
        });
    }

    private static ThemeVariant Variant(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static ResourceDictionary Load(Uri source) => (ResourceDictionary)AvaloniaXamlLoader.Load(source);

    private static List<string> KeysOf(ResourceDictionary dictionary, ThemeVariant variant)
    {
        List<string> keys = [.. dictionary.Keys.OfType<string>()];

        if (dictionary.ThemeDictionaries.TryGetValue(variant, out IThemeVariantProvider? themed) &&
            themed is ResourceDictionary scoped)
        {
            keys.AddRange(scoped.Keys.OfType<string>());
        }

        return keys;
    }

    /// <summary>
    /// Hosts a pair of colour and brush dictionaries in a window of the given variant, paints one
    /// border per brush key, and reads back the colour each border actually got — the brushes resolve
    /// their colour through DynamicResource, so only a consumer in a themed tree sees the real value.
    /// </summary>
    private static Dictionary<string, Color> Paint(Uri colors, Uri brushes, ThemeVariant variant, IEnumerable<string> keys)
    {
        Window window = new() { RequestedThemeVariant = variant, Width = 100, Height = 100 };
        window.Resources.MergedDictionaries.Add(Load(colors));
        window.Resources.MergedDictionaries.Add(Load(brushes));

        StackPanel panel = new();
        Dictionary<string, Border> borders = [];

        foreach (string key in keys)
        {
            Border border = new();
            border.Bind(Border.BackgroundProperty, border.GetResourceObservable(key));
            borders[key] = border;
            panel.Children.Add(border);
        }

        window.Content = panel;
        window.Show();
        window.UpdateLayout();

        Dictionary<string, Color> painted = borders.ToDictionary(
            pair => pair.Key,
            pair => Assert.IsAssignableFrom<ISolidColorBrush>(pair.Value.Background).Color);

        window.Close();

        return painted;
    }
}
