using System.Text.Json.Nodes;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Rom;

public sealed class SchemaAlignmentTests
{
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void AreaFlagEnums_MatchRegistries()
    {
        AssertRegistry("area.schema.json", "actFlag", RomFlags.Act);
        AssertRegistry("area.schema.json", "affectFlag", RomFlags.Affect);
        AssertRegistry("area.schema.json", "offensiveFlag", RomFlags.Offensive);
        AssertRegistry("area.schema.json", "immFlag", RomFlags.Immunity);
        AssertRegistry("area.schema.json", "formFlag", RomFlags.Form);
        AssertRegistry("area.schema.json", "partFlag", RomFlags.Part);
        AssertRegistry("area.schema.json", "roomFlag", RomFlags.Room);
        AssertRegistry("area.schema.json", "exitFlag", RomFlags.Exit);
        AssertRegistry("area.schema.json", "extraFlag", RomFlags.Extra);
        AssertRegistry("area.schema.json", "wearFlag", RomFlags.Wear);
        AssertRegistry("area.schema.json", "containerFlag", RomFlags.Container);
        AssertRegistry("area.schema.json", "weaponSpecialFlag", RomFlags.WeaponSpecial);
        AssertRegistry("area.schema.json", "furnitureFlag", RomFlags.Furniture);
        AssertRegistry("area.schema.json", "gateFlag", RomFlags.Gate);
    }

    [Fact]
    public void AreaValueEnums_MatchTables()
    {
        AssertTable("area.schema.json", "size", RomTables.Sizes);
        AssertTable("area.schema.json", "position", RomTables.Positions);
        AssertTable("area.schema.json", "sex", RomTables.Sexes);
        AssertTable("area.schema.json", "race", RomTables.Races);
        AssertTable("area.schema.json", "itemType", RomTables.ItemTypes);
        AssertTable("area.schema.json", "applyLocation", RomTables.ApplyLocations);
        AssertTable("area.schema.json", "sector", RomTables.Sectors);
        AssertTable("area.schema.json", "liquid", RomTables.Liquids);
        AssertTable("area.schema.json", "weaponClass", RomTables.WeaponClasses);
        AssertTable("area.schema.json", "attackType", RomTables.AttackTypes);
        AssertTable("area.schema.json", "specialProcedure", RomTables.Specials);
    }

    [Fact]
    public void PlayerEnums_MatchTables()
    {
        AssertRegistry("player.schema.json", "playerFlag", RomFlags.PlayerAct);
        AssertRegistry("player.schema.json", "communicationFlag", RomFlags.Communication);
        AssertRegistry("player.schema.json", "wiznetFlag", RomFlags.Wiznet);
        AssertRegistry("player.schema.json", "affectFlag", RomFlags.Affect);
        AssertTable("player.schema.json", "applyLocation", RomTables.ApplyLocations);
        AssertTable("player.schema.json", "race", RomTables.PcRaces);
        AssertTable("player.schema.json", "class", RomTables.Classes);
        AssertTable("player.schema.json", "sex", RomTables.Sexes);
        AssertTable("player.schema.json", "position", RomTables.Positions);
    }

    private static void AssertRegistry(string schemaFile, string definition, FlagRegistry registry)
    {
        var expected = ReadEnum(schemaFile, definition);
        Assert.Equal(expected, registry.PersistedNames);
    }

    private static void AssertTable(string schemaFile, string definition, ValueTable table)
    {
        var expected = ReadEnum(schemaFile, definition);
        Assert.Equal(expected, table.Names);
    }

    private static IReadOnlyList<string> ReadEnum(string schemaFile, string definition)
    {
        var path = Path.Combine(SchemaDirectory, schemaFile);
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        return root["$defs"]![definition]!["enum"]!.AsArray()
            .Select(node => node!.GetValue<string>())
            .ToArray();
    }
}
