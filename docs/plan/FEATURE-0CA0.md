**Status:** DONE
**Type:** FEATURE
**Branch:** feature/feature-0ca0-app-name
**Run:** bugfix/2026-10-18-checkbox-rename-release

# FEATURE-0CA0 — Rename the app to "Enigma tasks"

## Objective

The launcher shows the application as **Enigma tasks** — exactly that, as the user wrote it —
instead of "Lists". The in-app name (the Settings page's About line) says the same.

## Context & constraints

- "Lists" is written in four places: `ApplicationTitle` in
  `src/Enigma.MobileTasks.Android/Enigma.MobileTasks.Android.csproj` (the manifest's application
  label, through the SDK's `_ApplicationLabel`), `Label` on `MainActivity` (the launcher entry),
  `android:label` in `Properties/AndroidManifest.xml`, and `Strings.AppName`.
- The About line is `Strings.VersionLine` = `"Enigma.MobileTasks {0}"`.
- `ApplicationId` `io.github.enigmalibs.mobiletasks` identifies the installed app and its private
  storage: changing it would install a second app and leave the lists behind in the first.
- "Lists" is also the first page's header — the name of the page, not of the app.

## Steps

1. Android head: `ApplicationTitle`, the activity `Label` and the manifest `android:label` →
   `Enigma tasks`. Keep `ApplicationId` unchanged, and say why in the csproj comment.
2. `Strings.AppName` → `"Enigma tasks"`; `Strings.VersionLine` → `AppName + " {0}"`, so the About
   line reads `Enigma tasks 1.x.y`.
3. Tests: the version-line test asserts the new name; it compares with the solution's version (read
   from the test assembly, built from the same `Directory.Build.props`) instead of a hard-coded
   `1.0.0`, so it no longer needs editing on every release.
4. Build the Android head and confirm the packaged manifest's application and launcher labels.

## Acceptance criteria

- The packaged `AndroidManifest.xml` carries `android:label="Enigma tasks"` on the application and
  on the launcher activity; `package` is still `io.github.enigmalibs.mobiletasks`.
- The Settings page's About line reads `Enigma tasks <version>`.
- The Lists page's header is still "Lists".
- Solution build at zero warnings, the whole suite passes, and the Android head builds at zero
  warnings.

## Out of scope

- Renaming the repository, the solution, the projects, the namespaces or the `ApplicationId`.
- A new launcher icon.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Exact spelling | `Enigma tasks` | Verbatim from the request | `Enigma Tasks` (the desktop's casing — not what was asked) |
| Change the `ApplicationId`? | No | It is the app's identity on the phone; a new one is a second app and strands the lists | `io.github.enigmalibs.enigmatasks` |
| Where the name appears in the app | The About line, through `Strings.AppName` | The one place the app names itself; one constant feeds it | Also renaming the "Lists" page header (that is the page's name) |
| One source for the three Android labels? | Keep the three literals, side by side | Matches the existing head; a `strings.xml` resource for one value is more machinery than it saves | `@string/app_name` resource |
