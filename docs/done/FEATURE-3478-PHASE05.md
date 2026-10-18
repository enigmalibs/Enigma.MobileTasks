# FEATURE-3478-PHASE05 — Lists page

## Summary

The Lists page does everything a list library needs on a phone. "+" in the navigation bar (and a
"New list" button in the empty state) asks for a name, creates the file and opens the new list. Tapping
a card opens its checklist, pushed on the navigation stack. A card's ⋮ menu renames it (prompt starting
from the current name; the file follows), duplicates it (fresh identifiers, " (copy)") or deletes it
(destructive confirmation first). Each outcome is reported in the snackbar, and a failure is reported
rather than thrown. Progress bars turn green when a list is finished.

`IListActions` (ported in shape from Enigma.Tasks) keeps the files and `IActiveListService` in step.
Unlike the desktop, it **flushes pending saves before a rename or a delete**, so a tick made a moment
earlier is not written after the delete, bringing the file back.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.App/Services/{IListActions,ListActions}.cs`
- `src/Enigma.MobileTasks.App/ViewModels/Pages/{ChecklistPageViewModel,CategoryCardViewModel}.cs`,
  `Views/Pages/ChecklistPage.axaml(.cs)` — the Checklist route, read-only for now (PHASE06 makes it
  interactive).
- `tests/Enigma.MobileTasks.App.UnitTests/{ListActionsTests,Infrastructure/RecordingNavigation}.cs`

Modified:
- `ViewModels/Pages/{ListsPageViewModel,ListCardViewModel}.cs` — commands (new, open, rename,
  duplicate, delete); cards carry their own commands.
- `Views/Pages/ListsPage.axaml(.cs)` — tappable cards, ⋮ `MenuFlyout`, navigation-bar command bar,
  empty-state button; code-behind hands the command bar its DataContext.
- `Navigation/{ShellPage,ContainerPageFactory}.cs`, `DependencyInjection/ServiceCollectionExtensions.cs`,
  `Resources/Strings.cs`, `Themes/Styles.axaml` (`Button.cardbody`, `ProgressBar.listprogress`).
- `tests/…/{ListsPageViewModelTests,ShellRenderTests}.cs`, `Infrastructure/{TempApp,HeadlessAvaloniaFixture}.cs`.
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- **Checklist route introduced here, read-only.** The plan puts "Open (push the Checklist page)" in
  this phase and the page in PHASE06; a minimal page (header = list name, one card per category with
  its progress) makes the navigation real and tested now, and PHASE06 grows it.
- Fix cycle 1: content set through `NavigationPage.TopCommandBar` is shown inside the navigation bar's
  template, outside the page's tree, so it did not inherit the page's DataContext and the "+" bound to
  nothing; `ListsPage` now hands its DataContext to the bar (`OpeningACard_PushesTheChecklistPage`
  caught it).
- Fix cycle 2: a navigation push plays a page transition, and headless animation time only moves on a
  render tick; the fixture's async pump now ticks the render timer and fails after 10 s instead of
  hanging the run.
- Follow-up for Enigma.Tasks (desktop): its `ListActions.DeleteAsync` does not flush pending saves
  first, so a tick within 400 ms of a delete can bring the file back.
- On-device menu rows are Fluent's desktop height (~37 dp); larger touch rows would be a styling
  follow-up.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does a card's menu reach the page's commands? | The card carries its own commands, delegating to the page | A flyout lives outside the visual tree; `$parent` bindings from it find nothing | `$parent[ItemsControl]` bindings (silently null in a flyout) |
| Where do the page's actions go? | `NavigationPage.TopCommandBar` ("+"), plus an accent button in the empty state | The bar is always visible; the empty state needs an obvious first step | A floating action button (new control, no house precedent) |
| Flush before rename/delete? | Yes, in `ListActions` | A pending save would otherwise land on a deleted or moved file | Cancelling the pending save (loses the latest tick on rename) |
| Undo a list deletion? | No — confirm first, as on the desktop and as planned | A list is a file; the confirmation already says to export first | Snackbar undo for lists (re-writing a file the user meant to delete) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 166 passed, 0 failed (11 new: new list
  create/cancel, open, rename/cancel, duplicate, delete declined/confirmed, flush-before-delete,
  flush-before-rename, unwritable folder reported, real navigation push + "+" binding).
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors.
- On-device (Pixel 6 / API 36 emulator, Release `.apk`): "+" opens the prompt focused with the
  keyboard up; typing "Vacances" and the keyboard's Done creates the list and pushes its checklist
  (back arrow, "This list is empty"); system back returns to the Lists page with the card (0 / 0);
  the ⋮ menu shows Rename, Duplicate, Delete.
- Documentation sweep: `README.md` holds only the title — nothing made stale.
