# TASKS

Roadmap for porting ROM 2.4b6 to C#. This file is the single source of truth
for what to work on next. The workflow for picking up, finishing and committing
a task is defined in [AGENTS.md](AGENTS.md).

**How to read this file**

- Work on the **first unchecked task** that has no unfinished dependency.
- Every task lists deliverables and an acceptance test. All of them must pass
  before the task is proposed as finished.
- Only the developer can mark a task as finished. Agents must ask first.
- After a task is confirmed, remaining tasks may be reorganized (added,
  removed, split, reordered) to reflect what was learned.

**Legend:** `[ ]` todo · `[~]` in progress · `[x]` confirmed finished

Last reviewed: 2026-11-20

---

## Guiding principles

1. **Port the game, not the C.** We reproduce behaviour and content. Pointer
   juggling, fixed buffers, `sh_int`, global mutable state and `sprintf` are
   C implementation details, not requirements.
2. **All runtime game data is human-readable JSON.** Areas, players, notes,
   gods, music and configuration data live as JSON in `Data/`. There are no
   `.are` files, pfiles, `music.txt` or other ROM formats at runtime.
3. **Names, not bitmasks.** JSON stores flags and enums as canonical names
   (`["Aggressive", "Undead"]`, `"Warrior"`, `"Slash"`), never raw letters such
   as `"ABCDEFGHIJK"` or opaque integers.
4. **Legacy format lives in one place.** Only `VibeROM.AreaImporter` reads the
   original `.are` format. The server never contains a legacy parser.
5. **JSON is the source of truth.** After the initial migration, data is edited
   as JSON and validated against schemas. The importer remains a migration and
   regression tool, not the runtime loader.
6. **Parity is measurable.** Every command, spell, skill, special procedure and
   area has an auditable status. No silent omissions.
7. **Tests first for logic.** Combat, magic, resets, parsing and saves are
   testable without a socket. Integration tests drive real telnet sessions.
8. **Docs move with code.** Each task updates the relevant `Docs/` page.
9. **Licensing is non-negotiable.** Credits in login, `credits`, and the ROM
   help entry must match the original requirements.

---

## Milestone 0 — Foundation and data migration


---

## Milestone 1 — Server, login and interaction

### [ ] T020 — TCP server and telnet protocol
- Depends on: T003, T007
- Async listener bound to the configured `server.bindAddress` and
  `server.port`, descriptor per connection, output buffering, greeting, telnet
  negotiation (IAC, WILL/WONT/DO/DONT, NAWS, TTYPE, terminal type), ANSI colour.
- Use `Colour.ToAnsi` for socket output and `Colour.Strip` for logs.
- **Done when:** integration tests connect, negotiate, receive the greeting and
  disconnect cleanly; slow/garbage clients cannot crash the server.

### [ ] T021 — Connection lifecycle
- Depends on: T020
- Connection states, input line editing/backspace, idle timeout, max connections
  from configuration, host resolution, link-dead handling, logging of
  connects/disconnects.
- **Done when:** tests cover simultaneous connects, abrupt disconnects, idle
  timeouts and reconnect within the grace window.

### [ ] T022 — Login flow
- Depends on: T021, T030 (player save/load; the returning-character branch
  cannot be completed before persistence exists)
- Greeting → MOTD/rules → name → password (PBKDF2, never `crypt`) → email →
  confirmation → enter world. Include the new-character branch, ban/site
  checks, wizlock, max-online enforcement and reconnect to an existing
  character.
- **Done when:** end-to-end tests cover new, returning, bad password, banned,
  wizlocked and reconnecting clients; login screen contains required credits.

### [ ] T023 — Character creation
- Depends on: T022, T005
- Race/class/sex selection, stat rolling and reroll limits, weapon choice,
  starting groups/skills, starting room, outfit and initial affects.
- **Done when:** created characters match the C defaults for every race/class
  combination (table-driven test), and invalid input re-prompts safely.

### [ ] T024 — Game loop and pulses
- Depends on: T021
- 250 ms pulse with named tick counters; drive descriptor flushing, character,
  object, area and weather updates, time advancement, autosave using
  `gameplay.autosaveIntervalSeconds`, and copyover.
