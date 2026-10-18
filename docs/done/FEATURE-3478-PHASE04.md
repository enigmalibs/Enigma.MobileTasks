# FEATURE-3478-PHASE04 — App shell, theme and Android head

## Summary

The application exists and runs on Android. `Enigma.MobileTasks.App` (plain `net10.0`) holds the whole
UI: `App` and its IHost composition root, the Enigma palette, the shell, its services and a first
Lists page. `Enigma.MobileTasks.Android` (`net10.0-android`, API 24+) is platform glue only. On a
Pixel 6 / API 36 emulator the trimmed Release `.apk` starts on the Lists page, edge to edge, in the
Enigma Light and Dark variants.

- **Theme** — Enigma.Avalonia.Desktop 1.2.0's `Colors.axaml` and `Brushes.axaml`, vendored verbatim
  (provenance note on top), merged through `Themes/Theme.axaml`; `PaletteParityTests` loads the
  1.2.0 package's own dictionaries and asserts every colour key and every brush's painted colour
  match, in Dark and Light. App styles (`Themes/Styles.axaml`) use 48 dp touch targets and re-colour
  Fluent's navigation bar with the Enigma surface and foreground.
- **Shell** — `ShellView`: a `PageNavigationHost` hosting `ShellNavigationPage` (a `NavigationPage`
  whose system back button closes an open dialog first), a snackbar and a dialog overlay, both inset
  by the safe area. Services: `IActiveListService` (lists, open list, debounced saves, `Touched`),
  `IDialogService` (confirm / prompt overlay, one at a time, cancelled by back),
  `INotificationService` (snackbar with optional action, auto-dismiss), `INavigationService` +
  `IPageFactory` (transient views, singleton ViewModels, so a re-created activity rebinds its state).
