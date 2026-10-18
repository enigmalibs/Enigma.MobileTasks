using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Enigma.MobileTasks.Android;

/// <summary>
/// The Android application object, which starts Avalonia with the shared <see cref="App.App"/>.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia 12 starts the application here, once per process; <see cref="MainActivity"/> only hosts
/// the view the application's <c>MainViewFactory</c> builds. Named <c>MainApplication</c> rather than
/// the template's <c>Application</c>, which would shadow both <c>Android.App.Application</c> and
/// <c>Avalonia.Application</c>.
/// </para>
/// <para>
/// Every <c>using</c> sits above the namespace declaration on purpose: inside
/// <c>Enigma.MobileTasks.Android</c>, an inline <c>Android.…</c> name would resolve against this very
/// namespace. At file scope it resolves in the global namespace. For the same reason the shared
/// application class is written <c>App.App</c>: from here, <c>App</c> alone is the
/// <c>Enigma.MobileTasks.App</c> namespace.
/// </para>
/// </remarks>
[Application]
public class MainApplication : AvaloniaAndroidApplication<App.App>
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="javaReference">The handle of the Java object this instance wraps.</param>
    /// <param name="transfer">How ownership of <paramref name="javaReference"/> is transferred.</param>
    /// <remarks>The constructor the Android runtime calls; never called from managed code.</remarks>
    protected MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    /// <summary>
    /// Adds the Inter font the Enigma theme is designed around to Avalonia's set-up.
    /// </summary>
    /// <param name="builder">The builder Avalonia prepared for Android.</param>
    /// <returns>The builder.</returns>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
