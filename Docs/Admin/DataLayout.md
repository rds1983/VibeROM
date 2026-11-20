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
cross-references (exits, keys, resets, shops, specials), builds the `World`
indexes and logs the entity totals. Duplicate vnums, unknown names, schema
errors and dangling references stop startup with a message naming the area and
vnum/reset.

The 52-area snapshot in this repository loads with zero errors. Empty object
materials become `none` at runtime and are logged as warnings; the JSON files
themselves are not rewritten.

See [DataFormats.md](../Developer/DataFormats.md) for the file format and
[ImportingLegacyData.md](ImportingLegacyData.md) for regenerating the snapshot.
