# Area Importer

`VibeROM.AreaImporter` is a one-way migration tool that converts the original
ROM 2.4b6 `.are` files into the JSON format described in
[DataFormats.md](DataFormats.md). It is a development tool, not part of the
server, and the server never references it.

## Why a separate tool

- The legacy format is only needed once (and when importing new legacy areas).
- The server must not contain `~`-string parsing, letter bitmasks or other ROM
  file-format code.
- Migration mistakes must be visible, testable and reproducible.

## Usage

```sh
dotnet run --project VibeROM.AreaImporter -- \
  --source <rom-2.4b6>/area \
  --output Data/areas \
  --report <temp>/import-report.json
```

| Option | Meaning |
| --- | --- |
| `--source <dir>` | Directory containing `area.lst` and the `.are` files |
| `--output <dir>` | Destination for `*.json` and `index.json` |
| `--file <name>` | Import a single area (bypasses `area.lst`) |
| `--report <path>` | Write a JSON import report (counts, warnings, errors) |
| `--check` | Import to a temporary directory and compare with `--output` |
| `--strict` | Treat unknown flags/names and dangling references as errors |

Exit codes: `0` success, `1` data error (nothing written), `2` usage error.

The original `.are` files are **not committed** to this repository. Point
`--source` at a local ROM 2.4b6 checkout when refreshing the initial snapshot.

## Mapping rules

- **Bit letters → canonical names.** `A`/`B`/`C` act bits become
  `"Aggressive"`, `"StayArea"`, … via the shared name registry (T005). The
  importer must never emit raw letters or bitmasks.
- **Numbers → enums.** Sex, size, position, sector, item type, weapon class,
  attack type, wear location, apply location, liquid, reset type, door state,
  shop buy types, and special procedure names.
- **Strings → plain JSON strings.** Leading spaces and `~` terminators are
  removed exactly as the C loader does. Original spelling, colour codes and
  line breaks are preserved.
- **`value[0..3]` → typed objects** per item type (`weapon`, `container`,
  `food`, …). Prove each mapping against `const.c`, OLC and sample areas.
- **Resets → ordered array** with a `type` discriminator. Preserve order and
  the legacy conditional flag.
- **One area file → one JSON file.** Keep the legacy base name and record it as
  `legacyFile`.
- **`area.lst` → `index.json`.** Preserve load order and add source provenance.

## Determinism

- Same input → byte-identical output (2-space indent, LF, UTF-8 without BOM,
  stable property order, entities sorted by vnum, resets in original order).
- Running the importer twice must produce no diff.
- `--check` re-imports into a temporary directory and compares bytes with the
  committed data. Drift means either the importer changed or the JSON was
  hand-edited; both require review. It never overwrites data automatically.

## Error policy

- Unknown flag letters, enum names, specials, item types or malformed sections:
  record file, line, column and raw value.
- In `--strict` mode (default in CI), any unknown value is a failure and no
  output is written. In non-strict mode they are reported and the affected
  value is omitted only if the schema allows it; silent loss is forbidden.
- Duplicate vnums, dangling exit/reset/shop references and invalid vnum ranges
  are errors.
- No partial output: write to a temporary directory and swap on success.

## Tests

- Tokenizer edge cases (`~` strings, `|` sums, comments, EOF, negatives).
- Golden import of small hand-written `.are` fixtures under
  `VibeROM.Tests/Fixtures/`.
- Old-format `#MOBOLD`/`#OBJOLD` and OLC `#AREADATA` fixtures.
- Byte-identical re-run and `--check` drift detection.
- Full-corpus import is run locally and summarized in `Data/README.md`; CI
  validates the committed JSON instead of shipping legacy game data.
