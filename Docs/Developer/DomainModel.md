# Domain Model

Status: implemented by T006.

The domain model lives in `VibeROM.Common/Domain`. It mirrors the ROM entity
model while using explicit references, nullable annotations and immutable
prototype data instead of raw C pointers.

## Prototype vs instance

- **Prototypes** are immutable records loaded from area JSON: `AreaData`,
  `MobilePrototype`, `ObjectPrototype`, `RoomPrototype`, `ExitPrototype`,
  `Reset`, `ShopData`, `HelpEntry`, `SocialData`, `SpecialData`.
- **Instances** are mutable classes created at runtime: `Character`,
  `GameObject`, `Room`, `Descriptor`, `Affect`, `PlayerData`.
- Flags on prototypes and instances are `int` masks interpreted through
  `RomFlags`. Enums (`Race`, `ItemType`, `Position`, `Sex`, `Size`,
  `WeaponClass`, `ApplyLocation`, …) come from `VibeROM.Common.Rom`.
- Names are canonical JSON names; the model never stores ROM letters.

## Entity map

| Entity | Kind | Purpose |
| --- | --- | --- |
| `AreaData` | record | Area metadata plus all prototypes/resets/shops/specials/socials/helps |
| `MobilePrototype` | record | Mobile vnum, descriptions, race, flags, dice, armor, position |
| `ObjectPrototype` | record | Object vnum, descriptions, material, item type, flags, `ObjectTypeData` |
| `ObjectTypeData` | records | Typed item data (`WeaponData`, `ArmorData`, `ContainerData`, …) |
| `RoomPrototype` | record | Room name/description/flags/sector/rates/clan, exits, extra descriptions |
| `ExitPrototype` | record | Direction, destination vnum, exit flags, key vnum, description |
| `Reset` | records | `MobileReset`, `ObjectReset`, `PutReset`, `GiveReset`, `EquipReset`, `DoorReset`, `RandomizeReset` |
| `ShopData` | record | Keeper, buy types, profit percentages, hours |
| `HelpEntry`, `SocialData`, `SpecialData`, `ExtraDescription` | records | Content and metadata |
| `AffectData` | record | Immutable prototype affect |
| `Affect` | class | Mutable instance affect |
| `Character` | class | Live PC or NPC |
| `PlayerData` | class | Player-only state (password hash, skills, groups, aliases, conditions) |
| `GameObject` | class | Live object instance |
| `Room` | class | Live room: prototype plus people, objects and room affects |
| `Descriptor` | class | Connection, login state, input/output buffers |
| `KillData`, `MemoryData`, `GenerationData` | types | Kill counters, AI memory, character-generation choices |
| `NoteData`, `BanData` | records | Board entries and bans |
| `GameTime`, `Weather` | types | Calendar and weather state |

## Value conventions

- `Dice(count, size, bonus)` mirrors ROM's three-die arrays.
- `DamageDice(Dice, AttackType)` keeps the canonical attack noun.
- `ArmorValues(Pierce, Bash, Slash, Exotic)` matches ROM's four AC classes.
- `ObjectPrototype.WeightTenths` uses ROM's internal tenths of a pound; the
  JSON importer/serializer converts to/from `weightPounds`.
- `MobilePrototype.Armor` stores ROM's internal AC units (the importer
  multiplies area-file values by 10, as `load_mobiles` does).
- `ObjectPrototype.CostCopper` is copper coins.
- `ObjectTypeData` is a closed hierarchy. `ObjectPrototype.Validate` rejects a
  data type that does not match `ItemType` (for example a weapon carrying
  `PlainObjectData`).
- `Affect.Where` maps ROM's `TO_*` values. `Affect.Bits` is validated against
  the affect/immunity/weapon-special registry selected by `Where`.
- `PlayerData.LearnedSkills` is keyed by canonical skill name; `SetSkill`
  clamps proficiency to `0..100`.

## Ownership and references

The model uses strong references, not vnums, for live objects:

- `Room.People` → `Character.InRoom`; `Room.Contents` → `GameObject.InRoom`.
- `GameObject` has at most one owner: `Container`, `CarriedBy` or `InRoom`.
  `GameObject.Validate` enforces this.
- A contained object's `Container` must point back to the container, and
  `Room`/`Character` contents must point back to their owner. `Validate`
  methods report broken back-references.
- `Character.Inventory` contains carried items. Equipped items remain in the
  inventory and are additionally referenced by `Character.Equipment`; the
  dictionary key must equal `GameObject.WearLocation`. This mirrors ROM's
  `carrying` list + `wear_loc` field.
- `Character.Master` / `Followers` model charm/follow chains; `Leader` is the
  group leader. `Pet` is the player's pet.
- Prototypes reference each other by vnum in resets/exits/shops/specials.
  Cross-file/link resolution is T011/T013's job, not the model's;
  `LegacyAreaParser.ValidateReferences` reports missing targets across a legacy
  import with source positions.

## Reset engine

`ResetEngine` ports `reset_area`/`reset_room`:

