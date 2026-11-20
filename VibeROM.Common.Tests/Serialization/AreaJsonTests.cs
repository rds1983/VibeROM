using System.Text.Json.Nodes;
using Json.Schema;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Serialization;
using VibeROM.Common.Tests.Domain;

namespace VibeROM.Common.Tests.Serialization;

public sealed class AreaJsonTests
{
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void ValidFixtureMatchesCanonicalSnapshotAndSchema()
    {
        var fixture = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-area.json"));
        var area = AreaJson.Deserialize(fixture);

        var json = AreaJson.Serialize(area);

        Assert.Equal(File.ReadAllText(Path.Combine(FixtureDirectory, "canonical-area.json")), json);
        Assert.False(json.Contains('\r'));
        Assert.True(json.EndsWith('\n'));
        Assert.StartsWith("{\n  \"formatVersion\": 1,\n", json, StringComparison.Ordinal);

        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "area.schema.json"));
        Assert.Empty(JsonSchemaValidator.Validate(schema, json));
    }

    [Fact]
    public void DomainRoundTripsThroughCanonicalJson()
    {
        var fixture = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-area.json"));
        var area = AreaJson.Deserialize(fixture);

        var json = AreaJson.Serialize(area);
        var roundTripped = AreaJson.Deserialize(json);

        Assert.Equal(json, AreaJson.Serialize(roundTripped));
        Assert.Equal(new ArmorValues(20, 20, 20, 20), area.Mobiles[0].Armor);
        Assert.Equal(50, area.Objects[0].WeightTenths);
        Assert.Equal(1, area.Objects[4].WeightTenths);
        Assert.Equal(area.Name, roundTripped.Name);
        Assert.Equal(area.Mobiles[0].Race, roundTripped.Mobiles[0].Race);
        Assert.Equal(area.Resets.Count, roundTripped.Resets.Count);
        Assert.Equal(area.Socials[0], roundTripped.Socials[0]);
    }

    [Fact]
    public void WriterSortsEntitiesAndOmitsEmptyOptionalValues()
    {
        var area = WorldFixtures.CreateArea() with
        {
            Helps = [new HelpEntry("zeta", 0, "Z."), new HelpEntry("alpha", 0, "A.")],
            Mobiles =
            [
                WorldFixtures.CreateMobile(2001),
                WorldFixtures.CreateMobile(2000)
            ],
            Objects =
            [
                WorldFixtures.CreateWeapon(3001),
                WorldFixtures.CreateWeapon(3000)
            ],
            Rooms =
            [
                WorldFixtures.CreateRoom(1001),
                WorldFixtures.CreateRoom(1000)
            ],
            Socials =
            [
                CreateSocial("zeta"),
                CreateSocial("alpha")
            ]
        };

        var json = AreaJson.Serialize(area);

        Assert.True(IndexOf(json, "\"alpha\"") < IndexOf(json, "\"zeta\""));
        Assert.True(IndexOf(json, "\"vnum\": 2000") < IndexOf(json, "\"vnum\": 2001"));
        Assert.True(IndexOf(json, "\"vnum\": 3000") < IndexOf(json, "\"vnum\": 3001"));
        Assert.True(IndexOf(json, "\"vnum\": 1000") < IndexOf(json, "\"vnum\": 1001"));
        Assert.DoesNotContain("\"security\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"legacyFile\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"actFlags\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ifFlag\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"affects\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"extraDescriptions\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void WritesWeightPoundsAndFileUnitArmor()
    {
        var area = WorldFixtures.CreateArea() with
        {
            Mobiles =
            [
                WorldFixtures.CreateMobile() with
                {
                    Armor = new ArmorValues(20, 10, -10, 0)
                }
            ],
            Objects =
            [
                WorldFixtures.CreateWeapon() with
                {
                    WeightTenths = 5
                }
            ]
        };

        var json = AreaJson.Serialize(area);

        Assert.Contains("\"pierce\": 2", json, StringComparison.Ordinal);
        Assert.Contains("\"bash\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"slash\": -1", json, StringComparison.Ordinal);
        Assert.Contains("\"weightPounds\": 0.5", json, StringComparison.Ordinal);

        var roundTripped = AreaJson.Deserialize(json);
        Assert.Equal(new ArmorValues(20, 10, -10, 0), roundTripped.Mobiles[0].Armor);
        Assert.Equal(5, roundTripped.Objects[0].WeightTenths);
    }

    [Fact]
    public void UnknownFlagFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["mobiles"]!.AsArray()[0]!.AsObject()["actFlags"] = new JsonArray("Aggro");
            return area.ToJsonString();
        });

        Assert.Equal("/mobiles/0/actFlags/0", exception.Path);
        Assert.Contains("Aggro", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NonPersistedFlagFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["mobiles"]!.AsArray()[0]!.AsObject()["actFlags"] = new JsonArray("IsNpc");
            return area.ToJsonString();
        });

        Assert.Equal("/mobiles/0/actFlags/0", exception.Path);
    }

    [Fact]
    public void UnknownEnumNameFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["mobiles"]!.AsArray()[0]!.AsObject()["race"] = "Wizard";
            return area.ToJsonString();
        });

        Assert.Equal("/mobiles/0/race", exception.Path);
    }

    [Fact]
    public void UnknownResetTypeFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["resets"]!.AsArray()[0]!.AsObject()["type"] = "Bogus";
            return area.ToJsonString();
        });

        Assert.Equal("/resets/0/type", exception.Path);
    }

    [Fact]
    public void MissingItemDataFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["objects"]!.AsArray()[0]!.AsObject().Remove("weapon");
            return area.ToJsonString();
        });

        Assert.Equal("/objects/0/weapon", exception.Path);
    }

    [Fact]
    public void WrongItemDataFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            var weapon = area["objects"]!.AsArray()[0]!.AsObject();
            weapon["container"] = new JsonObject
            {
                ["capacity"] = 10,
                ["flags"] = new JsonArray(),
                ["keyVnum"] = 0
            };
            return area.ToJsonString();
        });

        Assert.Equal("/objects/0/container", exception.Path);
    }

    [Fact]
    public void UnsupportedFormatVersionFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["formatVersion"] = 2;
            return area.ToJsonString();
        });

        Assert.Equal("/formatVersion", exception.Path);
    }

    [Fact]
    public void WeightWithSubTenthPrecisionFailsWithJsonPath()
    {
        var exception = DeserializeException(json =>
        {
            var area = JsonNode.Parse(json)!.AsObject();
            area["objects"]!.AsArray()[0]!.AsObject()["weightPounds"] = 0.15m;
            return area.ToJsonString();
        });

        Assert.Equal("/objects/0/weightPounds", exception.Path);
    }

    [Fact]
    public void WriterRejectsMobileArmorNotInInternalUnits()
    {
        var area = WorldFixtures.CreateArea() with
        {
            Mobiles =
            [
                WorldFixtures.CreateMobile() with
                {
                    Armor = new ArmorValues(5, 0, 0, 0)
                }
            ]
        };

        var exception = Assert.Throws<DataFormatException>(() => AreaJson.Serialize(area));

        Assert.Equal("/mobiles/0/armor", exception.Path);
    }

    [Fact]
    public void SchemaValidationRunsBeforeDeserialization()
    {
        var fixture = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-area.json"));
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "area.schema.json"));
        var area = JsonNode.Parse(fixture)!.AsObject();
        area["unknown"] = true;

        var exception = Assert.Throws<DataFormatException>(
            () => AreaJson.Deserialize(area.ToJsonString(), schema));

        Assert.Null(exception.Path);
        Assert.Contains(exception.Errors, error => error.Contains("unknown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SchemaValidatorReportsUnknownFlagAtPath()
    {
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "area.schema.json"));
        var fixture = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-area.json"));
        var area = JsonNode.Parse(fixture)!.AsObject();
        area["mobiles"]!.AsArray()[0]!.AsObject()["actFlags"] = new JsonArray("Aggro");

        var errors = JsonSchemaValidator.Validate(schema, area.ToJsonString());

        Assert.Contains(errors, error =>
            error.Contains("/mobiles/0/actFlags", StringComparison.Ordinal));
    }

    private static DataFormatException DeserializeException(Func<string, string> mutate)
    {
        var fixture = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-area.json"));
        return Assert.Throws<DataFormatException>(() => AreaJson.Deserialize(mutate(fixture)));
    }

    private static int IndexOf(string text, string value) =>
        text.IndexOf(value, StringComparison.Ordinal);

    private static SocialData CreateSocial(string name) =>
        new(
            name,
            "You act.",
            "$n acts.",
            "You act at $N.",
            "$n acts at $N.",
            "$n acts at you.",
            "They are not here.",
            "You act at yourself.",
            "$n acts at $mself.");
}
