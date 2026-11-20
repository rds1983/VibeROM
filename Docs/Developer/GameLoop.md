# Game loop and pulses

VibeROM runs ROM's 4 Hz pulse loop (`PULSE_PER_SECOND` in `merc.h`). The
scheduler lives in `VibeROM.Server/Game/PulseEngine.cs` and starts with the
server in `Program`.

## Pulse schedule

One pulse is 250 ms. Counters start at zero, so the first pulse fires every
named update, exactly like ROM's static counters in `update_handler`.

| Update | Interval | Pulses | Handler flag |
| --- | --- | --- | --- |
| Aggression | every pulse | 1 | `GamePulse.Aggression` |
| Violence | 3 s | 12 | `GamePulse.Violence` |
| Mobile | 4 s | 16 | `GamePulse.Mobile` |
| Music | 6 s | 24 | `GamePulse.Music` |
| Tick | 60 s | 240 | `GamePulse.Tick` |
| Area | 120 s | 480 | `GamePulse.Area` |

Handlers implement `IGamePulseHandler` and are registered with
`PulseEngine.Register`. A pulse passes the combined flags that are due, so one
handler can serve several subsystems. ROM's `aggr_update` runs on every pulse;
its combined flag is `Aggression`.

The engine isolates subsystems: an exception from one handler is logged and
the remaining handlers still run on that pulse and later ones. A test throws
from one handler and verifies the next handler keeps receiving pulses.

## Time and sunlight

`GameTime` (`VibeROM.Common/Domain/GameTime.cs`) reproduces ROM's calendar:

- Game hour zero is Unix time `650336715`; one game hour per 60-second tick.
- 24 hours per day, 35 days per month, 17 months per year.
- `GameClockPulseHandler` advances the hour on every tick and logs dawn,
  sunrise, sunset and night transitions.
- Boot sunlight uses `db.c`'s thresholds; `AdvanceHour` uses
  `weather_update`'s transition messages.
- Weather pressure, temperature, sky and area repop belong to T060; this task
  only advances the calendar.

`World.Time` holds the calendar so command handlers can read it once the
`time`/`weather` commands land.

## Autosave

`AutosavePulseHandler` checks `gameplay.autosaveIntervalSeconds` on tick
pulses. When the interval has elapsed it calls
`LoginService.SaveOnlinePlayers`, which snapshots the online descriptors and
saves each character without detaching it. Zero disables autosave.

The server also saves all online players during graceful shutdown before the
listener is disposed. Logout still saves through the normal
`ConnectionClosed` path.

## Output flushing

ROM buffers descriptor output and flushes it once per pulse. VibeROM's
`ClientSession` writer drains a bounded channel continuously, so messages are
not held for 250 ms; the pulse loop does not batch output.

## Copyover

ROM 2.4b6 has no copyover implementation (no command, state file or `execv`).
A graceful restart procedure is therefore an operations feature and is tracked
by T075: save online players, stop the listener, restart the process and let
clients reconnect.

## Tests

- `PulseEngineTests` runs 240 pulses (60 simulated seconds) and asserts
  aggression/violence/mobile/music/tick/area counts, the two-minute schedule,
  and exception isolation.
- `GameTimeTests` covers the epoch mapping, sunlight thresholds, transition
  messages and day/month/year rollover.
- `AutosavePulseHandlerTests` uses a manual `TimeProvider` to prove the
  interval, tick-only behaviour and the zero-disables rule.
- `LoginServiceTests.AutosaveSavesOnlinePlayerWithoutDetaching` proves the
  autosave writes the live character and keeps the session playing.
