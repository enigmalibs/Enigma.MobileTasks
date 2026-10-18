# BUG-7F85 — Checkboxes misaligned with their text

## Summary

- **Cause.** Avalonia 12.1.1's Fluent `CheckBox` template draws the box inside a 32-pixel grid pinned
  to the top of the control. The app's checklist rows (`CheckBox.row`) are 48 pixels tall — the
  Android touch target — with the label centred, so the box sat on y = 16 and the label on y = 24:
  8 pixels high, on every row. A headless probe of the template confirmed the shape.
- **Fix.** `Themes/Styles.axaml` centres that grid in the row
  (`CheckBox.row /template/ Grid#RootGrid > Grid`), so box and label share the row's middle whatever
  the font. The 48-pixel row, ticking, the strikethrough and the edit-mode menus are unchanged.
- **Same defect, same fix, on the Settings page.** The theme choices (`RadioButton.setting`) sit on
  Fluent's `RadioButton` template, which pins its circle the same way; it is centred the same way
  (`RadioButton.setting /template/ Border#RootBorder > Grid > Grid`).

## Files/modules touched

Created:
- `tests/Enigma.MobileTasks.App.UnitTests/RowAlignmentRenderTests.cs`

Modified:
- `src/Enigma.MobileTasks.App/Themes/Styles.axaml` — the two template styles.
- `docs/roadmap.md`, `docs/plan/BUG-7F85.md` (→ `DONE`).

## Deviations & follow-ups

- None from the plan.
- The fix depends on Fluent's template shape (an unnamed grid under `RootGrid` / `RootBorder`). The
  new render tests fail if an Avalonia upgrade changes that shape — check them first when the
  Avalonia set moves to 12.1.4 or later.
- A wrapped, multi-line label centres the box on the whole text block (as a Material list item does
  for one and two lines), not on its first line.
- Line endings: LF throughout.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What does the test compare? | The glyph part's centre (`NormalRectangle`, `OuterEllipse`) with `PART_ContentPresenter`'s, ≤ 1 px, plus a ≥ 48 px row | The presenter is exactly the label's box; named parts survive restyling | Pixel sampling of a rendered frame (fragile to anti-aliasing and the palette) |
| Which rows are tested? | Every visible row of the real Checklist page (the Judith fixture) and the three theme choices of the real Settings page | Proves the styles reach the pages, not a hand-built control | A standalone `CheckBox` in a test window |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx --no-incremental` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 208 passed, 0 failed (2 new:
  `ChecklistRows_CentreTheBoxOnTheLabel`, `ThemeChoices_CentreTheCircleOnTheLabel`).
- Red first: with both styles set back to `Top`, both new tests fail with "The glyph's centre is at
  16 and the label's at 24", on every row; restored, both pass.
- A headless render of the Checklist page (Liste Judith, 400 × 800) shows every box level with its
  label.
- The Android head was not touched (styles only); it is rebuilt in FEATURE-0CA0 and FEATURE-6F1B.
- Documentation sweep: nothing stale — no prose doc describes the row's geometry.