- **Done when:** a 60-second accelerated test proves ticks fire at the expected
  cadence and survive an exception in one subsystem.

### [ ] T025 — Channels, socials and emotes
- Depends on: T022, T013
- say, tell, reply, gossip, quote, yell, shout, gtell, immtalk, question, answer,
  music, emote, pose, smote, pmote, socials (from `social.json`), channels,
  deaf/quiet/nochannels, wiznet and snoop plumbing.
- Expand every message through `ActFormatter` so `$n`/`$N`/`$p` visibility and
  pronouns match ROM.
- **Done when:** every channel has tests for sender/receiver/ignored/deaf
  audiences and prefix matching of social names.

### [ ] T026 — Movement and doors
- Depends on: T024, T016
- Walk in six directions, `go`, exits, doors (open/close/lock/unlock/pick),
  enter/leave triggers, sector movement delay, follow, flee, recall.
- **Done when:** movement matrix tests cover closed/locked/hidden exits,
  follower chains, sector delays and recall restrictions.

### [ ] T027 — Visibility, look and examine
- Depends on: T026
- `can_see`, light sources, darkness, invis/hide/sneak, infravision, room
  description/contents/exits, look in/at, examine, extra descriptions.
- Implement `IVisibilityService`; build `ActVisibility` from the rules and
  format messages per recipient.
- **Done when:** visibility truth tables and room rendering snapshots pass for
  day/night, blind, invis and hidden actors.

### [ ] T028 — Session preferences and aliases
- Depends on: T022
- prompt, colour, brief/compact, autoassist/autoexit/autogold/autolist/autoloot/
  autosac/autosplit, nofollow, noloot, nosummon, afk, wimpy, aliases and
  `unalias`.
- Colour preference controls `Colour.ToAnsi(..., enabled)` and is persisted
  with the player.
- **Done when:** preferences persist across save/load and each toggle has a
  behavioural test.

---

## Milestone 2 — Characters, objects and economy

### [ ] T031 — Character information commands
- Depends on: T030, T027
- score, inventory, equipment, worth, time, weather, who, whois, where, help,
  skills, spells, groups, group, consider, compare, commands, areas, vnum,
  wizlist, credits, info, motd, news, changes, rules.
- **Done when:** snapshot tests cover mortal and immortal views, and help lookup
  handles levels, keywords and aliases like the original.

### [ ] T032 — Object manipulation and encumbrance
- Depends on: T007, T027
- get/drop/give/put/wear/remove/wield/hold/take/sacrifice/junk/donate, `all`
  semantics, weight/count limits, item affects on equip, coins and splitting.
- Use `World` attach/detach/equip helpers; do not mutate owners directly.
- **Done when:** inventory invariant tests (no duplicate ownership, correct
  weight/carry limits, equipment slots) pass across thousands of random actions.

### [ ] T033 — Containers, locks and special items
- Depends on: T032
- Containers, close/open/lock/unlock, keys, boats, fountains, portals, warp
  stones, room keys, furniture, jukebox hooks.
- **Done when:** each item type has an interaction test, including failure paths.

### [ ] T034 — Consumables
- Depends on: T032, T042
- eat, drink, pour, fill, quaff, recite, zap, brandish; food/water/hunger,
  drunkenness, potions, pills, scrolls, wands, staves, poison coating.
- **Done when:** charges, depletion, level checks, spell effects, and hunger/
  thirst thresholds are covered by tests.

### [ ] T035 — Shops, money and the healer
- Depends on: T032
- buy/sell/list/value, haggle, shop hours, keeper profit, `steal` from shops,
  currency handling, healer services.
- **Done when:** pricing matches the original formulas for a matrix of charisma
  and keeper profit values; shop inventory restocks correctly.

### [ ] T036 — Notes, boards and mail (JSON)
- Depends on: T030
- note/read/unread/replay, idea, penalty, news, changes, board expiry, immortal
  forwarding and author filtering. Persist to `Data/notes/*.json` with atomic
  writes and `notes.schema.json` validation.
- **Done when:** persistence across restart, expiry rules, schema rejection and
  permission tests pass for mortal/immortal boards.

