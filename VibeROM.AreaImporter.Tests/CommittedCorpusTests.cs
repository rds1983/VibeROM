using System.Text.Json.Nodes;
using VibeROM.Common.Domain;
using VibeROM.Common.Serialization;

namespace VibeROM.AreaImporter.Tests;

public sealed class CommittedCorpusTests
{
    private static readonly string AreaDirectory = Path.Combine(AppContext.BaseDirectory, "areas");
    private static readonly string SchemaPath = Path.Combine(AppContext.BaseDirectory, "schema", "area.schema.json");

    [Fact]
    public void IndexAndEveryAreaValidate()
    {
        var index = JsonNode.Parse(File.ReadAllText(Path.Combine(AreaDirectory, "index.json")))!.AsObject();
        Assert.Equal(1, index["formatVersion"]!.GetValue<int>());
        Assert.Equal("ROM", index["source"]!["project"]!.GetValue<string>());
        Assert.Equal("2.4b6", index["source"]!["version"]!.GetValue<string>());
        Assert.Equal("1998-05-29", index["source"]!["releaseDate"]!.GetValue<string>());

        var names = index["areas"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
        Assert.Equal(52, names.Length);
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("help.json", names[0]);
        Assert.Equal("wyvern.json", names[^1]);
        Assert.DoesNotContain("thanos.json", names);
        Assert.DoesNotContain("proto.json", names);

        var schema = JsonSchemaValidator.Load(SchemaPath);
        var totals = new Totals();
        foreach (var name in names)
        {
            var path = Path.Combine(AreaDirectory, name);
            Assert.True(File.Exists(path), $"{name} is missing.");

            var json = File.ReadAllText(path);
            Assert.Empty(JsonSchemaValidator.Validate(schema, json));
            var area = AreaJson.Deserialize(json, schema);
            Assert.Empty(area.Validate());
            totals.Add(area);
        }

        Assert.Equal(52, totals.Areas);
        Assert.Equal(248, totals.Helps);
        Assert.Equal(986, totals.Mobiles);
        Assert.Equal(1265, totals.Objects);
        Assert.Equal(3126, totals.Rooms);
        Assert.Equal(5233, totals.Resets);
        Assert.Equal(62, totals.Shops);
        Assert.Equal(411, totals.Specials);
        Assert.Equal(244, totals.Socials);
    }

    [Fact]
    public void CorpusHasNoDanglingExitKeys()
    {
        var schema = JsonSchemaValidator.Load(SchemaPath);
        var areas = Directory.GetFiles(AreaDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "index.json", StringComparison.Ordinal))
            .Select(path => AreaJson.Deserialize(File.ReadAllText(path), schema))
            .ToArray();
        var objectVnums = areas.SelectMany(area => area.Objects).Select(obj => obj.Vnum).ToHashSet();

        var dangling = areas
            .SelectMany(area => area.Rooms)
            .SelectMany(room => room.Exits.Values
                .Where(exit => exit.KeyVnum > 0 && !objectVnums.Contains(exit.KeyVnum))
                .Select(exit => (Room: room.Vnum, exit.KeyVnum)))
            .ToArray();

        Assert.Empty(dangling);
    }

    private sealed class Totals
    {
        public int Areas { get; private set; }

        public int Helps { get; private set; }

        public int Mobiles { get; private set; }

        public int Objects { get; private set; }

        public int Rooms { get; private set; }

        public int Resets { get; private set; }

        public int Shops { get; private set; }

        public int Specials { get; private set; }

        public int Socials { get; private set; }

        public void Add(AreaData area)
        {
            Areas++;
            Helps += area.Helps.Count;
            Mobiles += area.Mobiles.Count;
            Objects += area.Objects.Count;
            Rooms += area.Rooms.Count;
            Resets += area.Resets.Count;
            Shops += area.Shops.Count;
            Specials += area.Specials.Count;
            Socials += area.Socials.Count;
        }
    }
}
