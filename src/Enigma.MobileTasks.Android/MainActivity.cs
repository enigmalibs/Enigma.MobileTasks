using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Enigma.MobileTasks.Android;

/// <summary>
/// The launcher activity: the window Avalonia renders the application into.
/// </summary>
/// <remarks>
/// Empty by design — in Avalonia 12 the activity only hosts the view; start-up belongs to
/// <see cref="MainApplication"/>. The configuration changes it declares keep the activity alive when
/// the phone rotates, changes size or switches between light and dark mode, so Avalonia re-lays out
/// and re-themes in place instead of the activity being torn down with its navigation stack.
/// </remarks>
[Activity(
    Label = "Enigma tasks",
    Theme = "@style/EnigmaMobileTasksTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
