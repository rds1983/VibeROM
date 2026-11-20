using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Domain;

public sealed class WorldTests
{
    [Fact]
    public void World_BuildsIndexesAndCounters()
    {
        var world = new World([WorldFixtures.CreateArea()]);

        Assert.Equal(1, world.TopArea);
        Assert.Equal(1, world.TopMobIndex);
        Assert.Equal(2, world.TopObjectIndex);
        Assert.Equal(2, world.TopRoom);
        Assert.Equal(1, world.TopHelp);
        Assert.Equal(2000, world.TopVnumMob);
        Assert.Equal(3001, world.TopVnumObj);
        Assert.Equal(1001, world.TopVnumRoom);

        Assert.Equal(2000, world.GetMobile(2000).Vnum);
        Assert.Equal(3000, world.GetObjectPrototype(3000).Vnum);
        Assert.Equal(1000, world.GetRoomPrototype(1000).Vnum);
        Assert.Equal(1000, world.GetRoom(1000).Vnum);
        Assert.Single(world.FindHelp("rom"));
        Assert.Single(world.FindHelp("DIKU"));
    }

    [Fact]
    public void World_DuplicateVnumAcrossAreas_Throws()
    {
        var first = WorldFixtures.CreateArea("First");
        var second = WorldFixtures.CreateArea(
            "Second",
            weaponVnum: 3100,
            containerVnum: 3101,
            room1Vnum: 1100,
            room2Vnum: 1101);

        var exception = Assert.Throws<WorldDataException>(() => new World([first, second]));

        Assert.Contains(exception.Errors, error => error.Contains("Duplicate mobile vnum 2000", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownLookup_Throws()
    {
        var world = new World([WorldFixtures.CreateArea()]);

        Assert.Throws<KeyNotFoundException>(() => world.GetMobile(9999));
        Assert.Throws<KeyNotFoundException>(() => world.GetObjectPrototype(9999));
        Assert.Throws<KeyNotFoundException>(() => world.GetRoom(9999));
    }

    [Fact]
    public void MoveObjectToRoom_UpdatesOwnership()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var room = world.GetRoom(1000);
        var character = world.CreateMobile(world.GetMobile(2000), room);
        var obj = world.CreateObject(world.GetObjectPrototype(3000), carrier: character);

        world.MoveObjectToRoom(obj, room);

        Assert.Null(obj.CarriedBy);
        Assert.Same(room, obj.InRoom);
        Assert.Contains(obj, room.Contents);
        Assert.Empty(character.Inventory);
    }

    [Fact]
    public void MoveObjectToContainer_UpdatesContents()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var room = world.GetRoom(1000);
        var container = world.CreateObject(world.GetObjectPrototype(3001), room: room);
        var obj = world.CreateObject(world.GetObjectPrototype(3000), room: room);

        world.MoveObjectToContainer(obj, container);

        Assert.Same(container, obj.Container);
        Assert.Contains(obj, container.Contents);
        Assert.DoesNotContain(obj, room.Contents);
    }

    [Fact]
    public void MoveObjectToContainer_RejectsCycles()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var outer = world.CreateObject(world.GetObjectPrototype(3001));
        var inner = world.CreateObject(world.GetObjectPrototype(3001));

        world.MoveObjectToContainer(inner, outer);

        Assert.Throws<InvalidOperationException>(() => world.MoveObjectToContainer(outer, inner));
        Assert.Throws<InvalidOperationException>(() => world.MoveObjectToContainer(outer, outer));
    }

    [Fact]
    public void EquipAndUnequip_KeepInventoryConsistent()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var character = world.CreateMobile(world.GetMobile(2000));
        var weapon = world.CreateObject(world.GetObjectPrototype(3000), carrier: character);

        world.EquipObject(character, weapon, WearSlot.Wield);

        Assert.Same(weapon, character.Equipment[WearSlot.Wield]);
        Assert.Equal(WearSlot.Wield, weapon.WearLocation);
        Assert.Contains(weapon, character.Inventory);
        Assert.Empty(character.Validate());

        world.UnequipObject(character, WearSlot.Wield);

