using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Domain;

public sealed class GameTimeTests
{
    private const long Epoch = GameTime.EpochOffsetSeconds;

    [Fact]
    public void FromUnixTime_MatchesRomCalendar()
    {
        var start = GameTime.FromUnixTime(Epoch);

        Assert.Equal(0, start.Hour);
        Assert.Equal(0, start.Day);
        Assert.Equal(0, start.Month);
        Assert.Equal(0, start.Year);
        Assert.Equal(SunState.Dark, start.Sunlight);

        var later = GameTime.FromUnixTime(Epoch + 60 * (24 * 35 * 2 + 24 * 3 + 7));

        Assert.Equal(7, later.Hour);
        Assert.Equal(3, later.Day);
        Assert.Equal(2, later.Month);
        Assert.Equal(0, later.Year);
    }

    [Theory]
    [InlineData(0, SunState.Dark)]
    [InlineData(4, SunState.Dark)]
    [InlineData(5, SunState.Rise)]
    [InlineData(6, SunState.Light)]
    [InlineData(12, SunState.Light)]
    [InlineData(19, SunState.Set)]
    [InlineData(20, SunState.Dark)]
    [InlineData(23, SunState.Dark)]
    public void BootSunlight_MatchesRomThresholds(int hour, SunState expected)
    {
        var time = GameTime.FromUnixTime(Epoch + 60 * hour);

        Assert.Equal(expected, time.Sunlight);
    }

    [Fact]
    public void AdvanceHour_RaisesSunTransitions()
    {
        Assert.Equal(
            ["The day has begun."],
            GameTime.FromUnixTime(Epoch + 60 * 4).AdvanceHour());
        Assert.Equal(
            ["The sun rises in the east."],
            GameTime.FromUnixTime(Epoch + 60 * 5).AdvanceHour());
        Assert.Equal(
            ["The sun slowly disappears in the west."],
            GameTime.FromUnixTime(Epoch + 60 * 18).AdvanceHour());
        Assert.Equal(
            ["The night has begun."],
            GameTime.FromUnixTime(Epoch + 60 * 19).AdvanceHour());
        Assert.Empty(GameTime.FromUnixTime(Epoch + 60 * 12).AdvanceHour());
    }

    [Fact]
    public void AdvanceHour_RollsDayMonthAndYear()
    {
        var day = GameTime.FromUnixTime(Epoch + 60 * 23);
        day.AdvanceHour();
        Assert.Equal(0, day.Hour);
        Assert.Equal(1, day.Day);
        Assert.Equal(0, day.Month);

        var month = GameTime.FromUnixTime(Epoch + 60 * (24 * 34 + 23));
        month.AdvanceHour();
        Assert.Equal(0, month.Day);
        Assert.Equal(1, month.Month);
        Assert.Equal(0, month.Year);

        var year = GameTime.FromUnixTime(Epoch + 60 * (24 * (35 * 16 + 34) + 23));
        year.AdvanceHour();
        Assert.Equal(0, year.Day);
        Assert.Equal(0, year.Month);
        Assert.Equal(1, year.Year);
    }
}
