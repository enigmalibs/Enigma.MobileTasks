# FEATURE-3478-PHASE08 — Settings page and README

## Summary

- **Settings page** (the gear on the Lists page): the theme — System (follow the phone's dark mode),
  Light or Dark — saved in the settings and applied live, plus a storage card (how many lists, where
  they live, how to move them off the phone, a warning that uninstalling deletes them) and an About
  card (version, format compatibility). Radio buttons use the Enigma accent through Fluent's
  RadioButton keys.
- **Live dark-mode following restored (plan step 1a, the gap PHASE04 left).** `App.ApplyTheme(System)`
  requests Avalonia's `ThemeVariant.Default` again, which is what Android drives when the phone
  switches mode. It requests the platform's variant explicitly only when `Default` leaves the
  application with no actual Light/Dark variant, as the headless platform does after Dark → Default.
- **README** — what the app is, how to use it, how to move lists between the phone, Enigma.Tasks and
  another phone, the file format, building (`-t:Run`, the Release `.apk`, why a plain Debug build is
  not installable), testing and the project layout.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.App/ViewModels/Pages/SettingsPageViewModel.cs`,
  `Views/Pages/SettingsPage.axaml(.cs)`
- `tests/Enigma.MobileTasks.App.UnitTests/SettingsPageViewModelTests.cs`

Modified:
- `App.axaml.cs` (`ApplyTheme`: Default + fallback), `App.axaml` (comment), `Navigation/{ShellPage,ContainerPageFactory}.cs`,
  `DependencyInjection/ServiceCollectionExtensions.cs`, `ViewModels/Pages/ListsPageViewModel.cs`
  (`SettingsCommand`), `Views/Pages/ListsPage.axaml` (gear), `Themes/{Styles,Theme}.axaml`,
  `Resources/Strings.cs`, `tests/…/ThemeTests.cs`.
- `README.md` — written (it held only the title).
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (PHASE08 and the item → `DONE`).

## Deviations & follow-ups

- Step 1a (added to this phase by PHASE04's build) is done and verified on the device; see the
  evidence below.
- Fix cycle 1: the new live-theme test waited for "anything but Light" after choosing System, which
  never ends when the headless fallback lands on Light; it now runs the queued dispatcher jobs and
  asserts the invariant (Light or Dark actual, Default or the platform's variant requested).
- The storage summary says "1 lists" for a single list — kept as the one-format string; a plural
  form is a wording follow-up.
- `RELEASENOTES.md` stays the empty placeholder the bootstrap created; release notes belong to a
  release item (dotnet-release).
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How is System applied? | `ThemeVariant.Default`, falling back to the platform's variant only when no actual variant resolves | Android follows a live switch only through Default; the fallback keeps a headless or unresolvable platform from painting blank | Explicit variant always (PHASE04's cycle 3 — loses live following); Default always (blank screen on such platforms) |
| How does a choice reach the screen? | The page writes the settings; `App.ObserveTheme` applies on `Changed` | One path for live switching and for the next start | The page setting `Application.RequestedThemeVariant` itself (two writers) |
| Where does the version come from? | The assembly version (`Directory.Build.props` `<Version>`) | One number for the whole solution | A hand-written constant |
| Settings entry point | Gear in the Lists page's navigation bar | Settings are rare; the bar already holds the page's actions | A tab bar (one more always-visible element for a rare page) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 206 passed, 0 failed (7 new Settings tests:
  default, choose and save, unchecking does nothing, live repaint through `ObserveTheme`, storage
  count, version line, gear navigation; `ThemeTests` System test rewritten for Default + fallback).
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors.
- On-device (emulator, Release `.apk`, background pixel sampled):
  - Under System, switching the phone to night mode turned the open app dark (#1E1F22), live.
  - The Settings page showed 2 lists and "Enigma.MobileTasks 1.0.0".
  - Dark pinned, phone switched to light: stayed dark.
  - System chosen, phone light: turned light (#F7F8FA), with no blank screen.
  - After a restart under System with the phone in night mode: started dark.
- Documentation sweep: `README.md` written in this dev (it was only a title); no other prose docs
  exist.
