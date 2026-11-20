# Data Formats

Status: **format version 1** — defined by T002; canonical reader/writer added
by T010. The JSON Schemas in `Data/schema/` are the authoritative field lists;
this page explains the model and the decisions behind it.

All runtime game data is human-readable JSON. The legacy ROM `.are` format,
pfiles and `music.txt` are import input only; the server never reads them.
`VibeROM.AreaImporter` is the only component that understands `.are`.

## Directory layout

```
Data/
  areas/
    index.json          Load order and provenance (replaces area.lst)
    help.json           One area file per legacy area
    social.json
    rom.json
    ...
  players/
    <Name>.json
  notes/
    notes.json  ideas.json  penalties.json  news.json  changes.json
  gods/
    <Name>.json
  music/
    songs.json
  schema/
    index.schema.json
    area.schema.json
    player.schema.json
    notes.schema.json
    music.schema.json
    god.schema.json
```

## Conventions

1. **camelCase** property names.
2. **`formatVersion` first** in every file. It is currently `1`; breaking
   changes bump it and require a documented migration.
3. **Enums are strings** in PascalCase: `"Warrior"`, `"Medium"`, `"Slash"`.
4. **Flags are arrays of canonical names**, in original ROM bit order:
   `"actFlags": ["Aggressive", "StayArea", "Undead"]`. Never letters, never
   bitmasks. Empty flag sets are omitted; absent means none. UI/debug output may
   render the same value as `Aggressive | StayArea | Undead`.
5. **Cross-references are numeric vnums**, not names or nested objects.
6. **Item-type-specific data lives in a named object** (`weapon`, `container`,
   `drink`, …), never in an opaque `values` array. Plain item types may carry a
   temporary `rawValues` migration fallback.
7. **Text is plain.** No `~` terminators. Colour codes, spacing and wording are
   preserved. Multi-line text uses `\n` inside the JSON string.
8. **Deterministic serialization.** 2-space indent, UTF-8 without BOM, LF line
   endings, mobiles/objects/rooms sorted by vnum, helps by keyword and socials
   by name, resets in original order, one trailing newline.
9. **Strict objects.** Schemas use `additionalProperties: false`; item variants
   use `unevaluatedProperties: false`. Unknown properties and unknown names are
   errors, not silent omissions.
10. **Optional empties are omitted** (`null`, `[]`, and zeros that only mean
    "default"). Required structural fields are always present.

Flag names are canonical PascalCase forms of the C constants. The full lists
are the `enum` arrays in `Data/schema/area.schema.json`; examples:

| C constant | JSON name |
| --- | --- |
| `ACT_AGGRESSIVE` | `Aggressive` |
| `AFF_INFRARED` | `Infrared` |
| `OFF_KICK_DIRT` | `KickDirt` |
| `IMM_FIRE` | `Fire` (also used for resist/vulnerability) |
| `FORM_INSTANT_DECAY` | `InstantDecay` |
| `PART_LONG_TONGUE` | `LongTongue` |
| `ROOM_NO_RECALL` | `NoRecall` |
| `SECT_WATER_NOSWIM` | `WaterNoSwim` |
| `EX_PICKPROOF` | `PickProof` |
| `ITEM_ANTI_GOOD` | `AntiGood` |
| `ITEM_WEAR_FINGER` | `Finger` |
| `APPLY_HITROLL` | `HitRoll` |
| `WEAPON_TWO_HANDS` | `TwoHands` |
| `DAM_SLASH` | `Slash` |
| `LIQ_ORANGE_JUICE` | `OrangeJuice` |

`ACT_IS_NPC` and `ACT_PET` are derived by the engine and are not stored in
`actFlags`. `AFF_UNUSED_FLAG` is not part of the schema.

## `index.json`

```json
{
  "formatVersion": 1,
  "source": {
    "project": "ROM",
    "version": "2.4b6",
    "releaseDate": "1998-05-29"
  },
  "areas": ["help.json", "social.json", "rom.json", "midgaard.json"]
}
```

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `source.project` | string | yes | Source project (`ROM`) |
| `source.version` | string | yes | Source version (`2.4b6`) |
| `source.releaseDate` | date | yes | `1998-05-29` |
| `areas` | string[] | yes | Area file names in load order; unique, lowercase |

