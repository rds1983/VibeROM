namespace VibeROM.AreaImporter.Tests;

internal static class RomAreaFixture
{
    public const string EnvironmentVariable = "VIBEROM_ROM_AREA_DIR";

    public static string? Directory
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariable);
            return !string.IsNullOrWhiteSpace(value) && System.IO.Directory.Exists(value)
                ? value
                : null;
        }
    }
}

/// <summary>
/// Skips the test unless <c>VIBEROM_ROM_AREA_DIR</c> points at the original
/// ROM 2.4b6 area directory. The legacy files are not committed.
/// </summary>
public sealed class RomAreaFactAttribute : FactAttribute
{
    public RomAreaFactAttribute()
    {
        if (RomAreaFixture.Directory is null)
        {
            Skip = $"Set {RomAreaFixture.EnvironmentVariable} to the ROM 2.4b6 area directory.";
        }
    }
}
