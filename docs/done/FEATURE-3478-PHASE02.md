# FEATURE-3478-PHASE02 — JSON file format and compatibility

## Summary

The list file format is in: `ListJsonSerializer` writes exactly what `JSON.stringify(list, null, 2)`
writes (property order, two-space indent, `\n`, no trailing newline, UTF-8 without BOM, accents
literal) and reads forgivingly; `ListValidator` refuses what Enigma.Tasks refuses, with the same codes
and messages. Both are ported from `Enigma.Tasks.Core` unchanged apart from namespaces, together with
the reference file `liste-judith.json` — the very fixture Enigma.Tasks asserts its own output against
— so the byte-identical round trip here is byte-identity with the desktop application.

## Files/modules touched

Created:
- `src/Enigma.MobileTasks.Core/Serialization/` — `ListJsonSerializer`, `ListValidator`,
  `ListValidationError`, `ListValidationResult`, `ListValidationException`.
- `tests/Enigma.MobileTasks.Core.UnitTests/Fixtures/liste-judith.json` (byte-identical copy, `cmp`
  verified), `ListJsonSerializerTests` (ported + one new golden-string test), `ListValidatorTests`
  (ported).

Modified:
- `tests/…/Infrastructure/TestLists.cs` — the reference-file readers.
- `tests/…/Enigma.MobileTasks.Core.UnitTests.csproj` — `*.json` fixture copy-glob.
- `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- Two doc comments carried over from Enigma.Tasks said the validation messages are in French; they
  are English there and here. Corrected in `ListValidator` and `ListValidationError` (the desktop
  repository keeps the stale wording — worth the same one-line fix there).
- Follow-up worth checking in both repositories: `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` is
  documented to escape supplementary-plane characters, so a list containing an emoji is likely
  written as `😀` where `JSON.stringify` writes it literally. Both apps read either form, and
  phone and desktop write the same bytes, so the compatibility this feature needs holds; only
  byte-identity with the web app would differ for such a list.
- Line endings: LF throughout; the fixture keeps its original no-trailing-newline ending.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Add tests beyond the port? | One golden-string test: empty category + quotes in a name | It is the exact shape of a list created on the phone and exported before anything is added | Rely on the reference fixture alone (it has no empty category) |
| Where does the fixture come from? | Enigma.Tasks' `tests/…/Fixtures/liste-judith.json`, byte for byte | Same file both suites assert against = same contract | Regenerating a fixture from this code (would prove nothing) |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 68 passed, 0 failed (35 new: serializer
  round trip, indentation, accents, property order, forgiving reads, refusals; every validator code).
- Documentation sweep: `README.md` holds only the title — nothing made stale.
