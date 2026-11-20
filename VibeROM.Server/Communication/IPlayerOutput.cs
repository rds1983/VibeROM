using VibeROM.Common.Domain;
using VibeROM.Server.Network;

namespace VibeROM.Server.Communication;

/// <summary>Delivers rendered text to one player session (and its snoopers).</summary>
public interface IPlayerOutput
{
    void Send(Character character, string text);

    void Send(Descriptor descriptor, string text);
}

/// <summary>Telnet implementation of <see cref="IPlayerOutput"/>.</summary>
public sealed class TelnetPlayerOutput : IPlayerOutput
{
    private readonly TelnetServer _server;

    public TelnetPlayerOutput(TelnetServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public void Send(Character character, string text)
    {
        if (character.Descriptor is { } descriptor)
        {
            Send(descriptor, text);
        }
    }

    public void Send(Descriptor descriptor, string text)
    {
        _ = _server.SendAsync(descriptor, text);

        if (descriptor.SnoopBy is { } snooper)
        {
            Send(snooper, text);
        }
    }
}
