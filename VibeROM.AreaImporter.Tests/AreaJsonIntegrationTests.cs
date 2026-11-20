using VibeROM.AreaImporter.Legacy;
using VibeROM.Common.Serialization;

namespace VibeROM.AreaImporter.Tests;

public sealed class AreaJsonIntegrationTests
{
    private static readonly string SchemaPath =
        Path.Combine(AppContext.BaseDirectory, "schema", "area.schema.json");

    [Fact]
    public void ParsedFixtureSerializesAndRoundTrips()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "modern.are");
        var area = new LegacyAreaParser().Parse(File.ReadAllText(path), path).Area;
        var schema = JsonSchemaValidator.Load(SchemaPath);

        var json = AreaJson.Serialize(area);

        Assert.Empty(JsonSchemaValidator.Validate(schema, json));
        var roundTripped = AreaJson.Deserialize(json, schema);
        Assert.Equal(json, AreaJson.Serialize(roundTripped));
    }

    [RomAreaFact]
    public void ParsedRomCorpusSerializesAndRoundTrips()
    {
        var directory = RomAreaFixture.Directory!;
        var parser = new LegacyAreaParser();
        var schema = JsonSchemaValidator.Load(SchemaPath);
        var failures = new List<string>();

        foreach (var file in Directory.GetFiles(directory, "*.are").Order(StringComparer.Ordinal))
        {
            try
            {
                var area = parser.Parse(File.ReadAllText(file), file).Area;
                var json = AreaJson.Serialize(area);
                var errors = JsonSchemaValidator.Validate(schema, json);
                if (errors.Count > 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: {errors[0]}");
                    continue;
                }

                var roundTripped = AreaJson.Deserialize(json, schema);
                if (AreaJson.Serialize(roundTripped) != json)
                {
                    failures.Add($"{Path.GetFileName(file)}: JSON round-trip changed the document.");
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                failures.Add($"{Path.GetFileName(file)}: {exception.Message}");
            }
        }

        Assert.Empty(failures);
    }
}
