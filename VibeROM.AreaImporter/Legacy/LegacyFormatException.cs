using System.Globalization;
using System.Text;

namespace VibeROM.AreaImporter.Legacy;

public readonly record struct LegacyPosition(int Offset, int Line, int Column);

public sealed class LegacyFormatException : Exception
{
    public LegacyFormatException(string message, string fileName, LegacyPosition position)
        : base($"{fileName}:{position.Line}:{position.Column}: {message}")
    {
        FileName = fileName;
        Position = position;
    }

    public string FileName { get; }

    public LegacyPosition Position { get; }

    public int Line => Position.Line;

    public int Column => Position.Column;

    public int Offset => Position.Offset;
}