### [ ] T037 — Music data and jukebox (JSON)
- Depends on: T002, T003
- Convert the original `music.txt` into `Data/music/songs.json` (one-time
  conversion, committed as JSON; no `.txt` at runtime), validated against
  `music.schema.json`. Load, list, play and drive jukebox objects.
- **Done when:** all bundled songs are present, the JSON validates, and
  playback/listing are snapshot tested.

### [ ] T038 — Rent, quit, delete and autosave
- Depends on: T030, T035
- quit/rent/save/delete, rent costs and inventory stashing, link-dead cleanup,
  autosave cadence and crash-safe writes.
- **Done when:** quit/rent/relogin cycles preserve or intentionally discard
  state exactly as specified, including pets and affects.

---

## Milestone 3 — Progression, skills and spells

### [ ] T040 — Experience, levels and training
- Depends on: T030
- gain, group XP, level advancement, HP/mana/move gains, titles, practice,
  train, adept caps, penalties and death XP loss.
- **Done when:** XP tables and per-level gains match the C formulas for all
  classes; advancement tests cover cap levels and immortal transitions.

### [ ] T041 — Skill framework and mundane skills
- Depends on: T040
- Skill lookup by name/sn, improvement rolls, guild practice, lag, and the
  weapon/utility skills: axe, dagger, flail, mace, polearm, spear, sword, whip,
  shield block, bash, trip, kick, disarm, backstab, rescue, envenom, hide,
  sneak, steal, pick, fast healing, meditation, haggle, lore, recall, scrolls,
  staves, wands, hand-to-hand, berserk, dirt, enhanced damage, parry, dodge.
- Use `DiceRoller` for every roll and `ArgumentParser` for command arguments.
- Resolve skills/groups through `RomTables.FindSkill`/`FindGroup` and use the
  `Skills` definitions for levels, ratings, mana and lag.
- **Done when:** each skill has success/failure tests at low, mid and cap
  proficiency, including lag and positional requirements.

### [ ] T042 — Affect engine
- Depends on: T006
- Affect add/remove/find/join, duration and level handling, apply types,
  stat recomputation, equipment and room affects, dispel magic, and save
  mechanics by damage type.
- **Done when:** property-based tests prove stats always equal base + affects +
  equipment after random sequences of add/remove/expire.

### [ ] T043 — Spell framework
- Depends on: T042, T041
- Casting pipeline: target resolution, mana, cast lag, spell level, saves,
  damage, messages, group spells, object-cast spells, and failure/backfire paths.
- Read cast metadata from the `Skills` table (`SkillTarget`, `Position`,
  `MinMana`, `Beats`, messages).
- **Done when:** the pipeline is tested with stub spells for every target type
  (offensive, defensive, room, object, self, group).

### [ ] T044 — Offensive and utility spells
- Depends on: T043
- Port all damage, curse, dispel and utility spells from `magic.c` (98 total),
  preserving damage formulas, saves and messages.
- **Done when:** every spell in the original table has an implementation and a
  test; none are stubs; a parity report lists all 98 with status.

### [ ] T045 — Affect-heavy and special spells
- Depends on: T042, T043
- sanctuary, protection, bless, curse, poison, plague, disease, haste, slow,
  fly, invis, pass door, stone skin, shield, armour, giant strength, regenerate,
  and object enchantment spells.
- **Done when:** stacking, dispel and expiry interactions are covered by tests.

---

## Milestone 4 — Combat and AI

### [ ] T050 — Combat core
- Depends on: T041, T042
- Combat rounds, THAC0/AC, hit/dam rolls, multi-attacks, damage types and
  resistances, criticals, death, corpses, loot, XP awards, PvP rules, wimpy,
  flee and combat lag.
- Use `DiceRoller` for every combat roll; do not introduce ad-hoc RNG.
- Classify damage with `RomTables.DamageTypes`/`AttackTypes`.
- **Done when:** seeded simulation over thousands of fights stays within
  expected damage ranges and never violates invariants (no dead attackers,
  no negative HP, no orphaned corpses).

### [ ] T051 — Combat skills and tactics
- Depends on: T050
- bash, trip, kick, backstab, disarm, rescue, envenom, berserk, dirt, shield
  block, parry/dodge interactions, assist and tanking.