Load order is significant and matches the legacy `area.lst`. The index replaces
the old list file; it contains no vnum data itself.

## Area file

One file per area. `help.json` and `social.json` are ordinary area files that
contain only their respective sections.

### Area metadata

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `legacyFile` | string | no | Original `.are` file name, e.g. `midgaard.are` |
| `name` | string | yes | Area display name |
| `credits` | string | no | Original credits line |
| `vnumRange.low` | vnum | yes | Lowest vnum in the area |
| `vnumRange.high` | vnum | yes | Highest vnum in the area |
| `security` | integer 0–9 | no | OLC security level; default `9` |
| `builders` | string | no | OLC builder list |
| `helps` | array | no | Help entries |
| `mobiles` | array | no | Mobile prototypes |
| `objects` | array | no | Object prototypes |
| `rooms` | array | no | Room prototypes |
| `resets` | array | no | Ordered resets |
| `shops` | array | no | Shop definitions |
| `specials` | array | no | Special-procedure assignments |
| `socials` | array | no | Social commands |

Empty sections are omitted. `vnumRange.high >= vnumRange.low` is checked by the
loader (T014), not by the schema.

### Helps

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `keywords` | string | yes | Help keywords |
| `level` | integer -1..100 | yes | Minimum level; `-1` means available to all |
| `text` | string | yes | Help body, no `~` |

### Mobiles

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `vnum` | vnum | yes | Prototype vnum |
| `keywords` | string | yes | Lookup keywords |
| `shortDescription` | string | yes | "the guard" |
| `longDescription` | string | yes | Room line |
| `description` | string | yes | Look description |
| `race` | enum | yes | `Unique`, `Human`, `Elf`, `Dwarf`, `Giant`, and all monster races |
| `actFlags` | enum[] | no | `Sentinel`, `Aggressive`, … |
| `affectedBy` | enum[] | no | Permanent affect flags |
| `offensiveFlags` | enum[] | no | Combat behaviours, including `Assist*` bits |
| `immunities` | enum[] | no | Damage/source immunities |
| `resistances` | enum[] | no | Resistances |
| `vulnerabilities` | enum[] | no | Vulnerabilities |
| `form` | enum[] | no | `Biped`, `Undead`, … |
| `parts` | enum[] | no | `Head`, `Arms`, … |
| `alignment` | integer −1000..1000 | yes | Alignment |
| `group` | integer ≥ 0 | yes | Group number used for group XP |
| `level` | integer 0–100 | yes | Level |
| `hitRoll` | integer | yes | Hit bonus |
| `hitDice` | dice | yes | `{count,size,bonus}` |
| `manaDice` | dice | yes | `{count,size,bonus}` |
| `damage` | damage | yes | `{count,size,bonus,attackType}` |
| `armor` | object | yes | `{pierce,bash,slash,exotic}` |
| `startPosition` | enum | yes | Initial position |
| `defaultPosition` | enum | yes | Idle position |
| `sex` | enum | yes | `Neutral`, `Male`, `Female` |
| `gold` | integer ≥ 0 | yes | Starting wealth |
| `size` | enum | yes | `Tiny` … `Giant` |
| `material` | string | yes | Free-form material name |

`actFlags`, `affectedBy`, `offensiveFlags`, `immunities`, `resistances`,
`vulnerabilities`, `form` and `parts` are the flags written in the area file.
The loader adds the race defaults afterwards, exactly like ROM. They are not
duplicated in the JSON.

The `race` enum contains all 29 distinct `race_table` entries, including
monster races such as `Bat`, `Dragon` and `Wyvern`; player saves use only the
four PC races. `offensiveFlags` includes both `OFF_*` combat bits and the
`ASSIST_*` bits (`AssistAll` … `AssistVnum`).

`armor` values use the original area-file units in JSON. The runtime model
stores ROM's internal AC scale (ten times the file value), so the deserializer
multiplies by 10 and the writer divides by 10. New-format areas have integer
values; old-format conversion (`convert_mobile()`) can leave a fractional
file-unit value such as `6.9`, so mobile armor JSON numbers are not required to
be integral. `damage.attackType` is the attack noun (`Slash`, `Pierce`, …),
from which the engine derives the damage type.

### Objects

