using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Text;

public sealed class RomStringsTests
{
    [Theory]
    [InlineData("Sword", "sword", true)]
    [InlineData("Sword", "SWORD", true)]
    [InlineData("Sword", "sword ", false)]
    [InlineData("", "", true)]
    [InlineData(null, null, true)]
    [InlineData(null, "x", false)]
    public void EqualsIgnoreCase_Works(string? left, string? right, bool expected)
    {
        Assert.Equal(expected, RomStrings.EqualsIgnoreCase(left, right));
    }

    [Theory]
    [InlineData("Sword", "sw", true)]
    [InlineData("Sword", "SW", true)]
    [InlineData("Sword", "word", false)]
    [InlineData("Sword", "", true)]
    [InlineData(null, "s", false)]
    public void HasPrefixIgnoreCase_Works(string? value, string? prefix, bool expected)
    {
        Assert.Equal(expected, RomStrings.HasPrefixIgnoreCase(value, prefix));
    }

    [Theory]
    [InlineData("long sword", "SWORD", true)]
    [InlineData("long sword", "axe", false)]
    [InlineData(null, "a", false)]
    public void ContainsIgnoreCase_Works(string? value, string? needle, bool expected)
    {
        Assert.Equal(expected, RomStrings.ContainsIgnoreCase(value, needle));
    }

    [Theory]
    [InlineData("long sword", "WORD", true)]
    [InlineData("long sword", "sword", true)]
    [InlineData("long sword", "long", false)]
    public void HasSuffixIgnoreCase_Works(string value, string suffix, bool expected)
    {
        Assert.Equal(expected, RomStrings.HasSuffixIgnoreCase(value, suffix));
    }

    [Fact]
    public void SmashTilde_ReplacesTildesWithHyphens()
    {
        Assert.Equal("a-b--c", RomStrings.SmashTilde("a~b~~c"));
    }

    [Theory]
    [InlineData("hELLO", "Hello")]
    [InlineData("hello", "Hello")]
    [InlineData("", "")]
    [InlineData("élan", "Élan")]
    public void Capitalize_LowercasesAndUppercasesFirstCharacter(string input, string expected)
    {
        Assert.Equal(expected, RomStrings.Capitalize(input));
    }
}
