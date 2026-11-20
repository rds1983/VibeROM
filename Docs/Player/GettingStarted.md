# Getting started

## Connect

The server listens on `server.bindAddress` and `server.port` (by default
`0.0.0.0:4000`). Connect with any telnet/MUD client, for example:

```text
telnet localhost 4000
```

The login screen shows the DikuMUD, Merc DikuMUD and ROM 2.4 credits, the
current MOTD and rules, then asks for a name.

## Create a character

1. Type a new name (2–20 letters/digits, starting with a letter) and confirm
   the spelling.
2. Choose a password (5–80 characters) and type it again.
3. Enter an email address and confirm it.
4. Pick a race from [Races](Races.md).
5. Pick a sex: `M` or `F`.
6. Pick a class from [Classes](Classes.md).
7. Pick an alignment: `G`ood, `N`eutral or `E`vil.
8. Answer the customization question. The point-buy menu is not available
   yet (T029); answer `N` to use the standard starting package.
9. Pick a weapon from the list shown for your class.

Invalid answers simply ask again, so a typo never loses the character.

## Your first character

The standard package matches ROM 2.4b6:

- Your race's base stats, plus +3 to your class's prime attribute.
- `rom basics`, your class's basic and default skills/groups, recall at 50 and
  the weapon you chose at 40.
- A school banner (light), vest (body), your weapon, and a shield unless the
  weapon is two-handed.
- A map of Midgaard in your inventory.
- Level 1, 20 hit points, 100 mana, 100 movement, 3 training sessions and
  5 practices.
- A class title, for example `the Swordpupil` for a level-1 warrior.

You start in the Mud School (`gameplay.startingRoomVnum`, vnum 3700 by
default).

## What happens next

Saving and reconnecting already work: your character is written to
`Data/players/<Name>.json` when you disconnect. Speaking, emotes, tells,
channels and socials are available (see [Communication](Communication.md));
movement and the remaining interaction commands are being ported in the
following tasks. See the [project README](../../README.md) and
[administrator documentation](../Admin/README.md) for running a server.
