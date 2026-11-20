using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Domain;

public sealed class CharacterCreationTests
{
    [Theory]
    [InlineData(Race.Human, PlayerClass.Mage, 13, 13, 16, 13, 13, 1000, "the Apprentice of Magic")]
    [InlineData(Race.Human, PlayerClass.Cleric, 13, 13, 13, 16, 13, 1000, "the Believer")]
    [InlineData(Race.Human, PlayerClass.Thief, 13, 16, 13, 13, 13, 1000, "the Pilferer")]
    [InlineData(Race.Human, PlayerClass.Warrior, 16, 13, 13, 13, 13, 1000, "the Swordpupil")]
    [InlineData(Race.Elf, PlayerClass.Mage, 12, 15, 17, 13, 11, 1000, "the Apprentice of Magic")]
    [InlineData(Race.Elf, PlayerClass.Cleric, 12, 15, 14, 16, 11, 1000, "the Believer")]
    [InlineData(Race.Elf, PlayerClass.Thief, 12, 18, 14, 13, 11, 1000, "the Pilferer")]
    [InlineData(Race.Elf, PlayerClass.Warrior, 15, 15, 14, 13, 11, 1000, "the Swordpupil")]
    [InlineData(Race.Dwarf, PlayerClass.Mage, 14, 10, 15, 14, 15, 1000, "the Apprentice of Magic")]
    [InlineData(Race.Dwarf, PlayerClass.Cleric, 14, 10, 12, 17, 15, 1000, "the Believer")]
    [InlineData(Race.Dwarf, PlayerClass.Thief, 14, 13, 12, 14, 15, 1000, "the Pilferer")]
    [InlineData(Race.Dwarf, PlayerClass.Warrior, 17, 10, 12, 14, 15, 1000, "the Swordpupil")]
    [InlineData(Race.Giant, PlayerClass.Mage, 16, 11, 14, 13, 14, 2000, "the Apprentice of Magic")]
    [InlineData(Race.Giant, PlayerClass.Cleric, 16, 11, 11, 16, 14, 1000, "the Believer")]
    [InlineData(Race.Giant, PlayerClass.Thief, 16, 14, 11, 13, 14, 1000, "the Pilferer")]
    [InlineData(Race.Giant, PlayerClass.Warrior, 19, 11, 11, 13, 14, 1000, "the Swordpupil")]
    public void Defaults_MatchRomForEveryRaceAndClass(
        Race race,
        PlayerClass playerClass,
        int strength,
        int dexterity,
        int intelligence,
        int wisdom,
        int constitution,
        int experience,
        string title)
    {
        var (_, character) = Create(race, playerClass);

        var expected = new[] { strength, dexterity, intelligence, wisdom, constitution };
        Assert.Equal(expected, Enumerable.Range(0, 5).Select(character.GetPermanentStat));
        Assert.Equal(expected, Enumerable.Range(0, 5).Select(character.GetModifiedStat));
        Assert.Equal(race, character.Race);
        Assert.Equal(playerClass, character.Class);
        Assert.Equal(1, character.Level);
        Assert.Equal(experience, character.Experience);
        Assert.Equal(20, character.Hit);
        Assert.Equal(20, character.MaxHit);
        Assert.Equal(100, character.Mana);
        Assert.Equal(100, character.MaxMana);
        Assert.Equal(100, character.Move);
        Assert.Equal(100, character.MaxMove);
        Assert.Equal(new ArmorValues(100, 100, 100, 100), character.Armor);
        Assert.Equal(3, character.Train);
        Assert.Equal(5, character.Practice);
        Assert.Equal(title, character.Player!.Title);
        Assert.Equal(
            RomTables.PcRaceDefinitions.Single(definition => definition.Race == race).Points,
            character.Player.Points);
        Assert.Equal(CharacterCreation.SchoolRoomVnum, character.InRoom!.Prototype.Vnum);
    }

