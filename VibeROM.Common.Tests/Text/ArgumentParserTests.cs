using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Text;

public sealed class ArgumentParserTests
{
    [Theory]
    [InlineData("north", "north", "")]
    [InlineData("  North   east ", "north", "east ")]
    [InlineData("Bob the builder", "bob", "the builder")]
    [InlineData("'Hello World' rest", "hello world", "rest")]
    [InlineData("\"Hello World\" rest", "hello world", "rest")]
    [InlineData("'unterminated rest", "unterminated rest", "")]
    [InlineData("a\tb", "a\tb", "")]
    [InlineData("", "", "")]
    [InlineData("   ", "", "")]
    [InlineData("''rest", "", "rest")]
    public void OneArgument_MatchesRomSemantics(
        string input,
        string expectedValue,
        string expectedRemainder)
    {
        var token = ArgumentParser.OneArgument(input);

        Assert.Equal(expectedValue, token.Value);
        Assert.Equal(expectedRemainder, token.Remainder);
    }

    [Fact]
    public void Split_ParsesMultipleArguments()
    {
        var values = ArgumentParser.Split("get 'long sword' 2.bag");

        Assert.Equal(["get", "long sword", "2.bag"], values);
    }

    [Fact]
    public void Split_EmptyInput_ReturnsNoArguments()
    {
        Assert.Empty(ArgumentParser.Split("   "));
    }
}
