using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>
/// ROM's <c>time_info</c> calendar (db.c boot calculation and update.c
/// <c>weather_update</c>). One game hour passes per 60-second tick.
/// </summary>
public sealed class GameTime
{
    /// <summary>Unix timestamp ROM treats as game hour zero.</summary>
    public const long EpochOffsetSeconds = 650336715;

    public const int HoursPerDay = 24;
    public const int DaysPerMonth = 35;
    public const int MonthsPerYear = 17;

    private GameTime(int hour, int day, int month, int year)
    {
        Hour = hour;
        Day = day;
        Month = month;
        Year = year;
        Sunlight = SunlightFor(hour);
    }

    public int Hour { get; private set; }

    public int Day { get; private set; }

    public int Month { get; private set; }

    public int Year { get; private set; }

    public SunState Sunlight { get; private set; }

    public static GameTime FromUnixTime(long unixSeconds)
    {
        var hours = (unixSeconds - EpochOffsetSeconds) / 60;
        var days = hours / HoursPerDay;

        return new GameTime(
            (int)(hours % HoursPerDay),
            (int)(days % DaysPerMonth),
            (int)(days / DaysPerMonth % MonthsPerYear),
            (int)(days / DaysPerMonth / MonthsPerYear));
    }

    public static GameTime FromTime(DateTimeOffset time) =>
        FromUnixTime(time.ToUnixTimeSeconds());

    /// <summary>
    /// Advances one game hour and returns the messages ROM broadcasts for the
    /// transition (none, except at dawn/rise/set/night).
    /// </summary>
    public IReadOnlyList<string> AdvanceHour()
    {
        var messages = new List<string>(1);

        switch (++Hour)
        {
            case 5:
                Sunlight = SunState.Light;
                messages.Add("The day has begun.");
                break;

            case 6:
                Sunlight = SunState.Rise;
                messages.Add("The sun rises in the east.");
                break;

            case 19:
                Sunlight = SunState.Set;
                messages.Add("The sun slowly disappears in the west.");
                break;

            case 20:
                Sunlight = SunState.Dark;
                messages.Add("The night has begun.");
                break;

            case 24:
                Hour = 0;
                Day++;
                break;
        }

        if (Day >= DaysPerMonth)
        {
            Day = 0;
            Month++;
        }

        if (Month >= MonthsPerYear)
        {
            Month = 0;
            Year++;
        }

        return messages;
    }

    private static SunState SunlightFor(int hour) =>
        hour switch
        {
            < 5 => SunState.Dark,
            < 6 => SunState.Rise,
            < 19 => SunState.Light,
            < 20 => SunState.Set,
            _ => SunState.Dark
        };
}