    [Theory]
    [InlineData(Race.Human, PlayerClass.Mage)]
    [InlineData(Race.Human, PlayerClass.Cleric)]
    [InlineData(Race.Human, PlayerClass.Thief)]
    [InlineData(Race.Human, PlayerClass.Warrior)]
    [InlineData(Race.Elf, PlayerClass.Mage)]
    [InlineData(Race.Elf, PlayerClass.Cleric)]
    [InlineData(Race.Elf, PlayerClass.Thief)]
    [InlineData(Race.Elf, PlayerClass.Warrior)]
    [InlineData(Race.Dwarf, PlayerClass.Mage)]
    [InlineData(Race.Dwarf, PlayerClass.Cleric)]
    [InlineData(Race.Dwarf, PlayerClass.Thief)]
    [InlineData(Race.Dwarf, PlayerClass.Warrior)]
    [InlineData(Race.Giant, PlayerClass.Mage)]
    [InlineData(Race.Giant, PlayerClass.Cleric)]
    [InlineData(Race.Giant, PlayerClass.Thief)]
    [InlineData(Race.Giant, PlayerClass.Warrior)]
    public void DefaultPackage_GrantsGroupsRecallAndWeapon(Race race, PlayerClass playerClass)
    {
        var (_, character) = Create(race, playerClass);

        var classDefinition = RomTables.ClassDefinitions.Single(
            definition => definition.Class == playerClass);
        Assert.Contains("rom basics", character.Player!.KnownGroups);
        Assert.Contains(classDefinition.BaseGroup, character.Player.KnownGroups);
        Assert.Contains(classDefinition.DefaultGroup, character.Player.KnownGroups);
        Assert.Equal(CharacterCreation.RecallProficiency, character.Player.GetSkill("recall"));

        var choices = CharacterCreation.AvailableWeapons(character);
        Assert.NotEmpty(choices);
        Assert.Equal(
            CharacterCreation.WeaponProficiency,
            character.Player.GetSkill(choices[0].SkillName));
    }

    [Fact]
    public void RaceAffectsAndBonusSkills_MatchRom()
    {
        var (_, human) = Create(Race.Human, PlayerClass.Warrior);
        Assert.Equal(0, human.AffectedBy);
        Assert.Equal(0, human.Resistances);
        Assert.Equal(0, human.Vulnerabilities);
        Assert.Equal(Size.Medium, human.Size);

        var (_, elf) = Create(Race.Elf, PlayerClass.Mage);
        Assert.True((elf.AffectedBy & RomFlags.Affect.Get("Infrared").Bit) != 0);
        Assert.True((elf.Resistances & RomFlags.Immunity.Get("Charm").Bit) != 0);
        Assert.True((elf.Vulnerabilities & RomFlags.Immunity.Get("Iron").Bit) != 0);
        Assert.Equal(Size.Small, elf.Size);
        Assert.Equal(1, elf.Player!.GetSkill("sneak"));
        Assert.Equal(1, elf.Player.GetSkill("hide"));

        var (_, dwarf) = Create(Race.Dwarf, PlayerClass.Cleric);
        Assert.True((dwarf.Resistances & RomFlags.Immunity.Get("Poison").Bit) != 0);
        Assert.True((dwarf.Resistances & RomFlags.Immunity.Get("Disease").Bit) != 0);
        Assert.True((dwarf.Vulnerabilities & RomFlags.Immunity.Get("Drowning").Bit) != 0);
        Assert.Equal(1, dwarf.Player!.GetSkill("berserk"));

        var (_, giant) = Create(Race.Giant, PlayerClass.Warrior);
        Assert.True((giant.Resistances & RomFlags.Immunity.Get("Fire").Bit) != 0);
        Assert.True((giant.Resistances & RomFlags.Immunity.Get("Cold").Bit) != 0);
        Assert.True((giant.Vulnerabilities & RomFlags.Immunity.Get("Mental").Bit) != 0);
        Assert.True((giant.Vulnerabilities & RomFlags.Immunity.Get("Lightning").Bit) != 0);
        Assert.Equal(Size.Large, giant.Size);
        Assert.Equal(1, giant.Player!.GetSkill("bash"));
        Assert.Equal(1, giant.Player.GetSkill("fast healing"));
    }

