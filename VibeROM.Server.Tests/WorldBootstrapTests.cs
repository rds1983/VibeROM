using VibeROM.Common.Randomness;
using VibeROM.Common.Serialization;

namespace VibeROM.Server.Tests;

public sealed class WorldBootstrapTests
{
    private static readonly string AreasDirectory = Path.Combine(AppContext.BaseDirectory, "areas");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void LoadsAndResetsTheCompleteCorpus()
    {
        var result = WorldBootstrap.Load(
            AreasDirectory,
            SchemaDirectory,
            new DiceRoller(new Random(9876)));

        Assert.Empty(result.World.Validate());
        Assert.Equal(52, result.Inventory.AreaCount);
        Assert.Equal(986, result.Inventory.Mobiles);
        Assert.Equal(1265, result.Inventory.Objects);
        Assert.Equal(3126, result.Inventory.Rooms);
        Assert.Equal(5233, result.Inventory.Resets);
        Assert.Equal(62, result.Inventory.Shops);
        Assert.Equal(411, result.Inventory.Specials);
        Assert.Equal(248, result.Inventory.Helps);
        Assert.Equal(244, result.Inventory.Socials);
        Assert.Equal(result.Inventory.AreaCount, result.Inventory.Areas.Count);
        Assert.Equal(result.World.Areas.Count, result.Inventory.Areas.Count);

        Assert.True(result.World.Characters.Count > 0, "Reset should create mobiles.");
        Assert.True(result.World.GameObjects.Count > 0, "Reset should create objects.");
        Assert.Contains("52 areas", result.Inventory.Summary, StringComparison.Ordinal);
        Assert.Contains("986 mobiles", result.Inventory.Summary, StringComparison.Ordinal);
    }
}
