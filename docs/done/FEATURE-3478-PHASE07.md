# FEATURE-3478-PHASE07 — JSON import and export

## Summary

Lists now leave and arrive through Android's own file picker (the Storage Access Framework), with no
storage permission:

- **Export as JSON** — from a list card's ⋮ menu or the Checklist page's ⋮ menu. The picker proposes
  `<slug>.json` (the desktop's file-name rule) and the file receives the serializer's bytes: UTF-8
  without BOM, `\n`, no trailing newline. That is byte for byte what Enigma.Tasks writes for the same
  list.
- **Import** — the file icon in the Lists page's navigation bar, or "Import from a file" in its empty
  state. Pick one file or several. Every readable list is added as a new file; every other file is
  reported with the validator's reason ("… could not be imported: The file has no "categories"
  array.") or counted when there are several. A refused file adds nothing.

`IFilePicker` / `IPickedFile` abstract the picker, which makes import and export testable with
in-memory files. `StorageFilePicker` implements them over `TopLevel.StorageProvider`, attached each
time Android builds a shell.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.App/Services/{IFilePicker,IPickedFile,StorageFilePicker}.cs`
- `tests/Enigma.MobileTasks.App.UnitTests/{ImportExportTests,StorageFilePickerTests}.cs`,
  `Infrastructure/FakeFilePicker.cs` (+ `InMemoryFile`)

Modified:
- `Services/{IListActions,ListActions}.cs` — `ExportAsync`, `ImportAsync`, one combined message.
- `ViewModels/Pages/{ListsPageViewModel,ListCardViewModel,ChecklistPageViewModel}.cs` — `ImportCommand`,
  `ExportCommand`s.
- `Views/Pages/{ListsPage,ChecklistPage}.axaml` — import button, empty-state import, export entries.
- `App.axaml.cs` (attach the storage provider), `DependencyInjection/ServiceCollectionExtensions.cs`,
  `Resources/Strings.cs`, `tests/…/ShellRenderTests.cs`.
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- Fix cycle 1 (found on the emulator, not by the suite): with `DefaultExtension = "json"`, Android's
  save picker proposed **`vacances.json.json`**, because it appends the default extension even to a
  name that already ends in it. The option is gone; the suggested name carries its extension and the
  file type's MIME type tells the documents UI it is JSON. The options are now built by testable
  `CreateSaveOptions` / `CreateOpenOptions`, and `StorageFilePickerTests` pins this.
- Overwriting an *existing* file through the save picker relies on Avalonia opening the document in
  truncating mode. The normal path (a new document, which Android's documents UI creates) was verified
  on the device.
- After an import the app stays on the Lists page instead of opening the list: an import can bring
  several lists.
- Follow-ups (out of scope, as planned): Android share sheet, "Open with" for `.json` files, export of
  every list at once, Markdown.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Picker abstraction | `IFilePicker` returning `IPickedFile` (name + read/write streams, disposable) | Streams, not paths, are what SAF gives; in-memory fakes make the feature testable | Passing `IStorageFile` through (Avalonia types in every test) |
| Where does the storage provider come from? | `App.CreateShell` attaches it when the shell enters the visual tree | The provider belongs to the activity's top level, re-created with the activity | Resolving it lazily from `Application.Current` (no top level there) |
| How are several outcomes reported? | One snackbar message: imported count/name + refused file/count | One message, readable at a glance | One message per file (each replaces the last) |
| Import picker filter | "JSON list" + "All files" | Mislabelled MIME types from cloud providers; content is validated | JSON only |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 199 passed, 0 failed. 11 new:
  - the reference list exported **byte for byte as Enigma.Tasks writes it**;
  - export cancelled;
  - the desktop reference file imported unchanged;
  - a non-list refused with its reason;
  - several files with partial refusal;
  - import cancelled;
  - **phone-to-phone round trip arriving as the same file**;
  - card and checklist export commands;
  - save and open picker options, and use before attach.
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors.
- On-device (emulator, Release `.apk`), through the real Android documents UI:
  - "Export as JSON" proposed `vacances.json`, and Save wrote `/sdcard/Download/vacances.json` (495 bytes, LF, no BOM).
  - Import of Enigma.Tasks' `liste-judith.json` from Downloads added "Liste Judith" (80 / 94), with the snackbar above the gesture bar.
- **Desktop cross-check:** the phone-written `vacances.json` was read by Enigma.Tasks' own
  `ListJsonSerializer` (its sources compiled into a throwaway console in the scratchpad, without
  touching the Enigma.Tasks repository): "Vacances", 1 category, 3 items, 1 ticked. Its re-write was
  byte-identical to the phone's file.
- Documentation sweep: `README.md` holds only the title — nothing made stale (README in PHASE08).
