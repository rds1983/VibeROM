using VibeROM.AreaImporter.Legacy;
using VibeROM.Common.Domain;

namespace VibeROM.AreaImporter;

/// <summary>
/// Repairs legacy exit keys that point at a vnum which is not an object (the
/// source data sometimes uses room vnums or a missing object). The key is
/// cleared with a recorded repair entry so the import stays deterministic.
/// </summary>
public static class AreaRepairer
{
    public static (LegacyAreaParseResult Result, IReadOnlyList<ImportMessage> Repairs)
        RepairDanglingExitKeys(LegacyAreaParseResult result, IReadOnlySet<int> objectVnums)
    {
        var repairedContexts = new HashSet<string>(StringComparer.Ordinal);
        var repairs = new List<ImportMessage>();
        var rooms = new List<RoomPrototype>(result.Area.Rooms.Count);
        var changed = false;

        foreach (var room in result.Area.Rooms)
        {
            var exits = new Dictionary<Direction, ExitPrototype>();
            var roomChanged = false;
            foreach (var (direction, exit) in room.Exits)
            {
                if (exit.KeyVnum > 0 && !objectVnums.Contains(exit.KeyVnum))
                {
                    var context = $"room {room.Vnum} {direction.ToString().ToLowerInvariant()} exit key";
                    var reference = result.References.FirstOrDefault(candidate =>
                        candidate.Target == LegacyReferenceTarget.Object
                        && candidate.Vnum == exit.KeyVnum
                        && candidate.Context == context);
                    repairs.Add(new ImportMessage(
                        reference is null
                            ? result.Area.LegacyFile ?? result.Area.Name
                            : Path.GetFileName(reference.SourceFile),
                        reference?.Position.Line ?? 0,
                        reference?.Position.Column ?? 0,
                        $"Exit key vnum {exit.KeyVnum} is not an object; using no key."));
                    repairedContexts.Add(context);
                    exits[direction] = exit with { KeyVnum = -1 };
                    roomChanged = true;
                    changed = true;
                }
                else
                {
                    exits[direction] = exit;
                }
            }

            rooms.Add(roomChanged ? room with { Exits = exits } : room);
        }

        if (!changed)
        {
            return (result, repairs);
        }

        var references = result.References
            .Where(reference => !repairedContexts.Contains(reference.Context))
            .ToArray();
        return (result with
        {
            Area = result.Area with { Rooms = rooms },
            References = references
        }, repairs);
    }
}
