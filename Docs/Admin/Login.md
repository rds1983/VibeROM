# Login

The login screen includes the original DikuMUD, Merc DikuMUD and ROM 2.4
credits, the current MOTD/rules, and then asks for a name.

## Flow

1. New name: confirm the spelling, choose and confirm a password (5–80
   characters), enter and confirm an email address, then enter the world.
2. Existing name: verify the password (PBKDF2-SHA256, never `crypt`), load the
   JSON save and enter the world.
3. Three wrong passwords disconnect the client.

Passwords are stored only as PBKDF2 hashes in `Data/players/<Name>.json`.
Character creation (race/class/stats/outfit) is refined by T023; the current
new-player branch creates a baseline Human Warrior so the login and save flow
can be tested end to end.

## Access controls

The `gameplay` configuration controls login policy:

| Key | Meaning |
| --- | --- |
| `wizlock` | Reject new names while set |
| `maxOnline` | Maximum logged-in players (`0` = unlimited) |
| `startingRoomVnum` | Starting room for new characters |
| `bannedSites` | Host/IP prefixes rejected before the name prompt |
| `bannedNames` | Names rejected at the name prompt |

Existing online characters can reconnect; the older connection is saved,
detached and closed before the new one is loaded.

The server writes a save on logout and on reconnect replacement. Saves use the
atomic temp-file/rename path from `PlayerJson`.
