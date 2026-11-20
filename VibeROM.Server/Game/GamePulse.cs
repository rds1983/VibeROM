namespace VibeROM.Server.Game;

/// <summary>
/// Pulses scheduled by the game loop. ROM calls <c>aggr_update</c> every pulse
/// and the remaining updates at fixed pulse counts.
/// </summary>
[Flags]
public enum GamePulse
{
    None = 0,
    Aggression = 1 << 0,
    Violence = 1 << 1,
    Mobile = 1 << 2,
    Music = 1 << 3,
    Tick = 1 << 4,
    Area = 1 << 5
}

/// <summary>A subsystem driven by the game loop.</summary>
public interface IGamePulseHandler
{
    ValueTask OnPulseAsync(GamePulse pulse, CancellationToken cancellationToken);
}
