using VibeROM.Common.Randomness;

namespace VibeROM.Common.Tests.Randomness;

public sealed class DiceRollerTests
{
    [Fact]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DiceRoller(null!));
    }

    [Theory]
    [InlineData(0, 1, 6, 1)]
    [InlineData(5, 1, 6, 6)]
    [InlineData(2, 2, 4, 4)]
    public void NumberRange_ReturnsInclusiveValue(int scripted, int from, int to, int expected)
    {
        var roller = new DiceRoller(new ScriptedRandom(scripted));

        Assert.Equal(expected, roller.NumberRange(from, to));
    }

    [Fact]
    public void NumberRange_ZeroZero_ReturnsZero()
    {
        var roller = new DiceRoller(new ScriptedRandom());

        Assert.Equal(0, roller.NumberRange(0, 0));
    }

    [Fact]
    public void NumberRange_InvertedRange_ReturnsFrom()
    {
        var roller = new DiceRoller(new ScriptedRandom());

        Assert.Equal(5, roller.NumberRange(5, 4));
        Assert.Equal(1, roller.NumberRange(1, 1));
    }

    [Fact]
    public void NumberPercent_ReturnsOneToOneHundred()
    {
        Assert.Equal(1, new DiceRoller(new ScriptedRandom(1)).NumberPercent());
        Assert.Equal(100, new DiceRoller(new ScriptedRandom(100)).NumberPercent());
    }

    [Fact]
    public void NumberBits_ReturnsLowBits()
    {
        Assert.Equal(3, new DiceRoller(new ScriptedRandom(3)).NumberBits(2));
        Assert.Equal(0, new DiceRoller(new ScriptedRandom(0)).NumberBits(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void NumberBits_InvalidWidth_Throws(int width)
    {
        var roller = new DiceRoller(new ScriptedRandom());

        Assert.Throws<ArgumentOutOfRangeException>(() => roller.NumberBits(width));
    }

    [Theory]
    [InlineData(0, 5, 4)]
    [InlineData(1, 5, 5)]
    [InlineData(3, 5, 6)]
    [InlineData(0, 1, 1)]
    public void NumberFuzzy_AdjustsAndClamps(int bits, int number, int expected)
    {
        var roller = new DiceRoller(new ScriptedRandom(bits));

        Assert.Equal(expected, roller.NumberFuzzy(number));
    }

    [Fact]
    public void Dice_SumsIndividualRolls()
    {
        var roller = new DiceRoller(new ScriptedRandom(0, 1, 5));

        Assert.Equal(9, roller.Dice(3, 6));
    }

    [Theory]
    [InlineData(0, 6, 0)]
    [InlineData(3, 1, 3)]
    [InlineData(2, 0, 0)]
    [InlineData(2, -1, 0)]
    public void Dice_EdgeSizes(int number, int size, int expected)
    {
        var roller = new DiceRoller(new ScriptedRandom());

        Assert.Equal(expected, roller.Dice(number, size));
    }

    [Fact]
    public void NumberRange_StaysInBounds()
    {
        var roller = new DiceRoller(new Random(12345));

        for (var index = 0; index < 10_000; index++)
        {
            Assert.InRange(roller.NumberRange(1, 6), 1, 6);
        }
    }

    [Fact]
    public void NumberPercent_StaysInBounds()
    {
        var roller = new DiceRoller(new Random(12345));

        for (var index = 0; index < 10_000; index++)
        {
            Assert.InRange(roller.NumberPercent(), 1, 100);
        }
    }

    private sealed class ScriptedRandom(params int[] values) : Random
    {
        private int _index;

        public override int Next(int minValue, int maxValue)
        {
            if (_index >= values.Length)
            {
                throw new InvalidOperationException("No scripted random value remains.");
            }

            var value = values[_index++];
            if (value < minValue || value >= maxValue)
            {
                throw new InvalidOperationException(
                    $"Scripted value {value} is outside [{minValue}, {maxValue}).");
            }

            return value;
        }
    }
}
