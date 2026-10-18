**Status:** DONE
**Type:** FEATURE
**Branch:** feature/feature-6f1b-release-1-1-0
**Run:** bugfix/2026-10-18-checkbox-rename-release

# FEATURE-6F1B — Release 1.1.0 and signed APK guide

## Objective

Cut **1.1.0** — the checkbox fix (BUG-7F85) and the new name (FEATURE-0CA0) — and document in
`docs/` exactly how to build a **release `.apk` signed with your own key**, every command written
out and verified on this machine.

## Context & constraints

- One version for the solution: `<Version>` in `Directory.Build.props` (1.0.0), which the Android
  head uses as `ApplicationDisplayVersion` (versionName). `ApplicationVersion` (versionCode) is a
  hand-written `1`.
- 1.0.0's Release `.apk` is signed with the SDK's **debug** key. Android refuses to update an app
  with a package signed by a different key (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`), and uninstalling
  deletes the lists — so the first release-signed install needs an export first.
- .NET for Android signs with the MSBuild properties `AndroidKeyStore`, `AndroidSigningKeyStore`,
  `AndroidSigningKeyAlias`, `AndroidSigningStorePass` and `AndroidSigningKeyPass`; the two
  passwords accept `env:<VAR>` and `file:<path>` so they never appear on a command line.
- `RELEASENOTES.md` exists and is empty; the repository has no tags and no remote. This is a
  non-packable app: the dotnet-release app path applies (no pack/push, no MSI — it is Android).
- `dotnet list package --outdated` (2026-10-18): Avalonia set 12.1.1 → 12.1.4,
  Microsoft.Extensions.Hosting 10.0.12 → 10.0.13, xunit.v3 4.0.1 → 4.0.2,
  Microsoft.Testing.Extensions.CodeCoverage 18.11.2 → 18.12.0.

## Steps

1. `Directory.Build.props`: `<Version>` → `1.1.0`.
2. Android head: derive versionCode from `Version` — `major × 10000 + minor × 100 + patch`
   (1.1.0 → 10100, above 1.0.0's `1`) — so a release bumps one number.
3. Dependency refresh, non-coupled only: Hosting 10.0.13, xunit.v3 4.0.2, CodeCoverage 18.12.0. Hold
   the coupled Avalonia set at 12.1.1 and record it.
4. `.gitignore`: ignore `*.keystore` and `*.jks`, so a signing key can never be committed by accident.
5. `docs/RELEASE.md` — the release runbook, adapted to this app: pre-flight, version bump, the
   one-time keystore (`keytool`) and why to back it up, the signed `dotnet publish` with `env:`
   passwords, where the `.apk` lands, verifying it (`apksigner verify --print-certs`, the version
   and label via `aapt2 dump badging`), installing or updating it (`adb install -r`), the
   debug-to-release key switch, the tag, and the errors you hit when a password or alias is wrong.
6. `RELEASENOTES.md` — a 1.1.0 section (subsequent-release variant) over a short 1.0.0 baseline.
7. `README.md` — a what's-new callout for 1.1, and the build section pointing at `docs/RELEASE.md`
   for a signed release.
8. Verify the documented commands for real with a throwaway keystore in the scratchpad: build,
   `apksigner verify`, `aapt2 dump badging` (label `Enigma tasks`, versionName 1.1.0, versionCode
   10100); install on the emulator if one starts.
9. Print the runbook (merge, tag `1.1.0`) — never run it.

## Acceptance criteria

- Version 1.1.0 everywhere it shows: the assemblies, versionName, the About line; versionCode 10100.
- `docs/RELEASE.md` takes a reader from nothing to a signed, verified, installed release `.apk`, with
  every command copy-pasteable, and each one run successfully during this dev.
- The signed `.apk` verifies with `apksigner` against the release certificate, not the debug one.
- `RELEASENOTES.md` describes 1.1.0; the README points at the runbook.
- No keystore, password or `.apk` is committed; `*.keystore` / `*.jks` are ignored.
- Solution build at zero warnings (Debug and Release), the whole suite passes, and the Android head
  builds at zero warnings.

## Out of scope

- Publishing to a store (`.aab`, Play App Signing), CI signing, tagging or pushing (printed only).
- The Avalonia 12.1.4 bump (a coupled set — its own decision).
- A Windows MSI profile.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where do the signing settings live? | On the command line, documented; passwords through `env:` | No secret or machine path in the repository; nothing changes for a plain Release build | Properties in the csproj (machine paths, tempting to add passwords); an untracked local `.props` (more machinery for one command) |
| `dotnet build` or `dotnet publish`? | `dotnet publish -c Release` | The documented distribution route; output in its own `publish/` folder | `dotnet build -c Release` (fine, but mixes the release artefact with build output) |
| versionCode | Derived from `Version` (major×10000 + minor×100 + patch) | One number to bump; it can never be forgotten | Hand-bumped `2` (one more manual step every release) |
| Runbook location | `docs/RELEASE.md` | The house runbook name (dotnet-release), next to the other docs | `docs/building-a-signed-apk.md` (a second naming scheme) |
| Release-notes shape | 1.1.0 section over a short 1.0.0 baseline | The file is empty; the baseline gives 1.1.0 something to be relative to | 1.1.0 only |
| Dependency refresh | Non-coupled patch bumps; Avalonia set held back | House rule: coupled ecosystems move together, on their own decision | Bumping Avalonia to 12.1.4 inside the release |
| Keystore type and key | PKCS12 (keytool's default), RSA 4096, 10 000 days, one password for store and key | PKCS12 uses one password for both anyway; Android needs a validity past 2033 for store use | JKS (deprecated by keytool) |
| Tag | Printed `1.1.0`, bare | No tags exist; the house default; the run never tags | `v1.1.0` |
| MSI profile | None | Android application | — |