Common fields for every item:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `vnum` | vnum | yes | Prototype vnum |
| `keywords` | string | yes | Lookup keywords |
| `shortDescription` | string | yes | "a long sword" |
| `longDescription` | string | yes | Room line |
| `material` | string | yes | Free-form material name; may be empty |
| `itemType` | enum | yes | `Weapon`, `Container`, … |
| `extraFlags` | enum[] | no | `Glow`, `Hum`, `NoDrop`, … |
| `wearFlags` | enum[] | no | `Take`, `Finger`, `Wield`, `Hold`, … |
| `level` | integer 0–100 | yes | Object level |
| `weightPounds` | number ≥ 0 | yes | Human units; model stores tenths |
| `costCopper` | integer ≥ 0 | yes | Cost in copper coins |
| `condition` | integer 0–100 | yes | Condition percentage |
| `affects` | affect[] | no | Object affects / flag affects |
| `extraDescriptions` | extraDescription[] | no | `look at` details |

`affects` entries use:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `where` | enum | no | `Object` (default), `Affect`, `Immune`, `Resist`, `Vulnerable` |
| `location` | enum | yes | `HitRoll`, `ArmorClass`, `None`, … |
| `modifier` | integer | yes | Amount |
| `flags` | enum[] | no | Affect/immunity flags for non-`Object` entries |

`extraDescriptions` entries use `{ "keywords": "...", "description": "..." }`.

#### Item-type data

Each variant requires exactly one matching data object. A wrong or extra data
object is rejected by `unevaluatedProperties`.

| `itemType` | Data object | Fields |
| --- | --- | --- |
| `Light` | `light` | `duration` (integer; `-1` = infinite) |
| `Scroll`, `Potion`, `Pill` | `spells` | `level`, `names` (1–4 spell names) |
| `Wand`, `Staff` | `charges` | `level`, `total`, `remaining`, `spell` |
| `Weapon` | `weapon` | `class`, `diceCount`, `diceSize`, `attackType`, `specialFlags` |
| `Armor` | `armor` | `pierce`, `bash`, `slash`, `exotic` |
| `Container` | `container` | `capacity`, `flags`, `keyVnum` |
| `DrinkContainer` | `drink` | `capacity`, `remaining`, `liquid`, `poisoned` |
| `Fountain` | `fountain` | same shape as `drink` |
| `Food` | `food` | `hours`, `poisoned` |
| `Money` | `money` | `gold`, `silver` |
| `Furniture` | `furniture` | `maxPeople`, `flags` |
| `Portal` | `portal` | `exitFlags`, `gateFlags`, `toRoomVnum` |
| `Map` | `map` | `roomVnum` |
| `Treasure`, `Clothing`, `Trash`, `Key`, `Boat`, `NpcCorpse`, `PcCorpse`, `Protect`, `WarpStone`, `RoomKey`, `Gem`, `Jewelry`, `Jukebox` | none | Optional `rawValues` (exactly 5 integers) |

`weapon.class` is `Exotic`, `Sword`, `Dagger`, `Spear`, `Mace`, `Axe`, `Flail`,
`Whip` or `Polearm`. `weapon.attackType` uses the attack-noun enum (`Hit`,
`Slice`, `Stab`, `Slash`, … `Chill`). `weapon.specialFlags` uses `Flaming`,
`Frost`, `Vampiric`, `Sharp`, `Vorpal`, `TwoHands`, `Shocking`, `Poison`.

`rawValues` is a migration fallback only. It must not be used for a type that
has a named data object, and it should disappear as remaining types are mapped
(`Jukebox` is typed in T037).

Example:

```json
{
  "vnum": 1001,
  "keywords": "sword example",
  "shortDescription": "an example sword",
  "longDescription": "An example sword lies here.",
  "material": "iron",
  "itemType": "Weapon",
  "wearFlags": ["Take", "Wield"],
  "level": 10,
  "weightPounds": 5,
  "costCopper": 100,
  "condition": 100,
  "weapon": {
    "class": "Sword",
    "diceCount": 2,
    "diceSize": 6,
    "attackType": "Slash",
    "specialFlags": ["Flaming"]
  }
}
```