- Resets are grouped by owning room exactly as `load_resets` assigns them
  (`M` room, `O` room, `P` after the last object room, `G`/`E` after the last
  mob room, `D`/`R` explicit).
- Rooms run in vnum order; each room starts with `last=false`, no last mob or
  object, and level `0`. `M` sets the last mob and `level = mob.level - 2`.
- `M`/`O`/`P` check world/room limits; `P` uses the last object container;
  `G`/`E` require a successful preceding reset and the last mob, with shop
  stock levels and `Inventory` flags.
- `D` updates runtime door flags; `R` randomizes runtime exits.
- Areas containing player characters are skipped. Before an empty-area repop,
  NPCs and objects in the area are destroyed so repeated repops are
  deterministic.
- `ResetEngine` takes a seeded `DiceRoller`; tests reset the same area twice
  with the same seed and compare complete snapshots.

## Validation

- `WorldBootstrap.Load(areasDirectory, schemaDirectory, DiceRoller?)` loads the
  full corpus, applies every area reset and returns the world plus a
  `WorldInventory` containing totals and per-area prototype counts. The server
  logs this summary at startup; the committed inventory lives in
  [DataInventory.md](DataInventory.md).
- `AreaData.Validate()` checks metadata, vnum range, duplicate vnums per entity
  type, and validates every nested prototype/reset/shop/special/social/help.
- `MobilePrototype.Validate()` checks masks against `RomFlags`, dice, positions
  and material.
- `ObjectPrototype.Validate()` checks item data type, flags, weight/cost/
  condition, affects and extra descriptions.
- `RoomPrototype.Validate()` checks room flags, rates, exits (direction keys
  match) and extra descriptions.
- `Reset` variants check non-negative vnums, wear slots, door states, the
  randomize door range and `-1` (no limit) max counts.
- `Character.Validate()` checks NPC/player consistency, vitals, inventory and
  equipment references, and affects.
- `GameObject.Validate()` checks condition, flags, owner uniqueness, container
  back-references and affects.
- `Room.Validate()` checks people/objects back-references and duplicates.

## Act and visibility hooks

- `Character` implements `IActActor`; `GameObject` implements `IActObject`, so
  `ActFormatter` can format messages directly.
- `IVisibilityService` supplies `CanSee`/`CanSeeObject`. T027 implements the
  real light/invisibility rules; `AllVisibleVisibilityService` is available for
  tests and trusted contexts.
- `VisibilityExtensions.ToActVisibility` converts the service into the
  `ActVisibility` used by `ActFormatter`.

## World indexes and lifecycle

`World` is the runtime aggregate and the only supported place to change
ownership:

- Constructor takes `AreaData` objects, validates them, rejects duplicate vnums
  across areas, and builds prototype indexes:
  `Mobiles`, `ObjectPrototypes`, `RoomPrototypes`, `Rooms` (runtime), `Helps`,
  and `Shops` (by keeper vnum).
- `TopArea`, `TopMobIndex`, `TopObjectIndex`, `TopRoom`, `TopHelp` are collection
  counts; `TopVnumMob/Obj/Room` are the highest vnums.
- `GetMobile`, `GetObjectPrototype`, `GetRoomPrototype`, `GetRoom` and
  `GetShop` throw `KeyNotFoundException` and log a warning for unknown vnums.
- `WorldReferenceValidator` checks exits, keys, container/portal/map
  destinations, reset targets, shop keepers, specials, prototype vnum ranges
  and overlapping area ranges. `WorldLoader` uses it before returning a
  validated world.
  `TryGetRoom` is the non-throwing variant.
- `FindHelp(keyword)` is a case-insensitive keyword index built from every token
  in help keywords.
- `CreateMobile`, `CreatePlayer` and `CreateObject` register runtime entities.
  `CreateObject` accepts at most one initial destination: room, container or
  carrier.
- `MoveCharacterToRoom`, `MoveObjectToRoom`, `MoveObjectToCharacter` and
  `MoveObjectToContainer` detach from the previous owner before attaching.
  Container cycles are rejected.
- `EquipObject`/`UnequipObject` maintain `Character.Equipment` and
  `GameObject.WearLocation`; equipment items always remain in inventory.
- `DestroyObject` detaches, recursively destroys contents, clears affects/extra
  descriptions and removes the object from the world.
- `DestroyCharacter` removes the character from its room, breaks follower/pet
  and fighting/reply links, destroys inventory/equipment, clears affects and
  removes the character from the world.
- `Validate()` re-checks prototypes, runtime rooms, characters, objects,
  ownership back-references and duplicate registrations. The fuzz test in
  `WorldTests` performs thousands of random moves/equips/destroys and asserts a
  clean world.

## Planned follow-ups

- T007 adds vnum indexes and attach/detach lifecycle helpers.
- T010 maps the model to/from JSON, including typed `ObjectTypeData` and reset
  variants.
- T013 adds the validated JSON world loader and reference checks; T014 hardens
  startup validation and corrupt-input handling.
- T015 adds the reset engine; T052 applies race defaults.
- T027 replaces `AllVisibleVisibilityService` with real visibility rules.
