# Communication

Commands and socials use ROM's prefix matching: `gos hello` runs `gossip`,
`smi` runs `smile`, and `'` is shorthand for `say`.

## Speaking and emotes

| Command | What it does |
| --- | --- |
| `say <text>` or `'<text>` | Speak to everyone in the room |
| `emote <text>` | Third-person emote in the room |
| `pmote <text>` | Emote where each viewer's name becomes "you" |
| `pose` | A random pose based on your class and level |
| `smote <text>` | Staff emote; must include your own name |

## Tells

- `tell <player> <text>` sends a private message.
- `reply <text>` answers the last person who told you something.
- `deaf` stops incoming tells and toggles the "tells" channel status.
- `quiet` blocks channels and tells; you still hear says and emotes.
- `afk` marks you away; tells are held and `replay` shows and clears them.
- `gtell <text>` talks to your group.

## Channels

`gossip`, `quote`, `question`/`answer`, `music`, `auction` and `grats` are
global channels. Typing the command with no text toggles the channel on or
off; sending text turns it back on automatically. `quiet` or a revoked
channel privilege blocks sending, and each channel can be switched off to
ignore it.

`shout <text>` reaches everyone on the mud (off-topic shouts can be ignored
with an empty `shout`), while `yell <text>` only reaches your current area.

`channels` lists every channel's on/off status plus your quiet, AFK, shout,
tell, channel and emote restrictions.

## Socials

Socials come from `social.json` and support no target, a target in the room,
or yourself:

```text
smile
smile Bob
smile Alander
```

`socials` lists all available social names. Sleeping characters cannot use
socials except `snore`; `quiet` does not block socials, but an anti-social
setting does.

## Staff

Staff channels and tools follow the same rules:

- `immtalk <text>` talks to immortal staff; empty toggles it.
- `wiznet` toggles or configures wiznet notifications (`on`, `off`, `status`,
  `show`, or an option name).
- `snoop <player>` forwards that player's input and output to you; `snoop`
  yourself cancels all your snoops.
- `nochannels <player>` revokes or restores another player's channel access.

These commands require the relevant trust level.