    [Fact]
    public void Outfit_EquipsBannerVestWeaponAndShield()
    {
        var (_, warrior) = Create(Race.Human, PlayerClass.Warrior, weapon: "sword");

        Assert.Equal(CharacterCreation.SchoolBannerVnum, warrior.Equipment[WearSlot.Light].Vnum);
        Assert.Equal(CharacterCreation.SchoolVestVnum, warrior.Equipment[WearSlot.Body].Vnum);
        Assert.Equal(3702, warrior.Equipment[WearSlot.Wield].Vnum);
        Assert.Equal(CharacterCreation.SchoolShieldVnum, warrior.Equipment[WearSlot.Shield].Vnum);
        Assert.Contains(warrior.Inventory, item => item.Vnum == CharacterCreation.SchoolMapVnum);
        Assert.All(warrior.Equipment.Values, item => Assert.Equal(0, item.CostCopper));
        Assert.Equal(
            50,
            warrior.Inventory.Single(item => item.Vnum == CharacterCreation.SchoolMapVnum).CostCopper);
        Assert.Equal(CharacterCreation.WeaponProficiency, warrior.Player!.GetSkill("sword"));
    }

    [Fact]
    public void Outfit_SkipsShieldForTwoHandedWeapon()
    {
        var (_, warrior) = Create(Race.Human, PlayerClass.Warrior, weapon: "staff");

        Assert.Equal(3718, warrior.Equipment[WearSlot.Wield].Vnum);
        Assert.False(warrior.Equipment.ContainsKey(WearSlot.Shield));
    }

    [Fact]
    public void FemaleCharacterUsesFeminineTitle()
    {
        var (_, thief) = Create(Race.Human, PlayerClass.Thief, sex: Sex.Female);

        Assert.Equal("the Pilferess", thief.Player!.Title);
        Assert.Equal(Sex.Female, thief.Player.TrueSex);
    }

    [Theory]
    [InlineData(Race.Human, PlayerClass.Warrior, 0, 1000)]
    [InlineData(Race.Human, PlayerClass.Warrior, 40, 1000)]
    [InlineData(Race.Human, PlayerClass.Warrior, 50, 1500)]
    [InlineData(Race.Human, PlayerClass.Warrior, 60, 2000)]
    [InlineData(Race.Elf, PlayerClass.Cleric, 60, 2500)]
    [InlineData(Race.Dwarf, PlayerClass.Mage, 60, 3000)]
    [InlineData(Race.Giant, PlayerClass.Warrior, 60, 2100)]
    public void ExpPerLevel_MatchesRomFormula(
        Race race,
        PlayerClass playerClass,
        int points,
        int expected)
    {
        var (_, character) = Create(race, playerClass);

        Assert.Equal(expected, CharacterCreation.ExpPerLevel(character, points));
    }

