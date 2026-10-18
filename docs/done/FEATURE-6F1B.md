# FEATURE-6F1B — Release 1.1.0 and signed APK guide

## Summary

- **Version 1.1.0** (`Directory.Build.props`) — the assemblies, Android's versionName and the About
  line all read 1.1.0.
- **versionCode is derived from the version** in the Android head: `major × 10000 + minor × 100 +
  patch`, so 1.1.0 is **10100** (1.0.0 shipped as 1). A release now bumps one number.
- **`docs/RELEASE.md`** — the release runbook, adapted from dotnet-release's template to this app:
  the toolchain; creating the release key once with `keytool` and why to back it up; preparing the
  release (version, notes, callout, Release pre-flight, merge); the signed `dotnet publish` from a
  clean `bin`/`obj`, passwords through `env:`, and what each signing property does; checking the
  `.apk` with `apksigner` (signer, fingerprint) and `aapt2` (package, versionCode, versionName,
  label); installing and updating with `adb install -r`; the one-time debug-key → release-key switch
  (export, uninstall, install, import); tagging; and a table of the real error messages.
- **`RELEASENOTES.md`** — a 1.1.0 section (new name, signed releases, the versionCode, the checkbox
  fix, the 1.0.0 migration, dependencies) over a 1.0.0 baseline.
- **`README.md`** — the 1.1 what's-new callout and the app's phone name; the build section separates
  the debug-key Release build (to try a build) from a release (→ `docs/RELEASE.md`); the layout lists
  the runbook.
- **`.gitignore`** — `*.keystore` and `*.jks`.
- **Dependencies** — the non-coupled bumps; the Avalonia set held back (see below).

## Files/modules touched

Created:
- `docs/RELEASE.md`

Modified:
- `Directory.Build.props` (`<Version>` 1.1.0), `Directory.Packages.props` (three bumps)
- `src/Enigma.MobileTasks.Android/Enigma.MobileTasks.Android.csproj` (versionCode formula)
- `RELEASENOTES.md` (was empty), `README.md`, `.gitignore`
- `docs/roadmap.md`, `docs/plan/FEATURE-6F1B.md` (→ `DONE`)

## Deviations & follow-ups

- **Two pitfalls found while verifying, now in the runbook.** (1) When signing fails, the SDK
  leaves a `-Signed.apk` that is *not* signed (`apksigner`: `DOES NOT VERIFY`,
  `Missing META-INF/MANIFEST.MF`). The next `dotnet publish` takes it as up to date, exits 0 and
  copies it to `publish/`. (2) `dotnet clean` leaves `publish/` in place. So step 3 starts by deleting
  the head's `bin` and `obj`, and step 4's `apksigner verify` is mandatory.
- **`apksigner` runs the `java` on `PATH`** (JDK 27 on this machine), not `JAVA_HOME`'s, and prints
  four harmless `WARNING: A restricted method …` lines first. The runbook says so.
- **`README.md` said "206 tests"** — stale since BUG-7F85 added two; corrected here to 208 (that
  dev's sweep missed it).
- **The repository has no remote and no tags.** The tag step is printed for the user (bare `1.1.0`),
  never run.
- Follow-up: the Avalonia set (12.1.1 → 12.1.4 available) is its own change. When it moves, check
  `RowAlignmentRenderTests` first (BUG-7F85's fix depends on Fluent's template shape).
- Line endings: LF throughout.

## Dependencies

| Package | Old → new | Notes |
|---|---|---|
| `Microsoft.Extensions.Hosting` | 10.0.12 → 10.0.13 | runtime |
| `xunit.v3` | 4.0.1 → 4.0.2 | tests only |
| `Microsoft.Testing.Extensions.CodeCoverage` | 18.11.2 → 18.12.0 | tests only |
| `Avalonia`, `Avalonia.Android`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Headless`, `Avalonia.Skia` | held at 12.1.1 (12.1.4 available) | coupled set, with `CommunityToolkit.Mvvm` and `Enigma.Icons.Avalonia` |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does a release build start clean? | `rm -rf` the head's `bin` and `obj` | The only step that removes both the stale unsigned `-Signed.apk` and `publish/` | `dotnet clean` (leaves `publish/`, and with it a previous release) |
| How is the password entered? | `read -rsp` into an exported variable, `unset` afterwards | No echo, nothing in history; `env:` keeps it out of the command line and the log (checked: 0 occurrences even at `-v:diag`) | `-p:AndroidSigningStorePass=<password>` (history, logs); a password file in the repo |
| The versionCode expression | Three private `_Version*` properties, then `Add`/`Multiply` | Readable, one parse per component; MSBuild's integer arithmetic gives `10100`, not `10100.0` (checked with `aapt2`) | One nested expression |
| How is the 1.0.0 → 1.1.0 install proven without losing the emulator's lists? | Emulator restarted with `-read-only` (data image opened read-only, writes to `/tmp` overlays) | The real update failure and the fresh install both ran; the AVD's data image kept its pre-session timestamp | Uninstalling on the normal emulator (deletes the previous run's test lists) |
| What happens to the throwaway key and its `.apk`? | Deleted after verification, with the head's `bin`/`obj` | An `.apk` signed with a key whose password is in a transcript must not be installable by mistake | Leaving them in the scratchpad and the build output |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx --no-incremental`, Debug and `-c Release` — 0 warnings,
  0 errors each.
- `dotnet test --solution Enigma.MobileTasks.slnx`, Debug and `-c Release` — 208 passed, 0 failed
  each.
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors. Signer
  `CN=Android Debug, O=Android, C=US`, versionCode 10100: the plain build is unchanged, as the
  runbook says.
- **The runbook's commands, run for real** with a throwaway PKCS12 key in the scratchpad:
  - `keytool -genkeypair … -keyalg RSA -keysize 4096 -validity 10000` → PKCS12 keystore.
    `keytool -list -v … | grep SHA256` printed the fingerprint.
  - From a clean `bin`/`obj`, the signed `dotnet publish` (`env:` passwords) → exit 0, 0 warnings,
    `publish/io.github.enigmalibs.mobiletasks-Signed.apk` (45 MB).
  - `apksigner verify --verbose --print-certs` → `Verifies`, v2 and v3 true, signer `CN=Test…`,
    SHA-256 digest equal to the keystore's fingerprint.
  - `aapt2 dump badging` → `package: name='io.github.enigmalibs.mobiletasks' versionCode='10100'
    versionName='1.1.0'`, `application-label:'Enigma tasks'`.
  - Error messages produced on purpose for the runbook's table: wrong password
    (`keystore password was incorrect`), unset variable
    (`Failed to read Keystore password for signer #1: environment variable null not specified`),
    wrong alias (`entry "wrong-alias" does not contain a key`), each behind
    `error MSB6006: "java" exited with code 2`. The unsigned leftover gave `DOES NOT VERIFY`.
- **On the emulator** (Pixel 6, API 36, read-only session, 1.0.0 debug-signed installed):
  - `adb install -r` of the 1.1.0 release `.apk` → `INSTALL_FAILED_UPDATE_INCOMPATIBLE: Existing
    package io.github.enigmalibs.mobiletasks signatures do not match newer version`.
  - After `adb uninstall` → `Success`, `versionCode=10100 versionName=1.1.0`. A second
    `adb install -r` of the same `.apk` → `Success` (an in-place update with the same key).
  - Launched: the Lists page opens. Settings → About reads **Enigma tasks 1.1.0**, and the theme
    radio circles sit level with their labels (BUG-7F85, on a device).
- Documentation sweep (applied): `README.md` (callout, phone name, build section, test count 206 →
  208, layout line). No other prose docs exist.
