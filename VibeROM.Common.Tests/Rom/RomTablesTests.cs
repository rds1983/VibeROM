using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Rom;

public sealed class RomTablesTests
{
    [Fact]
    public void Validate_ReturnsNoErrors()
    {
        var errors = RomTables.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateOrThrow_DoesNotThrow()
    {
        RomTables.ValidateOrThrow();
    }

    [Fact]
    public void CoreTableCounts_MatchRom()
    {
        Assert.Equal(29, RomTables.RaceDefinitions.Count);
        Assert.Equal(4, RomTables.ClassDefinitions.Count);
        Assert.Equal(4, RomTables.PcRaceDefinitions.Count);
        Assert.Equal(8, RomTables.WeaponDefinitions.Count);
        Assert.Equal(36, RomTables.LiquidDefinitions.Count);
        Assert.Equal(40, RomTables.AttackTypeDefinitions.Count);
        Assert.Equal(20, RomTables.WiznetDefinitions.Count);
        Assert.Equal(22, RomTables.SpecialDefinitions.Count);
        Assert.Equal(135, RomTables.Skills.Count);
        Assert.Equal(27, RomTables.Groups.Count);
    }

    [Fact]
    public void RaceDefinitions_HaveExpectedFlags()
    {
        var human = RomTables.RaceDefinitions.Single(race => race.Race == Race.Human);
        var elf = RomTables.RaceDefinitions.Single(race => race.Race == Race.Elf);
        var dwarf = RomTables.RaceDefinitions.Single(race => race.Race == Race.Dwarf);
        var giant = RomTables.RaceDefinitions.Single(race => race.Race == Race.Giant);

        Assert.Contains("Biped", human.Form);
        Assert.Contains("Mammal", human.Form);
        Assert.Contains("Head", human.Parts);

        Assert.Contains("Infrared", elf.AffectedBy);
        Assert.Contains("Charm", elf.Resistances);
        Assert.Contains("Iron", elf.Vulnerabilities);

        Assert.Contains("Poison", dwarf.Resistances);
        Assert.Contains("Disease", dwarf.Resistances);
        Assert.Contains("Drowning", dwarf.Vulnerabilities);

        Assert.Contains("Fire", giant.Resistances);
        Assert.Contains("Cold", giant.Resistances);
        Assert.Contains("Mental", giant.Vulnerabilities);
        Assert.Contains("Lightning", giant.Vulnerabilities);
    }

    [Fact]
    public void SkillTable_SpotCheck()
    {
        var acidBlast = RomTables.Skills.Single(skill => skill.Name == "acid blast");

        Assert.Equal([28, 53, 35, 32], acidBlast.Levels);
        Assert.Equal([1, 1, 2, 2], acidBlast.Ratings);
        Assert.Equal("spell_acid_blast", acidBlast.SpellFunction);
        Assert.Equal(SkillTarget.CharOffensive, acidBlast.Target);
        Assert.Equal(Position.Fighting, acidBlast.MinimumPosition);
        Assert.Equal(20, acidBlast.MinMana);
        Assert.Equal(12, acidBlast.Beats);
    }

    [Fact]
    public void GroupTable_SpotCheck()
    {
        var basics = RomTables.Groups.Single(group => group.Name == "rom basics");

        Assert.Contains("recall", basics.Spells);
        Assert.Equal([0, 0, 0, 0], basics.Ratings);
    }

    [Fact]
    public void Titles_MatchClassLevelAndSex()
    {
        Assert.Equal("Man", RomTables.GetTitle(PlayerClass.Mage, 0, Sex.Male));
        Assert.Equal("Woman", RomTables.GetTitle(PlayerClass.Mage, 0, Sex.Female));
        Assert.Equal("Abjurer", RomTables.GetTitle(PlayerClass.Mage, 10, Sex.Male));
        Assert.Equal("Abjuress", RomTables.GetTitle(PlayerClass.Mage, 10, Sex.Female));
    }

    [Theory]
    [InlineData(ItemType.Weapon, 5)]
    [InlineData(ItemType.Light, 1)]
    [InlineData(ItemType.Jukebox, 34)]
    public void ItemTypes_HaveExpectedValues(ItemType itemType, int expected)
    {
        Assert.Equal(expected, RomTables.ItemTypes.Get(itemType.ToString()).Value);
    }

    [Fact]
    public void ValueTables_SpotChecks()
    {
        Assert.Equal(3, RomTables.Classes.Get("Warrior").Value);
        Assert.Equal(3, RomTables.AttackTypes.Get("Slash").Value);
        Assert.Equal(20, RomTables.AttackTypes.Get("DivinePower").Value);
        Assert.Equal(25, RomTables.Liquids.Get("OrangeJuice").Value);
        Assert.Equal(13, RomTables.Specials.Get("Guard").Value);
        Assert.Equal(3, RomTables.DamageTypes.Get("Slash").Value);
        Assert.Equal(1, RomTables.PcRaces.Get("Human").Value);
        Assert.DoesNotContain("Unused", RomTables.Sectors.Names);
    }

    [Fact]
    public void Classes_HaveExpectedPrimeAttributes()
    {
        Assert.Equal("Intelligence", RomTables.ClassDefinitions.Single(cls => cls.Class == PlayerClass.Mage).PrimeAttribute);
        Assert.Equal("Wisdom", RomTables.ClassDefinitions.Single(cls => cls.Class == PlayerClass.Cleric).PrimeAttribute);
        Assert.Equal("Dexterity", RomTables.ClassDefinitions.Single(cls => cls.Class == PlayerClass.Thief).PrimeAttribute);
        Assert.Equal("Strength", RomTables.ClassDefinitions.Single(cls => cls.Class == PlayerClass.Warrior).PrimeAttribute);
    }

    [Fact]
    public void FindSkill_UsesRomPrefixLookup()
    {
        Assert.Equal("invisibility", RomTables.FindSkill("invis")?.Name);
        Assert.Equal("acid blast", RomTables.FindSkill("acid b")?.Name);
        Assert.Null(RomTables.FindSkill("not-a-skill"));
    }

    [Fact]
    public void FindGroup_UsesRomPrefixLookup()
    {
        Assert.Equal("rom basics", RomTables.FindGroup("rom b")?.Name);
        Assert.Null(RomTables.FindGroup("not-a-group"));
    }

    [Fact]
    public void WiznetDefinition_BitsMatchFlags()
    {
        foreach (var definition in RomTables.WiznetDefinitions)
        {
            Assert.True(RomFlags.Wiznet.TryGet(definition.Name, out var flag));
            Assert.Equal(definition.Bit, flag.Bit);
        }
    }

    [Fact]
    public void ValueTable_RejectsDuplicates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ValueTable("test", [new(1, "One"), new(2, "one")]));

        Assert.Throws<InvalidOperationException>(() =>
            new ValueTable("test", [new(1, "One"), new(1, "Two")]));
    }
}