    [Fact]
    public void Lookups_UseRomPrefixesAndRejectNonPcRaces()
    {
        Assert.True(CharacterCreation.TryLookupRace("e", out var elf));
        Assert.Equal(Race.Elf, elf);
        Assert.False(CharacterCreation.TryLookupRace("unique", out _));
        Assert.False(CharacterCreation.TryLookupRace("unicorn", out _));

        Assert.True(CharacterCreation.TryLookupClass("w", out var warrior));
        Assert.Equal(PlayerClass.Warrior, warrior);
        Assert.False(CharacterCreation.TryLookupClass("paladin", out _));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CharacterCreation.FindPcRace(Race.Unique));
    }

    [Fact]
    public void TryChooseWeapon_RejectsUnlearnedWeapons()
    {
        var world = CreateWorld();
        var player = new PlayerData();
        var mage = world.CreatePlayer(player, "Tester", Race.Human, PlayerClass.Mage, Sex.Neutral);
        CharacterCreation.ApplyRace(mage);
        CharacterCreation.ApplyDefaultPackage(mage);

        Assert.False(CharacterCreation.TryChooseWeapon(mage, "staff", out _));
        Assert.True(CharacterCreation.TryChooseWeapon(mage, "dagger", out var dagger));
        Assert.Equal("dagger", dagger.Name);
        Assert.Equal(CharacterCreation.WeaponProficiency, mage.Player!.GetSkill("dagger"));
    }

    private static (World World, Character Character) Create(
        Race race,
        PlayerClass playerClass,
        Sex sex = Sex.Male,
        string? weapon = null)
    {
        var world = CreateWorld();
        var player = new PlayerData();
        var character = world.CreatePlayer(player, "Tester", race, playerClass, sex);
        CharacterCreation.ApplyRace(character);
        CharacterCreation.ApplyDefaultPackage(character);

        var choices = CharacterCreation.AvailableWeapons(character);
        var selected = weapon ?? choices[0].Name;
        Assert.True(CharacterCreation.TryChooseWeapon(character, selected, out _));
        CharacterCreation.Complete(world, character, CharacterCreation.SchoolRoomVnum);
        return (world, character);
    }

    private static World CreateWorld()
    {
        var room = new RoomPrototype(
            CharacterCreation.SchoolRoomVnum,
            "The Mud School",
            "A dusty school room.",
            0,
            Sector.Inside,
            100,
            100,
            0,
            new Dictionary<Direction, ExitPrototype>(),
            []);
        var area = new AreaData(
            "School",
            "tests",
            3000,
            3799,
            9,
            "tests",
            "school.are",
            [],
            [],
            CreateObjects(),
            [room],
            [],
            [],
            [],
            []);
        return new World([area]);
    }

    private static IReadOnlyList<ObjectPrototype> CreateObjects() =>
    [
        Weapon(3700, WeaponClass.Mace, "mace"),
        Weapon(3701, WeaponClass.Dagger, "dagger"),
        Weapon(3702, WeaponClass.Sword, "sword"),
        Armor(3703, WearSlot.Body, "vest"),
        Armor(3704, WearSlot.Shield, "shield"),
        Light(3716, "banner"),
        Weapon(3717, WeaponClass.Spear, "spear"),
        Weapon(3718, WeaponClass.Spear, "staff", RomFlags.WeaponSpecial.Get("TwoHands").Bit),
        Weapon(3719, WeaponClass.Axe, "axe"),
        Weapon(3720, WeaponClass.Flail, "flail"),
        Weapon(3721, WeaponClass.Whip, "whip"),
        Weapon(3722, WeaponClass.Polearm, "polearm", RomFlags.WeaponSpecial.Get("TwoHands").Bit),
        Map(CharacterCreation.SchoolMapVnum)
    ];

    private static ObjectPrototype Weapon(
        int vnum,
        WeaponClass weaponClass,
        string name,
        int specialFlags = 0) =>
        new(
            vnum,
            name,
            $"a {name}",
            $"A {name} lies here.",
            "steel",
            ItemType.Weapon,
            0,
            RomFlags.Wear.GetMask(["Take", "Wield"]),
            1,
            30,
            100,
            100,
            new WeaponData(weaponClass, 1, 6, "Slash", specialFlags),
            [],
            []);

    private static ObjectPrototype Armor(int vnum, WearSlot slot, string name) =>
        new(
            vnum,
            name,
            $"a {name}",
            $"A {name} lies here.",
            "cloth",
            ItemType.Armor,
            0,
            RomFlags.Wear.GetMask(["Take", slot.ToString()]),
            1,
            10,
            50,
            100,
            new ArmorData(new ArmorValues(1, 1, 1, 1)),
            [],
            []);

    private static ObjectPrototype Light(int vnum, string name) =>
        new(
            vnum,
            name,
            $"a {name}",
            $"A {name} lies here.",
            "cloth",
            ItemType.Light,
            0,
            RomFlags.Wear.GetMask(["Take"]),
            1,
            10,
            50,
            100,
            new LightData(0),
            [],
            []);

    private static ObjectPrototype Map(int vnum) =>
        new(
            vnum,
            "map",
            "a map",
            "A map lies here.",
            "paper",
            ItemType.Map,
            0,
            RomFlags.Wear.GetMask(["Take"]),
            1,
            10,
            50,
            100,
            new MapData(1),
            [],
            []);
}
