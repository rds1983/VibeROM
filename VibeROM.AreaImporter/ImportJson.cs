using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VibeROM.Common;

namespace VibeROM.AreaImporter;

/// <summary>Serializes import reports and index.json deterministically.</summary>
internal static class ImportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeReport(ImportReport report) =>
        WithTrailingNewline(JsonSerializer.Serialize(report, Options));

    public static string SerializeIndex(IReadOnlyList<string> areas) =>
        WithTrailingNewline(
            JsonSerializer.Serialize(
                new AreaIndex(
                    GameInfo.DataFormatVersion,
                    new AreaIndexSource(
                        GameInfo.RomProject,
                        GameInfo.RomVersionNumber,
                        GameInfo.RomReleaseDate),
                    areas),
                Options));

    private static string WithTrailingNewline(string json) =>
        json.EndsWith('\n') ? json : json + "\n";

    private sealed record AreaIndex(
        int FormatVersion,
        AreaIndexSource Source,
        IReadOnlyList<string> Areas);

    private sealed record AreaIndexSource(
        string Project,
        string Version,
        string ReleaseDate);
}
