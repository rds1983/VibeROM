using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Text;

public sealed class MoneyFormatterTests
{
    [Theory]
    [InlineData(0, 0, "0 gold and 0 silver")]
    [InlineData(10, 25, "10 gold and 25 silver")]
    [InlineData(1, 0, "1 gold and 0 silver")]
    public void Format_FormatsWallet(long gold, long silver, string expected)
    {
        Assert.Equal(expected, MoneyFormatter.Format(gold, silver));
    }

    [Theory]
    [InlineData(0, 0, "Odd...there's no coins in the pile.")]
    [InlineData(0, 1, "Wow. One silver coin.")]
    [InlineData(0, 2, "There are 2 silver coins in the pile.")]
    [InlineData(1, 0, "Wow. One gold coin.")]
    [InlineData(2, 0, "There are 2 gold coins in the pile.")]
    [InlineData(2, 3, "There are 2 gold and 3 silver coins in the pile.")]
    public void FormatPile_MatchesRomLookOutput(long gold, long silver, string expected)
    {
        Assert.Equal(expected, MoneyFormatter.FormatPile(gold, silver));
    }
}