### Rooms and exits

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `vnum` | vnum | yes | Room vnum |
| `name` | string | yes | Room name |
| `description` | string | yes | Room description |
| `roomFlags` | enum[] | no | `Dark`, `Indoors`, `Safe`, … |
| `sector` | enum | yes | `Inside`, `City`, `Forest`, `WaterSwim`, … |
| `healRate` | integer ≥ 0 | yes | Heal multiplier (100 = normal) |
| `manaRate` | integer ≥ 0 | yes | Mana multiplier (100 = normal) |
| `clan` | integer ≥ 0 | yes | Owning clan index, `0` = none |
| `exits` | object | no | Keyed by direction |
| `extraDescriptions` | extraDescription[] | no | Room details |

`exits` keys are `north`, `east`, `south`, `west`, `up`, `down`. Each exit has:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `toRoom` | vnum | yes | Destination room (`0` = unlinked) |
| `flags` | enum[] | no | `IsDoor`, `Closed`, `Locked`, `PickProof`, … |
| `keyVnum` | vnum | no | Key object (`-1` or `0` = none) |
| `description` | string | no | Door description |

### Resets

Resets are an ordered array; order is preserved and evaluated top to bottom.

| `type` | Required fields | Meaning |
| --- | --- | --- |
| `Mobile` | `mobileVnum`, `roomVnum`, `maxInWorld`, `maxInRoom` | Load a mobile |
| `Object` | `objectVnum`, `roomVnum`, `maxInWorld` | Load an object in a room |
| `Put` | `objectVnum`, `containerVnum`, `maxInWorld` | Put an object into a container |
| `Give` | `objectVnum`, `maxInWorld` | Give an object to a mobile |
| `Equip` | `objectVnum`, `wearLocation`, `maxInWorld` | Equip a mobile |
| `Door` | `roomVnum`, `direction`, `state` | Set a door state |
| `Randomize` | `roomVnum`, `door` | Randomize a room door |

Optional `ifFlag` (boolean, default `false`) preserves the legacy conditional
marker. `maxInWorld` and `maxInRoom` are counts; `-1` means no limit. `state` is
`Open`, `Closed` or `Locked`; `door` is an integer `0–6`.

### Shops

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `keeperVnum` | vnum | yes | Mobile vnum |
| `buyTypes` | enum[] | yes | Item types the shop buys |
| `profitBuy` | integer 0–1000 | yes | Buy price percentage |
| `profitSell` | integer 0–1000 | yes | Sell price percentage |
| `openHour` | integer 0–23 | yes | Opening hour |
| `closeHour` | integer 0–23 | yes | Closing hour |

### Specials

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `mobileVnum` | vnum | yes | Mobile vnum |
| `procedure` | enum | yes | Canonical special name, e.g. `Guard` |

The 22 allowed names are `BreathAny`, `BreathAcid`, `BreathFire`,
`BreathFrost`, `BreathGas`, `BreathLightning`, `CastAdept`, `CastCleric`,
`CastJudge`, `CastMage`, `CastUndead`, `Executioner`, `Fido`, `Guard`,
`Janitor`, `Mayor`, `Poison`, `Thief`, `Nasty`, `TrollMember`, `OgreMember`,
`Patrolman`.

### Socials

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `name` | string | yes | Lowercase command name |
| `charNoArgument` | string | yes | To the actor, no target |
| `othersNoArgument` | string | yes | To the room, no target |
| `charFound` | string | yes | To the actor, with target |
| `othersFound` | string | yes | To the room, with target |
| `victimFound` | string | yes | To the target |
| `charNotFound` | string | yes | Actor's target was not found |
| `charAuto` | string | yes | To the actor, targeting self |
| `othersAuto` | string | yes | To the room, targeting self |

All eight message fields may be empty strings; ROM uses an empty field for a
missing message.

A complete area fixture lives at
`VibeROM.Common.Tests/Fixtures/valid-area.json`.

## Player saves

