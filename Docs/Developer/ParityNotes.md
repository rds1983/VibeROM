# Parity notes

Deliberate deviations from ROM 2.4b6, with the reason and any follow-up. Keep
entries short and link to the code or task that owns them.

## T009 - Legacy section parsers

- **Unnamed flag bits are dropped.** The original data contains act/room bits
  that have no entry in `act_flags`/`room_flags` (for example act bit `E` and
  room bit `I`). ROM keeps the raw bit but no code reads it. The importer drops
  it and emits a warning; `--strict` will reject the import.
- **Non-persisted bits are dropped.** `ACT_IS_NPC` and `ACT_PET` are marked
  non-persisted in `RomFlags`; the runtime derives them (all mobiles are NPCs,
  pet-shop resets set `Pet`). JSON therefore never contains those names.
- **Unknown sectors fall back to `Inside`.** `immort.are` and `sewer.are`
  contain sector `-1`, which ROM never defines. The importer warns and uses
  `Inside` so the room remains loadable.
- **Out-of-range numeric values clamp to zero.** A few joke objects store a
  full flag mask in a food/money field (`grove.are`, `redferne.are`), which
  truncates to a negative number. The importer warns and clamps the value to
  `0`; ROM has no meaningful behaviour for the negative value.
- **Old-format conversion is deterministic.** `convert_mobile()` picks a random
  damage type (`slash`, `pound` or `pierce`). The importer chooses by
  `vnum % 3` so re-imports are byte-identical. The 2.4b6 corpus contains no
  `#MOBOLD`/`#OBJOLD` sections, so this only affects future imports.
- **Old objects keep a level of `0`.** ROM derives old-object levels from the
  reset that loads them (`convert_objects()`), which is not available while the
  object section is parsed. The importer keeps the four file values and leaves
  level at `0`; it does not invent reset context.
- **Empty object materials are preserved.** ROM stores `""` for objects such as
  donation pits and fountains. The JSON schema allows an empty string instead
  of inventing `none`; old-format objects, whose material ROM itself sets to
  an empty string, use `none` because the old loader never supplies one.

## Loader follow-ups (T013)

- ROM's `load_rooms()` sets `ROOM_LAW` for vnums `3000..3399`. The importer
  stores the file flags; the JSON loader must apply the same range rule so
  Midgaard rooms keep their behaviour.
- ROM adds race act/affect/offence/immunity/form/part defaults and
  `ACT_IS_NPC` when mobiles are created. The importer stores only the file
  flags, so the loader must add the race and NPC defaults.