- Format the three audience messages through `ActFormatter` and real
  `ActVisibility`.
- **Done when:** each tactic has success/failure/lag tests and correct messages
  to all three audiences (actor, victim, room).

### [ ] T052 — Mobile AI and update cycle
- Depends on: T050, T015
- Mobile update: wandering, scavenging, aggressive/pursuit, follower recall,
  special-room behaviour, charm/fear, and idle/mob act flags.
- Apply `RaceDefinitions` defaults (act/affect/offensive/immunity/resistance/
  vulnerability/form/part masks) when mobiles are created, using `World`
  lifecycle helpers and indexes.
- **Done when:** scenario tests show mobs wander within their area, pursue
  correctly, return home and never leak from the world.

### [ ] T053 — Special procedures
- Depends on: T052
- Port the 22 `spec_*` functions from `special.c` (this ROM build has no
  mob/object/room programs) and register them by canonical name from JSON.
- Use `SpecialDefinitions` names as the canonical/legacy mapping.
- **Done when:** each special has a test, is registered in the parity table,
  and an unknown special in JSON fails validation.

### [ ] T054 — Groups, pets and followers
- Depends on: T050, T052
- Group formation, XP split, charm/pets, order, follow chains, group tells and
  summon restrictions.
- **Done when:** group XP and charm lifecycle tests pass, including logout and
  death cleanup.

---

## Milestone 5 — World systems

### [ ] T060 — Time, weather and area repop
- Depends on: T024, T015
- In-game clock, sun/moon phases, weather transitions, sector effects, area age
  and player-count repop rules.
- **Done when:** a simulated 30-day cycle is deterministic under a seed and
  repop decisions match the original rules.

### [ ] T061 — Guilds, clans and sacrifice
- Depends on: T030, T035
- Guild rooms/trainers, clan table and commands, clan chat, clan equipment,
  and sacrifice rewards/behaviour (this ROM build has no player deity or tithe
  system).
- **Done when:** permission and membership tests cover every clan rank, and
  guild-only rooms reject outsiders.

### [ ] T062 — Help and social content tooling
- Depends on: T013, T031
- Validate all JSON help entries and socials, detect duplicates/orphans, provide
  an admin report, and ensure `help rom` matches the required text.
- **Done when:** the content audit runs in CI and fails on missing required
  entries.

---

## Milestone 6 — Immortals and OLC

### [ ] T063 — Immortal information and movement commands
- Depends on: T031
- stat, vnum, areas, alist, resets, mwhere, owhere, goto, transfer, at, force,
  echo/gecho/pecho/zecho, holylight, invis/wizinvis/incognito, poofin/poofout.
- **Done when:** command-level tests cover trust levels 37–60 and every failure
  message for insufficient trust.

### [ ] T064 — Immortal administration commands
- Depends on: T063
- advance, trust, deny, freeze, log, snoop, switch/return, peace, violate,
  load/mload/oload/clone, purge, restore, set, string, flag, title, wizlock,
  wiznet, memory, sockets, asave. Persist god summaries to `Data/gods/*.json`
  using `god.schema.json`.
- **Done when:** each command has permission tests and cannot corrupt player,
  area or world state in failure paths.

### [ ] T065 — Ban and site administration
- Depends on: T022
- ban/permban/allow/pardon, wildcard site matching, ban JSON persistence and
  immunity rules.
- **Done when:** site-matcher table tests and persistence-across-restart tests
  pass.

### [ ] T066 — OLC foundation
- Depends on: T064, T010
- Editor framework, security levels, `edit`, `aedit`, `redit`, `oedit`, `medit`,
  buffer editing, and validation on save. Editors read/write the JSON model.
- **Done when:** create/edit/save/load round-trips preserve every field for
  each entity type.

### [ ] T067 — OLC save and area management
- Depends on: T066
- JSON area writer compatible with the loader, `asave`, area creation and
  deletion, builder attribution and `alist`/`show` reporting.
- **Done when:** an area created entirely in-game reloads identically after a
  restart, with stable JSON formatting.

---

## Milestone 7 — Parity, hardening and release

