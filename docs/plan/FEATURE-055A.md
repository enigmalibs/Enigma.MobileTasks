**Status:** DONE
**Type:** FEATURE
**Branch:** feature/feature-055a-quickadd-focus
**Run:** feature/2026-10-24-quickadd-focus-trash

# FEATURE-055A — Focus quick-add box after +

## Objective

In edit mode, adding an item with a category's **+** button puts the cursor straight back in that
category's "Add an item" box, so the next item can be typed at once — the same rhythm Enter already
gives — and a list is filled in one go.

## Context & constraints

- The quick-add row sits at the bottom of every category card on the Checklist page
  (`src/Enigma.MobileTasks.App/Views/Pages/ChecklistPage.axaml`, inside the categories'
  `DataTemplate`): a `TextBox.quickadd` bound to `CategoryCardViewModel.QuickAddText`, Enter bound to
  `AddItemCommand`, and a `Button.icon` with a `Plus` icon bound to the same command. It is visible in
  edit mode only.
- Enter keeps the box's focus already; the button does not — a tap focuses the button, which pulls
  focus (and on Android the soft keyboard) out of the box.
- Adding an item syncs the card in place (`ChecklistPageViewModel.Sync`, `ObservableCollectionExtensions.SyncWith`), so the
  box survives the add — focusing it is enough, nothing is rebuilt.
- Focus is the one concern a ViewModel cannot reach; the house handles it in view code-behind
  (`ShellView.OnDialogPropertyChanged` focuses the dialog prompt).
- `AddItemCommand` cannot execute on a blank box, so a disabled **+** does nothing and gets no click.

## Steps

1. In `ChecklistPage.axaml`, make the quick-add **+** button non-focusable and give it a `Click`
   handler.
2. In `ChecklistPage.axaml.cs`, the handler finds the quick-add `TextBox` next to the clicked button
   (same row `Grid`) and focuses it — commented like the surrounding code-behind.
3. Add headless tests on the real Checklist page driving the **+** with real pointer input
   (`Avalonia.Headless` mouse down/up at the button's centre).

## Acceptance criteria

- In edit mode, typing a label in a category's quick-add box and tapping **+** adds the item at the
  end of that category, clears the box, and leaves the keyboard focus in that same box.
- Tapping **+** while the box has focus never moves focus out of the box, not even between press and
  release (no keyboard hide/show flicker).
- Enter still adds and keeps focus; a blank box still adds nothing.
- Build at zero warnings; the whole suite passes, including the new tests.

## Out of scope

- Any other button or box (the category-name dialog already focuses its prompt).
- Scrolling the box into view after many additions — Avalonia brings a focused box into view.
- Changes to the quick-add's ViewModel behaviour.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where does the focus logic live? | A `Click` handler in `ChecklistPage` code-behind focusing the sibling box | Focus is view-only state; matches the existing `ShellView` idiom; no new types | A ViewModel `FocusRequested` event or messenger (plumbing for a view concern); a reusable attached behaviour (new infrastructure for one use) |
| Should **+** stop taking focus itself? | Yes, `Focusable="False"`, plus the explicit focus | Without it a tap moves focus to the button and Android hides then re-shows the keyboard on every add; the explicit focus covers a box that had lost focus before the tap | Explicit focus only (keyboard flicker); `Focusable="False"` only (does not bring focus back when the box had lost it) |
| How is it tested? | Headless test on the real page with pointer input at the button | Exercises Avalonia's real focus-on-press path, not just the handler | Calling the handler directly; ViewModel-only tests (focus is invisible there) |
| Scope | Only the **+** of the quick-add row | That is the draft | Re-focusing after other edit actions |
