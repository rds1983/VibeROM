# Text Engine

Status: implemented by T004. This page covers the shared text utilities in
`VibeROM.Common/Text` and `VibeROM.Common/Randomness`, and the ROM behaviours
they reproduce.

## Argument parsing

`ArgumentParser.OneArgument` is a port of ROM's `one_argument`:

- Skips leading whitespace.
- If the first non-space character is `'` or `"`, the token runs to the matching
  quote; the quotes are removed. Without a closing quote, the token runs to the
  end.
- Otherwise the token runs to the next **space** (ROM does not split unquoted
  tokens on tabs).
- The extracted token is lowercased.
- Leading whitespace after the token is skipped; trailing whitespace in the
  remainder is preserved.
- Returns `ArgumentToken(Value, Remainder)`.

```csharp
var token = ArgumentParser.OneArgument("  'Long Sword' 2.bag");
// token.Value      == "long sword"
// token.Remainder  == "2.bag"
```

`ArgumentParser.Split` repeatedly calls `OneArgument` and drops empty quoted
tokens.

## String helpers

`RomStrings` provides natural C# names for ROM's inverted boolean helpers:

| ROM | C# |
| --- | --- |
| `str_cmp(a,b) == 0` | `RomStrings.EqualsIgnoreCase(a,b)` |
| `str_prefix(a,b) == 0` | `RomStrings.HasPrefixIgnoreCase(b,a)` |
| `str_infix(a,b) == 0` | `RomStrings.ContainsIgnoreCase(b,a)` |
| `str_suffix(a,b) == 0` | `RomStrings.HasSuffixIgnoreCase(b,a)` |
| `smash_tilde(str)` | `RomStrings.SmashTilde(str)` (replaces `~` with `-`) |
| `capitalize(str)` | `RomStrings.Capitalize(str)` (lowercases all, uppercases first) |

All comparisons are `OrdinalIgnoreCase`.

## Colour codes

`Colour.ToAnsi(text, enabled)` converts ROM-style `{x` codes to ANSI escapes.
`Colour.Strip(text)` removes them for logs and plain output.

| Code | Meaning | Code | Meaning |
| --- | --- | --- | --- |
| `{x` | reset | `{n` | reset |
| `{k` | black | `{K` | bright black |
| `{r` | red | `{R` | bright red |
| `{g` | green | `{G` | bright green |
| `{y` | yellow | `{Y` | bright yellow |
| `{b` | blue | `{B` | bright blue |
| `{m` | magenta | `{M` | bright magenta |
| `{c` | cyan | `{C` | bright cyan |
| `{w` | white | `{W` | bright white |
| `{d` | dark grey | `{D` | light grey |

`{{` produces a literal `{`. Unknown codes such as `{z` are preserved, so text
containing braces is not lost. When colour is disabled, known codes are removed
and unknown braces are preserved.

This is a modern addition; the original ROM 2.4b6 source has no colour parser.

## Money

`MoneyFormatter`:

- `Format(gold, silver)` → `"10 gold and 25 silver"` (wallet/score style).
- `FormatPile(gold, silver)` → ROM's `ITEM_MONEY` look text:
  - `"Odd...there's no coins in the pile."`
  - `"Wow. One silver coin."` / `"Wow. One gold coin."`
  - `"There are 2 silver coins in the pile."` / `"There are 2 gold and 3 silver coins in the pile."`

## Dice and randomness

`DiceRoller` takes a `System.Random`, so tests can supply a scripted source.

| Method | ROM | Behaviour |
| --- | --- | --- |
| `NumberPercent()` | `number_percent` | 1..100 inclusive |
| `NumberRange(from,to)` | `number_range` | Inclusive; `(0,0)` → 0; `to < from` → `from` |
| `NumberBits(width)` | `number_bits` | Uniform low `width` bits; width 0..30 |
| `NumberFuzzy(n)` | `number_fuzzy` | −1/0/+1 with 25/50/25 weights, clamped to ≥1 |
| `Dice(number,size)` | `dice` | Sum of `number` rolls of `size`; size 0 → 0; size 1 → `number` |

Notes:

- ROM 2.4b6 has no `number_dice` function; the actual helpers above are the
  complete set from `db.c`.
- A negative die size returns 0 rather than reproducing ROM's accidental
  `number_range(1, negative)` result.
- `NumberBits` rejects widths above 30 to avoid signed-shift overflow.

## `act()` messages

`ActFormatter.Format(format, context, viewer, visibility)` is a pure port of
ROM's `act_new` token expansion. Delivery to `TO_CHAR`, `TO_ROOM`, `TO_VICT`,
`TO_NOTVICT` or `TO_ALL` is handled by the server layer (T025/T027), which
builds one message per recipient because visibility varies.

Tokens:

| Token | Source | Notes |
| --- | --- | --- |
| `$t` | `Text1` | Free text |
| `$T` | `Text2` | Free text |
| `$n` | actor | PC name or NPC short description; `someone` if not visible |
| `$N` | victim | Same visibility rules |
| `$e` / `$E` | actor / victim | he/she/it |
| `$m` / `$M` | actor / victim | him/her/it |
| `$s` / `$S` | actor / victim | his/her/its |
| `$p` | object 1 | Short description; `something` if not visible |
| `$P` | object 2 | Short description; `something` if not visible |
| `$d` | `Door` | First argument, lowercased; `door` if absent |
| `$$` | literal `$` | Modern escape; ROM 2.4b6 does not support it |

- Unknown codes and missing required arguments produce `" <@@@> "` (ROM's
  placeholder). A trailing `$` is handled without crashing.
- The first character of the formatted message is uppercased, matching
  `buf[0] = UPPER(buf[0])`.
- `Format` returns no trailing newline. `FormatLine` appends `\r\n` for telnet
  output (ROM appends `\n\r`; the modern server standardizes on `\r\n`).
- `ActVisibility` supplies `CanSeeActor` and `CanSeeObject` for the recipient.
  `ActVisibility.AllVisible` is available for tests and trusted contexts.
- `IActActor` and `IActObject` are deliberately small interfaces; the domain
  model in T006 will adapt its characters and objects to them.

Example:

```csharp
var context = new ActMessageContext(
    Actor: guard,
    Victim: player,
    Object1: sword,
    Door: "north gate");

var text = ActFormatter.FormatLine("$n opens the $d.", context, viewer);
```

## Tests

- `VibeROM.Common.Tests/Text` — argument parsing, strings, colour, money, every
  `$` token, visibility, placeholders and formatting.
- `VibeROM.Common.Tests/Randomness` — ranges, percent, bits, fuzzy, dice and
  distribution bounds.
