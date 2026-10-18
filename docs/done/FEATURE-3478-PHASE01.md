# FEATURE-3478-PHASE01 — Scaffold, model and operations

## Summary

The solution exists: house root configuration, a `.slnx`, the `Enigma.MobileTasks.Core` model library
and its xUnit v3 test project. The checklist model (lists, categories, items, progress, removals),
the identifier generator and every list operation are ported from `Enigma.Tasks.Core`, rule for rule,
so a list edited on the phone ends up in the same state as one edited on the desktop.

## Files/modules touched

Created:
- `.gitignore`, `.gitattributes`, `.editorconfig` — the house templates (identical to Enigma.Tasks').
- `Directory.Build.props` (version 1.0.0, LangVersion 14, nullable, no implicit usings, warnings as
  errors, code style in build), `Directory.Packages.props` (CPM; test group), `global.json` (SDK
  10.0.100 `latestFeature`, MTP runner), `LICENSE.md` (MIT), `RELEASENOTES.md` (empty placeholder),
  `Enigma.MobileTasks.slnx`.
- `src/Enigma.MobileTasks.Core/` — `Model/` (`TaskList`, `TaskCategory`, `TaskItem`, `ListSchema`,
  `ListProgress`, `ListRemoval`), `Identity/` (`IListIdGenerator`, `ListIdGenerator`), `Operations/`
  (`ListOperations`, `ItemLocation`).
- `tests/Enigma.MobileTasks.Core.UnitTests/` — `ListOperationsTests`, `ListProgressTests`,
  `ListSchemaTests` (ported), `ListIdGeneratorTests` (new), `Infrastructure/SequentialIdGenerator`,
  `Infrastructure/TestLists`.

Modified: `docs/roadmap.md`, `docs/plan/FEATURE-3478.md` (statuses).

## Deviations & follow-ups

- `ListOperationsTests.Describe` compared two lists through `ListJsonSerializer` in Enigma.Tasks;
  the serializer arrives in PHASE02, so the helper now renders a structural description that still
  includes every identifier. The assertion it backs is unchanged.
- Two remarks (`ListSchema`, `ListOperations`) now name Enigma.Tasks as the origin of the port and a
  party to the format contract.
- Line endings: every file written is LF; `.gitattributes` enforces `eol=lf`. Nothing to normalise.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Keep the ported namespaces? | Renamed `Enigma.Tasks.Core.*` → `Enigma.MobileTasks.Core.*`, no other code change | The port must stay diffable against its origin | Restructuring while porting |
| Solution version? | `<Version>1.0.0</Version>` in `Directory.Build.props` | Same single-version rule as Enigma.Tasks | Per-project versions |
| Add an id-shape test? | Yes — `ListIdGeneratorTests` | The plan's acceptance criterion; Enigma.Tasks had no direct test of it | Rely on the shape implicitly |

## Build/test evidence

- `dotnet build Enigma.MobileTasks.slnx` — Build succeeded, 0 warnings, 0 errors.
- `dotnet test --solution Enigma.MobileTasks.slnx` — 33 passed, 0 failed.
- Tests added: the ported operations/progress/schema suites and `ListIdGeneratorTests` (shape,
  uniqueness over 1,000 ids, empty prefix refused).
- Documentation sweep: `README.md` holds only the title — nothing made stale.
