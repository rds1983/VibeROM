namespace VibeROM.AreaImporter;

/// <summary>One legacy-to-JSON import request.</summary>
public sealed record ImportRequest(
    string SourceDirectory,
    string OutputDirectory,
    string? SingleFile,
    string? ReportPath,
    bool Strict,
    bool Check);

public sealed record ImportMessage(string File, int Line, int Column, string Message);

public sealed record ImportedArea(
    string LegacyFile,
    string JsonFile,
    int Helps,
    int Mobiles,
    int Objects,
    int Rooms,
    int Resets,
    int Shops,
    int Specials,
    int Socials);

public sealed record ImportReport(
    string Source,
    string Version,
    string ReleaseDate,
    string SourceDirectory,
    IReadOnlyList<ImportedArea> Areas,
    IReadOnlyList<ImportMessage> Repairs,
    IReadOnlyList<ImportMessage> Warnings,
    IReadOnlyList<ImportMessage> Errors)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Succeeded => Errors.Count == 0;
}

public sealed record ImportResult(
    int ExitCode,
    ImportReport Report,
    IReadOnlyList<string> Messages);