One file per player in `Data/players/<Name>.json`.

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `name` | string | yes | Character name (`^[A-Za-z][A-Za-z0-9]*$`) |
| `passwordHash` | string | yes | PBKDF2 hash; never plaintext |
| `race` | enum | yes | `Human`, `Elf`, `Dwarf`, `Giant` |
| `class` | enum | yes | `Mage`, `Cleric`, `Thief`, `Warrior` |
| `sex` | enum | yes | `Neutral`, `Male`, `Female` |
| `trueSex` | enum | no | Original sex before polymorph |
| `clan` | string | no | Clan name |
| `level` | integer 0–100 | yes | Level |
| `trust` | integer 0–100 | no | Trust level |
| `security` | integer 0–9 | no | OLC security |
| `colourEnabled` | boolean | no | Colour output enabled (default `true`) |
| `rent` | integer ≥ 0 | no | Retained rent value |
| `roomVnum` | vnum | yes | Saved room |
| `vitals` | object | yes | `hit`, `mana`, `move`, `maxHit`, `maxMana`, `maxMove` |
| `attributes` | object | yes | `strength`, `dexterity`, `intelligence`, `wisdom`, `constitution` |
| `money` | object | yes | `gold`, `silver` |
| `experience` | integer ≥ 0 | no | Experience points |
| `practice` | integer ≥ 0 | no | Practice sessions |
| `train` | integer ≥ 0 | no | Training sessions |
| `savingThrow` | integer | no | Save bonus |
| `alignment` | integer −1000..1000 | no | Alignment |
| `hitRoll` | integer | no | Hit bonus |
| `damageRoll` | integer | no | Damage bonus |
| `armor` | object | no | `pierce`, `bash`, `slash`, `exotic` |
| `wimpy` | integer ≥ 0 | no | Wimpy threshold |
| `position` | enum | yes | Current position |
| `conditions` | object | yes | `drunk`, `full`, `thirst`, `hunger` (−100..100) |
| `invisLevel` | integer 0–100 | no | Invisibility level |
| `incogLevel` | integer 0–100 | no | Incognito level |
| `lastLevel` | integer ≥ 0 | no | Level before remort/advance bookkeeping |
| `points` | integer | no | Creation points |
| `played` | integer ≥ 0 | no | Seconds played |
| `lines` | integer ≥ 0 | no | Terminal lines |
| `prompt` | string | no | Prompt format |
| `title` | string | no | Character title |
| `bamfin` | string | no | Arrival message |
| `bamfout` | string | no | Departure message |
| `playerFlags` | enum[] | no | `AutoAssist`, `AutoExit`, `HolyLight`, … |
| `communicationFlags` | enum[] | no | `Brief`, `Combine`, `Afk`, … |
| `wiznetFlags` | enum[] | no | `On`, `Ticks`, `Logins`, … |
| `affectedBy` | enum[] | no | Permanent affect flags |
| `aliases` | array | no | `{name, replacement}` |
| `skills` | array | no | `{name, proficiency}` (0–100) |
| `groups` | string[] | no | Skill group names |
| `affects` | array | no | `{type?/flags?, skill?, where?, level, duration, location, modifier}` |
| `inventory` | itemInstance[] | no | Carried items |
| `equipment` | equippedItem[] | no | `{wearLocation, item}` |
| `pet` | object | no | `{vnum, name, level, sex?, race?, …}` |
| `lastLogoff` | date-time | no | Last logout |
| `lastSaved` | date-time | no | Last save |
| `lastNote` | integer ≥ 0 | no | Last note timestamp |
| `lastIdea` | integer ≥ 0 | no | Last idea timestamp |
| `lastPenalty` | integer ≥ 0 | no | Last penalty timestamp |
| `lastNews` | integer ≥ 0 | no | Last news timestamp |
| `lastChanges` | integer ≥ 0 | no | Last changes timestamp |
| `confirmDelete` | boolean | no | Pending delete confirmation |

`itemInstance` is recursive: `{ "vnum", "condition"?, "timer"?, "contents"?,
"affects"?, "extraDescriptions"? }`. `equipment.wearLocation` is `Light`,
`FingerLeft`, `FingerRight`, `Neck1`, `Neck2`, `Body`, `Head`, `Legs`, `Feet`,
`Hands`, `Arms`, `Shield`, `About`, `Waist`, `WristLeft`, `WristRight`,
`Wield`, `Hold` or `Float`.

A complete player fixture lives at
`VibeROM.Common.Tests/Fixtures/valid-player.json`.

`VibeROM.Common.Serialization.PlayerJson` provides `Serialize`, `Deserialize`,
`Save` and `Load`. Saves are canonical JSON validated against
`player.schema.json`; `Save` writes a sibling temporary file, flushes it to
disk and atomically renames it over the target, so a killed process leaves
either the old save or the new one, never a partial file. A schema-invalid
character is rejected before the target file is touched.

## Notes

