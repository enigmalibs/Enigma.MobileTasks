**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** feature/feature-3478-phaseNN-<slug> (one per phase, listed below)
**Run:** feature/2026-10-18-android-tasks-app

# FEATURE-3478 — Enigma.MobileTasks Android app

## Objective

An Android application, built with Avalonia 12, for the reusable checklists Enigma.Tasks manages on
the desktop: lists of categories holding items that are ticked off and reset for the next time. It
paints with the Enigma.Avalonia.Desktop palette (Dark and Light variants), and it exports and imports
lists as JSON **in exactly the Enigma.Tasks file format**, so that:

- a list exported on the phone imports into the Enigma.Tasks desktop application unchanged;
- a list exported on one phone imports into Enigma.MobileTasks on another phone;
- a list exported by Enigma.Tasks (or the Listes web app it is compatible with) imports on the phone.

## Context & constraints

- **The format contract** is Enigma.Tasks' `ListJsonSerializer` (sibling repository
  `../Enigma.Tasks`): `schemaVersion` 1, property order `schemaVersion, name, categories[id, name,
  items[id, text, done]]`, two-space indentation, `\n` line endings, no trailing newline, UTF-8
  without BOM, non-ASCII written literally. Reading is forgiving (missing `schemaVersion`, `id`,
  `done`; trimmed text; repeated ids re-minted) and refuses a `schemaVersion` greater than 1.
  Identifiers look like `i-mtzxeu79-3-9zk2` (prefix, base-36 time, counter, random tail). Enigma.Tasks
  proves the contract with a byte-identical round trip of `liste-judith.json`.
- `Enigma.Tasks.Core` is app-internal and **not published**; the theme's mobile packaging
  (`Enigma.Avalonia.Mobile`) is **not published** either. `Enigma.Avalonia.Desktop` 1.2.0 is on
  nuget.org but is a desktop control library its own guidance rules out for mobile targets.
- The house Android reference is Enigma.Avalonia's mobile showcase: a plain `net10.0` UI library
  holding `App`, the IHost composition root and the views, plus a `net10.0-android` head holding only
  platform glue (`AvaloniaAndroidApplication<App>`, `AvaloniaMainActivity`, manifest, resources),
  listed in the `.slnx` with `<Build Project="false" />` so the solution builds without the Android
  workload. `SupportedOSPlatformVersion` is 24 (Avalonia.Android 12.1.1's own floor).
- Avalonia 12.1.1 ships the mobile page model: `NavigationPage` (push/pop, modal stack, system back
  button), `ContentPage` (`TopCommandBar`/`BottomCommandBar`), `CommandBar`, `PageNavigationHost`.
- Machine: .NET SDK 10.0.103, `android` workload, Android SDK platform 36, JDK 21; every package below
  is in the local NuGet cache.

## Decisions

- **Projects** — `src/Enigma.MobileTasks.Core` (net10.0, the model, format, storage, settings — no
  UI), `src/Enigma.MobileTasks.App` (net10.0, the whole UI: App, composition root, pages,
  ViewModels, services), `src/Enigma.MobileTasks.Android` (net10.0-android head),
  `tests/Enigma.MobileTasks.Core.UnitTests`, `tests/Enigma.MobileTasks.App.UnitTests` (xUnit v3,
  MTP; headless Avalonia for render tests).
- **Packages (CPM)** — Avalonia, Avalonia.Android, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter,
  Avalonia.Headless, Avalonia.Skia 12.1.1 · CommunityToolkit.Mvvm 8.4.2 · Enigma.Icons.Avalonia 1.0.0
  · Microsoft.Extensions.Hosting 10.0.12 · xunit.v3 4.0.1 · Microsoft.Testing.Extensions.CodeCoverage
  18.11.2 · Enigma.Avalonia.Desktop 1.2.0 (**test project only**, palette parity).
- **Model code is ported** from `Enigma.Tasks.Core`, namespaces renamed to `Enigma.MobileTasks.Core.*`,
  together with the reference fixture and the compatibility tests.
- **The palette is vendored** — `Themes/Colors.axaml` and `Themes/Brushes.axaml` copied from
  Enigma.Avalonia.Desktop **1.2.0** (commit `3667d58`), and guarded by a parity test that loads the
  1.2.0 package's own theme dictionary and compares every `Enigma*` key, in Dark and Light.
- **Storage** — one `.json` file per list in app-private storage
  (`Environment.SpecialFolder.LocalApplicationData/lists`), written atomically, saved 400 ms after the
  last change, and flushed when the application goes to the background.
- **Export / import** go through Avalonia's `IStorageProvider` (the Android system picker).
- **Navigation** — `NavigationPage` rooted at the Lists page; Checklist and Settings pages are pushed.
- **Theme** — System (follow the phone) by default; System / Light / Dark persisted.

## PHASE01 — Scaffold, model and operations

**Status:** DONE
**Branch:** feature/feature-3478-phase01-scaffold-model

Steps:
1. Root config: `.gitignore`, `.gitattributes` (LF), the full C# `.editorconfig` (as in the sibling
   Enigma repositories), `Directory.Build.props`, `Directory.Packages.props`, `global.json` (SDK
   10.0.100 `latestFeature` + MTP runner), `LICENSE.md` (MIT), `RELEASENOTES.md` (empty),
   `Enigma.MobileTasks.slnx` with `/src/` and `/tests/` folders.
