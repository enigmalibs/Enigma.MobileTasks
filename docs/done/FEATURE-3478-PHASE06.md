# FEATURE-3478-PHASE06 — Checklist page and editing

## Summary

The Checklist page is the real checklist now:

- **Everyday view** — a progress bar for the whole list, one card per category with its own count
  and a green check once finished, and rows you tick by tapping anywhere on them (a ticked row is
  struck through and steps back). Tapping a heading folds the card; folded cards are remembered per
  list in the settings. "Untick everything" in the ⋮ menu resets the list for the next time.
- **Edit mode** — the pencil in the navigation bar. An empty list opens in it. It shows a ⋮ menu per
  category (Rename, Move up, Move down, Untick everything, Delete) and per item (Rename, Move up,
  Move down, Delete), a quick-add box at the foot of each card, an "Add a category" button, and
  "Rename the list" in the page menu.
- **Undo** — deleting an item or a category puts "Undo" in the snackbar, which restores it at its
  position. A category that still holds items asks first; an empty one goes at once. As on the
  desktop, an item moved past the top or bottom of its category hops into the neighbouring category.

`IListEditor`/`ListEditor` port the desktop editor's rules: every change goes through
`ListOperations` and becomes a scheduled save, and the undo is forgotten when another list opens. They
add ticking, and a `StructureChanged` event the page syncs on. The page syncs **in place**
(`ObservableCollectionExtensions.SyncWith`), keeping the ViewModels of the categories and items that
survive. The quick-add box therefore keeps its focus, and the phone's keyboard stays up, from one item
to the next. Ticked boxes are painted with the Enigma accent through Fluent's CheckBox keys.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.App/Services/{IListEditor,ListEditor}.cs`
- `src/Enigma.MobileTasks.App/ViewModels/Pages/ItemRowViewModel.cs`,
  `ViewModels/ObservableCollectionExtensions.cs`
- `tests/Enigma.MobileTasks.App.UnitTests/{ChecklistPageViewModelTests,ListEditorTests}.cs`

Modified:
- `ViewModels/Pages/{ChecklistPageViewModel,CategoryCardViewModel}.cs` — from the read-only PHASE05
  route to the interactive page.
- `Views/Pages/ChecklistPage.axaml(.cs)` — command bar, progress header, cards, rows, menus,
  quick-add, empty state.
- `Themes/Styles.axaml` (`Button.cardheader`, `CheckBox.row`, `ToggleButton.icon`, `Button.outline`,
  `TextBox.quickadd`), `Themes/Theme.axaml` (Fluent CheckBox keys → Enigma accent),
  `Resources/Strings.cs`, `DependencyInjection/ServiceCollectionExtensions.cs`.
- `tests/…/{ShellRenderTests,ThemeTests}.cs`.
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- **In-place sync instead of rebuilding** (not in the plan): rebuilding the cards after each edit
  closed the keyboard after every quick-added item, which defeats quick-add on a phone.
- **Edit mode is not persisted** (the desktop persists it): an empty list opens in edit mode and any
  other list in the everyday view, which is what a phone user expects to come back to.
- Follow-up (styling): Fluent's CheckBox template pins its box to the top of the row, so in a 48 dp row
  the label sits a few pixels lower than the box; and flyout menu rows keep Fluent's desktop height.
- No fix cycle was needed; the CheckBox accent override was added after the first green run, from the
  emulator check.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does the page learn about edits? | `IListEditor.StructureChanged` (anything beyond one tick) | Ticks only re-count; everything else re-syncs, from one path | Re-syncing on `Touched` (would re-sync on every tick) |
| Rebuild or sync? | In-place `SyncWith`, reusing ViewModels by model reference | Keeps focus and keyboard; cheap at checklist sizes | Clear-and-refill |
| Rows follow edit mode how? | Bind through `Card.Page.IsEditing` | One source of truth; no per-row copy to keep in step | Copying `IsEditing` into every card and row |
| Delete a category with items? | Confirm, then undo available | Undo in a snackbar is easy to miss for a whole category | Undo only |
| Ticked-box colour | Override Fluent's `CheckBoxCheck*Checked*` keys in `Theme.axaml` | Same technique as the desktop's TextBox override; vendored files untouched | Re-templating CheckBox |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 188 passed, 0 failed (22 new: open, tick +
  save, untick all / per category, folding remembered, empty list in edit mode, add category,
  quick-add keeps its card, rename item and category, delete + undo, confirm for a non-empty category,
  no prompt for an empty one, hop between categories, move category, rename list, edit toggle, editor
  edges, checklist render in and out of edit mode, ticked-box accent).
- `dotnet build src/Enigma.MobileTasks.Android -c Release` — 0 warnings, 0 errors.
- On-device (emulator, Release `.apk`): "Vacances" (empty) opened in edit mode; "Add a category" →
  "Habits"; three items quick-added in a row with Enter (focus and keyboard kept); out of edit mode a
  tap ticked "Pulls" (struck through, 1 / 3 on the card, the header and the Lists page). After
  force-stop and relaunch the tick was still there, and the box is #3574F0, the Enigma Light accent.
- Documentation sweep: `README.md` holds only the title — nothing made stale.
