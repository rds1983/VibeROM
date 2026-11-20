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

## T012 - ROM 2.4b6 corpus migration

- **`thanos.are` is omitted.** It is listed in the source `area.lst` but is not
  present in the available corpus. `Data/areas/index.json` contains the 52
  areas that exist; the missing area is documented in `Data/README.md`.
- **`proto.are` is omitted.** It is the unlisted Merc prototype template with
  empty sections, so importing it would add no entities and no load-order entry.
- **Ten invalid exit keys are cleared.** The source uses room vnums or a
  missing object as key vnums. ROM can never match those keys, so the importer
  sets them to "no key" and records the change in the report's `repairs` list.
  This is behaviour-preserving and keeps `--check` deterministic.
- **Reserved flag bits and source quirks remain warnings.** The 166 warnings
  are unnamed act/room bits, two `-1` sectors mapped to `Inside`, and two
  negative food/money values clamped to `0`. There are no unknown canonical
  names and no errors.

## T013 - JSON world loader

- **Object levels are already final.** ROM's `convert_objects()` rewrites levels
  only for old-format objects; the imported 2.4b6 corpus contains none. The
  loader does not guess levels from resets. If old-format areas are imported in
  future, level conversion belongs in the importer (T009) where the format is
  still known.
- **Empty object materials become `none` at runtime.** The JSON may keep an
  empty string (donation pits, fountains); the loader logs a warning and uses
  `none` in the prototype instance.

## T015 - Reset engine

- **`O` resets use `MaxInWorld` (the file's second field).** This ROM's
  `reset_room` reads `pReset->arg4` for `O`, but `load_resets` leaves `arg4`
  zero for `O`; taken literally no object reset could ever run. The typed model
  already calls the field `MaxInWorld` and the OLC writer emits it as the limit,
  so the engine uses it. Documented rather than reproducing an unusable bug.
- **`ifFlag` is ignored.** ROM reads and discards the first reset number; it is
  not a percentage and does not gate execution in 2.4b6.
- **Area repops clear reset-created contents.** ROM only calls `reset_area` for
  empty areas; the engine additionally destroys NPCs/objects in the area before
  repopulating so repeated tests and repops start from the same state. Players
  still suppress the repop.
- **`D` resets update runtime exit flags.** ROM applies the door state to
  `rs_flags` during file loading and `reset_room` merely restores it. The engine
  performs the same runtime update from the typed `DoorReset` and restores the
  prototype flags at the start of every room reset.

## T022 - Login flow

- **Passwords use PBKDF2-SHA256, not `crypt`.** ROM stores a DES `crypt` hash;
  the port stores `pbkdf2-sha256$iterations$salt$hash` so saves are portable and
  not brute-forceable with the same ease. Verification still uses a constant-
  time comparison.
- **Bans come from configuration.** `gameplay.bannedSites` is a host-prefix list
  and `gameplay.bannedNames` an exact name list; ROM reads `siteban` and
  banned-name files. The behaviour at the prompt is the same, the storage is
  JSON config.
- **Email confirmation is a port addition.** ROM 2.4b6 does not ask for an
  email address during creation; the port keeps the account email in the JSON
  save for administration.
- **Reconnect closes the old session immediately.** ROM marks the old descriptor
  linkdead and resolves the duplicate at the next input. The port saves and
  detaches the old character before loading it for the new connection, so only
  one session can ever own a character.
- **Three wrong passwords disconnect.** ROM counts failures the same way but the
  threshold lives in `LoginService` as a simple constant until a configurable
  access-control task exists.

## T023 - Character creation

- **No stat rolling.** ROM 2.4b6's `nanny()` copies `pc_race_table[race].stats`
  when the race is chosen and adds +3 to the class prime attribute at entry;
  there is no `CON_ROLL_STATS` state. The port follows that exactly rather than
  inventing rerolls.
- **Race stats are stored in character stat order.** ROM's `pc_race_table`
  arrays are strength, intelligence, wisdom, dexterity, constitution; the port
  stores strength, dexterity, intelligence, wisdom, constitution and remaps the
  three non-human rows. The resulting character stats are identical.
- **The customize menu is deferred (T029).** Answering `Y` prints a notice and
  uses the default package; `N` behaves like ROM. The default package, weapon
  choice, outfit, map, title, prime bonus, train/practice and experience match
  `CON_READ_MOTD`.
- **Race help prints the race list.** ROM calls `do_help "race help"`; the help
  command and its content arrive with T031/T062, so the login prompt repeats the
  available races instead.
- **The school room is configurable.** ROM hard-codes `ROOM_VNUM_SCHOOL` (3700);
  the port uses `gameplay.startingRoomVnum`, which defaults to 3700.

## T024 - Game loop and pulses

- **No copyover.** ROM 2.4b6 contains no copyover command, state file or
  `execv`; a graceful save/stop/restart procedure belongs to operations and is
  tracked by T075.
- **Output is not batched per pulse.** ROM flushes descriptor buffers every
  250 ms. The port's session writer drains a channel continuously, which keeps
  prompts responsive; the pulse schedule only drives subsystem updates.
- **Only the calendar is ported here.** The tick advances `time_info` and
  sunlight per `weather_update`, but pressure, temperature, sky changes and
  area repop remain T060.
- **Counters start at zero.** This matches ROM's static `update_handler`
  counters, so the first pulse fires every named update (including the first
  area pulse) before the intervals begin.

## T025 - Channels, socials and emotes

- **Social NPC auto-responses are deferred.** ROM's `check_social` sometimes
  makes an awake NPC echo the social or slap the player. That needs mobile AI
  (T052); the player/victim messages themselves match ROM.
- **AFK tells are runtime-only.** ROM buffers tells in the player buffer; the
  port keeps `Character.TellBuffer` in memory (not in the JSON save), and
  `replay` prints and clears it.
- **Link-dead tell branches are simplified.** Every online character has a
  descriptor, so the "misplaced link" path only triggers for a stale `reply`
  target.
- **Snoop private-room checks are deferred.** Trust, snoop-proof, busy and loop
  checks are implemented; room-owner/private-room checks wait for room flags and
  ownership (T027/T061).
- **Shout wait state is not applied.** ROM sets a 12-beat wait after shouting;
  combat/timing arrives with T050.
- **Pmote/smote personalization matches intent, not the C walker bug.** A
  viewer's name becomes "you", `Name's` becomes "your", and a trailing `Name`s`
  drops the plural `s`.
- **Clan talk is deferred.** `clantalk` needs clans (T061); it is not part of
  T025's channel list.
- **Auction and grats are included.** They are ROM channels and appear in the
  `channels` table, so their commands were ported alongside the listed
  channels.
- **Snoop/wiznet are plumbing plus commands.** Output forwarding, wiznet
  filtering and the trust-gated commands exist; login/logout wiznet events are
  wired in later tasks.