2. `src/Enigma.MobileTasks.Core` (net10.0, `GenerateDocumentationFile`): port `Model/*`
   (`TaskList`, `TaskCategory`, `TaskItem`, `ListSchema`, `ListProgress`, `ListRemoval`),
   `Identity/*` (`IListIdGenerator`, `ListIdGenerator`), `Operations/*` (`ListOperations`,
   `ItemLocation`).
3. `tests/Enigma.MobileTasks.Core.UnitTests`: port `ListOperationsTests`, `ListProgressTests`,
   `ListSchemaTests` and the `SequentialIdGenerator`/`TestLists.Sample` infrastructure, plus an id
   generator shape test.

Acceptance criteria:
- `dotnet build Enigma.MobileTasks.slnx` succeeds with zero warnings; `dotnet test --solution
  Enigma.MobileTasks.slnx` passes.
- Every ported rule behaves as in Enigma.Tasks (the ported tests pass unchanged apart from
  namespaces).
- Generated ids match `^[cil]-[0-9a-z]+-[0-9a-z]+-[0-9a-z]+$`.

## PHASE02 — JSON file format and compatibility

**Status:** DONE
**Branch:** feature/feature-3478-phase02-json-format

Steps:
1. Port `Serialization/*` for JSON: `ListJsonSerializer`, `ListValidator`, `ListValidationError`,
   `ListValidationResult`, `ListValidationException`.
2. Copy the reference fixture `Fixtures/liste-judith.json` (byte for byte) into the Core test
   project with a copy-to-output glob.
3. Port `ListJsonSerializerTests` and `ListValidatorTests`.

