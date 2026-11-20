using VibeROM.Common.Domain;
using VibeROM.Common.Randomness;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Domain;

public sealed class ResetEngineTests
{
    [Fact]
    public void ResetAreaCreatesMobileObjectsPutGiveAndEquip()
    {
        var area = CreateArea();
        var world = new World([area]);
        var engine = new ResetEngine(new DiceRoller(new Random(42)));

        Assert.True(engine.ResetArea(world, area));

        var room = world.GetRoom(1000);
        var mobile = Assert.Single(room.People);
        var bag = Assert.Single(room.Contents, obj => obj.Vnum == 3002);
        Assert.Single(bag.Contents, obj => obj.Vnum == 3001);
        Assert.Contains(mobile.Inventory, obj => obj.Vnum == 3000);
        Assert.Contains(mobile.Inventory, obj => obj.Vnum == 3004);
        Assert.Equal(WearSlot.None, mobile.Inventory.Single(obj => obj.Vnum == 3000).WearLocation);
        Assert.Equal(WearSlot.Body, mobile.Inventory.Single(obj => obj.Vnum == 3004).WearLocation);
        Assert.Empty(world.Validate());
    }

    [Fact]
    public void RepopTwiceWithSameSeedProducesIdenticalSnapshot()
    {
        var area = CreateArea();
        var world = new World([area]);

        var firstEngine = new ResetEngine(new DiceRoller(new Random(1234)));
        Assert.True(firstEngine.ResetArea(world, area));
        var first = Snapshot(world);

        var secondEngine = new ResetEngine(new DiceRoller(new Random(1234)));
        Assert.True(secondEngine.ResetArea(world, area));
        var second = Snapshot(world);

        Assert.Equal(first, second);
        Assert.Empty(world.Validate());
    }

    [Fact]
    public void AreaWithPlayerIsNotReset()
    {
        var area = CreateArea();
        var world = new World([area]);
        var room = world.GetRoom(1000);
        var player = world.CreatePlayer(new PlayerData(), "Tester", Race.Human, PlayerClass.Warrior, Sex.Male);
        world.MoveCharacterToRoom(player, room);

        var engine = new ResetEngine(new DiceRoller(new Random(1)));

        Assert.False(engine.ResetArea(world, area));
        Assert.Single(room.People);
        Assert.Empty(room.Contents);
        Assert.Empty(world.Validate());
    }

    [Fact]
    public void DoorAndRandomizeResetsUpdateRuntimeExits()
    {
        var area = CreateDoorArea();
        var world = new World([area]);
        var room = world.GetRoom(1000);
        Assert.True(new ResetEngine(new DiceRoller(new Random(2))).ResetArea(world, area));

        var north = room.GetExit(Direction.North);
        Assert.NotNull(north);
        Assert.True(RomFlags.Exit.HasFlag(north.Flags, "Closed"));
        Assert.True(RomFlags.Exit.HasFlag(north.Flags, "Locked"));

        var east = room.GetExit(Direction.East);
        Assert.NotNull(east);
        Assert.False(RomFlags.Exit.HasFlag(east.Flags, "Closed"));
        Assert.False(RomFlags.Exit.HasFlag(east.Flags, "Locked"));
    }

    private static string Snapshot(World world)
    {
        var lines = new List<string>();
        foreach (var room in world.Rooms.Values.OrderBy(room => room.Vnum))
        {
            lines.Add($"room {room.Vnum}: people={string.Join(',', room.People.Select(person => person.Prototype?.Vnum ?? 0).Order())}");
            foreach (var obj in room.Contents.OrderBy(obj => obj.Vnum))
            {
                lines.Add($" object {obj.Vnum} level={obj.Level} contents={string.Join(',', obj.Contents.Select(c => c.Vnum).Order())}");
            }
        }

        return string.Join('\n', lines);
    }

    private static AreaData CreateArea()
    {
        var mobile = new MobilePrototype(
            2000, "guard", "the guard", "A guard stands here.", "A guard.",
            Race.Human, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 0,
            new Dice(1, 4, 0), new Dice(1, 4, 0),
            new DamageDice(new Dice(1, 4, 0), "Slash"),
            new ArmorValues(0, 0, 0, 0),
            Position.Standing, Position.Standing, Sex.Male, 0, Size.Medium, "human");

        var sword = new ObjectPrototype(
            3000, "sword", "a sword", "A sword lies here.", "steel",
            ItemType.Weapon, 0, 0, 1, 10, 1, 100,
            new WeaponData(WeaponClass.Sword, 1, 4, "Slash", 0), [], []);
        var gem = new ObjectPrototype(
            3001, "gem", "a gem", "A gem lies here.", "gem",
            ItemType.Treasure, 0, 0, 1, 1, 1, 100,
            new PlainObjectData(), [], []);
        var bag = new ObjectPrototype(
            3002, "bag", "a bag", "A bag lies here.", "leather",
            ItemType.Container, 0, 0, 1, 10, 1, 100,
            new ContainerData(50, 0, 0), [], []);
        var armor = new ObjectPrototype(
            3004, "armor", "some armor", "Some armor lies here.", "steel",
            ItemType.Armor, 0, 0, 1, 10, 1, 100,
            new ArmorData(new ArmorValues(1, 1, 1, 1)), [], []);

        var room = new RoomPrototype(
            1000, "Room", "A room.", 0, Sector.Inside, 100, 100, 0,
            new Dictionary<Direction, ExitPrototype>(), []);

        return new AreaData(
            "Reset Area",
            "tests",
            1000,
            2999,
            9,
            "tests",
            "reset.are",
            [],
            [mobile],
            [sword, gem, bag, armor],
            [room],
            [
                new MobileReset(false, 2000, 1000, 1, 1),
                new ObjectReset(false, 3002, 1000, 1),
                new PutReset(false, 3001, 3002, 1),
                new GiveReset(false, 3000, 1),
                new EquipReset(false, 3004, WearSlot.Body, 1)
            ],
            [],
            [],
            []);
    }

    private static AreaData CreateDoorArea()
    {
        var exits = new Dictionary<Direction, ExitPrototype>
        {
            [Direction.North] = new(Direction.North, 1000, 0, -1, ""),
            [Direction.East] = new(Direction.East, 1000, 0, -1, "")
        };
        var room = new RoomPrototype(
            1000, "Doors", "A room.", 0, Sector.Inside, 100, 100, 0, exits, []);
        return new AreaData(
            "Door Area",
            "tests",
            1000,
            1999,
            9,
            "tests",
            "doors.are",
            [],
            [],
            [],
            [room],
            [
                new DoorReset(false, 1000, Direction.North, DoorState.Locked),
                new DoorReset(false, 1000, Direction.East, DoorState.Open)
            ],
            [],
            [],
            []);
    }
}
