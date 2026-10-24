# FEATURE-02BA — Release 1.2.0

## Summary

- **Version 1.2.0** (`Directory.Build.props`). The assemblies, Android's versionName and the About
  line read 1.2.0. The Android head derives **versionCode 10200**, so 1.2.0 updates 1.1.0 (10100) in
  place when signed with the same key.
- **`RELEASENOTES.md`** gets a 1.2.0 section above 1.1.0's. *New Features* covers the quick-add **+**
  that keeps the cursor (FEATURE-055A), the trash button on every item row (FEATURE-9AEF) and the
  shorter item menu. *Dependencies* records no changes, with the Avalonia set held at 12.1.1.
  *Version* gives 1.2.0 (10200) and notes the in-place update.
- **`README.md`**: the what's-new callout now names 1.2.
- **Dependencies**: `dotnet list package --outdated` showed only the coupled Avalonia set
  (12.1.1 → 12.1.4), which is held back as one set. Nothing else was outdated, so
  `Directory.Packages.props` is unchanged.

## Files/modules touched

Created:
- `docs/done/FEATURE-02BA.md`

Modified:
- `Directory.Build.props` (`<Version>` 1.1.0 → 1.2.0)
- `RELEASENOTES.md`, `README.md`
- `docs/roadmap.md`, `docs/plan/FEATURE-02BA.md` (→ `DONE`)

## Deviations & follow-ups

- **Not done by the run: merging into `develop` and `main`, and tagging.** The draft asked for both
  merges. A `/vibe` run never writes to the default branch and never switches to a branch that is not
  its own, so the end-of-run report gives the exact commands instead.
- **The Android head's Release build shows one environment warning (XA0033)**, where 1.1.0 recorded
  none. The .NET Android SDK 36.1.118 cannot parse this machine's JDK version string
  `21.0.13.0.2` (five components; JDK build dated 2026-10-22, after 1.1.0's build on 2026-10-18). It
  is the only warning, the build succeeds, and the `.apk`'s badging is correct. It is not suppressed
  in the project, because that would hide a real toolchain message on every machine. It goes away
  with a JDK whose version has at most four components, or with an Android SDK that parses this one.
- **1.1.0 was never tagged.** The report gives an optional `git tag 1.1.0 a708965` (the 1.1.0 merge
  on `main`).
- Follow-up, unchanged: the Avalonia 12.1.4 set is a work item of its own. Check
  `RowAlignmentRenderTests` first when it moves.
- `docs/RELEASE.md` still uses 1.1.0 for its worked examples. They remain accurate, so they were not
  touched.
- Line endings: LF throughout.

## Dependencies

| Package | Old → new | Notes |
|---|---|---|
| `Avalonia`, `Avalonia.Android`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Headless`, `Avalonia.Skia` | held at 12.1.1 (12.1.4 available) | coupled set, with `CommunityToolkit.Mvvm` and `Enigma.Icons.Avalonia` |
| everything else | unchanged | already latest (2026-10-24) |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The XA0033 warning on the Android head | Recorded as an environment follow-up, not suppressed | It comes from the local JDK's version string, not the project; `NoWarn` would hide it from every machine | `<NoWarn>XA0033</NoWarn>` in the csproj; pinning `JAVA_HOME` to the unsupported JDK 27 |
| What happens to the verification `.apk`? | The head's `bin`/`obj` deleted after checking | A debug-signed 1.2.0 `.apk` lying in `bin/` is the wrong file to install; the runbook starts clean anyway | Leaving it in the build output |
| Release-notes wording | Written for someone using the phone (what changed when you tap), not by work-item ID | Matches the 1.1.0 section's voice | Listing FEATURE IDs |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx -c Release --no-incremental`: 0 warnings, 0 errors. The
  same in Debug.
- `dotnet test --solution Enigma.MobileTasks.slnx -c Release`: 214 passed, 0 failed. Same in Debug.
- The built `Enigma.MobileTasks.App.dll` carries version `1.2.0.0`. The About line formats it as
  `1.2.0` (`Assembly.GetName().Version.ToString(3)`).
- `dotnet build src/Enigma.MobileTasks.Android -c Release` from a clean `bin`/`obj`: 0 errors, and the
  single XA0033 environment warning above.
  - `aapt2 dump badging` → `package: name='io.github.enigmalibs.mobiletasks' versionCode='10200'
    versionName='1.2.0'`, `application-label:'Enigma tasks'`.
  - `apksigner verify --print-certs` → `CN=Android Debug, O=Android, C=US`. This is as expected for a
    plain build; the release key is applied by `docs/RELEASE.md` step 3.
- Documentation sweep: `README.md` (the callout). Nothing else stale: `docs/RELEASE.md`'s 1.1.0
  examples are still true, and the test count (214) is current.
