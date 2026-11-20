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
  Cross-file/link resolution is T013/T014's job, not the model's.

## Validation

- `AreaData.Validate()` checks metadata, vnum range, duplicate vnums per entity
  type, and validates every nested prototype/reset/shop/special/social/help.
- `MobilePrototype.Validate()` checks masks against `RomFlags`, dice, positions
  and material.
- `ObjectPrototype.Validate()` checks item data type, flags, weight/cost/
  condition, affects and extra descriptions.
- `RoomPrototype.Validate()` checks room flags, rates, exits (direction keys
  match) and extra descriptions.
- `Reset` variants check non-negative vnums, wear slots, door states and the
  randomize door range.
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

## Planned follow-ups

- T007 adds vnum indexes and attach/detach lifecycle helpers.
- T010 maps the model to/from JSON, including typed `ObjectTypeData` and reset
  variants.
- T013/T014 resolve cross-references and enforce consistency across areas.
- T015/T052 apply resets and race defaults.
- T027 replaces `AllVisibleVisibilityService` with real visibility rules.
