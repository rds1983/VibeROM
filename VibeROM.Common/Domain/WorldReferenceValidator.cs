using VibeROM.Common.Domain;

namespace VibeROM.Common.Serialization;

/// <summary>Validates cross-prototype references after a world has been indexed.</summary>
public static class WorldReferenceValidator
{
    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        ValidateVnumRanges(world, errors);

        foreach (var area in world.Areas)
        {
            foreach (var room in area.Rooms)
            {
                foreach (var (direction, exit) in room.Exits)
                {
                    var directionName = direction.ToString().ToLowerInvariant();
                    if (exit.ToRoomVnum > 0 && !world.RoomPrototypes.ContainsKey(exit.ToRoomVnum))
                    {
                        errors.Add(
                            $"Area '{area.Name}': room {room.Vnum} {directionName} exit points at unknown room vnum {exit.ToRoomVnum}.");
                    }

                    if (exit.KeyVnum > 0 && !world.ObjectPrototypes.ContainsKey(exit.KeyVnum))
                    {
                        errors.Add(
                            $"Area '{area.Name}': room {room.Vnum} {directionName} exit key points at unknown object vnum {exit.KeyVnum}.");
                    }
                }
            }

            foreach (var obj in area.Objects)
            {
                switch (obj.Data)
                {
                    case ContainerData container when container.KeyVnum > 0
                                                       && !world.ObjectPrototypes.ContainsKey(container.KeyVnum):
                        errors.Add(
                            $"Area '{area.Name}': object {obj.Vnum} container key points at unknown object vnum {container.KeyVnum}.");
                        break;

                    case PortalData portal when portal.ToRoomVnum > 0
                                               && !world.RoomPrototypes.ContainsKey(portal.ToRoomVnum):
                        errors.Add(
                            $"Area '{area.Name}': object {obj.Vnum} portal points at unknown room vnum {portal.ToRoomVnum}.");
                        break;

                    case MapData map when map.RoomVnum > 0
                                          && !world.RoomPrototypes.ContainsKey(map.RoomVnum):
                        errors.Add(
                            $"Area '{area.Name}': object {obj.Vnum} map points at unknown room vnum {map.RoomVnum}.");
                        break;
                }
            }

            for (var index = 0; index < area.Resets.Count; index++)
            {
                var reset = area.Resets[index];
                switch (reset)
                {
                    case MobileReset mobile:
                        CheckMobile(world, area, index, mobile.MobileVnum, errors);
                        CheckRoom(world, area, index, mobile.RoomVnum, errors);
                        break;

                    case ObjectReset obj:
                        CheckObject(world, area, index, obj.ObjectVnum, errors);
                        CheckRoom(world, area, index, obj.RoomVnum, errors);
                        break;

                    case PutReset put:
                        CheckObject(world, area, index, put.ObjectVnum, errors);
                        CheckObject(world, area, index, put.ContainerVnum, errors);
                        break;

                    case GiveReset give:
                        CheckObject(world, area, index, give.ObjectVnum, errors);
                        break;

                    case EquipReset equip:
                        CheckObject(world, area, index, equip.ObjectVnum, errors);
                        break;

                    case DoorReset door:
                        CheckRoom(world, area, index, door.RoomVnum, errors);
                        break;

                    case RandomizeReset randomize:
                        CheckRoom(world, area, index, randomize.RoomVnum, errors);
                        break;
                }
            }

            foreach (var shop in area.Shops)
            {
                if (!world.Mobiles.ContainsKey(shop.KeeperVnum))
                {
                    errors.Add(
                        $"Area '{area.Name}': shop keeper {shop.KeeperVnum} is not a known mobile.");
                }
            }

            foreach (var special in area.Specials)
            {
                if (!world.Mobiles.ContainsKey(special.MobileVnum))
                {
                    errors.Add(
                        $"Area '{area.Name}': special procedure {special.Procedure} points at unknown mobile vnum {special.MobileVnum}.");
                }
            }
        }

        return errors;
    }

    private static void ValidateVnumRanges(World world, ICollection<string> errors)
    {
        var ranges = new List<(AreaData Area, int Low, int High)>();
        foreach (var area in world.Areas)
        {
            var hasEntities = area.Mobiles.Count + area.Objects.Count + area.Rooms.Count > 0;
            var hasRange = area.LowVnum > 0 && area.HighVnum > 0;
            if (hasEntities && !hasRange)
            {
                errors.Add($"Area '{area.Name}' has prototypes but no positive vnum range.");
            }

            if (!hasRange)
            {
                continue;
            }

            ranges.Add((area, area.LowVnum, area.HighVnum));
            foreach (var mobile in area.Mobiles)
            {
                CheckRange(area, "mobile", mobile.Vnum, errors);
            }

            foreach (var obj in area.Objects)
            {
                CheckRange(area, "object", obj.Vnum, errors);
            }

            foreach (var room in area.Rooms)
            {
                CheckRange(area, "room", room.Vnum, errors);
            }
        }

        var ordered = ranges.OrderBy(range => range.Low).ThenBy(range => range.High).ToArray();
        if (ordered.Length == 0)
        {
            return;
        }

        var highest = ordered[0];
        for (var index = 1; index < ordered.Length; index++)
        {
            var current = ordered[index];
            if (current.Low <= highest.High)
            {
                errors.Add(
                    $"Area '{current.Area.Name}' vnum range {current.Low}-{current.High} overlaps " +
                    $"area '{highest.Area.Name}' range {highest.Low}-{highest.High}.");
            }

            if (current.High > highest.High)
            {
                highest = current;
            }
        }
    }

    private static void CheckRange(AreaData area, string kind, int vnum, ICollection<string> errors)
    {
        if (vnum < area.LowVnum || vnum > area.HighVnum)
        {
            errors.Add(
                $"Area '{area.Name}': {kind} {vnum} is outside its vnum range {area.LowVnum}-{area.HighVnum}.");
        }
    }

    private static void CheckMobile(
        World world,
        AreaData area,
        int resetIndex,
        int vnum,
        ICollection<string> errors)
    {
        if (!world.Mobiles.ContainsKey(vnum))
        {
            errors.Add($"Area '{area.Name}': reset {resetIndex} points at unknown mobile vnum {vnum}.");
        }
    }

    private static void CheckObject(
        World world,
        AreaData area,
        int resetIndex,
        int vnum,
        ICollection<string> errors)
    {
        if (!world.ObjectPrototypes.ContainsKey(vnum))
        {
            errors.Add($"Area '{area.Name}': reset {resetIndex} points at unknown object vnum {vnum}.");
        }
    }

    private static void CheckRoom(
        World world,
        AreaData area,
        int resetIndex,
        int vnum,
        ICollection<string> errors)
    {
        if (!world.RoomPrototypes.ContainsKey(vnum))
        {
            errors.Add($"Area '{area.Name}': reset {resetIndex} points at unknown room vnum {vnum}.");
        }
    }
}
