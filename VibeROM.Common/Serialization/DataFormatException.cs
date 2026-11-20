namespace VibeROM.Common.Serialization;

/// <summary>
/// Raised when JSON data cannot be read into the domain model. The message
/// starts with the JSON Pointer path of the offending value.
/// </summary>
public sealed class DataFormatException : Exception
{
    public DataFormatException(string path, string message)
        : base($"{path}: {message}")
    {
        Path = path;
        Errors = [message];
    }

    public DataFormatException(IReadOnlyList<string> errors)
        : base("JSON data validation failed:" + Environment.NewLine + "- " +
               string.Join($"{Environment.NewLine}- ", errors))
    {
        Errors = errors;
    }

    /// <summary>JSON Pointer of the first error, or null for aggregate errors.</summary>
    public string? Path { get; }

    public IReadOnlyList<string> Errors { get; }
}
