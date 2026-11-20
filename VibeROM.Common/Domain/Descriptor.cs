namespace VibeROM.Common.Domain;

/// <summary>One network connection and its login/session state.</summary>
public sealed class Descriptor
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Host { get; set; } = string.Empty;

    public ConnectionState State { get; set; } = ConnectionState.GetName;

    public Character? Character { get; set; }

    public Character? Original { get; set; }

    public Descriptor? SnoopBy { get; set; }

    public bool IsPlaying => State == ConnectionState.Playing;

    public string InputBuffer { get; set; } = string.Empty;

    public Queue<string> Output { get; } = new();

    public bool ColourEnabled { get; set; }

    public int TerminalWidth { get; set; } = 80;

    public int TerminalHeight { get; set; } = 24;

    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
}
