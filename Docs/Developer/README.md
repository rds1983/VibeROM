# Developer documentation

Architecture, data formats, parity tracking and contributor guidance for the
C# port of ROM 2.4b6.

Current status and the next unit of work are in [TASKS.md](../../TASKS.md).
Agent workflow and coding standards are in [AGENTS.md](../../AGENTS.md).

Planned pages:

- `Architecture.md` — projects, layers, game loop, dependency rules
- `DomainModel.md` — entities, ownership and lifecycle
- `DataFormats.md` — JSON layout, naming rules, schemas, examples
- `AreaImporter.md` — legacy `.are` → JSON tool and mapping rules
- `SaveFormat.md` — player JSON schema and versioning
- `CommandParity.md` — all 242 commands and implementation status
- `SpellParity.md` — all 98 spells, skills and special procedures
- `ParityNotes.md` — deliberate deviations from ROM behaviour
- `Testing.md` — unit, integration, full-corpus and soak tests
- `Performance.md` — benchmarks and targets
- `Contributing.md` — workflow, style and review checklist

The original C source (ROM 2.4b6) is the behavioural reference. It is not
vendored in this repository, and its file formats are not used at runtime.