        Assert.Empty(character.Equipment);
        Assert.Equal(WearSlot.None, weapon.WearLocation);
        Assert.Contains(weapon, character.Inventory);
    }

    [Fact]
    public void EquipObject_RequiresCarriedObject()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var character = world.CreateMobile(world.GetMobile(2000));
        var weapon = world.CreateObject(world.GetObjectPrototype(3000), room: world.GetRoom(1000));

        Assert.Throws<InvalidOperationException>(() => world.EquipObject(character, weapon, WearSlot.Wield));
    }

    [Fact]
    public void DestroyObject_DetachesAndDestroysContents()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var room = world.GetRoom(1000);
        var container = world.CreateObject(world.GetObjectPrototype(3001), room: room);
        var weapon = world.CreateObject(world.GetObjectPrototype(3000), container: container);

        world.DestroyObject(container);

        Assert.DoesNotContain(container, world.GameObjects);
        Assert.DoesNotContain(weapon, world.GameObjects);
        Assert.Empty(room.Contents);
    }

    [Fact]
    public void DestroyCharacter_RemovesInventoryAndFightingLinks()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var room = world.GetRoom(1000);
        var character = world.CreateMobile(world.GetMobile(2000), room);
        var other = world.CreateMobile(world.GetMobile(2000), room);
        var weapon = world.CreateObject(world.GetObjectPrototype(3000), carrier: character);
        character.Fighting = other;
        other.Fighting = character;
        other.Reply = character;

        world.DestroyCharacter(character);

        Assert.DoesNotContain(character, world.Characters);
        Assert.DoesNotContain(character, room.People);
        Assert.Null(other.Fighting);
        Assert.Null(other.Reply);
        Assert.DoesNotContain(weapon, world.GameObjects);
        Assert.Empty(room.Contents);
    }

    [Fact]
    public void World_ValidateCatchesStaleBackReferences()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var room = world.GetRoom(1000);
        var obj = world.CreateObject(world.GetObjectPrototype(3000), room: room);

        obj.InRoom = world.GetRoom(1001);

        Assert.Contains(world.Validate(), error => error.Contains("does not point back", StringComparison.Ordinal));
    }

    [Fact]
    public void FuzzLifecycle_KeepsWorldConsistent()
    {
        var world = new World([WorldFixtures.CreateArea()]);
        var random = new Random(12345);
        var rooms = world.Rooms.Values.ToArray();
        var characters = rooms
            .SelectMany((room, index) => Enumerable.Range(0, 2)
                .Select(_ => world.CreateMobile(world.GetMobile(2000), room)))
            .ToArray();

        var weaponPrototype = world.GetObjectPrototype(3000);
        var containerPrototype = world.GetObjectPrototype(3001);
        var live = new List<GameObject>();
        for (var index = 0; index < 200; index++)
        {
            var prototype = index % 3 == 0 ? containerPrototype : weaponPrototype;
            live.Add(world.CreateObject(prototype, room: rooms[random.Next(rooms.Length)]));
        }

        for (var iteration = 0; iteration < 5_000; iteration++)
        {
            live.RemoveAll(obj => !world.GameObjects.Contains(obj));
            if (live.Count == 0)
            {
                break;
            }

            var obj = live[random.Next(live.Count)];
            switch (random.Next(8))
            {
                case 0:
                    world.MoveObjectToRoom(obj, rooms[random.Next(rooms.Length)]);
                    break;

                case 1:
                    world.MoveObjectToCharacter(obj, characters[random.Next(characters.Length)]);
                    break;

                case 2:
                    var container = live[random.Next(live.Count)];
                    try
                    {
                        world.MoveObjectToContainer(obj, container);
                    }
                    catch (InvalidOperationException)
                    {
                        // Cycle attempts are expected in fuzzing.
                    }

                    break;

                case 3:
                    var carrier = characters[random.Next(characters.Length)];
                    if (ReferenceEquals(obj.CarriedBy, carrier))
                    {
                        try
                        {
                            world.EquipObject(carrier, obj, RandomSlot(random));
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }

                    break;

                case 4:
                    var unequip = characters[random.Next(characters.Length)];
                    world.UnequipObject(unequip, RandomSlot(random));
                    break;

                case 5:
                    world.DestroyObject(obj);
                    live.Remove(obj);
                    break;

                case 6:
                    live.Add(world.CreateObject(
                        random.Next(2) == 0 ? weaponPrototype : containerPrototype,
                        room: rooms[random.Next(rooms.Length)]));
                    break;

                case 7:
                    world.MoveObjectToRoom(obj, rooms[random.Next(rooms.Length)]);
                    break;
            }
        }

        Assert.Empty(world.Validate());
        live.RemoveAll(obj => !world.GameObjects.Contains(obj));
        Assert.Equal(live.Count, world.GameObjects.Count);
    }

    private static WearSlot RandomSlot(Random random)
    {
        var slots = new[]
        {
            WearSlot.Light,
            WearSlot.Body,
            WearSlot.Head,
            WearSlot.Wield,
            WearSlot.Hold,
            WearSlot.About
        };

        return slots[random.Next(slots.Length)];
    }
}
