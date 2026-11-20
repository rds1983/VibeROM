using System.Text;

namespace VibeROM.AreaImporter.Legacy;

/// <summary>
/// Tokenizer for the original ROM 2.4b6 area format. Mirrors the
/// <c>fread_letter/word/number/flag/string/to_eol</c> functions but tracks
/// line/column positions and uses modern JSON-friendly line endings.
/// </summary>
public sealed class LegacyAreaReader
{
    public const int MaxWordLength = 4096;

    private readonly string _text;
    private readonly string _fileName;
    private int _index;
    private int _line = 1;
    private int _column = 1;

    public LegacyAreaReader(string text, string fileName = "<memory>")
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _fileName = string.IsNullOrWhiteSpace(fileName) ? "<memory>" : fileName;
    }

    public string FileName => _fileName;

    public bool AtEnd => _index >= _text.Length;

    public LegacyPosition Position => new(_index, _line, _column);

    public char? PeekChar() => AtEnd ? null : _text[_index];

    /// <summary>Peeks at the next non-whitespace character without consuming it.</summary>
    public char? PeekNonWhitespaceChar()
    {
        var index = _index;
        while (index < _text.Length && char.IsWhiteSpace(_text[index]))
        {
            index++;
        }

        return index < _text.Length ? _text[index] : null;
    }

    /// <summary>Skips whitespace and returns the next non-whitespace character.</summary>
    public char ReadLetter()
    {
        SkipWhitespace();
        if (AtEnd)
        {
            throw Error("Expected a letter but reached end of file.");
        }

        return Advance();
    }

    /// <summary>Reads a quoted or whitespace-delimited word.</summary>
    public string ReadWord()
    {
        SkipWhitespace();
        var start = Position;
        if (AtEnd)
        {
            throw Error("Expected a word but reached end of file.", start);
        }

        var builder = new StringBuilder();
        var first = PeekChar()!.Value;

        if (first is '\'' or '"')
        {
            var quote = Advance();
            while (!AtEnd && PeekChar() != quote)
            {
                if (builder.Length >= MaxWordLength)
                {
                    throw Error($"Word exceeds {MaxWordLength} characters.", start);
                }

                builder.Append(Advance());
            }

            if (AtEnd)
            {
                throw Error($"Unterminated {quote} word.", start);
            }

            Advance();
            return builder.ToString();
        }

        while (!AtEnd && !char.IsWhiteSpace(PeekChar()!.Value))
        {
            if (builder.Length >= MaxWordLength)
            {
                throw Error($"Word exceeds {MaxWordLength} characters.", start);
            }

            builder.Append(Advance());
        }

        return builder.ToString();
    }

    /// <summary>Reads a decimal number, including ROM's <c>|</c> additive form.</summary>
    public int ReadNumber()
    {
        SkipWhitespace();
        var start = Position;
        if (AtEnd)
        {
            throw Error("Expected a number but reached end of file.", start);
        }

        var negative = false;
        var first = PeekChar()!.Value;
        if (first == '+')
        {
            Advance();
        }
        else if (first == '-')
        {
            negative = true;
            Advance();
        }

        if (AtEnd || !IsAsciiDigit(PeekChar()!.Value))
        {
            throw Error("Expected a number.", start);
        }

        var value = 0;
        while (!AtEnd && IsAsciiDigit(PeekChar()!.Value))
        {
            var digit = Advance() - '0';
            try
            {
                checked
                {
                    value = value * 10 + digit;
                }
            }
            catch (OverflowException)
            {
                throw Error("Number is too large.", start);
            }
        }

        if (negative)
        {
            value = -value;
        }

        if (!AtEnd && PeekChar() == '|')
        {
            Advance();
            try
            {
                checked
                {
                    value += ReadNumber();
                }
            }
            catch (OverflowException)
            {
                throw Error("Number is too large.", start);
            }
        }

        return value;
    }

    /// <summary>
    /// Reads a flag vector: ROM letters (<c>A</c>..<c>Z</c>, <c>a</c>..<c>z</c>),
    /// decimal numbers, optional signs and <c>|</c> additive components.
    /// </summary>
    public long ReadFlag()
    {
        SkipWhitespace();
        var start = Position;
        if (AtEnd)
        {
            throw Error("Expected a flag value but reached end of file.", start);
        }

        var negative = false;
        if (PeekChar() == '-')
        {
            negative = true;
            Advance();
        }

        long value = 0;

        if (!AtEnd && !IsAsciiDigit(PeekChar()!.Value) && PeekChar() != '-')
        {
            while (!AtEnd && IsAsciiLetter(PeekChar()!.Value))
            {
                value += FlagConvert(Advance());
            }
        }

        if (!AtEnd && PeekChar() == '-')
        {
            Advance();
            return -ReadNumber();
        }

        while (!AtEnd && IsAsciiDigit(PeekChar()!.Value))
        {
            value = value * 10 + (Advance() - '0');
        }

        if (!AtEnd && PeekChar() == '|')
        {
            Advance();
            value += ReadFlag();
        }

        return negative ? -value : value;
    }

    /// <summary>
    /// Reads a <c>~</c>-terminated string. Leading whitespace is skipped, the
    /// terminator is consumed, and CR/LF is normalized to <c>\n</c>.
    /// </summary>
    public string ReadString()
    {
        SkipWhitespace();
        var start = Position;
        if (AtEnd)
        {
            throw Error("Expected a string but reached end of file.", start);
        }

        if (PeekChar() == '~')
        {
            Advance();
            return string.Empty;
        }

        var builder = new StringBuilder();
        while (true)
        {
            if (AtEnd)
            {
                throw Error("Unterminated string (missing '~').", start);
            }

            var character = Advance();
            switch (character)
            {
                case '~':
                    return builder.ToString();
                case '\r':
                    break;
                case '\n':
                    builder.Append('\n');
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
    }

    /// <summary>Consumes the rest of the line and any immediately following newlines.</summary>
    public void ReadToEndOfLine()
    {
        while (!AtEnd && PeekChar() is not ('\n' or '\r'))
        {
            Advance();
        }

        while (!AtEnd && PeekChar() is '\n' or '\r')
        {
            Advance();
        }
    }

    /// <summary>
    /// Reads a line-terminated string (ROM's <c>fread_string_eol</c>), used by
    /// socials. Leading whitespace is skipped and the line ending is consumed.
    /// </summary>
    public string ReadStringEol()
    {
        SkipWhitespace();
        var start = Position;
        if (AtEnd)
        {
            throw Error("Expected a line string but reached end of file.", start);
        }

        var builder = new StringBuilder();
        while (!AtEnd && PeekChar() is not ('\n' or '\r'))
        {
            builder.Append(Advance());
        }

        if (!AtEnd)
        {
            if (PeekChar() == '\r')
            {
                Advance();
            }

            if (!AtEnd && PeekChar() == '\n')
            {
                Advance();
            }
        }

        return builder.ToString();
    }

    public LegacyFormatException CreateError(string message) =>
        new(message, _fileName, Position);

    public LegacyFormatException CreateError(string message, LegacyPosition position) =>
        new(message, _fileName, position);

    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(PeekChar()!.Value))
        {
            Advance();
        }
    }

    private char Advance()
    {
        var character = _text[_index++];
        if (character == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return character;
    }

    private LegacyFormatException Error(string message) => Error(message, Position);

    private LegacyFormatException Error(string message, LegacyPosition position) =>
        new(message, _fileName, position);

    private static bool IsAsciiDigit(char character) => character is >= '0' and <= '9';

    private static bool IsAsciiLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static long FlagConvert(char letter)
    {
        if (letter is >= 'A' and <= 'Z')
        {
            return 1L << (letter - 'A');
        }

        if (letter is >= 'a' and <= 'z')
        {
            return 1L << (26 + (letter - 'a'));
        }

        return 0;
    }
}
