using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace VibeROM.Common.Tests;

public sealed class SchemaTests
{
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly Dictionary<string, JsonSchema> SchemaCache = new(StringComparer.Ordinal);

    public static TheoryData<string, string> ValidSamples { get; } = new()
    {
        { "area.schema.json", "valid-area.json" },
        { "index.schema.json", "valid-index.json" },
        { "player.schema.json", "valid-player.json" },
        { "notes.schema.json", "valid-notes.json" },
        { "music.schema.json", "valid-music.json" },
        { "god.schema.json", "valid-god.json" }
    };

    [Theory]
    [MemberData(nameof(ValidSamples))]
    public void ValidSample_MatchesSchema(string schemaFile, string sampleFile)
    {
        var results = Evaluate(schemaFile, LoadFixture(sampleFile));

        Assert.True(results.IsValid, FormatFailures(results));
    }

    [Fact]
    public void UnknownFlag_FailsAtTheItemPath()
    {
        var area = ParseFixture("valid-area.json").AsObject();
        var mobile = area["mobiles"]!.AsArray()[0]!.AsObject();
        mobile["actFlags"] = new JsonArray("Aggro");

        var results = Evaluate("area.schema.json", area.ToJsonString());

        Assert.False(results.IsValid);
        AssertFailureAt(results, "/mobiles/0/actFlags");
    }

    [Fact]
    public void MissingItemData_FailsOnTheObject()
    {
        var area = ParseFixture("valid-area.json").AsObject();
        var weapon = area["objects"]!.AsArray()[0]!.AsObject();
        weapon.Remove("weapon");

        var results = Evaluate("area.schema.json", area.ToJsonString());

        Assert.False(results.IsValid);
        AssertFailureAt(results, "/objects/0");
    }

    [Fact]
    public void WrongItemData_FailsOnTheObject()
    {
        var area = ParseFixture("valid-area.json").AsObject();
        var weapon = area["objects"]!.AsArray()[0]!.AsObject();
        weapon["container"] = new JsonObject
        {
            ["capacity"] = 10,
            ["flags"] = new JsonArray(),
            ["keyVnum"] = 0
        };

        var results = Evaluate("area.schema.json", area.ToJsonString());

        Assert.False(results.IsValid);
        AssertFailureAt(results, "/objects/0");
    }

    [Fact]
    public void UnknownAreaProperty_FailsAtTheRoot()
    {
        var area = ParseFixture("valid-area.json").AsObject();
        area["unknown"] = true;

        var results = Evaluate("area.schema.json", area.ToJsonString());

        Assert.False(results.IsValid);
        Assert.Contains(
            Flatten(results),
            failure => !failure.IsValid && failure.InstanceLocation.ToString() == string.Empty);
    }

    [Fact]
    public void InvalidPlayerClass_FailsAtClass()
    {
        var player = ParseFixture("valid-player.json").AsObject();
        player["class"] = "Bard";

        var results = Evaluate("player.schema.json", player.ToJsonString());

        Assert.False(results.IsValid);
        AssertFailureAt(results, "/class");
    }

    private static EvaluationResults Evaluate(string schemaFile, string instanceJson)
    {
        var schema = GetSchema(schemaFile);
        using var document = JsonDocument.Parse(instanceJson);

        return schema.Evaluate(
            document.RootElement,
            new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            });
    }

    private static JsonSchema GetSchema(string schemaFile)
    {
        lock (SchemaCache)
        {
            if (!SchemaCache.TryGetValue(schemaFile, out var schema))
            {
                schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(SchemaDirectory, schemaFile)));
                SchemaCache[schemaFile] = schema;
            }

            return schema;
        }
    }

    private static string LoadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(FixtureDirectory, fileName));

    private static JsonNode ParseFixture(string fileName) => JsonNode.Parse(LoadFixture(fileName))!;

    private static void AssertFailureAt(EvaluationResults results, string path)
    {
        var failures = Flatten(results).Where(result => !result.IsValid).ToArray();

        Assert.NotEmpty(failures);
        Assert.Contains(
            failures,
            failure => failure.InstanceLocation.ToString().StartsWith(path, StringComparison.Ordinal));
    }

    private static string FormatFailures(EvaluationResults results) =>
        string.Join(
            Environment.NewLine,
            Flatten(results)
                .Where(result => !result.IsValid)
                .Select(result => $"{result.InstanceLocation}: {FormatErrors(result)}"));

    private static string FormatErrors(EvaluationResults result) =>
        result.Errors is null ? string.Empty : string.Join(", ", result.Errors.Values);

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
