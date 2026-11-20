# AGENTS.md

Instructions for AI agents working in this repository. Read this file and
[TASKS.md](TASKS.md) before doing anything.

## Project

VibeROM is a C#/.NET port of **ROM 2.4b6** (May 29 1998), a DikuMUD/Merc
derivative. The original C source is the behavioural reference, not a template.
We port game logic, content and user-visible behaviour; we do not transliterate
C idioms.

- Stack: C# on .NET 10, NLog, xUnit.
- All runtime game data is human-readable JSON in `Data/` (areas, players,
  notes, gods, music, schemas).
- `VibeROM.AreaImporter` is the only component that understands the original
  `.are` format. The server never contains a legacy parser.
- The original C project may be available locally (for example
  `D:\Projects\chaos\ROM`) for reference. It is not part of this repository.

## Repository map

| Path | Purpose |
| --- | --- |
| `VibeROM/` | Game server source and `VibeROM.csproj` |
| `VibeROM.AreaImporter/` | Console tool: original `.are` → JSON |
| `VibeROM.Tests/` | xUnit tests for all projects |
| `Data/areas/` | Area JSON (one file per area) + `index.json` |
| `Data/players/` | Player save files (`*.json`) |
| `Data/notes/` | Notes, ideas, penalties, news, changes (`*.json`) |
| `Data/gods/` | God/wizlist files (`*.json`) |
| `Data/music/` | `songs.json` |
| `Data/schema/` | JSON Schemas for every data file |
| `Docs/Player/` | Player-facing guide |
| `Docs/Admin/` | Build, configuration, operations |
| `Docs/Developer/` | Architecture, data formats, contributor guide |
| `logs/` | Runtime logs (generated, never committed) |

## Data rules

1. **JSON only at runtime.** No `.are`, pfiles, `music.txt` or other ROM
   formats are read by the server. They are import input for the importer only.
2. **Human-readable names.** Flags and enums are JSON strings or arrays of
   canonical names — `["Aggressive", "Undead"]`, `"Warrior"`, `"Slash"` — never
   C letters (`"ABCDEFGHIJK"`), raw bitmasks or opaque integers. Item-type-
   specific data uses named objects (`weapon`, `container`, `food`, …).
3. **Canonical names live in one registry.** Do not invent spelling variants.
   The same registry powers importer, serializer, loader and docs.
4. **Deterministic output.** Same input → byte-identical JSON: 2-space indent,
   UTF-8 without BOM, LF, mobiles/objects/rooms sorted by vnum, helps by
   keyword and socials by name, resets in original order, `formatVersion` in
   every file.
5. **JSON is the source of truth.** After migration, edit and validate JSON
   directly. Re-running the importer over edited data requires explicit review
   and must not silently overwrite authored content.
6. **Never commit legacy game data.** Original `.are` files are not stored in
   this repository. Small, hand-written fixtures for importer tests are allowed
   under `VibeROM.Tests/Fixtures/`.
7. **Schema every change.** A format change updates the JSON Schema, the format
   doc, the serializer/loader and the importer in the same task.

## The workflow (follow in order)

### 1. Get the next task from `TASKS.md`

- Pick the **first unchecked** (`[ ]`) task with no unfinished dependency.
- Read the task fully, including its acceptance criteria.
- Read the relevant original C source and existing C# code before writing
  anything. If the task is ambiguous, ask the developer before starting.
- Mark the task `[~]` in progress in `TASKS.md` (optional but preferred) and
  keep changes scoped to that task. Do not bundle unrelated work.

### 2. Do it

- Implement the deliverables listed in the task.
- Prefer small, focused classes over large static functions. Use dependency
  injection where it improves testability; avoid new mutable global state.
- Keep user-visible text as close to the original as practical.
- Keep the importer one-way: it may reference the server project, never the
  reverse. The server must build and run without the importer.

### 3. Test thoroughly

Testing is not "the build passed". Do all of the following that apply:

- `dotnet build VibeROM.slnx` — must pass with no warnings (warnings are errors).
- `dotnet test VibeROM.slnx` — all unit and integration tests green.
- Add xUnit tests for every new behaviour, including failure paths and edge
  cases. Use table-driven and property-based tests where the original logic is
  formula-heavy.
- Deterministic tests: inject `TimeProvider`/seeds; never use `Thread.Sleep`,
  real sockets or wall-clock time in unit tests.
- After any data-format, loader or reset change: validate the full JSON corpus
  and run the full-area load test.
- After any importer change: run it twice and confirm byte-identical output;
  run `--check`; test malformed input.
- For network, login, combat, magic or world-loading changes, run the real
  server and exercise the change via a telnet client (manual or scripted).
- Record the actual commands run and their results in your final report.

If a test cannot be written for a justified reason, say so explicitly; never
claim something is tested when it is not.

### 4. Update the docs

Every task updates the documentation it invalidates. At minimum:

