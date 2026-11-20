using VibeROM.Common.Domain;
using VibeROM.Common.Randomness;

namespace VibeROM.Common.Serialization;

public sealed record AreaInventory(
    string Name,
    string? LegacyFile,
    int Helps,
    int Mobiles,
    int Objects,
    int Rooms,
    int Resets,
    int Shops,
    int Specials,
    int Socials);

public sealed record WorldInventory(
    int AreaCount,
    int Helps,
    int Mobiles,
    int Objects,
    int Rooms,
    int Resets,
    int Shops,
    int Specials,
    int Socials,
    IReadOnlyList<AreaInventory> Areas)
{
    public string Summary =>
        $"{AreaCount} areas, {Mobiles} mobiles, {Objects} objects, {Rooms} rooms, " +
        $"{Resets} resets, {Shops} shops, {Specials} specials, {Helps} helps, {Socials} socials";

    public static WorldInventory From(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var areas = world.Areas
            .Select(area => new AreaInventory(
                area.Name,
                area.LegacyFile,
                area.Helps.Count,
                area.Mobiles.Count,
                area.Objects.Count,
                area.Rooms.Count,
                area.Resets.Count,
                area.Shops.Count,
                area.Specials.Count,
                area.Socials.Count))
            .ToArray();

        return new WorldInventory(
            areas.Length,
            areas.Sum(area => area.Helps),
            areas.Sum(area => area.Mobiles),
            areas.Sum(area => area.Objects),
            areas.Sum(area => area.Rooms),
            areas.Sum(area => area.Resets),
            areas.Sum(area => area.Shops),
            areas.Sum(area => area.Specials),
            areas.Sum(area => area.Socials),
            areas);
    }
}

public sealed record WorldBootstrapResult(
    World World,
    WorldInventory Inventory,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Loads the committed JSON corpus, validates it, applies every area reset and
/// returns the world plus a complete per-area inventory.
/// </summary>
public static class WorldBootstrap
{
    public static WorldBootstrapResult Load(
        string areasDirectory,
        string schemaDirectory,
        DiceRoller? roller = null)
    {
        var loaded = WorldLoader.Load(areasDirectory, schemaDirectory);
        var engine = new ResetEngine(roller ?? new DiceRoller(Random.Shared));
        engine.ResetWorld(loaded.World);

        var errors = loaded.World.Validate();
        if (errors.Count > 0)
        {
            throw new WorldDataException(errors);
        }

        return new WorldBootstrapResult(
            loaded.World,
            WorldInventory.From(loaded.World),
            loaded.Warnings);
    }
}