- **Lifetime** — settings are loaded and the theme applied before the first view; the lists load once
  per process when the first shell exists, with unreadable files reported; pending saves flush when
  the application is deactivated to the background.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.App/` — `Enigma.MobileTasks.App.csproj`, `App.axaml(.cs)`,
  `Themes/{Colors,Brushes}.axaml` (vendored), `Themes/Theme.axaml`, `Themes/Styles.axaml`,
  `DependencyInjection/ServiceCollectionExtensions.cs`, `Resources/Strings.cs`,
  `Services/{IActiveListService,ActiveListService,IDialogService,DialogService,INotificationService,NotificationService}.cs`,
  `Navigation/{ShellPage,IPageFactory,ContainerPageFactory,INavigationService,NavigationService,ShellNavigationPage}.cs`,
  `ViewModels/ShellViewModel.cs`, `ViewModels/Pages/{ListsPageViewModel,ListCardViewModel}.cs`,
  `Views/ShellView.axaml(.cs)`, `Views/Pages/ListsPage.axaml(.cs)`.
- `src/Enigma.MobileTasks.Android/` — csproj, `MainApplication.cs`, `MainActivity.cs`,
  `Properties/AndroidManifest.xml`, `Resources/values{,-night}/colors.xml`, `Resources/values/styles.xml`,
  `Resources/drawable/icon.png` (Enigma.Tasks' 256 px launcher art).
- `tests/Enigma.MobileTasks.App.UnitTests/` — csproj, `Infrastructure/{HeadlessAvaloniaFixture,TempApp,TestLists}.cs`,
  `Fixtures/liste-judith.json`, `CompositionRootTests`, `DialogServiceTests`, `NotificationServiceTests`,
  `ActiveListServiceTests`, `ShellViewModelTests`, `ListsPageViewModelTests`, `ShellRenderTests`,
  `PaletteParityTests`, `ThemeTests`.

Modified:
- `Directory.Packages.props` — Hosting, the Avalonia 12.1.1 set (+ Android, Headless, Skia),
  CommunityToolkit.Mvvm, Enigma.Icons.Avalonia, Enigma.Avalonia.Desktop 1.2.0 (test-only).
- `Enigma.MobileTasks.slnx` — App, Android head (`<Build Project="false" />`), App tests.
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses; PHASE08 step 1a added, see below).

## Deviations & follow-ups

- **Known gap, scheduled in PHASE08 (step 1a): System no longer follows a live dark-mode switch.**
  Fix cycle 3 made `App.ApplyTheme(System)` resolve to an explicit Light/Dark from
  `Application.PlatformSettings`, because headless Avalonia left the application with *no* actual
  variant after Dark → `Default`, and every Enigma brush then painted transparent. On the emulator,
  start-up picks the phone's variant correctly, but Android does not raise
  `Application.PlatformSettings.ColorValuesChanged`, so switching the phone to dark mode while the app
  is open is not followed (it was, with `Default`). The budget allowed no fourth cycle on this dev;
  PHASE08 owns theme behaviour and carries the fix.
- **Edge to edge and safe areas** (not in the plan, found on the emulator): the shell sets
  `TopLevel.AutoSafeAreaPadding="False"` and `DisplayEdgeToEdgePreference = true`; with the top
  level's automatic padding on, the whole shell was inset *and* the navigation bar added the status-bar
  inset again — a 97 dp bar under an empty strip.
- **Navigation bar colours** are set by reaching into Fluent's template (`PART_NavigationBar`, the
  header's generated `TextBlock`): the bar's grey is a template value, so overriding Fluent's
  `NavigationControlNavBarBackground` key had no effect. `NavigationBar_UsesTheEnigmaColours` guards it.
- Fix cycles: (1) `ShellViewModel.InitializeAsync` now returns the first call's task to every caller,
  so a second shell waits for the lists instead of returning early; (2) the render test reads the
  frame's real byte order (`Rgba8888` on headless Skia); (3) the theme change above.
- A Debug `dotnet build` uses Fast Deployment, so its `.apk` alone carries no assemblies and aborts on
  launch; install with `-t:Install`/`-t:Run`, or use the Release `.apk` (README, PHASE08).
- Line endings: LF throughout; the PNG is binary per `.gitattributes`.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does a dialog take the back button? | `ShellNavigationPage` overrides `OnSystemBackButtonPressed` and asks `IDialogService.TryCancel` first | The overlay is drawn over the stack, not pushed on it | Modal pages per prompt (heavy); a `TopLevel.BackRequested` handler (ordering against the host's) |
| Where do the shell overlays live? | A `Panel` over the `PageNavigationHost` in `ShellView`, margin = safe area | One place for every page; testable headlessly | Per-page overlays |
| Pages from the container? | `IPageFactory` resolving transient views + singleton ViewModels | Survives activity re-creation; one place that knows the views | ViewModel-first `DataTemplate` locator (pages are `Page`s, pushed by instance) |
| Navigation bar theming | Template part styles, not resource keys | The grey is a template value | Living with Fluent's grey |
| Brushes per window in tests? | The parity test hosts each pair of dictionaries in its own window | Only a themed consumer resolves a `DynamicResource` colour | Comparing XAML text (the package ships compiled XAML only) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors (AVLN count included).
- `dotnet test --solution Enigma.MobileTasks.slnx` — 155 passed, 0 failed (two consecutive runs);
  42 new UI tests: palette parity (colours and brushes × 2 variants), navigation bar colours ×
  2 variants, shell render (root page, background pixel, dialog scrim), composition root and
  lifetimes, dialog / notification / active-list services, shell start-up reporting, Lists page cards,
  theme application.
- `dotnet build src/Enigma.MobileTasks.Android` (Debug) and `-c Release` — 0 warnings, 0 errors;
  Release `.apk` 44.7 MB, trimmed.
- On-device: Release `.apk` installed on the `Pixel_6_API_36` emulator (booted `-read-only`, so the
  AVD itself is unchanged): starts on the Lists page with the empty state, Enigma Light background,
  surface-coloured navigation bar drawn edge to edge behind the status bar, Enigma foreground title.
- Documentation sweep: `README.md` holds only the title — nothing made stale (the README is written in
  PHASE08).
