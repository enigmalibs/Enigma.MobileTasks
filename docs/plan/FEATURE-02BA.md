**Status:** TODO
**Type:** FEATURE
**Branch:** feature/feature-02ba-release-1-2-0
**Run:** feature/2026-10-24-release-1-2-0

# FEATURE-02BA — Release 1.2.0

## Objective

Cut **1.2.0**: the quick-add box that keeps its focus after **+** (FEATURE-055A) and the trash button
on every item row (FEATURE-9AEF). Leave the release ready to merge into `develop` and then `main`, to
tag, and to build as a signed `.apk` with `docs/RELEASE.md`.

## Context & constraints

- One version for the solution: `<Version>` in `Directory.Build.props` (1.1.0). The Android head
  derives versionName from it, and versionCode as `major × 10000 + minor × 100 + patch`, so 1.2.0 is
  **10200**.
- A non-packable Android app. The dotnet-release *routine release* path applies: version, release
  notes, README what's-new callout, dependency refresh, printed runbook. No NuGet pack/push, no MSI.
- `dotnet list package --outdated` (2026-10-24) shows only the coupled Avalonia set:
  `Avalonia`, `Avalonia.Android`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`,
  `Avalonia.Headless`, `Avalonia.Skia` 12.1.1 → 12.1.4. Nothing else is outdated.
- The run starts from `feature/2026-10-24-quickadd-focus-trash` (both features merged). `develop` and
  `main` are both at `a708965` (1.1.0). The repository has no tags (1.1.0 was never tagged) and no
  remote.
- A `/vibe` run never writes to the default branch (`main`) and never switches to a branch that is not
  its own (`develop`). It never tags and never pushes.

## Steps

1. `Directory.Build.props`: `<Version>` → `1.2.0`.
2. Dependency refresh: none to apply. Hold the Avalonia set at 12.1.1 and record it.
3. `RELEASENOTES.md`: prepend a `v1.2.0` section in the 1.1.0 section's style (New Features,
   Dependencies, Version).
4. `README.md`: the what's-new callout for 1.2.
5. Pre-flight in Release: solution build at zero warnings, whole suite green.
6. Android head: Release build (debug key) at zero warnings. `aapt2 dump badging` shows
   versionName `1.2.0`, versionCode `10200`, label `Enigma tasks`. Then delete the head's `bin`/`obj`
   so no stale `.apk` is left behind.
7. Hand over in the end-of-run report: the merges into `develop` and `main`, the `1.2.0` tag, and the
   signed-`.apk` commands.

## Acceptance criteria

- Version 1.2.0 everywhere it shows: the assemblies, versionName, the About line; versionCode 10200.
- `RELEASENOTES.md` opens with a 1.2.0 section describing both features and the held-back Avalonia
  set; the README callout names 1.2.
- Solution build at zero warnings and the whole suite green in Release (and Debug). The Android head
  builds in Release at zero warnings with the expected badging.
- Nothing is written to `develop` or `main`, and nothing is tagged or pushed. The report gives the
  exact commands.

## Out of scope

- Merging into `develop`/`main`, tagging, pushing, and the signed `dotnet publish` with the real key.
  These are yours to run; the commands are in the report.
- The Avalonia 12.1.4 bump (a coupled set; its own work item).
- Changes to `docs/RELEASE.md`. Its procedure is unchanged and was verified end to end for 1.1.0.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Who merges into `develop` and `main`? | Not the run. The exact commands go in the end-of-run report | `/vibe` forbids any write to the default branch and any switch to a branch that is not the run's own; the release preparation is allowed, so the run delivers it and hands the merges over | Running the merges (breaks the run's own rules); stopping the run (would throw away the allowed release work) |
| Version | 1.2.0, versionCode 10200 | Two new features, nothing breaking: a minor bump; the formula derives the code | 1.1.1 (features are not a patch) |
| Dependency refresh | None applied; the Avalonia set held at 12.1.1 | House rule: the coupled set moves together on its own decision, and BUG-7F85's row fix depends on Fluent's template | Bumping Avalonia inside the release |
| How is the release verified? | Release build + tests, and an Android Release build checked with `aapt2` | Proves the number reaches the phone; signing is unchanged since 1.1.0's end-to-end check | Repeating the signed publish with a throwaway key (re-verifies an unchanged procedure) |
| Tag | Bare `1.2.0`, printed; plus the missing `1.1.0` tag as an optional command | Matches the runbook and the house default; a run never tags | `v1.2.0` |
| Release-notes shape | 1.2.0 section prepended, non-empty sub-sections only | Same as the 1.1.0 section | A `CHANGELOG.md` (house rule: one release-notes file) |
