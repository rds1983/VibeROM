using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Serialization;

namespace VibeROM.Server.Tests;

public sealed class WorldLoaderTests
{
    private static readonly string AreasDirectory = Path.Combine(AppContext.BaseDirectory, "areas");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void LoadsFullCorpusAndValidates()
    {
        var result = WorldLoader.Load(AreasDirectory, SchemaDirectory);

        Assert.Empty(result.World.Validate());
        Assert.Equal(52, result.World.TopArea);
        Assert.Equal(986, result.World.TopMobIndex);
        Assert.Equal(1265, result.World.TopObjectIndex);
        Assert.Equal(3126, result.World.TopRoom);
        Assert.Equal(248, result.World.TopHelp);
        Assert.Equal(5233, result.World.Areas.Sum(area => area.Resets.Count));
        Assert.Equal(62, result.World.Shops.Count);
        Assert.Equal(411, result.World.Areas.Sum(area => area.Specials.Count));
        Assert.Equal(244, result.World.Areas.Sum(area => area.Socials.Count));

        Assert.Equal("Midgaard", result.World.Areas[7].Name);
        Assert.Equal(3000, result.World.GetMobile(3000).Vnum);
        Assert.Equal(3000, result.World.GetObjectPrototype(3000).Vnum);
        Assert.Equal(3100, result.World.GetRoom(3100).Vnum);
        Assert.Equal(3100, result.World.GetShop(3100).KeeperVnum);
        Assert.NotNull(result.World.GetRoom(3100).GetExit(Direction.North));
    }

    [Fact]
    public void MissingReferencedAreaFailsWithPreciseError()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        WriteIndex(areas, "missing.json");

        var exception = Assert.Throws<WorldDataException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains(exception.Errors, error => error.Contains("missing.json", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownReferenceFailsWithAreaAndVnum()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        var area = CreateArea("Broken", badExit: true);
        WriteArea(areas, "broken.json", area);
        WriteIndex(areas, "broken.json");

        var exception = Assert.Throws<WorldDataException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains(exception.Errors, error =>
            error.Contains("Broken", StringComparison.Ordinal)
            && error.Contains("unknown room vnum 9999", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateVnumsAcrossAreasAreRejected()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        WriteArea(areas, "first.json", CreateArea("First"));
        WriteArea(areas, "second.json", CreateArea("Second"));
        WriteIndex(areas, "first.json", "second.json");

        var exception = Assert.Throws<WorldDataException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains(exception.Errors, error => error.Contains("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void AppliesMaterialDefaultAndIndexesShops()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        var area = CreateArea("Fixups", emptyMaterial: true, withShop: true);
        WriteArea(areas, "fixups.json", area);
        WriteIndex(areas, "fixups.json");

        var result = WorldLoader.Load(areas, SchemaDirectory);

        Assert.Equal("none", result.World.GetObjectPrototype(1001).Material);
        Assert.Contains(result.Warnings, warning => warning.Contains("no material", StringComparison.Ordinal));
        Assert.Equal(1000, result.World.GetShop(1000).KeeperVnum);
    }

    [Fact]
    public void RejectsPrototypesOutsideTheirAreaVnumRange()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        WriteArea(areas, "range.json", CreateArea("Range", low: 2000, high: 2999));
        WriteIndex(areas, "range.json");

        var exception = Assert.Throws<WorldDataException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains(exception.Errors, error =>
            error.Contains("outside its vnum range 2000-2999", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsOverlappingAreaVnumRanges()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        WriteArea(areas, "first.json", CreateArea("First", vnum: 1000, low: 1000, high: 1999));
        WriteArea(areas, "second.json", CreateArea("Second", vnum: 2000, low: 1500, high: 2499));
        WriteIndex(areas, "first.json", "second.json");

        var exception = Assert.Throws<WorldDataException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains(exception.Errors, error =>
            error.Contains("overlaps", StringComparison.Ordinal)
            && error.Contains("Second", StringComparison.Ordinal));
    }

    [Fact]
    public void TruncatedAreaJsonFailsAsDataFormatException()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        File.WriteAllText(Path.Combine(areas, "broken.json"), "{ \"formatVersion\": ");
        WriteIndex(areas, "broken.json");

        var exception = Assert.Throws<DataFormatException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains("broken.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Invalid JSON", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TruncatedIndexJsonFailsAsDataFormatException()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        File.WriteAllText(Path.Combine(areas, "index.json"), "{ \"formatVersion\": ");

        var exception = Assert.Throws<DataFormatException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains("index.json", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownEnumNameFailsWithJsonPathAndFileName()
    {
        using var temp = new TempDirectory();
        var areas = temp.CreateDirectory("areas");
        var area = CreateArea("Names");
        var json = AreaJson.Serialize(area).Replace(
            "\"race\": \"Human\"",
            "\"race\": \"Wizard\"",
            StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(areas, "names.json"), json);
        WriteIndex(areas, "names.json");

        var exception = Assert.Throws<DataFormatException>(
            () => WorldLoader.Load(areas, SchemaDirectory));

        Assert.Contains("names.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains(exception.Errors, error =>
            error.Contains("/mobiles/0/race", StringComparison.Ordinal));
    }

    private static void WriteIndex(string areasDirectory, params string[] names) =>
        File.WriteAllText(
            Path.Combine(areasDirectory, "index.json"),
            $$"""
              {
                "formatVersion": 1,
                "source": {
                  "project": "ROM",
                  "version": "2.4b6",
                  "releaseDate": "1998-05-29"
                },
                "areas": [{{string.Join(", ", names.Select(name => $"\"{name}\""))}}]
              }
              """);

    private static void WriteArea(string areasDirectory, string name, AreaData area) =>
        File.WriteAllText(Path.Combine(areasDirectory, name), AreaJson.Serialize(area));

    private static AreaData CreateArea(
        string name,
        bool badExit = false,
        bool emptyMaterial = false,
        bool withShop = false,
        int vnum = 1000,
        int low = 1000,
        int high = 1999)
    {
        var mobile = new MobilePrototype(
            vnum + 0, "guard", "the guard", "A guard stands here.", "A guard.",
            Race.Human, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 0,
            new Dice(1, 4, 0), new Dice(1, 4, 0),
            new DamageDice(new Dice(1, 4, 0), "Slash"),
            new ArmorValues(0, 0, 0, 0),
            Position.Standing, Position.Standing, Sex.Male, 0, Size.Medium, "human");
        var obj = new ObjectPrototype(
            vnum + 1, "gem", "a gem", "A gem lies here.", emptyMaterial ? string.Empty : "gem",
            ItemType.Treasure, 0, 0, 1, 1, 1, 100,
            new PlainObjectData(), [], []);
        var exits = new Dictionary<Direction, ExitPrototype>();
        if (badExit)
        {
            exits[Direction.North] = new ExitPrototype(Direction.North, 9999, 0, -1, "");
        }

        var room = new RoomPrototype(
            vnum + 2, "Room", "A room.", 0, Sector.Inside, 100, 100, 0, exits, []);
        return new AreaData(
            name,
            "tests",
            low,
            high,
            9,
            "tests",
            "test.are",
            [],
            [mobile],
            [obj],
            [room],
            [],
            withShop ? [new ShopData(vnum, [ItemType.Treasure], 100, 100, 0, 23)] : [],
            [],
            []);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "viberom-world-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

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
