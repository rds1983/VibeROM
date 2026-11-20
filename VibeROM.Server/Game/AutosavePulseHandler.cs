using NLog;

namespace VibeROM.Server.Game;

/// <summary>
/// Runs an autosave action every <c>gameplay.autosaveIntervalSeconds</c>. The
/// interval is checked on tick pulses (once per game hour), and zero disables
/// autosave.
/// </summary>
public sealed class AutosavePulseHandler : IGamePulseHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Action _save;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastSave;

    public AutosavePulseHandler(
        Action save,
        int intervalSeconds,
        TimeProvider? timeProvider = null)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        if (intervalSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds),
                intervalSeconds,
                "Autosave interval must not be negative.");
        }

        _interval = TimeSpan.FromSeconds(intervalSeconds);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastSave = _timeProvider.GetUtcNow();
    }

    public ValueTask OnPulseAsync(GamePulse pulse, CancellationToken cancellationToken)
    {
        if (_interval <= TimeSpan.Zero || (pulse & GamePulse.Tick) == 0)
        {
            return ValueTask.CompletedTask;
        }

        var now = _timeProvider.GetUtcNow();
        if (now - _lastSave < _interval)
        {
            return ValueTask.CompletedTask;
        }

        _save();
        _lastSave = now;
        Logger.Debug("Autosave finished at {Time}.", now);
        return ValueTask.CompletedTask;
    }
}
