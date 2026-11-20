using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using VibeROM.Common.Domain;

namespace VibeROM.Common.Serialization;

/// <summary>
/// Canonical area JSON reader/writer. Use <see cref="Serialize"/> and
/// <see cref="Deserialize"/>, or pass <see cref="Options"/> to
/// <see cref="JsonSerializer"/> directly.
/// </summary>
public static class AreaJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize(AreaData area)
    {
        var json = JsonSerializer.Serialize(area, Options);
        return json.EndsWith('\n') ? json : json + "\n";
    }

    public static AreaData Deserialize(string json, JsonSchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (schema is not null)
        {
            JsonSchemaValidator.ValidateOrThrow(schema, json);
        }

        try
        {
            return JsonSerializer.Deserialize<AreaData>(json, Options)
                ?? throw new DataFormatException("/", "Area JSON is empty.");
        }
        catch (DataFormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new DataFormatException(exception.Path ?? "/", exception.Message);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new AreaDataConverter());
        return options;
    }

    private sealed class AreaDataConverter : JsonConverter<AreaData>
    {
        public override AreaData Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return AreaJsonReader.Read(document.RootElement);
        }

        public override void Write(
            Utf8JsonWriter writer,
            AreaData value,
            JsonSerializerOptions options) =>
            AreaJsonWriter.Write(writer, value);
    }
}
