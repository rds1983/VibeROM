using NLog;
using VibeROM.Common.Domain;

namespace VibeROM.Server.Game;

/// <summary>
/// Advances the game calendar and sunlight once per 60-second tick. ROM does
/// this in <c>weather_update</c>; weather pressure and sky remain T060.
/// </summary>
public sealed class GameClockPulseHandler : IGamePulseHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly World _world;

    public GameClockPulseHandler(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public ValueTask OnPulseAsync(GamePulse pulse, CancellationToken cancellationToken)
    {
        if ((pulse & GamePulse.Tick) == 0)
        {
            return ValueTask.CompletedTask;
        }

        var messages = _world.Time.AdvanceHour();
        foreach (var message in messages)
        {
            Logger.Info("Game time: {Message}", message);
        }

        Logger.Debug(
            "Game time: year {Year}, month {Month}, day {Day}, hour {Hour} ({Sunlight}).",
            _world.Time.Year,
            _world.Time.Month,
            _world.Time.Day,
            _world.Time.Hour,
            _world.Time.Sunlight);

        return ValueTask.CompletedTask;
    }
}
