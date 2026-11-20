using System.Text.Json;
using Json.Schema;

namespace VibeROM.Common.Serialization;

/// <summary>Loads and evaluates the JSON Schemas in Data/schema.</summary>
public static class JsonSchemaValidator
{
    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, JsonSchema> Cache = new(StringComparer.Ordinal);

    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true
    };

    public static JsonSchema Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        lock (CacheLock)
        {
            if (!Cache.TryGetValue(fullPath, out var schema))
            {
                var buildOptions = new BuildOptions
                {
                    SchemaRegistry = new SchemaRegistry()
                };
                schema = JsonSchema.FromText(File.ReadAllText(fullPath), buildOptions);
                Cache[fullPath] = schema;
            }

            return schema;
        }
    }

    public static IReadOnlyList<string> Validate(JsonSchema schema, string json)
    {
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
            return Validate(schema, document.RootElement);
        }
    }

    public static IReadOnlyList<string> Validate(JsonSchema schema, JsonElement instance)
    {
        var results = schema.Evaluate(instance, Options);
        if (results.IsValid)
        {
            return [];
        }

        return Flatten(results)
            .Where(result => !result.IsValid)
            .Select(result =>
            {
                var location = result.InstanceLocation.ToString();
                if (string.IsNullOrEmpty(location))
                {
                    location = "/";
                }

                var errors = result.Errors is null
                    ? "validation failed"
                    : string.Join(", ", result.Errors.Values);
                return $"{location}: {errors}";
            })
            .ToArray();
    }

    public static void ValidateOrThrow(JsonSchema schema, string json, string? sourceName = null)
    {
        var errors = Validate(schema, json);
        if (errors.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            errors = errors
                .Select(error => $"{sourceName}: {error}")
                .ToArray();
        }

        throw new DataFormatException(errors);
    }

    private static IEnumerable<EvaluationResults> Flatten(EvaluationResults results)
    {
        yield return results;

        if (results.Details is null)
        {
            yield break;
        }

        foreach (var detail in results.Details)
        {
            foreach (var nested in Flatten(detail))
            {
                yield return nested;
            }
        }
    }
}
