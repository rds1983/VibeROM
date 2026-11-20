namespace VibeROM.Common.Domain;

/// <summary>Raised when areas cannot form a consistent world.</summary>
public sealed class WorldDataException : Exception
{
    public WorldDataException(IReadOnlyList<string> errors)
        : base("World data validation failed:" + Environment.NewLine + "- " +
               string.Join($"{Environment.NewLine}- ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
