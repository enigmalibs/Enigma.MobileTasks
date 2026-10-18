# FEATURE-0CA0 — Rename the app to "Enigma tasks"

## Summary

- The launcher now shows the app as **Enigma tasks** (it showed "Lists"). The name is written where
  Android reads it — `ApplicationTitle` in the Android head's csproj, `Label` on `MainActivity` (the
  launcher entry), `android:label` in `Properties/AndroidManifest.xml` — each commented to change
  with the others.
- In the app, `Strings.AppName` is `Enigma tasks` and the Settings page's About line is built from it:
  `Enigma tasks 1.0.0` (1.1.0 from FEATURE-6F1B).
- The `ApplicationId` stays `io.github.enigmalibs.mobiletasks`, now with the reason in the csproj:
  it is the installed app's identity and the key to its private storage, so the rename updates the
  app in place and the lists survive.
- The version-line test asserts the new name and compares with the solution's version read from the
  test assembly, so a release no longer has to edit it.

## Files/modules touched

Modified:
- `src/Enigma.MobileTasks.Android/Enigma.MobileTasks.Android.csproj`, `MainActivity.cs`,
  `Properties/AndroidManifest.xml`
- `src/Enigma.MobileTasks.App/Resources/Strings.cs` (`AppName`, `VersionLine`)
- `tests/Enigma.MobileTasks.App.UnitTests/SettingsPageViewModelTests.cs` (the version-line test,
  renamed `TheVersionLine_IsTheAppsNameAndTheSolutionsVersion`)
- `docs/roadmap.md`, `docs/plan/FEATURE-0CA0.md` (→ `DONE`)

## Deviations & follow-ups

- None from the plan.
- The first page's header stays "Lists" — it names the page, not the app.
- Launchers truncate long names on narrow grids; "Enigma tasks" (12 characters) fits most, and may
  show as "Enigma tas…" on the narrowest.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does the About line get the name? | `VersionLine = AppName + " {0}"` | One constant feeds both; a constant expression keeps it a `const` | A second literal "Enigma tasks {0}" |
| What does the version test compare with? | The test assembly's version | Same `Directory.Build.props`, so it is the solution's version without being the code under test | A hard-coded `1.1.0` (breaks every release) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 208 passed, 0 failed.
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors.
- `aapt2 dump badging …-Signed.apk`: `package: name='io.github.enigmalibs.mobiletasks'`,
  `application-label:'Enigma tasks'`, `launchable-activity … label='Enigma tasks'`.
- Documentation sweep: nothing stale — no prose doc names the launcher label; the README's
  what's-new callout for the release is FEATURE-6F1B's.
