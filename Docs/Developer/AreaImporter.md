# Area Importer

`VibeROM.AreaImporter` is a one-way migration tool that converts the original
ROM 2.4b6 `.are` files into the JSON format described in
[DataFormats.md](DataFormats.md). It depends only on `VibeROM.Common`; it is a
development tool, not part of the server, and the server never references it.

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

### Output and atomicity

- Reads files in `area.lst` order until `$`. `--file` bypasses the list.
- Writes one `<legacy-base>.json` per area plus `index.json` with source
  provenance (`ROM`, `2.4b6`, `1998-05-29`) and the load order.
- Builds the complete output in a temporary sibling directory, validates every
  area and the index against `Data/schema/`, then swaps the directory into
  place. A failure never leaves a partial data set; the previous output is
  restored on swap failure.
- `index.json` and every area file are byte-deterministic; the only
  run-specific path is in the separate import report.

### Report

`--report <path>` writes a deterministic JSON report:

```json
{
  "source": "ROM",
  "version": "2.4b6",
  "releaseDate": "1998-05-29",
  "sourceDirectory": "...",
  "areas": [
    { "legacyFile": "midgaard.are", "jsonFile": "midgaard.json", "mobiles": 65, ... }
  ],
  "warnings": [
    { "file": "tohell.are", "line": 168, "column": 7, "message": "..." }
  ],
  "errors": []
}
```

Counts are per area; warnings and errors carry file, line and column when the
source location is known. The report is written even when the import fails.

### Check and strict modes

- `--check` builds the output in a temporary directory and compares every
  expected `*.json` byte-for-byte. Missing, changed and unexpected JSON files
  are drift and exit `1`; the committed output is never modified.
- `--strict` promotes every warning (unknown reserved flag bits, clamped
  values, dangling references, alternative sector fallbacks) to an error and
  writes no output.

The original `.are` files are **not committed** to this repository. Point
`--source` at a local ROM 2.4b6 checkout when refreshing the initial snapshot.

## Legacy tokenizer

`VibeROM.AreaImporter.Legacy.LegacyAreaReader` mirrors ROM's `fread_*`
functions and tracks 1-based line/column positions:

| Method | ROM equivalent | Behaviour |
| --- | --- | --- |
| `ReadLetter()` | `fread_letter` | Skips whitespace and returns one character |
| `ReadWord()` | `fread_word` | Quoted (`'…'`, `"…"`) or whitespace-delimited word; max 4096 chars |
| `ReadNumber()` | `fread_number` | Optional sign, decimal digits, `|` additive form (`1\|64\|1048576`); leaves non-space delimiters unread |
| `ReadFlag()` | `fread_flag` | Letter bits (`A`..`Z`, `a`..`z`), numbers, signs and `|` sums; returns `long` |
| `ReadString()` | `fread_string` | Skips leading whitespace, reads to `~`, normalizes CR/LF to `\n`; `~` alone is empty |
| `ReadStringEol()` | `fread_string_eol` | Skips leading whitespace, reads one line and consumes its ending; used by socials |
| `ReadToEndOfLine()` | `fread_to_eol` | Consumes the comment and any immediately following newlines, leaving the next non-newline character |
| `PeekNonWhitespaceChar()` | - | Looks ahead across whitespace without consuming, for section terminators such as `S` and `#0` |

- Errors throw `LegacyFormatException` with `FileName`, `Line`, `Column`,
  `Offset` and a message formatted as `file:line:column: detail`.
- Unterminated strings/quotes, missing numbers, invalid digits, number overflow
  and over-long words are hard errors.
- `ReadFlag` does not map letters to canonical names; that mapping belongs to
  `LegacyLookup` using `RomFlags`/`RomTables`.
- A valid fixture lives at
  `VibeROM.AreaImporter.Tests/Fixtures/tokenizer.are`; edge cases are covered by
  `LegacyAreaReaderTests`.

## Section parsers

`LegacyAreaParser` reads a whole file and switches on the uppercase section
name, matching `boot_db()` in `db.c`:

| Section | Behaviour |
| --- | --- |
| `#AREA` | Legacy header: file, display name, credits, low/high vnum |
| `#AREADATA` | OLC header: `Name`, `Builders`, `VNUMs`, `Security`, `Credits`, `End` |
| `#HELPS` | `level keyword~ text~`; a keyword beginning with `$` terminates the section |
| `#MOBILES` | Modern mobile prototype, including `F <field> <flags>` removals |
| `#MOBOLD` | Old mobile prototype, converted with ROM's `convert_mobile()` rules |
| `#OBJECTS` | Modern object prototype and item-type-specific values |
| `#OBJOLD` | Old object prototype with numeric item type and fixed value layout |
| `#ROOMS` | Room, sector, flags, `H`/`M`/`C`/`O`/`D`/`E` fields and `S` terminator |
| `#RESETS` | `M`, `O`, `P`, `G`, `E`, `D`, `R`; `*` comments; `S` terminator |
| `#SHOPS` | Keeper, five buy types, profit and hours; `0` terminator |
| `#SPECIALS` | `M <vnum> <procedure>`; `*` comments; `S` terminator |
| `#SOCIALS` | Eight `fread_string_eol` message fields; `#` ends one social, `#0` the section |

