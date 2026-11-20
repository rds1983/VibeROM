using VibeROM.Common.Randomness;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>
/// Ports ROM's <c>reset_area</c>/<c>reset_room</c>. Resets are evaluated per
/// room in vnum order, with the same last-mob/last-object/level state and
/// deterministic random levels when supplied a seeded <see cref="DiceRoller"/>.
/// </summary>
public sealed class ResetEngine
{
    private const int LevelHero = 51;
    private const int Unlimited = 999;

    private readonly DiceRoller _roller;

    public ResetEngine(DiceRoller roller)
    {
        _roller = roller ?? throw new ArgumentNullException(nameof(roller));
    }

    /// <summary>
    /// Clears reset-created contents in every area room and repopulates it.
    /// Areas containing player characters are skipped, matching ROM's
    /// empty-area repop rule. Returns false when the area was skipped.
    /// </summary>
    public bool ResetArea(World world, AreaData area)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(area);

        var rooms = area.Rooms
            .Select(prototype => world.GetRoom(prototype.Vnum))
            .OrderBy(room => room.Vnum)
            .ToArray();

        if (rooms.Any(room => room.People.Any(character => character.IsPlayer)))
        {
            return false;
        }

        foreach (var room in rooms)
        {
            foreach (var character in room.People.Where(character => character.IsNpc).ToArray())
            {
                world.DestroyCharacter(character);
            }

            foreach (var obj in room.Contents.ToArray())
            {
                world.DestroyObject(obj);
            }

            room.ResetExits();
        }

        var resetRooms = BuildRoomResets(area);
        foreach (var room in rooms)
        {
            if (resetRooms.TryGetValue(room.Vnum, out var resets))
            {
                ResetRoom(world, room, resets);
            }
        }

