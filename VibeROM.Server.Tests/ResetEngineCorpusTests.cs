using VibeROM.Common.Domain;
using VibeROM.Common.Randomness;
using VibeROM.Common.Serialization;

namespace VibeROM.Server.Tests;

public sealed class ResetEngineCorpusTests
{
    private static readonly string AreasDirectory = Path.Combine(AppContext.BaseDirectory, "areas");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Theory]
    [InlineData("Midgaard")]
    [InlineData("Holy Grove")]
    [InlineData("Troll Den")]
    [InlineData("Astral Plane")]
    public void RealAreaRepopsDeterministically(string areaName)
    {
        var world = WorldLoader.Load(AreasDirectory, SchemaDirectory).World;
        var area = world.Areas.Single(candidate => candidate.Name == areaName);

        var firstEngine = new ResetEngine(new DiceRoller(new Random(4242)));
        Assert.True(firstEngine.ResetArea(world, area));
        var first = Snapshot(world, area);
        Assert.NotEmpty(first);
        Assert.Empty(world.Validate());

        var secondEngine = new ResetEngine(new DiceRoller(new Random(4242)));
        Assert.True(secondEngine.ResetArea(world, area));
        var second = Snapshot(world, area);

        Assert.Equal(first, second);
        Assert.Empty(world.Validate());
    }

    private static string Snapshot(World world, AreaData area)
    {
        var lines = new List<string>();
        foreach (var roomPrototype in area.Rooms.OrderBy(room => room.Vnum))
        {
            var room = world.GetRoom(roomPrototype.Vnum);
            lines.Add($"room {room.Vnum}");
            foreach (var exit in room.Exits.OrderBy(exit => exit.Key))
            {
                lines.Add(
                    $" exit {exit.Key}->{exit.Value.ToRoomVnum} flags={exit.Value.Flags} key={exit.Value.KeyVnum}");
            }

            foreach (var person in room.People.OrderBy(person => person.Name, StringComparer.Ordinal))
            {
                lines.Add(
                    $" person {person.Name} vnum={person.Prototype?.Vnum} level={person.Level} " +
                    $"hit={person.Hit} mana={person.Mana} move={person.Move}");
                foreach (var obj in person.Inventory.OrderBy(obj => obj.Vnum))
                {
                    lines.Add(
                        $"  carry {obj.Vnum} level={obj.Level} wear={obj.WearLocation} " +
                        $"flags={obj.ExtraFlags} cost={obj.CostCopper}");
                }
            }

            foreach (var obj in room.Contents.OrderBy(obj => obj.Vnum))
            {
                AppendObject(lines, obj, 1);
            }
        }

        return string.Join('\n', lines);
    }

    private static void AppendObject(ICollection<string> lines, GameObject obj, int depth)
    {
        var indent = new string(' ', depth * 2);
        lines.Add(
            $"{indent}object {obj.Vnum} level={obj.Level} flags={obj.ExtraFlags} cost={obj.CostCopper}");
        foreach (var child in obj.Contents.OrderBy(child => child.Vnum))
        {
            AppendObject(lines, child, depth + 1);
        }
    }
}
