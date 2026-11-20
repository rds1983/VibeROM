# Canonical Names and Tables

Status: implemented by T005.

`VibeROM.Common/Rom` is the single source of truth for ROM flag bits, numeric
values and static tables. The importer, serializer, loader, tests and JSON
Schemas consume these registries instead of maintaining separate name lists.

## Flag registries

`RomFlags` exposes one validated `FlagRegistry` per bit-vector category:

| Registry | Source | Definitions | Persisted |
| --- | --- | --- | --- |
| `Act` | `ACT_*` | 22 | 20 |
| `PlayerAct` | `PLR_*` | 17 | 16 |
| `Affect` | `AFF_*` | 29 | 29 |
| `Offensive` | `OFF_*` + `ASSIST_*` | 21 | 21 |
| `Immunity` | `IMM_*`, used for immunities/resistances/vulnerabilities | 23 | 23 |
| `Form` | `FORM_*` | 26 | 26 |
| `Part` | `PART_*` | 22 | 22 |
| `Room` | `ROOM_*` | 14 | 14 |
| `Exit` | `EX_*` | 10 | 10 |
| `Extra` | `ITEM_*` extra flags | 24 | 24 |
| `Wear` | `ITEM_WEAR*`, `ITEM_TAKE`, `ITEM_WIELD`, `ITEM_HOLD` | 17 | 17 |
| `Container` | `CONT_*` | 5 | 5 |
| `WeaponSpecial` | `WEAPON_*` | 8 | 8 |
| `Communication` | `COMM_*` | 23 | 23 |
| `Wiznet` | `WIZ_*` | 20 | 20 |
| `Furniture` | `STAND_*` | 3 | 3 |
| `Gate` | `GATE_*` | 5 | 5 |

- `Bit` is the mask (`1 << letterIndex`), not the letter.
- Names are canonical PascalCase JSON names.
- `Persisted = false` marks engine-derived flags (`ACT_IS_NPC`, `ACT_PET`,
  `PLR_IS_NPC`) that are never written to JSON.
- `AFF_UNUSED_FLAG` is omitted from the registry and the schema.
- `Container` uses numeric masks `1, 2, 4, 8, 16`.
- `Offensive` includes the six assist bits (`AssistAll` … `AssistVnum`).

`FlagRegistry` validates non-empty names, power-of-two bits, unique bits and
unique names at construction time.

## Value tables

`RomTables` exposes canonical name/value tables:

| Table | Values | Names |
| --- | --- | --- |
| `Races` | 0–28 | all 29 race_table entries |
| `PcRaces` | 1–4 | Human, Elf, Dwarf, Giant |
| `Classes` | 0–3 | Mage, Cleric, Thief, Warrior |
| `Sexes` | 0–2 | Neutral, Male, Female |
| `Positions` | 0–8 | Dead … Standing |
| `Sizes` | 0–5 | Tiny … Giant |
| `Sectors` | 0–7, 9–10 | Inside … Desert (no `Unused`) |
| `ItemTypes` | 1–34 (sparse) | Light … Jukebox |
| `WeaponClasses` | 0–8 | Exotic … Polearm |
| `ApplyLocations` | 0–25 | None … SpellAffect |
| `SkillTargets` | 0–6 | internal cast targets |
| `DamageTypes` | 0–19 | None, Bash, Pierce, Slash, … Sound |
| `Liquids` | 0–35 | Water … Cordial |
| `AttackTypes` | 0–39 | Hit, Slice, … Chill |
| `Specials` | 0–21 | BreathAny … Patrolman |

`ValueTable` validates unique names and values and offers case-insensitive name
lookup. `ValueTable.FromEnum<T>` builds a table from a canonical enum.

## Complex tables

Generated from the ROM source and checked by `RomTables.Validate()`:

- `RaceDefinitions` — 29 races with act/affect/offensive/immunity/resistance/
  vulnerability/form/part masks and the PC-race flag. The trailing duplicate
  `unique` entry in `race_table` is deduplicated.
- `Skills` — 135 skill/spell rows with four class levels and ratings, spell
  function name (or `null`), target, minimum position, gsn, slot, mana, beats
  and messages.
- `Groups` — 27 groups with class ratings and skill/group references.
- `Titles` — 4 classes × 61 levels × 2 sexes. `GetTitle` clamps level to
  `0..60` and uses the female column only for `Sex.Female`.
- `ClassDefinitions`, `PcRaceDefinitions`, `WeaponDefinitions`,
  `LiquidDefinitions`, `AttackTypeDefinitions`, `WiznetDefinitions`,
  `SpecialDefinitions`. `PcRaceDefinitions.BaseStats`/`MaxStats` use character
  stat order (strength, dexterity, intelligence, wisdom, constitution); the
  C table's strength/intelligence/wisdom/dexterity/constitution order is
  remapped when the table is generated.

`FindSkill` and `FindGroup` reproduce ROM's prefix lookup (`skill_lookup`,
`group_lookup`): the first table entry whose name starts with the requested
text wins. This is why the `illusion` group can reference `invis` and resolve to
`invisibility`.

## Schema alignment

The JSON Schemas in `Data/schema/` are not maintained by hand separately from
the registries. `VibeROM.Common.Tests/Rom/SchemaAlignmentTests` reads the
schemas and asserts:

- every persisted flag registry equals the matching `enum` array, in order;
- every value table equals the matching `enum` array, in order;
- player flags, communication flags, wiznet flags, PC races and classes match
  the player schema.

Changing a name or adding a flag therefore requires updating both the registry
and the schema in the same task. The tests fail with the exact category.

## Validation

`RomTables.Validate()` checks:

- expected counts for races, classes, PC races, weapons, liquids, attack types,
  wiznet entries, specials, skills and groups;
- duplicate names in races, skills and groups;
- four class levels/ratings per skill and four ratings per group;
- valid skill targets and minimum positions;
- group references resolve through prefix lookup;
- class base/default groups exist;
- PC-race bonus skills exist and stat arrays have the right width;
- weapon classes and skill names;
- wiznet bit assignments match `RomFlags.Wiznet`;
- title dimensions;
- every flag registry has persisted names.

`RomTables.ValidateOrThrow()` is called during server startup, so a content
table regression fails fast and is logged before the listener starts.

## Adding or renaming a value

1. Add/rename the C# entry in `RomFlags` or `RomTables`.
2. Update the matching enum in `Data/schema/area.schema.json` or
   `player.schema.json`.
3. Update `Docs/Developer/DataFormats.md` if user-facing behaviour changes.
4. Run `dotnet test`; schema alignment and table validation must pass.

Legacy `.are` letters and words are intentionally **not** part of
`VibeROM.Common`. Their parsing belongs to `VibeROM.AreaImporter` (T008/T009).
