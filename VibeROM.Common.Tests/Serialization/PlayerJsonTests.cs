using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Serialization;

namespace VibeROM.Common.Tests.Serialization;

public sealed class PlayerJsonTests
{
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void ValidFixtureRoundTripsThroughPlayerJson()
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-player.json"));
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "player.schema.json"));

        var firstWorld = CreateWorld();
        var character = PlayerJson.Deserialize(json, firstWorld, schema);
        Assert.Empty(character.Validate());
        Assert.Equal("Alander", character.Name);
        Assert.Equal(10, character.Level);
        Assert.True(RomFlags.Affect.HasFlag(character.Affects[0].Bits, "Sanctuary"));
        Assert.Equal(2, character.Inventory.Count);
        Assert.Single(character.Equipment);
        Assert.NotNull(character.Pet);

        var canonical = PlayerJson.Serialize(character);
        Assert.Empty(JsonSchemaValidator.Validate(schema, canonical));

        var secondWorld = CreateWorld();
        var roundTripped = PlayerJson.Deserialize(canonical, secondWorld, schema);
        Assert.Equal(canonical, PlayerJson.Serialize(roundTripped));
        Assert.Empty(roundTripped.Validate());
    }

    [Fact]
    public void InvalidPlayerJsonIsRejectedWithPath()
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-player.json"));
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "player.schema.json"));
        var world = CreateWorld();
        var mutated = json.Replace("\"race\": \"Human\"", "\"race\": \"Wizard\"", StringComparison.Ordinal);

        var exception = Assert.Throws<DataFormatException>(
            () => PlayerJson.Deserialize(mutated, world, schema));

        Assert.Contains(exception.Errors, error => error.Contains("/race", StringComparison.Ordinal));
    }

    [Fact]
    public void SaveWritesAtomicallyAndLoadsBack()
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-player.json"));
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "player.schema.json"));
        var world = CreateWorld();
        var character = PlayerJson.Deserialize(json, world, schema);
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "Alander.json");

        PlayerJson.Save(path, character, schema);

        Assert.True(File.Exists(path));
        Assert.False(Directory.GetFiles(temp.Path, "*.tmp-*").Any());
        var loaded = PlayerJson.Load(path, CreateWorld(), schema);
        Assert.Equal("Alander", loaded.Name);
        Assert.Equal(character.Gold, loaded.Gold);
        Assert.Equal(character.Silver, loaded.Silver);
    }

    [Fact]
    public void SaveRejectsSchemaInvalidCharacterWithoutReplacingFile()
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-player.json"));
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, "player.schema.json"));
        var character = PlayerJson.Deserialize(json, CreateWorld(), schema);
        character.Name = "bad name";
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "Alander.json");
        File.WriteAllText(path, "old");

        Assert.Throws<DataFormatException>(() => PlayerJson.Save(path, character, schema));

        Assert.Equal("old", File.ReadAllText(path));
        Assert.False(Directory.GetFiles(temp.Path, "*.tmp-*").Any());
    }

    [Fact]
    public void UnknownObjectVnumFailsWithPath()
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-player.json"));
        var mutated = json.Replace("\"vnum\": 1001", "\"vnum\": 9999", StringComparison.Ordinal);

        var exception = Assert.Throws<DataFormatException>(
            () => PlayerJson.Deserialize(mutated, CreateWorld()));

        Assert.Equal("/equipment/0/item/vnum", exception.Path);
        Assert.Contains("9999", exception.Message, StringComparison.Ordinal);
    }

    private static World CreateWorld()
    {
        var mobile = new MobilePrototype(
            1000, "guard", "the example guard", "An example guard stands here.", "A guard.",
            Race.Human, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 0,
            new Dice(1, 4, 0), new Dice(1, 4, 0),
            new DamageDice(new Dice(1, 4, 0), "Slash"),
            new ArmorValues(0, 0, 0, 0),
            Position.Standing, Position.Standing, Sex.Male, 0, Size.Medium, "human");
        var weapon = new ObjectPrototype(
            1001, "sword", "an example sword", "An example sword lies here.", "steel",
            ItemType.Weapon, 0, 0, 10, 50, 100, 100,
            new WeaponData(WeaponClass.Sword, 2, 6, "Slash", 0), [], []);
        var bag = new ObjectPrototype(
            1003, "backpack", "a leather backpack", "A backpack lies here.", "leather",
            ItemType.Container, 0, 0, 1, 20, 15, 100,
            new ContainerData(50, 0, 0), [], []);
        var gem = new ObjectPrototype(
            1005, "gem", "a gem", "A gem lies here.", "gem",
            ItemType.Treasure, 0, 0, 1, 1, 1, 100,
            new PlainObjectData(), [], []);
        var room = new RoomPrototype(
            1000, "Room", "A room.", 0, Sector.Inside, 100, 100, 0,
            new Dictionary<Direction, ExitPrototype>(), []);
        var area = new AreaData(
            "Player Test",
            "tests",
            1000,
            1999,
            9,
            "tests",
            "player.are",
            [],
            [mobile],
            [weapon, bag, gem],
            [room],
            [],
            [],
            [],
            []);
        return new World([area]);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "viberom-player-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
