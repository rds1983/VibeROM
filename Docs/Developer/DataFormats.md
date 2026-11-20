# Data Formats

Status: **draft** — finalized and implemented by T002, T005, T010 and T013.
`formatVersion` is `1`.

All runtime game data is human-readable JSON. The legacy ROM `.are` format,
pfiles and `music.txt` are import input only; the server never reads them.

## Directory layout

```
Data/
  areas/
    index.json          Load order + provenance (replaces area.lst)
    help.json           One file per area, named after the legacy area file
    social.json
    rom.json
    midgaard.json
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
    ...
```

## Conventions

1. **camelCase** property names.
2. **`formatVersion` first** in every file. Breaking changes bump it; a
   migration is documented and tested.
3. **Enums are strings** in PascalCase (`"Warrior"`, `"Medium"`, `"Slash"`).
4. **Flags are arrays of canonical names**, in original ROM bit order:
   `"actFlags": ["Aggressive", "StayArea", "Undead"]`. Never letters, never
   bitmasks. Empty flags are omitted (absent means none). UI/debug output may
   render the same value as `Aggressive | Undead`.
5. **Named references, not pointers.** Cross-references use numeric vnums.
6. **Typed item data.** Item-type-specific values live in a named object
   (`weapon`, `container`, `food`, …), not in an opaque `values` array. An
   explicitly documented `rawValues` fallback exists only for item types that
   have no mapping yet.
7. **Text is plain.** No `~` terminators; preserve original colour codes,
   spacing and wording. Multi-line text uses `\n` inside the JSON string.
8. **Deterministic output.** 2-space indent, UTF-8 without BOM, LF line
   endings, mobiles/objects/rooms sorted by vnum, helps by keyword and socials
   by name, resets in original order, one trailing newline.
9. **Strict validation.** Schemas use `additionalProperties: false`; unknown
   flag/enum names are errors that include the JSON path. Unknown properties
   catch typos instead of being silently ignored.
10. **Optional empty values are omitted** (`null`, `[]`, `0` only where zero is
    meaningful). Required structural fields are always present.

## `index.json`

```json
{
  "formatVersion": 1,
  "source": {
    "project": "ROM",
    "version": "2.4b6",
    "releaseDate": "1998-05-29"
  },
  "areas": [
    "help.json",
    "social.json",
    "rom.json",
    "midgaard.json"
  ]
}
```

Load order is significant: it matches the legacy `area.lst`. `thanos.are`
(missing from the source) and `proto.are` (not listed) are resolved explicitly
in T012 and documented in `Data/README.md`.

## Area file

One file per area. Abridged example:

