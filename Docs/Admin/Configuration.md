# Configuration

VibeROM reads a single JSON configuration file. Every setting has a safe
default, so the server can start without a file.

## Where the server looks

In order:

1. `--config <path>` or `--config=<path>` on the command line.
2. The `VIBEROM_CONFIG` environment variable.
3. `VibeROM.json` in the current directory, walking up parent directories.
4. `VibeROM.json` next to the server executable.

If no file is found, defaults are used. If an explicitly requested file
(`--config` or `VIBEROM_CONFIG`) is missing, startup fails. If the file exists
but is malformed or invalid, startup fails with every validation error listed.

Relative paths in the file are resolved against the **directory containing the
configuration file**, not the process working directory. This lets a repository
root `VibeROM.json` point at `Data/` and `logs/` reliably.

## Default file

The repository ships [`VibeROM.json`](../../VibeROM.json):

```json
{
  "server": {
    "bindAddress": "0.0.0.0",
    "port": 4000,
    "maxConnections": 100,
    "idleTimeoutSeconds": 600
  },
  "data": {
    "directory": "Data",
    "areas": "areas",
    "players": "players",
    "notes": "notes",
    "gods": "gods",
    "music": "music",
    "schema": "schema"
  },
  "logging": {
    "level": "Info",
    "directory": "logs",
    "filePrefix": "vibrom",
    "maxArchiveFiles": 14
  },
  "gameplay": {
    "autosaveIntervalSeconds": 300
  }
}
```

Unknown properties are rejected, so a typo such as `"prot"` fails startup
instead of being ignored.

## Server

| Key | Type | Default | Environment variable | Meaning |
| --- | --- | --- | --- | --- |
| `server.bindAddress` | string | `0.0.0.0` | `VIBEROM_SERVER__BIND_ADDRESS` | `*`, `localhost`, or an IP address |
| `server.port` | integer 1–65535 | `4000` | `VIBEROM_SERVER__PORT` | Telnet listen port |
| `server.maxConnections` | integer ≥ 1 | `100` | `VIBEROM_SERVER__MAX_CONNECTIONS` | Maximum simultaneous connections |
| `server.idleTimeoutSeconds` | integer 1-86400 | `600` | `VIBEROM_SERVER__IDLE_TIMEOUT_SECONDS` | Idle connection timeout |

## Data paths

`data.directory` may be absolute or relative. The remaining entries are
relative subdirectories under it and may not contain `..` or be absolute.

| Key | Type | Default | Environment variable | Meaning |
| --- | --- | --- | --- | --- |
| `data.directory` | path | `Data` | `VIBEROM_DATA__DIRECTORY` | Root game data directory |
| `data.areas` | relative path | `areas` | `VIBEROM_DATA__AREAS` | Area JSON files + `index.json` |
| `data.players` | relative path | `players` | `VIBEROM_DATA__PLAYERS` | Player saves |
| `data.notes` | relative path | `notes` | `VIBEROM_DATA__NOTES` | Note boards |
| `data.gods` | relative path | `gods` | `VIBEROM_DATA__GODS` | God summaries |
| `data.music` | relative path | `music` | `VIBEROM_DATA__MUSIC` | Song catalogue |
| `data.schema` | relative path | `schema` | `VIBEROM_DATA__SCHEMA` | JSON Schemas |

## Logging

| Key | Type | Default | Environment variable | Meaning |
| --- | --- | --- | --- | --- |
| `logging.level` | enum | `Info` | `VIBEROM_LOGGING__LEVEL` | `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`, `Off` |
| `logging.directory` | path | `logs` | `VIBEROM_LOGGING__DIRECTORY` | Log directory (absolute or relative) |
| `logging.filePrefix` | string | `vibrom` | `VIBEROM_LOGGING__FILE_PREFIX` | File name prefix; no path separators |
| `logging.maxArchiveFiles` | integer ≥ 1 | `14` | `VIBEROM_LOGGING__MAX_ARCHIVE_FILES` | Number of daily archives to keep |

Every message goes to the console and to `<directory>/<prefix>.log`. The active
file rolls daily: the previous file becomes `<prefix>_001.log`,
`<prefix>_002.log`, and so on; only `maxArchiveFiles` archives are kept. The
layout is:

```
timestamp|LEVEL|Logger.Name|message
```

Logger names are categories such as `VibeROM.Server.Program`,
`VibeROM.Common.Configuration` and `VibeROM.AreaImporter.Program`. The area
importer uses the prefix `areaimport` in its own log directory.

Startup uses default logging first so configuration errors are recorded even
when `logging.directory` is invalid. After the file is loaded and validated,
logging is reconfigured with the file's settings.

## Gameplay

| Key | Type | Default | Environment variable | Meaning |
| --- | --- | --- | --- | --- |
| `gameplay.autosaveIntervalSeconds` | integer ≥ 0 | `300` | `VIBEROM_GAMEPLAY__AUTOSAVE_INTERVAL_SECONDS` | Automatic save interval; `0` disables |

## Environment overrides

Any key can be overridden without editing the file. Use the variable names in
the tables above. Environment values take precedence over the JSON file.
Invalid values (for example `VIBEROM_SERVER__PORT=abc`) fail startup with the
variable name in the message.

`VIBEROM_CONFIG` selects the configuration file itself and is not part of the
configuration schema.

Shell example (PowerShell):

```powershell
$env:VIBEROM_SERVER__PORT = "5001"
$env:VIBEROM_LOGGING__LEVEL = "Debug"
dotnet run --project VibeROM.Server
```

## Validation

Startup fails with exit code `1` and a list of every problem, for example:

```
Invalid VibeROM configuration ('D:\Projects\VibeROM\VibeROM.json'):
- server.port must be between 1 and 65535 (was 70000).
- logging.level must be one of Trace, Debug, Info, Warn, Error, Fatal, Off (was 'Loud').
```

Rules:

- `server.bindAddress` must be `*`, `localhost`, or a valid IP address.
- `server.port` must be in `1..65535`.
- `server.maxConnections` must be at least `1`.
- `server.idleTimeoutSeconds` must be between `1` and `86400`.
- Data and log directories must not be empty; data subfolders must be relative
  and must not contain `..`.
- `logging.level` must be one of the listed levels.
- `logging.filePrefix` must be a simple name without path separators.
- `logging.maxArchiveFiles` must be at least `1`.
- `gameplay.autosaveIntervalSeconds` must be `0` or greater.
- Unknown properties and invalid JSON abort startup.

## Command line

```
Usage: VibeROM.Server [--config <path>]

Options:
  --config <path>  Configuration file (default: search for VibeROM.json)
  -h, --help       Show this help
```

Exit codes: `0` success, `1` configuration/startup error, `2` bad arguments.

## Notes

- Configuration contains no passwords or credentials. Do not add secrets; use
  environment variables for machine-specific overrides.
- Never log full player files or password hashes. The logging layer is shared by
  the server and importer and does not log configuration objects wholesale.
- Until T020/T024, the server logs its resolved configuration and exits; the
  network listener is not started yet.
