using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;

namespace VibeROM.Server.Communication;

/// <summary>ROM <c>wiznet()</c>: sends a line to eligible immortal staff.</summary>
public sealed class WiznetService
{
    public const int ImmortalTrust = 52;

    private static readonly int OnBit = RomFlags.Wiznet.Get("On").Bit;
    private static readonly int PrefixBit = RomFlags.Wiznet.Get("Prefix").Bit;

    private readonly OnlinePlayers _online;
    private readonly IPlayerOutput _output;

    public WiznetService(OnlinePlayers online, IPlayerOutput output)
    {
        _online = online ?? throw new ArgumentNullException(nameof(online));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Send(
        string format,
        Character? source,
        int flag = 0,
        int flagSkip = 0,
        int minimumLevel = 0)
    {
        foreach (var descriptor in _online.Snapshot())
        {
            if (!descriptor.IsPlaying || descriptor.Character is not { } target)
            {
                continue;
            }

            if (target.Trust < ImmortalTrust || ReferenceEquals(target, source))
            {
                continue;
            }

            if ((target.WiznetFlags & OnBit) == 0)
            {
                continue;
            }

            if (flag != 0 && (target.WiznetFlags & flag) == 0)
            {
                continue;
            }

            if (flagSkip != 0 && (target.WiznetFlags & flagSkip) != 0)
            {
                continue;
            }

            if (target.Trust < minimumLevel)
            {
                continue;
            }

            if ((target.WiznetFlags & PrefixBit) != 0)
            {
                _output.Send(target, "--> ");
            }

            var context = new ActMessageContext(target, Victim: source);
            _output.Send(
                target,
                ActFormatter.FormatLine(format, context, target));
        }
    }
}
