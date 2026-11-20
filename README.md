# VibeROM

A C#/.NET port of **ROM 2.4b6** (May 29 1998), the classic DikuMUD/Merc-derived
multiplayer text RPG. The goal is to preserve the game logic and content while
replacing the C codebase with a clean, tested, modern .NET implementation.

> **Status:** early development. There is no playable build yet.
> See [TASKS.md](TASKS.md) for the roadmap.

## Technology

| Concern | Choice |
| --- | --- |
| Language / runtime | C# on .NET 10 |
| Logging | NLog (console + rolling file) |
| Tests | xUnit |
| Transport | TCP + telnet protocol (as in the original) |
| Game data | Human-readable JSON, validated by JSON Schema |
| Legacy import | `VibeROM.AreaImporter` (`.are` → JSON migration tool) |

## Repository layout

```
Docs/
  Player/      How to play: commands, classes, races, combat, equipment
  Admin/       How to build, configure and administer a running game
  Developer/   Architecture, data formats, testing, contributor guide
Data/
  areas/       Area JSON (one file per area) + index.json
  players/     Player save files (*.json)
  notes/       Boards: notes, ideas, penalties, news, changes (*.json)
  gods/        God/wizlist files (*.json)
  music/       songs.json
  schema/      JSON Schemas for all data files
VibeROM/              Application source and csproj
VibeROM.AreaImporter/ Console tool: original .are files → JSON
VibeROM.Tests/        xUnit test project
VibeROM.slnx          Solution
logs/                 Runtime logs (generated, ignored by git)
```

All runtime game data is JSON and never compiled into the binary. Flags and
enums are stored as readable names, not bitmasks:

```json
"actFlags": [ "Aggressive", "StayArea", "Undead" ],
"class": "Warrior",
"attackType": "Slash"
```

The original `.are` files are **not** stored in this repository. They are import
input for `VibeROM.AreaImporter`, which is the only component allowed to
understand the legacy format. After migration, the JSON files are the source of
truth and are edited/validated directly.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/).

The project scaffold is the first roadmap task (T001). Once it exists:

```sh
dotnet build VibeROM.slnx
dotnet test VibeROM.slnx
dotnet run --project VibeROM
```

Import the original areas (once, from a local ROM 2.4b6 checkout):

```sh
dotnet run --project VibeROM.AreaImporter -- --source <rom>/area --output Data/areas
```

The server listens for telnet connections on port `4000` by default. Connect
with any telnet/MUD client:

```sh
telnet localhost 4000
```

Full build, configuration and administration instructions live in
[Docs/Admin/](Docs/Admin/).

## Documentation

- [Docs/Player/](Docs/Player/) — player-facing game guide
- [Docs/Admin/](Docs/Admin/) — installation, configuration, operations
- [Docs/Developer/](Docs/Developer/) — codebase, data formats, contributor guide

## Credits and license

VibeROM is a derivative work of ROM 2.4b6, Merc 2.1 and DikuMUD. Original
authors retain copyright; their license terms apply to this project.

- DikuMUD — Sebastian Hammer, Michael Seifert, Hans Henrik Stærfeldt,
  Tom Madsen, Katja Nyboe (© 1990, 1991)
- Merc 2.1 — Michael Chastain, Michael Quan, Mitchell Tse (© 1992, 1993)
- ROM 2.4 — Russ Taylor, Gabrielle Taylor, Brian Moore (© 1993–1998)

This project is non-commercial. See [LICENSE](LICENSE) for the full terms,
including the requirement to display the original credits in the login
sequence and the `credits` command.
