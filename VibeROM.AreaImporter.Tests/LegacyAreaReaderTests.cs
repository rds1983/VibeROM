using VibeROM.AreaImporter.Legacy;

namespace VibeROM.AreaImporter.Tests;

public sealed class LegacyAreaReaderTests
{
    [Fact]
    public void ReadsValidFixtureInOrder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tokenizer.are");
        var reader = new LegacyAreaReader(File.ReadAllText(path), path);

        Assert.Equal('#', reader.ReadLetter());
        Assert.Equal("AREA", reader.ReadWord());
        Assert.Equal("tokenizer.are", reader.ReadString());
        Assert.Equal("Tokenizer Example", reader.ReadString());
        Assert.Equal("Credits line", reader.ReadString());
        Assert.Equal(1000, reader.ReadNumber());
        Assert.Equal(1099, reader.ReadNumber());
        Assert.Equal('*', reader.ReadLetter());
        reader.ReadToEndOfLine();
        Assert.Equal('#', reader.ReadLetter());
        Assert.Equal("END", reader.ReadWord());
        reader.ReadToEndOfLine();
        Assert.True(reader.AtEnd);
    }

    [Theory]
    [InlineData("north", "north")]
    [InlineData("  North  ", "North")]
    [InlineData("'hello world' next", "hello world")]
    [InlineData("\"hello world\" next", "hello world")]
    [InlineData("one", "one")]
    public void ReadWord_HandlesQuotesAndWhitespace(string input, string expected)
    {
        var reader = new LegacyAreaReader(input);

        Assert.Equal(expected, reader.ReadWord());
    }

    [Fact]
    public void ReadWord_UnterminatedQuote_ReportsPosition()
    {
        var reader = new LegacyAreaReader(" \n  'hello", "test.are");

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadWord());

        Assert.Equal(2, exception.Line);
        Assert.Equal(3, exception.Column);
        Assert.Contains("Unterminated", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadWord_TooLong_ReportsStart()
    {
        var reader = new LegacyAreaReader(new string('x', LegacyAreaReader.MaxWordLength + 1));

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadWord());

        Assert.Equal(1, exception.Line);
        Assert.Equal(1, exception.Column);
        Assert.Contains("exceeds", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("+42", 42)]
    [InlineData("-42", -42)]
    [InlineData("1|64|1048576", 1048641)]
    [InlineData("-1|2", 1)]
    [InlineData("  7  ", 7)]
    public void ReadNumber_HandlesRomNumberSyntax(string input, int expected)
    {
        var reader = new LegacyAreaReader(input);

        Assert.Equal(expected, reader.ReadNumber());
    }

    [Fact]
    public void ReadNumber_LeavesNonSpaceDelimiter()
    {
        var reader = new LegacyAreaReader("12abc");

        Assert.Equal(12, reader.ReadNumber());
        Assert.Equal("abc", reader.ReadWord());
    }

    [Fact]
    public void ReadNumber_InvalidValue_ReportsLineAndColumn()
    {
        var reader = new LegacyAreaReader("\n  nope", "test.are");

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadNumber());

        Assert.Equal(2, exception.Line);
        Assert.Equal(3, exception.Column);
        Assert.Contains("Expected a number", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadNumber_Overflow_ReportsStart()
    {
        var reader = new LegacyAreaReader("99999999999");

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadNumber());

        Assert.Equal(1, exception.Column);
        Assert.Contains("too large", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("A", 1L << 0)]
    [InlineData("Z", 1L << 25)]
    [InlineData("a", 1L << 26)]
    [InlineData("d", 1L << 29)]
    [InlineData("A|B", (1L << 0) | (1L << 1))]
    [InlineData("ab", (1L << 26) | (1L << 27))]
    [InlineData("12", 12)]
    [InlineData("-5", -5)]
    [InlineData("A-5", -5)]
    public void ReadFlag_HandlesLettersNumbersAndSums(string input, long expected)
    {
        var reader = new LegacyAreaReader(input);

        Assert.Equal(expected, reader.ReadFlag());
    }

    [Theory]
    [InlineData("~", "")]
    [InlineData("hello~", "hello")]
    [InlineData("  hello~", "hello")]
    [InlineData("\n\n  hello~", "hello")]
    [InlineData("line1\r\nline2~", "line1\nline2")]
    [InlineData("line1\nline2~", "line1\nline2")]
    [InlineData("line1\rline2~", "line1line2")]
    public void ReadString_HandlesRomStrings(string input, string expected)
    {
        var reader = new LegacyAreaReader(input);

        Assert.Equal(expected, reader.ReadString());
    }

    [Fact]
    public void ReadString_ConsumesOnlyFirstTilde()
    {
        var reader = new LegacyAreaReader("one~two~");

        Assert.Equal("one", reader.ReadString());
        Assert.Equal("two", reader.ReadString());
    }

    [Fact]
    public void ReadString_Unterminated_ReportsStart()
    {
        var reader = new LegacyAreaReader("\n  hello", "test.are");

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadString());

        Assert.Equal(2, exception.Line);
        Assert.Equal(3, exception.Column);
        Assert.Contains("Unterminated string", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadToEndOfLine_SkipsCommentAndBlankLines()
    {
        var reader = new LegacyAreaReader("* comment\n\nNext");

        reader.ReadLetter();
        reader.ReadToEndOfLine();

        Assert.Equal("Next", reader.ReadWord());
        Assert.Equal(3, reader.Position.Line);
    }

    [Fact]
    public void ReadLetter_AtEnd_ReportsPosition()
    {
        var reader = new LegacyAreaReader("  ", "test.are");

        var exception = Assert.Throws<LegacyFormatException>(() => reader.ReadLetter());

        Assert.Equal(1, exception.Line);
        Assert.Equal(3, exception.Column);
        Assert.Contains("end of file", exception.Message, StringComparison.Ordinal);
    }
}
