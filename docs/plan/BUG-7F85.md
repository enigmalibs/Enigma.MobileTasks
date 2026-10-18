**Status:** TODO
**Type:** BUG
**Branch:** bugfix/bug-7f85-checkbox-alignment
**Run:** bugfix/2026-10-18-checkbox-rename-release

# BUG-7F85 — Checkboxes misaligned with their text

## Objective

On the Checklist page, an item's checkbox sits visibly higher than its text. The box and the
label of every single-line row must share one vertical centre, on the phone as in the headless
render.

## Context & constraints

- A row is `CheckBox.row` (`src/Enigma.MobileTasks.App/Themes/Styles.axaml`): `MinHeight` 48 (the
  Android touch target), `VerticalContentAlignment="Center"`, so the label is centred in the 48 px.
- Avalonia 12.1.1's Fluent `CheckBox` template draws the box inside a fixed-height (32 px) grid
  pinned to the **top** of the control (`VerticalAlignment="Top"`). The box is therefore centred on
  y = 16 while the label is centred on y = 24: the box sits about 8 px high.
- `RadioButton.setting` (Settings page, the theme choice) has the same shape — `MinHeight` 48,
  content centred — over Fluent's `RadioButton` template, which pins its glyph the same way.
- The 48 px touch target is a deliberate house rule and stays.

## Steps

1. Confirm the cause with a headless probe: the Fluent template's glyph container and its
   alignment, for `CheckBox` and `RadioButton`.
2. In `Themes/Styles.axaml`, centre the glyph container of `CheckBox.row` and of
   `RadioButton.setting` vertically in the control, with a `/template/` style written against the
   template's named parts, commented like the surrounding styles.
3. Add headless render tests asserting that the box's vertical centre equals the label's within
   one pixel — a checklist row on the real Checklist page, and a theme choice on the real Settings
   page.

## Acceptance criteria

- On the Checklist page, every single-line row's box and label share a vertical centre (≤ 1 px).
- The same holds for the Settings page's theme radio buttons.
- A row is still at least 48 px tall; ticking, strikethrough and the edit-mode menu are unchanged.
- Build at zero warnings; the whole suite passes, including the new render tests.

## Out of scope

- First-line alignment for wrapped, multi-line labels (the box centres on the text block, as a
  Material list item does for one and two lines).
- Any other control's template, and the Avalonia 12.1.4 bump (held back; see FEATURE-6F1B).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How is the box aligned with the label? | Centre the template's glyph container vertically in the row | Exact for every single-line row whatever the font; one selector; keeps the 48 px target | First-line alignment (font-metric constants that drift with the font); a 32 px row (breaks the touch target); re-templating the CheckBox (a copy of Fluent to maintain) |
| Fix the Settings radio buttons as well? | Yes | Same template shape, same defect, same style file | Leaving a known identical misalignment one page away |
| Wrapped labels | Box centred on the whole text block | Falls out of the same fix; matches Material's one- and two-line list items | Top alignment with tuned paddings |
| How is it proven? | Headless render tests on the real pages, comparing bounds | Guards the template-dependent fix against a future Avalonia bump | Manual inspection only |
