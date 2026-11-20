namespace VibeROM.Common;

/// <summary>Static identity and format information for the running game.</summary>
public static class GameInfo
{
    public const string Name = "VibeROM";

    public const string RomVersion = "ROM 2.4b6";

    public const string RomProject = "ROM";

    public const string RomVersionNumber = "2.4b6";

    public const string RomReleaseDate = "1998-05-29";

    public const int DataFormatVersion = 1;

    public static string Describe() =>
        $"{Name} {RomVersion} ({RomReleaseDate}), data format v{DataFormatVersion}";
}