```json
{
  "formatVersion": 1,
  "legacyFile": "midgaard.are",
  "name": "Midgaard",
  "credits": "{ 5 35} Merc",
  "vnumRange": { "low": 3000, "high": 3099 },
  "security": 9,
  "builders": "None",

  "helps": [
    {
      "keywords": "rom",
      "level": 0,
      "text": "ROM 2.4 ..."
    }
  ],

  "mobiles": [
    {
      "vnum": 3000,
      "keywords": "guildmaster mage",
      "shortDescription": "the guildmaster",
      "longDescription": "The guildmaster is here, waiting for students.",
      "description": "He looks wise and patient.",
      "race": "Human",
      "sex": "Male",
      "actFlags": ["Sentinel", "Trainer", "Practice"],
      "affectedBy": ["Infrared"],
      "offensiveFlags": [],
      "immunities": [],
      "resistances": [],
      "vulnerabilities": [],
      "form": ["Biped"],
      "parts": ["Head", "Arms", "Legs"],
      "size": "Medium",
      "startPosition": "Standing",
      "defaultPosition": "Standing",
      "level": 20,
      "hitRoll": 5,
      "armor": [0, 0, 0, 0],
      "hitDice": { "count": 10, "size": 8, "bonus": 100 },
      "damage": { "count": 2, "size": 6, "type": "Slash" },
      "gold": 0
    }
  ],

  "objects": [
    {
      "vnum": 3001,
      "keywords": "long sword sword",
      "shortDescription": "a long sword",
      "longDescription": "A long sword lies here.",
      "material": "Iron",
      "itemType": "Weapon",
      "extraFlags": ["Glow"],
      "wearFlags": ["Take", "Wield"],
      "level": 10,
      "weightPounds": 5,
      "costCopper": 100,
      "condition": 100,
      "weapon": {
        "class": "Sword",
        "diceCount": 2,
        "diceSize": 6,
        "bonus": 1,
        "attackType": "Slash"
      },
      "affects": [
        { "location": "HitRoll", "modifier": 2 }
      ],
      "extraDescriptions": [
        { "keywords": "blade hilt", "description": "The hilt is worn..." }
      ]
    }
  ],

  "rooms": [
    {
      "vnum": 3000,
      "name": "The Temple",
      "description": "You are in a vast temple...",
      "roomFlags": ["Indoors", "Safe"],
      "sector": "City",
      "healRate": 100,
      "manaRate": 100,
      "clan": 0,
      "extraDescriptions": [],
      "exits": {
        "north": {
          "toRoom": 3001,
          "flags": [],
          "keyVnum": 0,
          "description": ""
        },
        "down": {
          "toRoom": 3054,
          "flags": ["Closed", "Locked"],
          "keyVnum": 3100,
          "description": "A heavy oak door."
        }
      }
    }
  ],

  "resets": [
    { "type": "Mobile", "ifFlag": 0, "mobileVnum": 3000, "roomVnum": 3000, "maxInWorld": 1 },
    { "type": "Object", "ifFlag": 0, "objectVnum": 3001, "roomVnum": 3000 },
    { "type": "Give",   "ifFlag": 0, "objectVnum": 3001, "mobileVnum": 3000 },
    { "type": "Equip",  "ifFlag": 0, "objectVnum": 3002, "mobileVnum": 3000, "wearLocation": "Wield" },
    { "type": "Put",    "ifFlag": 0, "objectVnum": 3003, "containerVnum": 3004 },
    { "type": "Door",   "ifFlag": 0, "roomVnum": 3000, "direction": "North", "state": "Locked" }
  ],

  "shops": [
    {
      "keeperVnum": 3000,
      "buyTypes": ["Weapon", "Armor"],
      "profitBuy": 120,
      "profitSell": 120,
      "openHour": 0,
      "closeHour": 23
    }
  ],

  "specials": [
    { "mobileVnum": 3000, "procedure": "BreathAny" }
  ],

  "socials": [
    {
      "name": "smile",
      "charNoArgument": "You smile happily.",
      "othersNoArgument": "$n smiles happily.",
      "charFound": "You smile at $N.",
      "othersFound": "$n smiles at $N.",
      "victimFound": "$n smiles at you.",
      "charAuto": "You smile at yourself.",
      "othersAuto": "$n smiles at $mself."
    }
  ]
}
```

Notes:

- `armor` is `[pierce, bash, slash, exotic]`.
- Physical quantities use human units: `weightPounds` (fractional allowed) and
  `costCopper`. The loader converts them to ROM's integer tenths-of-a-pound and
  copper units; the importer converts back without loss.
- `ifFlag` preserves the legacy conditional marker for fidelity; whether the
  ROM 2.4b6 reset engine uses it is verified in T015.
- `resets` order is preserved exactly; it is evaluated top to bottom.
- Reset variants are discriminated by `type`; fields not applicable to a type
  are absent and rejected if present.
- `procedure` uses canonical PascalCase names (`spec_breath_any` → `BreathAny`).
- `exits` keys are `north`, `east`, `south`, `west`, `up`, `down`.
- Room/shop/social fields mirror the original with readable names. The complete
  field list is defined by `area.schema.json` (T002) and the parsers (T009).

### Item-type data

Every `itemType` documents its own object. Examples:

```json
"itemType": "Container",
"container": { "capacity": 50, "keyVnum": 0, "flags": ["Closeable", "PickProof"] }
```

```json
"itemType": "DrinkContainer",
"drink": { "capacity": 5, "current": 5, "liquid": "Water", "poisoned": false }
```

```json
"itemType": "Light",
"light": { "duration": 0 }
```

```json
"itemType": "Wand",
"wand": { "spell": "Magic Missile", "level": 10, "charges": 5, "maxCharges": 5 }
```

The full per-type mapping is a T009 deliverable and must cover every ROM item
type, including `rawValues` only where genuinely unmapped.

## Other data files

| Data | File | Defined by |
| --- | --- | --- |
| Player saves | `Data/players/<Name>.json` | T030 |
| Notes/boards | `Data/notes/*.json` | T036 |
| Music | `Data/music/songs.json` | T037 |
| Gods/wizlist | `Data/gods/*.json` | T061 |
| Bans | `Data/bans.json` | T065 |

They follow the same conventions: `formatVersion`, camelCase, string enums,
no legacy encodings, atomic writes, schema validation.

## Versioning and migration

- A non-breaking addition may keep `formatVersion`.
- Removing/renaming a field, changing semantics or changing a canonical name
  bumps `formatVersion` and requires a migration task and tool.
- The importer writes the current `formatVersion`; it does not emit old ones.
- JSON is the source of truth after initial migration. Re-running the importer
  over hand-edited JSON must never silently overwrite authored content.
