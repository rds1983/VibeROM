# Login

The login screen includes the original DikuMUD, Merc DikuMUD and ROM 2.4
credits, the current MOTD/rules, and then asks for a name.

## Flow

1. New name: confirm the spelling, choose and confirm a password (5–80
   characters), enter and confirm an email address, then create the character.
2. Character creation follows ROM 2.4b6's `nanny()`: race, sex, class,
   alignment, the customization question and weapon choice. Stats are the
   fixed racial values from `pc_race_table`; the class prime attribute gains
   +3 when the character enters the world. ROM 2.4b6 has no stat rolling or
   rerolls.
3. Existing name: verify the password (PBKDF2-SHA256, never `crypt`), load the
   JSON save and enter the world.
4. Three wrong passwords disconnect the client.

Passwords are stored only as PBKDF2 hashes in `Data/players/<Name>.json`.

The standard creation path grants `rom basics`, the class base group and the
class default group, recall at 50, the chosen weapon at 40, the school outfit
(banner, vest, weapon, shield unless the weapon is two-handed), the Midgaard
map, and places the character in `gameplay.startingRoomVnum` (the Mud School,
3700, by default). The point-buy customization menu is tracked by T029; until
then answering `Y` is accepted but the default package is used.

## Access controls

The `gameplay` configuration controls login policy:

| Key | Meaning |
| --- | --- |
| `wizlock` | Reject new names while set |
| `maxOnline` | Maximum logged-in players (`0` = unlimited) |
| `startingRoomVnum` | Starting room for new characters (default `3700`) |
| `bannedSites` | Host/IP prefixes rejected before the name prompt |
| `bannedNames` | Names rejected at the name prompt |

Existing online characters can reconnect; the older connection is saved,
detached and closed before the new one is loaded.

The server writes a save on logout, periodically (every
`gameplay.autosaveIntervalSeconds`) and on reconnect replacement. Saves use the
atomic temp-file/rename path from `PlayerJson`.
