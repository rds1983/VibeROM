using VibeROM.Server.Game;

namespace VibeROM.Server.Tests.Game;

public sealed class PulseEngineTests
{
    [Fact]
    public async Task SixtySecondsOfPulses_FireRomCounters()
    {
        var engine = new PulseEngine();
        var handler = new CountingHandler();
        engine.Register(handler);

        for (var index = 0; index < 60 * PulseEngine.PulsesPerSecond; index++)
        {
            await engine.AdvanceAsync();
        }

        Assert.Equal(240, engine.Pulses);
        Assert.Equal(240, handler.Aggression);
        Assert.Equal(20, handler.Violence);
        Assert.Equal(15, handler.Mobile);
        Assert.Equal(10, handler.Music);
        Assert.Equal(1, handler.Tick);
        Assert.Equal(1, handler.Area);
    }

    [Fact]
    public async Task TwoMinutesOfPulses_FireAreaAndSecondTick()
    {
        var engine = new PulseEngine();
        var handler = new CountingHandler();
        engine.Register(handler);

        for (var index = 0; index < 2 * 60 * PulseEngine.PulsesPerSecond; index++)
        {
            await engine.AdvanceAsync();
        }

        Assert.Equal(480, engine.Pulses);
        Assert.Equal(40, handler.Violence);
        Assert.Equal(30, handler.Mobile);
        Assert.Equal(20, handler.Music);
        Assert.Equal(2, handler.Tick);
        Assert.Equal(1, handler.Area);
    }

    [Fact]
    public async Task ThrowingHandler_DoesNotStopOtherSubsystems()
    {
        var engine = new PulseEngine();
        engine.Register(new ThrowingHandler());
        var handler = new CountingHandler();
        engine.Register(handler);

        await engine.AdvanceAsync();
        await engine.AdvanceAsync();

        Assert.Equal(2, handler.Aggression);
        Assert.Equal(2, engine.Pulses);
    }

    private sealed class CountingHandler : IGamePulseHandler
    {
        public int Aggression { get; private set; }

        public int Violence { get; private set; }

        public int Mobile { get; private set; }

        public int Music { get; private set; }

        public int Tick { get; private set; }

        public int Area { get; private set; }

        public ValueTask OnPulseAsync(GamePulse pulse, CancellationToken cancellationToken)
        {
            if ((pulse & GamePulse.Aggression) != 0) Aggression++;
            if ((pulse & GamePulse.Violence) != 0) Violence++;
            if ((pulse & GamePulse.Mobile) != 0) Mobile++;
            if ((pulse & GamePulse.Music) != 0) Music++;
            if ((pulse & GamePulse.Tick) != 0) Tick++;
            if ((pulse & GamePulse.Area) != 0) Area++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IGamePulseHandler
    {
        public ValueTask OnPulseAsync(GamePulse pulse, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("subsystem failure");
    }
}
