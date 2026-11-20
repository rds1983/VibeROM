namespace VibeROM.Server.Network;

/// <summary>Telnet command bytes used by the server.</summary>
internal static class TelnetConstants
{
    public const byte Iac = 255;
    public const byte Dont = 254;
    public const byte Do = 253;
    public const byte Wont = 252;
    public const byte Will = 251;
    public const byte Sb = 250;
    public const byte Se = 240;

    public const byte Echo = 1;
    public const byte SuppressGoAhead = 3;
    public const byte TerminalType = 24;
    public const byte Naws = 31;

    public const byte TerminalTypeIs = 0;
    public const byte TerminalTypeSend = 1;
}

public sealed record TelnetServerOptions(
    System.Net.IPAddress BindAddress,
    int Port,
    int MaxConnections = 100,
    bool ColourEnabled = true)
{
    public string Greeting { get; init; } =
        "\r\n{W}Welcome to VibeROM{x}\r\nROM 2.4b6 (1998-05-29)\r\n\r\n";
}
