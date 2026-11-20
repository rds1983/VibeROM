using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Text;

public sealed class ColourTests
{
    [Theory]
    [InlineData("x", "\u001b[0m")]
    [InlineData("n", "\u001b[0m")]
    [InlineData("k", "\u001b[0;30m")]
    [InlineData("K", "\u001b[1;30m")]
    [InlineData("r", "\u001b[0;31m")]
    [InlineData("R", "\u001b[1;31m")]
    [InlineData("g", "\u001b[0;32m")]
    [InlineData("G", "\u001b[1;32m")]
    [InlineData("y", "\u001b[0;33m")]
    [InlineData("Y", "\u001b[1;33m")]
    [InlineData("b", "\u001b[0;34m")]
    [InlineData("B", "\u001b[1;34m")]
    [InlineData("m", "\u001b[0;35m")]
    [InlineData("M", "\u001b[1;35m")]
    [InlineData("c", "\u001b[0;36m")]
    [InlineData("C", "\u001b[1;36m")]
    [InlineData("w", "\u001b[0;37m")]
    [InlineData("W", "\u001b[1;37m")]
    [InlineData("d", "\u001b[1;30m")]
    [InlineData("D", "\u001b[0;37m")]
    public void ToAnsi_ConvertsCode(string code, string expected)
    {
        Assert.Equal(expected, Colour.ToAnsi($"{{{code}"));
    }

    [Fact]
    public void ToAnsi_HandlesResetAndLiteralBrace()
    {
        Assert.Equal(
            "\u001b[0;31mred\u001b[0m {not-a-code}",
            Colour.ToAnsi("{rred{x {{not-a-code}"));
    }

    [Fact]
    public void ToAnsi_Disabled_StripsKnownCodes()
    {
        Assert.Equal("red", Colour.ToAnsi("{Rred{x", enabled: false));
    }

    [Fact]
    public void ToAnsi_UnknownCode_IsPreserved()
    {
        Assert.Equal("{z", Colour.ToAnsi("{z"));
    }

    [Fact]
    public void Strip_RemovesKnownCodesAndKeepsUnknownText()
    {
        Assert.Equal("red {z", Colour.Strip("{rred{x {z"));
    }
}
