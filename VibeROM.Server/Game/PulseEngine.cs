using NLog;

namespace VibeROM.Server.Game;

/// <summary>
/// ROM's 4 Hz pulse engine (<c>update_handler</c> in update.c). Every handler is
/// isolated: an exception in one subsystem is logged and the remaining
/// handlers still run on this and later pulses.
/// </summary>
public sealed class PulseEngine
{
    public const int PulsesPerSecond = 4;
    public const int ViolenceInterval = 3 * PulsesPerSecond;
    public const int MobileInterval = 4 * PulsesPerSecond;
    public const int MusicInterval = 6 * PulsesPerSecond;
    public const int TickInterval = 60 * PulsesPerSecond;
    public const int AreaInterval = 120 * PulsesPerSecond;

    public static readonly TimeSpan PulseInterval = TimeSpan.FromMilliseconds(250);

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<IGamePulseHandler> _handlers = [];
    private readonly object _gate = new();

    // ROM's static counters start at zero, so the first pulse fires every
    // named update.
    private int _violence;
    private int _mobile;
    private int _music;
    private int _tick;
    private int _area;

    public long Pulses { get; private set; }

    public void Register(IGamePulseHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>
    /// Runs one pulse. Tests call this directly to advance simulated time
    /// without waiting for the wall clock.
    /// </summary>
    public async ValueTask AdvanceAsync(CancellationToken cancellationToken = default)
    {
        var pulse = GamePulse.Aggression;

        if (--_violence <= 0)
        {
            _violence = ViolenceInterval;
            pulse |= GamePulse.Violence;
        }

        if (--_mobile <= 0)
        {
            _mobile = MobileInterval;
            pulse |= GamePulse.Mobile;
        }

        if (--_music <= 0)
        {
            _music = MusicInterval;
            pulse |= GamePulse.Music;
        }

        if (--_tick <= 0)
        {
            _tick = TickInterval;
            pulse |= GamePulse.Tick;
        }

        if (--_area <= 0)
        {
            _area = AreaInterval;
            pulse |= GamePulse.Area;
        }

        Pulses++;

        IGamePulseHandler[] handlers;
        lock (_gate)
        {
            handlers = _handlers.ToArray();
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler.OnPulseAsync(pulse, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    exception,
                    "Pulse handler {Handler} failed for {Pulse}.",
                    handler.GetType().Name,
                    pulse);
            }
        }
    }

    /// <summary>Runs pulses every 250 ms until cancelled.</summary>
    public async Task RunAsync(
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var next = timeProvider.GetUtcNow();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                next += PulseInterval;
                var now = timeProvider.GetUtcNow();
                var delay = next - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, timeProvider, cancellationToken);
                }
                else if (delay < -PulseInterval * 4)
                {
                    // The process was paused; resume on the current cadence
                    // instead of trying to catch up a long backlog.
                    next = now;
                }

                await AdvanceAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
