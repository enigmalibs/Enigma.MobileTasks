# FEATURE-9AEF — Trash button on every item row

## Summary

- Every item row on the Checklist page ends with a **trash button**, both in the everyday view and in
  edit mode. One tap removes the item through the existing `ItemRowViewModel.DeleteCommand`. That
  saves the list, rebuilds the card in place, recounts progress, and shows the "deleted" snackbar
  with **Undo**. There is no confirmation dialog, since speed is the point and Undo covers mistakes.
- The trash is always the row's last control, so its position never changes. Edit mode adds the
  item's ⋮ menu just to its left.
- The item's ⋮ menu no longer offers Delete, and its separator is gone with it. It now holds Rename,
  Move up and Move down. The category menu is unchanged and keeps its Delete, which asks first when
  items remain.
- The button is a standard `Button.icon` (48 dp, secondary foreground) with a Regular `Trash` icon at
  `IconSizeMedium`, like every other icon button. Its accessible name is "Delete <item>":
  `Strings.DeleteItemFor` through the new `ItemRowViewModel.DeleteLabel`, which follows a rename.

## Files/modules touched

Created:
- `tests/Enigma.MobileTasks.App.UnitTests/ItemRowTrashRenderTests.cs`
- `docs/done/FEATURE-9AEF.md`

Modified:
- `src/Enigma.MobileTasks.App/Views/Pages/ChecklistPage.axaml`: the row is now `*,Auto,Auto` with the
  trash button, and the item menu loses Delete and its separator.
- `src/Enigma.MobileTasks.App/ViewModels/Pages/ItemRowViewModel.cs`: `DeleteLabel`, raised with
  `Text`, and the class summary.
- `src/Enigma.MobileTasks.App/Resources/Strings.cs`: `DeleteItemFor`.
- `tests/Enigma.MobileTasks.App.UnitTests/ChecklistPageViewModelTests.cs`:
  `DeleteLabel_NamesTheItem_AndFollowsARename`.
- `README.md`: *Using it*, which now covers the trash button and the shorter item menu, and the test
  count (210 → 214).
- `docs/roadmap.md`, `docs/plan/FEATURE-9AEF.md` (→ `DONE`).

## Deviations & follow-ups

- None from the plan.
- Long labels now wrap 48 px earlier in the everyday view, because the trash takes that width.
  Accepted as the cost of the feature.
- Not checked on a device. The headless render (400 × 800, both modes) shows the trash level with
  each label, in one column in both modes, with the ⋮ to its left in edit mode.
- Possible follow-up, out of scope here: a "Delete ticked items" action for shopping lists (clear
  what was bought in one go).
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How do the tests tell the two row buttons apart? | The menu is the button with a `Flyout`; the trash is the one bound to `DeleteCommand` | Uses what each button does, with no test-only names added to the template | `x:Name` on template parts (only there for the tests) |
| What does "never ticks the row" look like in a test? | Tap the trash with real pointer input, Undo, then assert the restored item is still not done and progress is back to `0 / 2` | The deleted row is gone, so its tick can only be checked on the restored item | Checking a neighbouring row (does not prove anything about the tapped one) |
| Which list proves layout? | The Judith reference list, every visible row, in both modes | Real label lengths, including a wrapped one, on the real page | A hand-built row |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx`: 214 passed, 0 failed. The 4 new tests are
  `EveryRow_EndsWithItsTrash_InTheEverydayViewAndInEditMode`, `TheItemMenu_NoLongerOffersDelete`,
  `TappingTheTrash_RemovesTheItemWithoutTickingIt_AndUndoPutsItBack` and
  `DeleteLabel_NamesTheItem_AndFollowsARename`. `RowAlignmentRenderTests` still passes, so box and
  label stay centred.
- Red first: the tests were written before the change and did not compile (`DeleteLabel` did not
  exist). The render tests were not run separately against the old view. Against it, they fail by
  construction: no button bound to `DeleteCommand`, and Delete still in the menu.
- A throwaway headless snapshot of both modes was inspected, then deleted (not committed).
- The Android head was not touched (shared UI project only).
- Documentation sweep: `README.md` *Using it* (the edit-mode bullet listed Delete on the item menu)
  and the test count.
