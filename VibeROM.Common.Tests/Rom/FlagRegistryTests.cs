using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Rom;

public sealed class FlagRegistryTests
{
    [Fact]
    public void AllRegistries_RoundTripPersistedBits()
    {
        foreach (var registry in RomFlags.All)
        {
            var mask = registry.PersistedDefinitions.Aggregate(0, (current, definition) => current | definition.Bit);
            var names = registry.GetNames(mask);

            Assert.Equal(registry.PersistedNames, names);
            Assert.Equal(mask, registry.GetMask(names));
        }
    }

    [Fact]
    public void AllRegistries_HaveNoNullOrEmptyNames()
    {
        foreach (var registry in RomFlags.All)
        {
            Assert.NotEmpty(registry.PersistedNames);
            Assert.All(registry.Definitions, definition => Assert.False(string.IsNullOrWhiteSpace(definition.Name)));
        }
    }

    [Theory]
    [InlineData("Aggressive", 1 << 5)]
    [InlineData("Undead", 1 << 14)]
    [InlineData("IsChanger", 1 << 29)]
    public void ActFlags_HaveExpectedBits(string name, int expected)
    {
        Assert.Equal(expected, RomFlags.Act.Get(name).Bit);
    }

    [Theory]
    [InlineData("AssistRace", 1 << 17)]
    [InlineData("AssistVnum", 1 << 20)]
    public void OffensiveFlags_IncludeAssistBits(string name, int expected)
    {
        Assert.Equal(expected, RomFlags.Offensive.Get(name).Bit);
    }

    [Fact]
    public void ContainerFlags_UseNumericMasks()
    {
        Assert.Equal(8, RomFlags.Container.Get("Locked").Bit);
    }

    [Fact]
    public void DerivedFlags_AreNotPersisted()
    {
        Assert.False(RomFlags.Act.Get("IsNpc").Persisted);
        Assert.False(RomFlags.Act.Get("Pet").Persisted);
        Assert.False(RomFlags.PlayerAct.Get("IsNpc").Persisted);
    }

    [Fact]
    public void GetMask_IsCaseInsensitive()
    {
        Assert.Equal(1 << 5, RomFlags.Act.GetMask(["AGGRESSIVE"]));
    }

    [Fact]
    public void GetNames_ReturnsBitOrder()
    {
        var mask = RomFlags.Act.GetMask(["Undead", "Sentinel", "Aggressive"]);

        Assert.Equal(["Sentinel", "Aggressive", "Undead"], RomFlags.Act.GetNames(mask));
    }

    [Fact]
    public void UnknownName_Throws()
    {
        Assert.Throws<ArgumentException>(() => RomFlags.Act.Get("NotAFlag"));
    }

    [Fact]
    public void DuplicateNames_AreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FlagRegistry("test", [new(1, "One"), new(2, "one")]));
    }

    [Fact]
    public void DuplicateBits_AreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FlagRegistry("test", [new(1, "One"), new(1, "Two")]));
    }

    [Fact]
    public void NonPowerOfTwoBits_AreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FlagRegistry("test", [new(3, "Bad")]));
    }
}
