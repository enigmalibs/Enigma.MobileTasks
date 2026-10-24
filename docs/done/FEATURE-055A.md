# FEATURE-055A — Focus quick-add box after +

## Summary

- **Cause.** On the Checklist page in edit mode, tapping a category's quick-add **+** pulled the focus out
  of the "Add an item" box. Avalonia focuses the first focusable control under a press, so the button
  took the focus. Once the add emptied the box, `AddItemCommand` could no longer execute, the button
  was disabled, and the focus went nowhere. On a phone that hides the keyboard after every item.
- **Fix.** The **+** is now `Focusable="False"`, so a tap leaves the cursor and the keyboard in the box.
  Its new `Click` handler in `ChecklistPage` code-behind also focuses the quick-add box on the same row,
  which brings the cursor back when the box had lost focus before the tap. Enter behaves as before, and
  a blank box still adds nothing.

## Files/modules touched

Created:
- `tests/Enigma.MobileTasks.App.UnitTests/QuickAddFocusRenderTests.cs`
- `docs/done/FEATURE-055A.md`

Modified:
- `src/Enigma.MobileTasks.App/Views/Pages/ChecklistPage.axaml`: the **+** is non-focusable and wired to
  `OnQuickAddClick`, and the quick-add comment is updated.
- `src/Enigma.MobileTasks.App/Views/Pages/ChecklistPage.axaml.cs`: `OnQuickAddClick`.
- `README.md`: the test count (208 → 210).
- `docs/roadmap.md`, `docs/plan/FEATURE-055A.md` (→ `DONE`).

## Deviations & follow-ups

- None from the plan.
- With the **+** non-focusable, keyboard Tab navigation skips it. Enter in the box does the same
  thing, and a phone has no Tab key.
- Not checked on a device. The headless tests use Avalonia's real pointer and focus path, but the
  Android soft keyboard itself is not emulated. Worth one tap-through on the phone at the next release.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How does the handler find the box? | The `TextBox` among the clicked button's `Panel` siblings | The two share the row `Grid` in the template. No name lookup is needed across the per-item name scopes | `x:Name` + `NameScope` lookup (more code for the same result); walking up to the card |
| Event handler inside a `DataTemplate`? | `Click="OnQuickAddClick"`, resolved against the page by the XAML compiler | Compiles with compiled bindings and keeps the handler next to `OnDataContextChanged` | Subscribing in code on `ContainerPrepared` (more code) |
| What do the tests prove? | Focus stays in the box between press and release, and a tap brings focus back from another category's box | Covers each half of the fix separately: removing either one fails its test | One combined assertion after the tap |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx`: 210 passed, 0 failed. The 2 new tests are
  `TappingPlus_AddsTheItemWithoutTheFocusEverLeavingTheBox` and `TappingPlus_PutsTheFocusBackInTheBoxItAddedFrom`.
- Red first: before the fix, the first test failed at the press with the **+** `Button` focused
  instead of the box. The second failed after the tap with nothing focused (`null`): the emptied
  box had disabled the button, which dropped the focus.
- The Android head was not touched (view-only change in the shared UI project).
- Documentation sweep: `README.md`'s test count updated. Nothing else described the quick-add
  focus.