### [ ] T070 — Command parity audit
- Depends on: Milestones 1–6
- Compare all 242 `cmd_table` entries against the C# command registry; record
  implementation status, trust, position and notes in
  `Docs/Developer/CommandParity.md`.
- **Done when:** every command is implemented or explicitly documented as
  intentionally removed with developer approval.

### [ ] T071 — Spell, skill and special parity audit
- Depends on: T044, T045, T051, T053
- Audit all 98 spells, every skill (`gsn_*`) and the 22 special procedures;
  record formulas, messages and status.
- **Done when:** the parity reports have no unimplemented or undocumented rows.

### [ ] T072 — Content parity and walkthrough
- Depends on: T015, T053
- Load and reset every JSON area; verify vnum references, shops, specials and
  exits. Smoke-walk each area's main path.
- **Done when:** the content report is green and a scripted walkthrough of the
  newbie school completes.

### [ ] T073 — End-to-end test harness
- Depends on: T020, T022, T050
- Scripted telnet client for scenarios: create character, move, fight, shop,
  cast, save, quit, relogin, admin actions. Deterministic seeds and snapshot
  assertions.
- **Done when:** the harness runs in CI against a real server instance and is
  documented for contributors.

### [ ] T074 — Performance and robustness
- Depends on: T073
- Targets: full JSON world load under 2 s, 100 concurrent idle connections,
  stable memory over 1 hour, no unhandled exceptions on malformed input or
  hostile clients.
- **Done when:** benchmarks are recorded in `Docs/Developer/Performance.md` and
  a soak test passes.

### [ ] T075 — Packaging and release
- Depends on: T074
- Framework-dependent and self-contained builds for Windows/Linux, ship
  `VibeROM.json` and `Data/` alongside the binary, data-folder copy step,
  version stamping, optional container image. The AreaImporter ships separately
  as a developer tool.
- **Done when:** a clean machine can build, run and connect using only
  `Docs/Admin/Building.md`.

### [ ] T076 — Documentation completion
- Depends on: Milestones 1–6
- Complete `Docs/Player/` (getting started, commands, classes, races, skills,
  magic, combat, equipment, economy, communication, death, clans, rules) and
  `Docs/Admin/` (build, configure, operate, backup, moderate, OLC, JSON data,
  importing legacy areas).
- **Done when:** every player-facing system and admin procedure has a page, and
  links are verified.

### [ ] T077 — License and credits compliance
- Depends on: T022, T062, T076
- Verify login screen, `credits`, `help rom`, `help diku`, `help merc`, README
  and LICENSE satisfy the Diku/Merc/ROM terms.
- **Done when:** a written compliance checklist in `Docs/Admin/Licensing.md`
  passes with the developer's sign-off.

---

## Backlog / ideas (not scheduled)

- Legacy pfile importer (JSON output, forced password reset).
- JSON format migration tooling for `formatVersion` bumps.
- Reproducible generator for race/skill/group/title tables from the original
  ROM source (current tables are checked-in generated data).
- CI workflow (GitHub Actions): build, test, format, JSON validation and
  importer determinism.
- Optional web client or WebSocket gateway.
- Docker Compose deployment with persistent data volume.
- JSON data linter beyond schema validation (cross-area consistency).
- Metrics endpoint (Prometheus) for connections, ticks and errors.

## Completed

- **T001 — Scaffold projects and build pipeline** (2026-11-20)
  `VibeROM.slnx` with `VibeROM.Common`, `VibeROM.Server`,
  `VibeROM.AreaImporter` and three xUnit test projects; centralized package
  versions, NLog, layering tests, clean build/test/format, and importer
  `--help` verified.
- **T002 — Define the JSON data format and schemas** (2026-11-20)
  Six JSON Schemas (area, index, player, notes, music, god), complete format
  documentation, valid fixtures and negative schema tests; canonical flag
  names, typed item data and strict unknown-property handling finalized.
- **T003 — Configuration and NLog setup** (2026-11-20)
  `VibeROM.json` with validated server/data/logging/gameplay sections,
  environment overrides, config search order and shared console + daily rolling
  file logging; invalid configuration fails fast with every field error.
- **T004 — Core utilities and text engine** (2026-11-20)
  Argument parsing, string helpers, brace colour→ANSI conversion, money
  formatting, dice/random helpers and the full `act()` token expander with
  visibility, pronouns, placeholders and deterministic tests.