A file with no `#AREA`/`#AREADATA` header (for example `help.are` and
`social.are`) produces a synthetic area whose name comes from the file name and
whose `legacyFile` records the original name. Trailing whitespace at end of
file is ignored.

Every parse result also carries a `References` list (mobile/object/room vnums
plus the source position that named them). `LegacyAreaParser.ValidateReferences`
takes the results of a whole import and reports targets that no area defines;
this must run after every file has been parsed because exits, resets, shops,
specials, container keys, portals and maps may point into another area.

## Mapping rules

- **Bit letters -> canonical names.** `A`/`B`/`C` act bits become
  `"Sentinel"`, `"Scavenger"`, ... through `RomFlags`. The importer never emits
  raw letters or bitmasks. Bits with no entry in the flag table (real data
  contains several reserved gaps) and non-persisted runtime bits (`IsNpc`,
  `Pet`) are dropped with a `LegacyParseWarning`; they have no ROM behaviour.
- **Numbers -> enums.** Sex, size, position, sector, item type, weapon class,
  attack type, wear location, apply location, liquid, reset type, door state,
  shop buy types and special procedure names resolve through `RomTables`, using
  ROM's case-insensitive prefix lookup where the C loader does.
- **Strings -> plain JSON strings.** Leading spaces and `~` terminators are
  removed exactly as the C loader does. Original spelling, colour codes and
  line breaks are preserved. Mobiles uppercase the first character of the long
  description and description, as ROM does.
- **`value[0..4]` -> typed objects** per item type (`weapon`, `armor`,
  `container`, `drink`, `food`, `money`, ...). Unmapped types keep
  `rawValues`. `money` maps `value[0]` to silver and `value[1]` to gold.
- **Resets -> ordered array** with a `type` discriminator. The legacy
  conditional flag is preserved and `-1` max counts are kept (ROM means "no
  limit").
- **One area file -> one JSON file.** Keep the legacy base name and record it as
  `legacyFile`.
- **`area.lst` -> `index.json`.** Preserve load order and add source provenance.

### Old-format conversion

Old mobiles pass through the equivalent of `convert_mobile()`: level-derived
hit/mana/damage dice, AC interpolation, wealth scaling, `Warrior` act flag,
race defaults and a material of `none`. ROM picks the converted damage type
randomly; the importer chooses `Slash`, `Pound` or `Pierce` from `vnum % 3` so
migration stays deterministic (recorded in `ParityNotes.md`).

Old objects keep their four stored values, apply ROM's armour value fixup and
use a level of `0`, because ROM derives old-object levels from reset context at
boot. The shipped 2.4b6 corpus contains no `#MOBOLD`/`#OBJOLD` sections.

## Determinism

- Same input -> byte-identical output (2-space indent, LF, UTF-8 without BOM,
  stable property order, entities sorted by vnum, resets in original order).
  T011 writes files through `AreaJson.Serialize`, the canonical writer described
  in [DataFormats.md](DataFormats.md).
- Running the importer twice must produce no diff.
- `--check` re-imports into a temporary directory and compares bytes with the
  committed data. Drift means either the importer changed or the JSON was
  hand-edited; both require review. It never overwrites data automatically.

## Error policy

- Unknown enum names, specials, item types, malformed numbers, duplicate vnums
  and unexpected section markers throw `LegacyFormatException` with file, line,
  column and the offending value.
- Flag bits with no canonical name are dropped with a warning because ROM
  defines no behaviour for them. Non-persisted bits (`IsNpc`, `Pet`) are dropped
  because the runtime derives them.
- Data quirks with a single ROM interpretation are repaired with a warning:
  a non-numeric `#AREA` vnum range becomes `0 0`, unknown sectors become
  `Inside`, and negative money/food values clamp to `0`.
- `--strict` (T011) turns warnings into failures and writes no output.
- No partial output: write to a temporary directory and swap on success.

## Tests

- Tokenizer edge cases (`~` strings, `|` sums, comments, EOF, negatives).
- Section parsers against hand-written modern, old-format and social fixtures
  under `VibeROM.AreaImporter.Tests/Fixtures/`; these fixtures are committed.
- `LegacyAreaParserTests` asserts every mapped entity, reset, shop, special and
  social field, plus failure paths (duplicate vnums, unknown sections, race,
  reset command, malformed dice) with source positions.
- `CommandLineTests` covers option syntax, required values and unknown options.
- `ImportRunnerTests` imports a temporary fixture area set twice and asserts
  byte-identical area files and index, schema validity, report contents, check
  drift, strict promotion, missing-file failure, single-file mode and that a
  failed import leaves the previous output untouched.
- `VIBEROM_ROM_AREA_DIR=<rom>/area dotnet test` additionally parses all 53
  original `.are` files, validates every area and asserts the counts for
  `proto.are`, `rom.are` (27 helps), `help.are` (141 helps) and `social.are`
  (244 socials). The same run serializes every parsed area through
  `AreaJson.Serialize`, validates it against `area.schema.json` and asserts a
  byte-identical round-trip. The tests skip when the environment variable is
  absent, so no legacy game data is committed to the repository.
