using VibeROM.Server.Game;

namespace VibeROM.Server.Tests.Game;

public sealed class AutosavePulseHandlerTests
{
    [Fact]
    public async Task SavesOnlyOnTickAfterTheConfiguredInterval()
    {
        var time = new MutableTimeProvider();
        var saves = 0;
        var handler = new AutosavePulseHandler(() => saves++, 300, time);

        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(0, saves);

        time.Advance(TimeSpan.FromSeconds(299));
        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(0, saves);

        time.Advance(TimeSpan.FromSeconds(1));
        await handler.OnPulseAsync(GamePulse.Aggression, CancellationToken.None);
        Assert.Equal(0, saves);

        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(1, saves);

        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(1, saves);

        time.Advance(TimeSpan.FromSeconds(300));
        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);
        Assert.Equal(2, saves);
    }

    [Fact]
    public async Task ZeroIntervalDisablesAutosave()
    {
        var time = new MutableTimeProvider();
        var saves = 0;
        var handler = new AutosavePulseHandler(() => saves++, 0, time);

        time.Advance(TimeSpan.FromHours(1));
        await handler.OnPulseAsync(GamePulse.Tick, CancellationToken.None);

        Assert.Equal(0, saves);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
