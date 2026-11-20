using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Json.Schema;
using VibeROM.Common.Domain;

namespace VibeROM.Common.Serialization;

/// <summary>Canonical player-save reader/writer with atomic file persistence.</summary>
public static class PlayerJson
{
    public static string Serialize(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(
                   buffer,
                   new JsonWriterOptions
                   {
                       Indented = true,
                       NewLine = "\n",
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                   }))
        {
            PlayerJsonWriter.Write(writer, character);
        }

        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        return json.EndsWith('\n') ? json : json + "\n";
    }

    public static Character Deserialize(string json, World world, JsonSchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(world);

        if (schema is not null)
        {
            JsonSchemaValidator.ValidateOrThrow(schema, json);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new DataFormatException("/", $"Invalid JSON: {exception.Message}");
        }

        using (document)
        {
            return PlayerJsonReader.Read(document.RootElement, world);
        }
    }

    public static void Save(string path, Character character, JsonSchema? schema = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = Serialize(character);
        if (schema is not null)
        {
            JsonSchemaValidator.ValidateOrThrow(schema, json);
        }

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(
                       temporary,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static Character Load(string path, World world, JsonSchema? schema = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Deserialize(File.ReadAllText(path), world, schema);
    }
}
