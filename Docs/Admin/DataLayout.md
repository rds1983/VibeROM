# Data layout

Runtime data lives under the directory configured by `data.directory`
(`Data` by default):

| Path | Contents |
| --- | --- |
| `Data/areas/index.json` | Area load order and ROM provenance |
| `Data/areas/*.json` | One canonical area file per area |
| `Data/schema/*.schema.json` | JSON Schemas used at startup |
| `Data/players/` | Player saves |
| `Data/notes/` | Notes, ideas, penalties, news, changes |
| `Data/gods/` | God/wizlist files |
| `Data/music/songs.json` | Music catalogue |
| `logs/` | NLog output (not committed) |

## Startup

The server reads `index.json`, loads every listed area in order through the
canonical `AreaJson` reader, validates the area and index schemas, checks
cross-references (exits, keys, resets, shops, specials) and vnum ranges,
builds the `World` indexes, applies every area reset and logs the entity
totals. Duplicate vnums, unknown names, schema errors, vnum-range violations,
overlapping area ranges and dangling references stop startup with a message
naming the area and vnum/reset.

Validate the data without starting the server:

```sh
dotnet run --project VibeROM.Server -- --validate-data
```

`--check` is an alias. The command exits `0` when the full corpus validates and
`1` with actionable errors otherwise.

The 52-area snapshot in this repository loads with zero errors. Empty object
materials become `none` at runtime and are logged as warnings; the JSON files
themselves are not rewritten. CI runs the full test suite and the
`--validate-data` command on every push and pull request.

See [DataFormats.md](../Developer/DataFormats.md) for the file format and
[ImportingLegacyData.md](ImportingLegacyData.md) for regenerating the snapshot.
