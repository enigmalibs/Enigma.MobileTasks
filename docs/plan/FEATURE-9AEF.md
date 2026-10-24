**Status:** TODO
**Type:** FEATURE
**Branch:** feature/feature-9aef-row-trash-button
**Run:** feature/2026-10-24-quickadd-focus-trash

# FEATURE-9AEF — Trash button on every item row

## Objective

Every item row of the Checklist page carries a trash button at its right end — in the everyday view
and in edit mode — so an item is removed in one tap: a bought item off a shopping list, a thing no
longer packed.

## Context & constraints

- An item row is the `vm:ItemRowViewModel` `DataTemplate` in
  `src/Enigma.MobileTasks.App/Views/Pages/ChecklistPage.axaml`: a `Grid ColumnDefinitions="*,Auto"`
  holding the `CheckBox.row` (the whole row ticks) and, in edit mode only, a `Button.icon` ⋮ whose
  `MenuFlyout` offers Rename, Move up, Move down, a separator and Delete.
- `ItemRowViewModel.DeleteCommand` already removes the item through `IListEditor.RemoveItem`, which
  schedules the save, rebuilds the card in place and shows a "deleted" snackbar with **Undo**.
- `Button.icon` is the house icon button: transparent, round, a 48 dp target, secondary foreground.
  Every icon button takes `IconSizeMedium`.
- Wording lives in `src/Enigma.MobileTasks.App/Resources/Strings.cs`; accessible names come from a
  ViewModel label property (`ActionsLabel` pattern).
- Rows sit in an `ItemsControl`; the reference list has 94 items — one extra button per row is
  negligible.

## Steps

1. `Strings.cs`: add `DeleteItemFor = "Delete {0}"` — the accessible name of a row's trash button.
2. `ItemRowViewModel`: add `DeleteLabel` (formatted from `Text`, raised when `Text` changes, like
   `ActionsLabel`).
3. `ChecklistPage.axaml`, item row: three columns `*,Auto,Auto` — the checkbox, the edit-mode ⋮, then
   an always-visible `Button.icon` with `<ei:Icon Kind="Trash" Size="{StaticResource IconSizeMedium}" />`,
   bound to `DeleteCommand`, named by `DeleteLabel`.
4. Remove Delete and its separator from the item's ⋮ menu (the category menu keeps its Delete).
5. Tests: ViewModel test for `DeleteLabel`; headless test on the real page — the trash is visible and
   rightmost on every row in both modes, the ⋮ only in edit mode, and tapping the trash removes the
   item and offers Undo without ticking the row.
6. README *Using it*: describe the trash button and the shorter item menu.

## Acceptance criteria

- In the everyday view every item row shows a trash button at its right end; tapping it removes that
  item, the progress counts update, the change is saved, and the snackbar offers Undo, which puts the
  item back where it was.
- In edit mode the row shows ⋮ then the trash, the trash still rightmost; the item ⋮ menu offers
  Rename, Move up and Move down — no Delete.
- Tapping the trash never ticks or unticks the row.
- The trash button is a 48 dp target and has the accessible name "Delete <item>".
- The row's box and label stay centred on each other (`RowAlignmentRenderTests` still passes).
- Build at zero warnings; the whole suite passes, including the new tests.

## Out of scope

- Trash buttons on category headers (the category ⋮ keeps its Delete, which asks when items remain).
- Swipe-to-delete, multi-select, "delete ticked items".
- A confirmation dialog for item deletion.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where does the trash sit next to the edit-mode ⋮? | Rightmost in both modes; ⋮ appears to its left in edit mode | The trash never moves when edit mode toggles, so the thumb learns one place | Trash left of ⋮ (it jumps on toggle); ⋮ replacing the trash in edit mode (contradicts "in edit mode AND normal mode") |
| Confirm before deleting? | No — the existing Undo snackbar | Speed is the point (shopping lists); same behaviour as the menu's Delete today | A confirmation dialog on every item |
| Keep Delete in the item ⋮ menu? | Remove it, with its separator | The trash sits right next to the menu in edit mode; one way to do one thing | Keeping a duplicate entry |
| Look of the button | `Trash`, Regular, `IconSizeMedium`, `Button.icon` secondary foreground | Same as every other icon button; red on 94 rows would shout | Error-red icon; `IconSizeSmall` (inconsistent with icon buttons) |
| Accessible name | "Delete {item}" via `Strings.DeleteItemFor` + `ItemRowViewModel.DeleteLabel` | Names the target for TalkBack; follows `ActionsLabel` | A bare "Delete" (ambiguous across 94 rows) |
| How is it tested? | ViewModel test + headless test with pointer input on the real page | Proves layout, visibility per mode and that the tap does not reach the checkbox | ViewModel-only tests |
