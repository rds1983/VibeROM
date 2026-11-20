# Importing legacy data

`VibeROM.AreaImporter` converts the original ROM 2.4b6 `.are` files into the
JSON files under `Data/areas/`. It is a development/operations tool; the server
never reads `.are` files.

## Prerequisites

- A local ROM 2.4b6 checkout, normally pointing at its `area/` directory.
- The directory must contain `area.lst` and the `.are` files listed in it.
- Original `.are` files are **never committed** to this repository.

## Import

```sh
dotnet run --project VibeROM.AreaImporter -- \
  --source <rom-2.4b6>/area \
  --output Data/areas \
  --report <temp>/import-report.json
```

The importer writes one `<name>.json` per area plus `index.json`. Area files
are canonical JSON (sorted entities, two-space indent, LF, no BOM). Output is
built in a temporary directory and swapped into place, so a failed import
leaves the previous data set untouched.

## Verify the committed data

```sh
dotnet run --project VibeROM.AreaImporter -- \
  --source <rom-2.4b6>/area \
  --output Data/areas \
  --check
```

`--check` builds the expected output in a temporary directory and compares it
byte-for-byte with `Data/areas`. Missing, changed or unexpected JSON files are
reported as drift and the command exits non-zero without modifying the output.

## Single area and strict mode

```sh
dotnet run --project VibeROM.AreaImporter -- \
  --source <rom-2.4b6>/area \
  --output <temp>/areas \
  --file proto.are
```

`--file` bypasses `area.lst`, which is useful for inspecting an unlisted file.
`--strict` promotes warnings (reserved flag bits, clamped values, dangling
references, alternative sector fallbacks) to errors and writes no output.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success, or `--check` found no drift |
| `1` | Data error; no output was written (or drift was found) |
| `2` | Usage error (missing/invalid options) |

The JSON report written by `--report` contains per-area counts and every
warning/error with its source location. See
[AreaImporter.md](../Developer/AreaImporter.md) for the parser and mapping
details.