One file per board: `notes.json`, `ideas.json`, `penalties.json`, `news.json`,
`changes.json`.

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `board` | enum | yes | `Notes`, `Ideas`, `Penalties`, `News`, `Changes` |
| `notes` | array | yes | Board entries |

Each entry: `id` (integer), `sender` (string), `date` (date-time),
`recipients` (string[]), `subject` (string), `text` (string), optional
`expiresAt` (date-time).

## Music

`Data/music/songs.json` replaces `music.txt`.

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `songs` | array | yes | Song catalogue |

Each song: `group`, `name`, `lyrics` (all strings, group/name non-empty).

## Gods

One file per immortal in `Data/gods/<Name>.json`.

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `formatVersion` | integer | yes | Always `1` |
| `name` | string | yes | Display name |
| `level` | integer 0–100 | yes | Level |
| `trust` | integer 0–100 | yes | Trust |

## Validation

- JSON Schema 2020-12. Schemas live in `Data/schema/` and are the source of
  truth for field names and enum values.
- `VibeROM.Common.Tests/SchemaTests.cs` validates one valid fixture per schema
  and asserts that invalid fixtures fail at the expected JSON path (unknown
  flag, missing item data, wrong item data, unknown property, invalid class).
- Run validation with `dotnet test VibeROM.slnx`.
- Schema validation is local and structural. Cross-file checks — duplicate
  vnums, dangling exits/resets/shops, vnum ranges — belong to the loader
  (T014).

## Serialization API

`VibeROM.Common.Serialization.AreaJson` is the single reader/writer for area
files:

| Member | Purpose |
| --- | --- |
| `AreaJson.Serialize(AreaData)` | Canonical string with one trailing `\n` |
| `AreaJson.Deserialize(string, JsonSchema?)` | Parse and validate; throws `DataFormatException` |
| `AreaJson.Options` | `JsonSerializerOptions` with the area converter, 2-space indent and LF |
| `JsonSchemaValidator.Load(path)` | Load and cache one schema file |
| `JsonSchemaValidator.Validate(schema, json)` | Return `"{json-pointer}: {detail}"` errors |
| `JsonSchemaValidator.ValidateOrThrow(...)` | Throw a `DataFormatException` with every error |

Canonical output follows the conventions above: schema property order,
mobiles/objects/rooms by vnum, helps by keyword, socials by name, resets in
original order, empty optional values omitted and `formatVersion` first.
`AreaJson.Deserialize` resolves every flag and enum name through `RomFlags` and
`RomTables`; unknown or non-persisted names throw a `DataFormatException` whose
`Path` is the JSON Pointer of the offending value (for example
`/mobiles/0/race`).

Two values differ between JSON and the runtime model:

- `weightPounds` is a human-readable number (`0.1`) and the model stores
  `WeightTenths` (`1`).
- Mobile `armor` is in area-file units and the model stores ROM's internal
  scale (ten times larger).

## World loading

`VibeROM.Common.Serialization.WorldLoader.Load(areasDirectory, schemaDirectory)`
is the runtime entry point:

1. Reads and validates `areas/index.json` with `index.schema.json`.
2. Loads each file in index order through `AreaJson.Deserialize(json,
   area.schema.json)`.
3. Applies prototype fix-ups: empty object materials become `none`, and shops
   are indexed by keeper mobile vnum (`World.Shops`).
4. Builds a `World` (rejecting duplicate vnums across areas) and runs
   `WorldReferenceValidator` plus `World.Validate()`.

`WorldReferenceValidator` checks exits, exit keys, container keys, portal/map
destinations, every reset target, shop keepers and special-procedure mobiles,
and reports the area, room/reset and missing vnum. It also verifies that every
mobile/object/room lies inside its area's `vnumRange` and that non-empty area
ranges do not overlap. The server calls the loader during startup and logs the
entity totals; no legacy `.are` parsing is involved.

Object levels are already final prototype data in JSON. ROM's
`convert_objects()` only rewrites old-format objects; the imported 2.4b6 corpus
contains none.

## Versioning and migration

- A non-breaking addition keeps `formatVersion = 1`.
- Removing/renaming a field, changing semantics or renaming a canonical flag
  bumps `formatVersion`, requires a migration tool and updates all schemas,
  fixtures, importer and loader in the same change.
- JSON is the source of truth after the initial migration. Re-running the
  importer over hand-edited JSON must never silently overwrite authored
  content.