        return true;
    }

    public void ResetWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        foreach (var area in world.Areas)
        {
            ResetArea(world, area);
        }
    }

    private static Dictionary<int, List<Reset>> BuildRoomResets(AreaData area)
    {
        var result = new Dictionary<int, List<Reset>>();
        var lastRoom = 0;
        var lastObjectRoom = 0;

        foreach (var reset in area.Resets)
        {
            var roomVnum = reset switch
            {
                MobileReset mobile => TrackMobile(mobile, ref lastRoom),
                ObjectReset obj => TrackObject(obj, ref lastObjectRoom),
                PutReset put => lastObjectRoom,
                GiveReset give => TrackGive(give, ref lastObjectRoom, lastRoom),
                EquipReset equip => TrackEquip(equip, ref lastObjectRoom, lastRoom),
                DoorReset door => door.RoomVnum,
                RandomizeReset randomize => randomize.RoomVnum,
                _ => 0
            };

            if (roomVnum <= 0)
            {
                continue;
            }

            if (!result.TryGetValue(roomVnum, out var list))
            {
                list = [];
                result[roomVnum] = list;
            }

            list.Add(reset);
        }

        return result;
    }

    private static int TrackMobile(MobileReset reset, ref int lastRoom)
    {
        lastRoom = reset.RoomVnum;
        return reset.RoomVnum;
    }

    private static int TrackObject(ObjectReset reset, ref int lastObjectRoom)
    {
        lastObjectRoom = reset.RoomVnum;
        return reset.RoomVnum;
    }

    private static int TrackGive(GiveReset reset, ref int lastObjectRoom, int lastRoom)
    {
        lastObjectRoom = lastRoom;
        return lastRoom;
    }

    private static int TrackEquip(EquipReset reset, ref int lastObjectRoom, int lastRoom)
    {
        lastObjectRoom = lastRoom;
        return lastRoom;
    }

    private void ResetRoom(World world, Room room, IReadOnlyList<Reset> resets)
    {
        Character? lastMob = null;
        GameObject? lastObj = null;
        var level = 0;
        var last = false;

        foreach (var reset in resets)
        {
            switch (reset)
            {
                case MobileReset mobile:
                    last = ResetMobile(world, room, mobile, out lastMob, out level);
                    break;

                case ObjectReset obj:
                    ResetObject(world, room, obj, level);
                    break;

                case PutReset put:
                    lastObj = ResetPut(world, room, put, lastObj);
                    break;

                case GiveReset give:
                    last = ResetGive(world, room, give, lastMob, level, equip: false, WearSlot.None);
                    break;

                case EquipReset equip:
                    last = ResetGive(world, room, equip, lastMob, level, equip: true, equip.WearLocation);
                    break;

                case DoorReset door:
                    ApplyDoor(room, door);
                    break;

                case RandomizeReset randomize:
                    ApplyRandomize(room, randomize);
                    break;
            }
        }
    }

    private bool ResetMobile(
        World world,
        Room room,
        MobileReset reset,
        out Character? lastMob,
        out int level)
    {
        lastMob = null;
        level = 0;

        var prototype = world.GetMobile(reset.MobileVnum);
        var inWorld = world.Characters.Count(character =>
            character.IsNpc && character.Prototype?.Vnum == reset.MobileVnum);
        if (reset.MaxInWorld >= 0 && inWorld >= reset.MaxInWorld)
        {
            return false;
        }

        var inRoom = room.People.Count(character =>
            character.IsNpc && character.Prototype?.Vnum == reset.MobileVnum);
        if (reset.MaxInRoom >= 0 && inRoom >= reset.MaxInRoom)
        {
            return false;
        }

        lastMob = world.CreateMobile(prototype, room);
        level = Math.Clamp(prototype.Level - 2, 0, LevelHero - 1);
        return true;
    }

    private void ResetObject(World world, Room room, ObjectReset reset, int level)
    {
        var prototype = world.GetObjectPrototype(reset.ObjectVnum);
        if (reset.MaxInWorld >= 0 && CountInstances(world, reset.ObjectVnum) >= reset.MaxInWorld)
        {
            return;
        }

        var obj = world.CreateObject(prototype, room: room);
        obj.Level = Math.Min(_roller.NumberFuzzy(level), LevelHero - 1);
        obj.CostCopper = 0;
    }

    private GameObject? ResetPut(World world, Room room, PutReset reset, GameObject? lastObj)
    {
        var prototype = world.GetObjectPrototype(reset.ObjectVnum);
        var containerPrototype = world.GetObjectPrototype(reset.ContainerVnum);
        var container = lastObj is not null && lastObj.Prototype.Vnum == reset.ContainerVnum
            ? lastObj
            : world.GameObjects.FirstOrDefault(obj => obj.Prototype.Vnum == reset.ContainerVnum);
        if (container is null || container.InRoom != room)
        {
            return lastObj;
        }

        if (reset.MaxInWorld >= 0 && CountInstances(world, reset.ObjectVnum) >= reset.MaxInWorld)
        {
            return lastObj;
        }

        var obj = world.CreateObject(prototype, container: container);
        obj.Level = _roller.NumberFuzzy(container.Level);
        return container;
    }

    private bool ResetGive(
        World world,
        Room room,
        Reset reset,
        Character? lastMob,
        int level,
        bool equip,
        WearSlot slot)
    {
        if (lastMob is null || lastMob.InRoom != room)
        {
            return false;
        }

        var objectVnum = reset switch
        {
            GiveReset give => give.ObjectVnum,
            EquipReset equipReset => equipReset.ObjectVnum,
            _ => 0
        };
        var maxInWorld = reset switch
        {
            GiveReset give => give.MaxInWorld,
            EquipReset equipReset => equipReset.MaxInWorld,
            _ => 0
        };

        var prototype = world.GetObjectPrototype(objectVnum);
        if (world.Shops.TryGetValue(lastMob.Prototype!.Vnum, out _))
        {
            var obj = world.CreateObject(prototype, carrier: lastMob);
            obj.Level = ShopObjectLevel(prototype.ItemType);
            obj.ExtraFlags |= RomFlags.Extra.GetMask(["Inventory"]);
            if (equip)
            {
                world.EquipObject(lastMob, obj, slot);
            }

            return true;
        }

        var limit = maxInWorld > 50 ? 6 : maxInWorld == -1 ? Unlimited : maxInWorld;
        if (limit >= 0 && CountInstances(world, objectVnum) >= limit)
        {
            return false;
        }

        var created = world.CreateObject(prototype, carrier: lastMob);
        created.Level = Math.Min(_roller.NumberFuzzy(level), LevelHero - 1);
        if (equip)
        {
            world.EquipObject(lastMob, created, slot);
        }

        return true;
    }

    private int ShopObjectLevel(ItemType itemType) =>
        itemType switch
        {
            ItemType.Pill or ItemType.Potion => _roller.NumberRange(0, 10),
            ItemType.Scroll or ItemType.Armor or ItemType.Weapon => _roller.NumberRange(5, 15),
            ItemType.Wand or ItemType.Treasure => _roller.NumberRange(10, 20),
            ItemType.Staff => _roller.NumberRange(15, 25),
            _ => 0
        };

    private static int CountInstances(World world, int objectVnum) =>
        world.GameObjects.Count(obj => obj.Prototype.Vnum == objectVnum);

    private static void ApplyDoor(Room room, DoorReset reset)
    {
        if (!room.Exits.TryGetValue(reset.Direction, out var exit))
        {
            return;
        }

        var flags = exit.Flags;
        var closed = RomFlags.Exit.GetMask(["Closed"]);
        var locked = RomFlags.Exit.GetMask(["Locked"]);
        flags = reset.State switch
        {
            DoorState.Open => flags & ~(closed | locked),
            DoorState.Closed => (flags | closed) & ~locked,
            DoorState.Locked => flags | closed | locked,
            _ => flags
        };
        room.SetExit(reset.Direction, exit with { Flags = flags });
    }

    private void ApplyRandomize(Room room, RandomizeReset reset)
    {
        var count = reset.Door;
        if (count <= 1)
        {
            return;
        }

        var slots = new ExitPrototype?[count];
        for (var index = 0; index < count; index++)
        {
            room.Exits.TryGetValue((Direction)index, out slots[index]);
        }

        for (var from = 0; from < count - 1; from++)
        {
            var to = _roller.NumberRange(from, count - 1);
            (slots[from], slots[to]) = (slots[to], slots[from]);
        }

        var exits = new Dictionary<Direction, ExitPrototype>(room.Exits);
        for (var index = 0; index < count; index++)
        {
            if (slots[index] is { } exit)
            {
                exits[(Direction)index] = exit with { Direction = (Direction)index };
            }
            else
            {
                exits.Remove((Direction)index);
            }
        }

        room.SetExits(exits);
    }
}