| Change | Docs to update |
| --- | --- |
| Player-visible behaviour, commands, classes, races, skills | `Docs/Player/` |
| Configuration, ports, paths, build/run, operations, OLC admin | `Docs/Admin/` |
| Architecture, domain model, formats, parity reports, testing | `Docs/Developer/` |
| JSON schema, field names, flags, item data | `Docs/Developer/DataFormats.md` |
| Importer behaviour, options, mapping, errors | `Docs/Developer/AreaImporter.md` |
| New data folder or file format | `Data/README.md` + `Docs/Developer/` |
| Task-level decisions and deviations from ROM | `Docs/Developer/ParityNotes.md` |

Update `TASKS.md` if the work reveals new tasks, missing prerequisites or
incorrect assumptions. Do not rewrite the roadmap silently; explain changes.

### 5. Ask the developer whether the task is finished

Stop and ask. Your report must contain:

1. Task ID and title.
2. What was implemented (and any deliberate deviations from ROM).
3. Test evidence: exact commands and results, plus manual checks performed.
4. Known limitations, follow-up work and risks.
5. Files and docs changed.

Then ask a direct question, e.g. **"T012 is implemented and verified. Is the
task finished, or should I address anything before marking it complete?"**

Do not mark the task finished, commit, or start the next task while waiting.

### 6. Only after explicit confirmation

- If the developer says no or asks for changes: keep the task `[~]`, apply the
  feedback, re-test, update docs and ask again.
- If the developer explicitly confirms completion:
  1. Mark the task `[x]` in `TASKS.md` and move it to the **Completed** section
     with the completion date.
  2. Review the whole of `TASKS.md`. You may reorganize the remaining tasks
     freely — add, remove, split, merge or reorder them — to reflect what you
     learned. Keep the numbering, dependencies and acceptance criteria coherent.
     Never remove a task that represents unimplemented ROM functionality without
     the developer's agreement.
  3. Commit and push.

## Git rules

- One task per commit. Do not mix unrelated changes.
- Never commit `bin/`, `obj/`, `logs/`, test results, secrets, local config or
  live player save files.
- Never commit original `.are` game data; only small importer test fixtures.
- Commit message format:

  ```
  T012: Migrate the ROM corpus to JSON

  - Add AreaImporter CLI and deterministic writer
  - Generate Data/areas/*.json and index.json
  - Document format and import provenance
  ```

- Commit only files related to the confirmed task.
- Push to the current branch on `origin`:
  `git push origin HEAD`.
- Never force-push, rewrite history or amend a pushed commit.
- If there is no remote configured or the push fails, report the exact error and
  leave the commit local. Do not silently skip the push.
- If the developer asks for a different commit/push policy, follow it for that
  task.

## Definition of done

A task is done only when **all** of these are true:

- [ ] Deliverables implemented as described.
- [ ] Build clean, all tests green.
- [ ] New logic has meaningful tests, including failure paths.
- [ ] Data changes validate against the JSON Schema; corpus load is clean.
- [ ] Importer output is deterministic (`--check` passes where applicable).
- [ ] Relevant `Docs/` pages and `Data/README.md` updated.
- [ ] `TASKS.md` status and any follow-ups updated.
- [ ] Developer has explicitly confirmed completion.
- [ ] Change committed and pushed.

## Coding standards

- .NET 10, nullable reference types on, file-scoped namespaces, async APIs with
  `CancellationToken` where I/O is involved.
- Treat warnings as errors; keep the build warning-free.
- Use `TimeProvider` instead of `DateTime.Now`/timers in testable code.
- Logging via NLog (`LogManager.GetCurrentClassLogger()`), structured messages,
  correct levels. Never log passwords, password hashes or full save files.
- Exceptions for programmer errors and unrecoverable startup problems; expected
  bad user input is handled and reported to the player, not thrown.
- Game text: use `\r\n` for telnet output, preserve original wording and colour
  codes unless the task explicitly changes them.
- JSON: `System.Text.Json` with explicit options; camelCase; string enums;
  no bitmasks; no `"unknown"` placeholders gaining a foothold — fail loudly on
  unrecognised names with the JSON path and expected values.
- Reference the source behaviour in comments where a formula or rule is
  non-obvious, e.g. `// ROM: fight.c, damage()`.
- Keep `Data/` files UTF-8, LF and human-editable. New formats must be
  documented and versioned.
- Licensing: never remove Diku/Merc/ROM copyright notices from imported data or
  documentation. Login screen, `credits` and help entries are compliance
  surfaces — treat changes there as high risk.

## When to ask instead of guessing

- The original ROM behaviour is ambiguous or contradicts the task.
- A task requires dropping or changing game content.
- A change would alter the player-visible experience beyond the task.
- A JSON schema/format decision would be hard to reverse.
- Tests cannot reproduce a bug or a target cannot be met.
- A commit would need to include unrelated or uncertain changes.