- **T005 — Canonical names, constants and flags** (2026-11-20)
  17 flag registries, 15 value tables and the race/skill/group/title/class
  definitions; schema-alignment and round-trip tests guarantee canonical names,
  bit values and table completeness.
- **T006 — Domain data model** (2026-11-20)
  Immutable area/mobile/object/room/exit/reset/shop/help/social prototypes,
  typed item data and resets, mutable characters/objects/rooms/descriptors,
  ownership invariants, `IActActor`/`IActObject` adapters and visibility hooks.
- **T007 — World indexes and entity lifecycle** (2026-11-20)
  `World` prototype/runtime indexes, top counters, help lookup, create/move/
  equip/destroy lifecycle helpers, ownership back-reference validation and a
  5,000-operation fuzz test.
- **T008 — Legacy area tokenizer (importer only)** (2026-11-20)
  `LegacyAreaReader` with ROM `fread_*` semantics, `~` strings, `|` numbers and
  flags, comments, CR/LF normalization and precise file/line/column errors;
  malformed-input and fixture tests.
- **T009 — Legacy section parsers (importer only)** (2026-11-20)
  Full `#AREA`/`#AREADATA`/`#HELPS`/`#MOBILES`/`#MOBOLD`/`#OBJECTS`/`#OBJOLD`/
  `#ROOMS`/`#RESETS`/`#SHOPS`/`#SPECIALS`/`#SOCIALS` parser, importer-only
  prefix lookups, typed item data, old-format conversion, cross-area reference
  validation with positions, modern/old/social fixtures and a full-corpus test
  covering all 53 `.are` files.
- **T010 — JSON serializer, deserializer and converters** (2026-11-20)
  Canonical `AreaJson` reader/writer with schema property order, sorted
  entities, LF/2-space output, flag and enum name resolution with JSON-pointer
  errors, typed item data, reset polymorphism, weight/AC conversions and
  `JsonSchemaValidator`; snapshot, round-trip and full-corpus serialization
  tests.
- **T011 — AreaImporter CLI** (2026-11-20)
  `area.lst`/`--file` import pipeline, canonical area JSON and `index.json`,
  schema validation, atomic directory swap, `--check` drift detection,
  `--strict` warning promotion, JSON import report, NLog diagnostics and
  CLI/runner tests.
- **T012 — Migrate the ROM 2.4b6 corpus to JSON** (2026-11-20)
  Committed 52 canonical area JSON files and `index.json`; documented
  provenance, totals, excluded `thanos.are`/`proto.are`, ten deterministic
  exit-key repairs and 166 source-data warnings; added committed-corpus schema,
  round-trip, totals and dangling-key tests.
- **T013 — JSON world loader** (2026-11-20)
  Validated index reader, schema-checked area loading through `AreaJson`,
  cross-reference validation, material/shop fix-ups, `World.Shops` indexing,
  server startup load and full-corpus load tests; server has no importer
  dependency.
- **T014 — Data validation and hardening** (2026-11-20)
  Vnum-range and overlap validation, hardened malformed/truncated JSON errors,
  `--validate-data` startup mode, corrupt-input and schema-violation tests, and
  a GitHub Actions workflow that builds, tests, formats and validates the
  committed corpus.
- **T015 — Reset engine** (2026-11-20)
  Per-room `M/O/P/G/E/D/R` resets with last-mob/object/level tracking, shop
  stock, runtime doors/exits and deterministic seeded repops; unit and
  real-corpus snapshot tests for Midgaard, Holy Grove, Troll Den and Astral
  Plane.
- **T016 — World bootstrap and full-load test** (2026-11-20)
  `WorldBootstrap` loads and resets the full corpus, reports per-area and total
  inventories, logs startup runtime counts and validates the world; full-corpus
  integration test and `DataInventory.md`.
- **T030 — JSON player save/load** (2026-11-20)
  Canonical player JSON with schema validation, full `Character`/`PlayerData`
  mapping, recursive inventory/equipment and pet data, and atomic
  temp-file/rename saves; round-trip, path-error and crash-safety tests.
