using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Server.Game;

namespace VibeROM.Server.Tests.Game;

public sealed class GameClockPulseHandlerTests
{
    [Fact]
    public async Task TickPulseAdvancesOneGameHour()
    {
        var boot = DateTimeOffset.FromUnixTimeSeconds(
            GameTime.EpochOffsetSeconds + 60 * 4);
        var world = new World(Array.Empty<AreaData>(), boot);
        var handler = new GameClockPulseHandler(world);

        await handler.OnPulseAsync(GamePulse.Aggression, CancellationToken.None);
        Assert.Equal(4, world.Time.Hour);

        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(5, world.Time.Hour);
        Assert.Equal(SunState.Light, world.Time.Sunlight);
    }
}
