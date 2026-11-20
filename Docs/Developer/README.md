# Developer documentation

Architecture, data formats, parity tracking and contributor guidance for the
C# port of ROM 2.4b6.

Current status and the next unit of work are in [TASKS.md](../../TASKS.md).
Agent workflow and coding standards are in [AGENTS.md](../../AGENTS.md).

Pages (some are still planned):

- `Architecture.md` — projects, layers, dependency rules (planned)
- [GameLoop.md](GameLoop.md) — 4 Hz pulses, named ticks, time, autosave and
  exception isolation (**available**)
- [DomainModel.md](DomainModel.md) — entities, ownership, invariants and
  validation (**available**)
- [DataFormats.md](DataFormats.md) — JSON layout, naming rules, schemas,
  examples (**available**)
- [AreaImporter.md](AreaImporter.md) — legacy `.are` → JSON tool and mapping
  rules (**available**)
- [TextEngine.md](TextEngine.md) — argument parsing, strings, colour, dice and
  `act()` tokens (**available**)
- [CanonicalNames.md](CanonicalNames.md) — flag/value registries, table
  validation and schema alignment (**available**)
- `SaveFormat.md` — player JSON schema and versioning (planned)
- `CommandParity.md` — all 242 commands and implementation status (planned)
- `SpellParity.md` — all 98 spells, skills and special procedures (planned)
- [ParityNotes.md](ParityNotes.md) — deliberate deviations from ROM behaviour (**available**)
- [DataInventory.md](DataInventory.md) — per-area prototype counts for the committed corpus (**available**)
- `Testing.md` — unit, integration, full-corpus and soak tests (planned)
- `Performance.md` — benchmarks and targets (planned)
- `Contributing.md` — workflow, style and review checklist (planned)

The original C source (ROM 2.4b6) is the behavioural reference. It is not
vendored in this repository, and its file formats are not used at runtime.