Acceptance criteria:
- Round-tripping `liste-judith.json` is byte-identical (the file is the one Enigma.Tasks asserts
  against, so equality here is equality with the desktop application's output).
- A hand-written file with no `schemaVersion`, ids or `done` reads; `schemaVersion: 2` is refused
  with code `schema-too-new`; every validator code of Enigma.Tasks is produced for the same input.

## PHASE03 — Device storage and settings

**Status:** DONE
**Branch:** feature/feature-3478-phase03-device-storage

Steps:
1. Port `Workspace/*`: `AtomicFile`, `ListFileName`, `WorkspaceEntry`, `WorkspaceLoadResult`,
   `WorkspaceProblem`, `WorkspaceSaveFailedEventArgs`, `SaveScheduler`, `IWorkspaceStore` and
   `WorkspaceStore` — the store gains stream-based import/export members (`ImportTextAsync` already
   exists; export writes to a caller-supplied `Stream`) because Android picker results are content
   URIs, not file paths. Markdown members are dropped.
2. Port `Settings/*`, adapted to the phone: `AppSettings` keeps `Theme` (default **System**) and
   `CollapsedCategories`; the workspace folder is fixed (`AppPaths.DefaultWorkspaceFolder` under
   `LocalApplicationData`), so `WorkspacePath`/`LastListFile`/`EditMode` are not persisted.
3. Port `WorkspaceStoreTests`, `SaveSchedulerTests`, `SettingsServiceTests`, `ListFileNameTests`
   (+ `TempWorkspace`, `RecordingWorkspaceStore`), adapted to the changes above.

Acceptance criteria:
- A list is one `<slug>.json` file whose bytes are `ListJsonSerializer.Serialize(list)`; an
  unreadable file is reported and left untouched; create / rename (file follows) / duplicate (new
  ids, " (copy)") / delete behave as on the desktop.
- Exporting to a stream writes exactly the serializer's bytes (UTF-8, no BOM).
- Saves are debounced and `FlushAsync` writes everything pending.

## PHASE04 — App shell, theme and Android head

**Status:** DONE
**Branch:** feature/feature-3478-phase04-app-shell

Steps:
1. `src/Enigma.MobileTasks.App` (net10.0 library): `App.axaml` with `FluentTheme`, the vendored
   `Themes/Colors.axaml` + `Themes/Brushes.axaml` merged through `Themes/Theme.axaml`, app styles
   (`Themes/Styles.axaml`: card, page title, caption, toolbar, 48 dp touch targets, icon scale).
2. Composition root: `Host.CreateEmptyApplicationBuilder`, `AddMobileTasksCore()` /
   `AddMobileTasksApp()` extension blocks; `IActivityApplicationLifetime.MainViewFactory` (and the
   single-view fallback) build a `ShellView` hosting a `NavigationPage` rooted at the Lists page,
   plus the dialog and notification overlays.
3. Shell services: `IActiveListService` (entries, current list, touch → scheduled save, save
   failures marshalled to the UI thread), `IDialogService` (confirm / text prompt overlay, closed by
   the system back button), `INotificationService` (snackbar with an optional action, auto-dismiss),
   `INavigationService` over the `NavigationPage`. Settings are loaded and the theme applied before
   the first view; pending saves flush when the app is deactivated to the background.
4. A first Lists page that shows the lists (name + progress) and an empty state.
5. `src/Enigma.MobileTasks.Android` (net10.0-android, min API 24, `ApplicationId`
   `io.github.enigmalibs.mobiletasks`, label "Lists", `.apk`): `MainApplication`, `MainActivity`,
   manifest, `values`/`values-night` window background = `EnigmaBackgroundColor`, launcher icon. In the
   `.slnx` with `<Build Project="false" />`.
6. `tests/Enigma.MobileTasks.App.UnitTests`: headless fixture, composition-root test (every
   registration resolves), shell render smoke test, **palette parity test** against
   Enigma.Avalonia.Desktop 1.2.0.

Acceptance criteria:
- Solution build zero warnings (AVLN count read), tests green; `dotnet build
  src/Enigma.MobileTasks.Android` succeeds with zero warnings.
- Every `Enigma*` colour and brush key of Enigma.Avalonia.Desktop 1.2.0 resolves to the same value
  in the app, in Dark and Light.
- The shell renders headlessly with the Lists page as root.

## PHASE05 — Lists page

**Status:** DONE
**Branch:** feature/feature-3478-phase05-lists-page

Steps:
1. `ListsPageViewModel` + `ListCardViewModel`: the lists ordered by name, each with name, file name,
   progress text ("80 / 94") and ratio; empty state; problems from the load reported once.
2. Commands: New list (prompt for a name, then open it), Open (push the Checklist page), Rename
   (prompt), Duplicate, Delete (confirm). A card's ⋮ menu carries the per-list commands; the top
   command bar carries New and Settings.
3. `IListActions` service (as on the desktop) keeping the store and `IActiveListService` in step and
   notifying the outcome.
4. Tests: ViewModel tests for every command (including cancel paths) and a render test.

Acceptance criteria:
- Create / rename / duplicate / delete change the files on disk and the page in step; deleting asks
  first; a cancelled prompt changes nothing.
- Icon-only buttons carry accessible names.

## PHASE06 — Checklist page and editing

**Status:** DONE
**Branch:** feature/feature-3478-phase06-checklist-page

Steps:
1. `ChecklistPageViewModel`, `CategoryCardViewModel`, `ItemRowViewModel`: header progress, one card
   per category with its own progress and a finished marker; tap a row to tick; tap a category
   header to fold (remembered per list in settings); "Untick everything" for the list and per
   category.
2. Edit mode (pencil toggle in the top command bar): category ⋮ menu (Rename, Move up, Move down,
   Untick, Delete — confirm when it still holds items), item ⋮ menu (Rename, Move up, Move down,
   Delete), a quick-add box per category, "Add category" at the end, list rename.
3. `IListEditor` (ported shape): every change through `ListOperations`, a scheduled save, and the
   last deletion kept for Undo — offered by the snackbar's action.
4. Tests: ViewModel tests for ticking, folding, every edit, undo, and a render test.

Acceptance criteria:
- Every change is saved (debounced) and survives a reload; moves hop between categories at the
  bounds as on the desktop; undo restores the last deleted item or category at its position.

## PHASE07 — JSON import and export

**Status:** TODO
**Branch:** feature/feature-3478-phase07-import-export

Steps:
1. `IFilePicker` over `TopLevel.StorageProvider` (registered when the shell attaches):
   save picker for export (suggested name `ListFileName.Suggest(name)`, JSON type), open picker for
   import (multiple selection, JSON + All files types).
2. `IListActions.ExportAsync(entry)` — writes the serializer's bytes to the picked file;
   `ImportAsync()` — reads every picked file, validates, adds each readable list as a new file, and
   reports "n lists imported" plus one line per refused file with the validator's message.
3. Export from the Lists page card menu and the Checklist page command bar; Import from the Lists
   page command bar.
4. Tests: export bytes equal the serializer's output; importing the reference fixture yields the
   same list; a refused file is reported and adds nothing; cancel paths change nothing.

Acceptance criteria:
- An exported file is byte-identical to what Enigma.Tasks writes for the same list (asserted via
  the reference fixture round trip through export → import → export).
- Import accepts files exported by Enigma.Tasks and by another Enigma.MobileTasks.

## PHASE08 — Settings page and README

**Status:** TODO
**Branch:** feature/feature-3478-phase08-settings-readme

Steps:
1. `SettingsPageViewModel`: theme System / Light / Dark (persisted, applied immediately), about
   card (application version), storage card (where lists live, how many).
1a. *(Added by PHASE04's build.)* Restore live following of the phone's dark mode while the
   preference is System: PHASE04 resolves System to an explicit Light/Dark (headless Avalonia left the
   application with no actual variant after Dark → Default, painting every Enigma brush transparent),
   and Android does not raise `Application.PlatformSettings.ColorValuesChanged`, so a phone switching
   to dark mode while the app is open is no longer followed. Request `ThemeVariant.Default` for System
   again and fall back to the explicit variant only when no actual variant resolves; verify on the
   emulator (`cmd uimode night yes|no`).
2. README: what the app is, build and install (`dotnet build … -t:Run`, `.apk` publish),
   using it, export/import to and from Enigma.Tasks, the file format, working on it.
3. Tests: theme persistence and application, render test.

Acceptance criteria:
- The theme choice survives a restart and switches the whole UI live.
- The README documents every command needed to build, test, run and package the app.

## Out of scope

- Markdown import/export, the Android share sheet, "Open with" intent filters for `.json` files,
  exporting every list in one go — follow-ups.
- Store publishing, release signing, iOS head, cloud sync, localisation beyond English.

## Decisions taken autonomously

| Question                                                         | Chosen                                                                                                         | Why                                                                                                               | Alternatives rejected                                                                                                                                       |
|------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| How to reuse Enigma.Tasks' model and format code?                | Port it into `Enigma.MobileTasks.Core` with its reference fixture and tests                                    | `Enigma.Tasks.Core` is unpublished; the golden round-trip test keeps the format honest                            | Cross-repo `ProjectReference` (repo no longer builds standalone, tied to the sibling's checkout); git submodule (new infrastructure)                        |
| How to use "the same colours/themes" as Enigma.Avalonia.Desktop? | Vendor its 1.2.0 `Colors.axaml`/`Brushes.axaml` + a parity test against the 1.2.0 NuGet package                | `Enigma.Avalonia.Mobile` is unpublished, and the desktop package is ruled out for mobile; the test fails on drift | Reference `Enigma.Avalonia.Desktop` in the app (desktop controls, Enigma.Core + BouncyCastle on a phone); link the sibling's source files (cross-repo path) |
| Solution shape?                                                  | Core lib + shared UI lib (net10.0) + Android head (net10.0-android, `Build="false"`) + 2 test projects         | The house mobile layout (Enigma.Avalonia showcase); solution builds without the workload                          | Single Android project (untestable without the workload)                                                                                                    |
| Package versions?                                                | Avalonia 12.1.1 set, CommunityToolkit.Mvvm 8.4.2, Enigma.Icons.Avalonia 1.0.0, Hosting 10.0.12, xunit.v3 4.0.1 | The exact coupled set of Enigma.Tasks and Enigma.Avalonia, all cached locally                                     | Latest 12.1.x patch (breaks the coupled set the sibling apps ship)                                                                                          |
| Where do lists live on the phone?                                | One JSON file per list in app-private `LocalApplicationData/lists`                                             | Same model as the desktop: each file already is the export format                                                 | SQLite (new dependency, conversion on every export); shared storage (needs permissions)                                                                     |
| How are saves made durable on Android?                           | 400 ms debounce + flush on deactivation to background                                                          | Android may kill a backgrounded process without notice                                                            | Save on every change only (disk on each keystroke); flush on exit only (never raised on Android)                                                            |
| Export/import mechanism?                                         | Avalonia `IStorageProvider` system picker; export one list, import one or many files                           | Works with Downloads, Drive, USB transfer, without Android-specific plumbing                                      | Share sheet / intents (platform glue, follow-up); fixed shared folder (permissions)                                                                         |
| Import file-type filter?                                         | "JSON list" (`*.json`, `application/json`) plus "All files"                                                    | Cloud providers often mislabel MIME types; content is validated anyway                                            | JSON-only filter (greys out mislabelled files)                                                                                                              |
| Markdown support?                                                | Not included                                                                                                   | Not asked for; JSON is the requested interchange format                                                           | Port the Markdown serializer (scope creep)                                                                                                                  |
| Navigation model?                                                | `NavigationPage` rooted at Lists; Checklist and Settings pushed                                                | Avalonia 12's built-in mobile navigation, system back button handled                                              | `TabbedPage` (the checklist is a drill-down, not a peer tab); hand-rolled content switching                                                                 |
| Dialogs and messages?                                            | Own ViewModel-driven `IDialogService` overlay and `INotificationService` snackbar                              | Testable without a window; mobile-idiomatic                                                                       | Enigma.Avalonia.Desktop's ContentDialog/InfoBar (desktop package); modal pages for every prompt (heavy)                                                     |
| Editing on a narrow screen?                                      | Edit-mode toggle, ⋮ menus on rows/headers, quick-add per category, undo via snackbar                           | Keeps the everyday view a clean checklist like the desktop; four buttons per row do not fit a phone               | Always-visible edit buttons; drag-and-drop reordering (fiddly on touch, follow-up)                                                                          |
| Default theme?                                                   | System, persisted System/Light/Dark                                                                            | Android reports dark mode reliably; the desktop's Dark default works around Linux                                 | Dark default                                                                                                                                                |
| Application id and label?                                        | `io.github.enigmalibs.mobiletasks`, "Lists"                                                                    | House showcase convention; same name as the desktop launcher entry                                                | `ch.lodjx.*` (not the house convention)                                                                                                                     |
| UI language?                                                     | English, strings centralised                                                                                   | Same as Enigma.Tasks                                                                                              | French                                                                                                                                                      |
| Phase split?                                                     | 8 phases, one reviewable commit each                                                                           | ≤ 30 minutes of review per dev                                                                                    | One big dev                                                                                                                                                 |
