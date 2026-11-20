using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace VibeROM.Common.Serialization;

public sealed record AreaIndexSource(
    string Project,
    string Version,
    string ReleaseDate);

public sealed record AreaIndexDocument(
    int FormatVersion,
    AreaIndexSource Source,
    IReadOnlyList<string> Areas);

/// <summary>Reads Data/areas/index.json.</summary>
public static class AreaIndexJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static AreaIndexDocument Deserialize(string json, JsonSchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (schema is not null)
        {
            JsonSchemaValidator.ValidateOrThrow(schema, json);
        }

        AreaIndexDocument? index;
        try
        {
            index = JsonSerializer.Deserialize<AreaIndexDocument>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new DataFormatException(exception.Path ?? "/", exception.Message);
        }

        if (index is null)
        {
            throw new DataFormatException("/", "Area index is empty.");
        }

        if (index.FormatVersion != GameInfo.DataFormatVersion)
        {
            throw new DataFormatException(
                "/formatVersion",
                $"Unsupported formatVersion {index.FormatVersion}; expected {GameInfo.DataFormatVersion}.");
        }

        if (index.Areas is null || index.Areas.Count == 0)
        {
            throw new DataFormatException("/areas", "Area index must list at least one area.");
        }

        return index;
    }
}
