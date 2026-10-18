# FEATURE-3478-PHASE03 — Device storage and settings

## Summary

The phone now has somewhere to keep lists. The workspace — one `<slug>.json` file per list, written
atomically, saved by a debounced scheduler — is ported from Enigma.Tasks, with the changes a phone
needs: the folder is fixed in app-private storage (`LocalApplicationData/Enigma.MobileTasks/lists`),
and import/export take **streams**, because what Android's file picker hands back is a content URI,
never a path. Exports write exactly the serializer's bytes (UTF-8, no BOM); imports read a stream
(BOM tolerated, 4 MB bound), validate it, and add the list as a new file. Settings keep the theme
(default **System**) and the folded categories per list, through a source-generated JSON context so
they survive Android's trimmed Release builds.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.Core/Workspace/` — `AtomicFile`, `ListFileName`, `SaveScheduler`,
  `WorkspaceEntry`, `WorkspaceLoadResult`, `WorkspaceProblem`, `WorkspaceSaveFailedEventArgs` (ported);
  `IWorkspaceStore`, `WorkspaceStore` (adapted: stream import/export, JSON only, size bound).
- `src/Enigma.MobileTasks.Core/Settings/` — `ISettingsService`, `SettingsChangedEventArgs`,
  `ThemePreference`, `SettingsService` (ported, adapted); `AppPaths`, `AppSettings`,
  `SettingsLocation` (phone shape); `SettingsJsonContext` (new, source-generated).
- `tests/Enigma.MobileTasks.Core.UnitTests/` — `WorkspaceStoreTests`, `SaveSchedulerTests`,
  `ListFileNameTests`, `SettingsServiceTests`, `Infrastructure/TempWorkspace`,
  `Infrastructure/RecordingWorkspaceStore`.

Modified:
- `src/Enigma.MobileTasks.Core/Enigma.MobileTasks.Core.csproj` — `IsTrimmable` (trim analyzers on).
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- **Settings serializer is source-generated** (not in the plan): an Android Release build is trimmed,
  and trimming turns reflection-based `System.Text.Json` off, so the desktop's
  `JsonSerializer.Deserialize<AppSettings>(json, options)` would compile, pass every test here and
  throw on the phone. `IsTrimmable` on Core makes the trim analyzer guard this at build time.
- Fix cycle 1: the source generator assigns every `init` property, so a partial settings file left
  `CollapsedCategories` `null` instead of empty. `SettingsService.Normalize` puts the default back;
  `LoadAsync_WithAPartialFile_KeepsTheDefaultsForTheRest` covers it.
- New refusal code `file-too-large` (import bound of 4 MB) — outside the web app's vocabulary, which
  never reads from a picker; only the phone produces it.
- `IWorkspaceStore` dropped the desktop's path-based `ImportAsync(string)` and the Markdown format;
  `ImportAsync(Stream)` and `ExportAsync(TaskList, Stream)` replace them.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Settings serialization under trimming? | `SettingsJsonContext` (source-generated) + `IsTrimmable` on Core | Reflection-based STJ is disabled in trimmed apps | Disabling trimming (bigger APK, hides the issue); `DynamicDependency` annotations (fragile) |
| Guard against huge picked files? | 4 MB read bound, `file-too-large` refusal | "All files" lets a user pick a video; reading it whole would crash the app | No bound |
| Workspace folder on the phone? | `LocalApplicationData/Enigma.MobileTasks/lists`, carried in `SettingsLocation.WorkspacePath` | Private, permission-free, removed with the app; injectable for tests | `ApplicationData` (no difference on Android, `.config` on Linux dev) |
| Same list imported twice? | Kept as two files (`-2` suffix) | The desktop's behaviour; an import never overwrites | Replacing the existing file (data loss) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 113 passed, 0 failed (45 new: store create /
  load / save / import / export / duplicate / rename / delete, BOM, size bound, desktop file
  round-trip; scheduler debounce and flush; file names; settings round trip, partial file,
  unreadable file, defaults).
- Documentation sweep: `README.md` holds only the title — nothing made stale.
