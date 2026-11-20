using VibeROM.AreaImporter.Legacy;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.AreaImporter.Tests;

public sealed class LegacyAreaParserTests
{
    [Fact]
    public void ParsesModernAreaHeaderAndHelps()
    {
        var result = ParseFixture("modern.are");

        Assert.Empty(result.Warnings);
        Assert.Empty(result.Area.Validate());

        var area = result.Area;
        Assert.Equal("Testland", area.Name);
        Assert.Equal("{ Test } Example", area.Credits);
        Assert.Equal("Tester", area.Builders);
        Assert.Equal(1000, area.LowVnum);
        Assert.Equal(1099, area.HighVnum);
        Assert.Equal(7, area.Security);
        Assert.Equal("modern.are", area.LegacyFile);

        var help = Assert.Single(area.Helps);
        Assert.Equal(-1, help.Level);
        Assert.Equal("TEST", help.Keywords);
        Assert.Equal("A test help entry.\n", help.Text);
    }

    [Fact]
    public void ParsesModernMobileAndRemovesFlags()
    {
        var area = ParseFixture("modern.are").Area;
        var mobile = Assert.Single(area.Mobiles);

        Assert.Equal(1000, mobile.Vnum);
        Assert.Equal("guard test", mobile.Keywords);
        Assert.Equal("the test guard", mobile.ShortDescription);
        Assert.Equal("A test guard stands here.\n", mobile.LongDescription);
        Assert.Equal(Race.Human, mobile.Race);
        Assert.Equal(RomFlags.Act.GetMask(["NoPurge"]), mobile.ActFlags);
        Assert.Equal(RomFlags.Affect.GetMask(["DetectInvis"]), mobile.AffectedBy);
        Assert.Equal(RomFlags.Offensive.GetMask(["AreaAttack", "Berserk"]), mobile.OffensiveFlags);
        Assert.Equal(RomFlags.Immunity.GetMask(["Summon", "Charm", "Magic"]), mobile.Immunities);
        Assert.Equal(500, mobile.Alignment);
        Assert.Equal(10, mobile.Level);
        Assert.Equal(5, mobile.HitRoll);
        Assert.Equal(new Dice(2, 6, 3), mobile.HitDice);
        Assert.Equal(new Dice(1, 4, 1), mobile.ManaDice);
        Assert.Equal(new Dice(1, 8, 2), mobile.Damage.Dice);
        Assert.Equal("Slash", mobile.Damage.AttackType);
        Assert.Equal(new ArmorValues(-20, -20, -20, -20), mobile.Armor);
        Assert.Equal(Position.Standing, mobile.StartPosition);
        Assert.Equal(Position.Standing, mobile.DefaultPosition);
        Assert.Equal(Sex.Male, mobile.Sex);
        Assert.Equal(100, mobile.Gold);
        Assert.Equal(Size.Medium, mobile.Size);
        Assert.Equal("steel", mobile.Material);
        Assert.Equal(
            RomFlags.Form.GetMask(["Edible", "Sentient", "Biped", "Mammal"]),
            mobile.Form);
        Assert.Equal(
            RomFlags.Part.GetMask(["Head", "Arms", "Legs", "Heart", "Brains", "Guts", "Hands", "Feet", "Fingers", "Ear", "Eye"]),
            mobile.Parts);
    }

    [Fact]
    public void ParsesAllItemTypeSpecificData()
    {
        var area = ParseFixture("modern.are").Area;

        Assert.Equal(14, area.Objects.Count);

        AssertWeapon(area.Objects[0]);
        AssertArmor(area.Objects[1]);
        AssertContainer(area.Objects[2]);
        AssertDrink(area.Objects[4]);
        AssertWand(area.Objects[5]);
        AssertScroll(area.Objects[6]);
        AssertLight(area.Objects[7]);
        AssertFood(area.Objects[8]);
        AssertMoney(area.Objects[9]);
        AssertFurniture(area.Objects[10]);
        AssertPortal(area.Objects[11]);
        AssertMap(area.Objects[12]);
        AssertPlainTreasure(area.Objects[13]);
    }

    [Fact]
    public void ParsesRoomsResetsShopsAndSpecials()
    {
        var area = ParseFixture("modern.are").Area;

        Assert.Equal(2, area.Rooms.Count);
        var room = area.Rooms[0];
        Assert.Equal("Test Room", room.Name);
        Assert.Equal(Sector.City, room.Sector);
        Assert.Equal(100, room.HealRate);
        Assert.Equal(100, room.ManaRate);
        var exit = Assert.Single(room.Exits).Value;
        Assert.Equal(Direction.North, exit.Direction);
        Assert.Equal(1001, exit.ToRoomVnum);
        Assert.Equal(-1, exit.KeyVnum);
        Assert.Equal(0, exit.Flags);
        Assert.Equal("North into the void.\n", exit.Description);
        var extra = Assert.Single(room.ExtraDescriptions);
        Assert.Equal("note", extra.Keywords);
        Assert.Equal("A note is pinned here.\n", extra.Description);

        Assert.Collection(
            area.Resets,
            reset => Assert.Equal(new MobileReset(false, 1000, 1000, 1, 2), reset),
            reset => Assert.Equal(new ObjectReset(false, 1002, 1000, 1), reset),
            reset => Assert.Equal(new PutReset(false, 1002, 1002, 1), reset),
            reset => Assert.Equal(new GiveReset(false, 1000, -1), reset),
            reset => Assert.Equal(new EquipReset(false, 1000, WearSlot.Wield, -1), reset),
            reset => Assert.Equal(new DoorReset(false, 1000, Direction.North, DoorState.Closed), reset),
            reset => Assert.Equal(new RandomizeReset(false, 1000, 0), reset));

        var shop = Assert.Single(area.Shops);
        Assert.Equal(1000, shop.KeeperVnum);
        Assert.Equal([ItemType.Weapon, ItemType.Armor, ItemType.Light], shop.BuyTypes);
        Assert.Equal(120, shop.ProfitBuy);
        Assert.Equal(110, shop.ProfitSell);
        Assert.Equal(6, shop.OpenHour);
        Assert.Equal(20, shop.CloseHour);

        var special = Assert.Single(area.Specials);
        Assert.Equal(1000, special.MobileVnum);
        Assert.Equal("Guard", special.Procedure);
    }

    [Fact]
    public void ParsesOldFormatAreaMobileAndObject()
    {
        var result = ParseFixture("oldformat.are");
        var area = result.Area;

        Assert.Empty(result.Warnings);
        Assert.Empty(area.Validate());
        Assert.Equal("Old Format Area", area.Name);
        Assert.Equal("{ 1 2} Legacy Credits", area.Credits);
        Assert.Equal(1000, area.LowVnum);
        Assert.Equal(1099, area.HighVnum);

        var mobile = Assert.Single(area.Mobiles);
        Assert.Equal(Race.Goblin, mobile.Race);
        Assert.Equal(RomFlags.Act.GetMask(["Sentinel", "Warrior"]), mobile.ActFlags);
        Assert.Equal(RomFlags.Form.GetMask(["Edible", "Sentient", "Biped", "Mammal"]), mobile.Form);
        Assert.Equal(new Dice(1, 16, 42), mobile.HitDice);
        Assert.Equal(new Dice(5, 10, 100), mobile.ManaDice);
        Assert.Equal(new Dice(2, 4, 3), mobile.Damage.Dice);
        Assert.Equal("Pound", mobile.Damage.AttackType);
        Assert.Equal(new ArmorValues(69, 69, 69, 85), mobile.Armor);
        Assert.Equal(1, mobile.Gold);
        Assert.Equal(Size.Medium, mobile.Size);
        Assert.Equal("none", mobile.Material);

        var obj = Assert.Single(area.Objects);
        Assert.Equal(ItemType.Weapon, obj.ItemType);
        var weapon = Assert.IsType<WeaponData>(obj.Data);
        Assert.Equal(WeaponClass.Sword, weapon.Class);
        Assert.Equal(2, weapon.DiceCount);
        Assert.Equal(3, weapon.DiceSize);
        Assert.Equal("Hit", weapon.AttackType);
        Assert.Equal(100, obj.CostCopper);
    }

    [Fact]
    public void ParsesSocialsIncludingEarlyTerminatorAndTrailingWhitespace()
    {
        var area = ParseFixture("socials.are").Area;

        Assert.Equal("Socials", area.Name);
        Assert.Equal("socials.are", area.LegacyFile);
        Assert.Equal(2, area.Socials.Count);

        var smile = area.Socials[0];
        Assert.Equal("smile", smile.Name);
        Assert.Equal("You smile happily.", smile.CharNoArgument);
        Assert.Equal("$n smiles happily.", smile.OthersNoArgument);
        Assert.Equal("You smile at $N.", smile.CharFound);
        Assert.Equal("$n smiles at $N.", smile.OthersFound);
        Assert.Equal("$n smiles at you.", smile.VictimFound);
        Assert.Equal("You don't see them.", smile.CharNotFound);
        Assert.Equal("You smile at yourself.", smile.CharAuto);
        Assert.Equal("$n smiles at $mself.", smile.OthersAuto);

        var nod = area.Socials[1];
        Assert.Equal("nod", nod.Name);
        Assert.Equal(string.Empty, nod.CharNoArgument);
        Assert.Equal(string.Empty, nod.OthersAuto);
    }

    [Fact]
    public void ProtoStyleAreaNumberRangeWarnsAndFallsBack()
    {
        const string text = """
            #AREA
            proto.are~
            Prototype~
            Credits~
            XX00 XX99
            #$
            """;

        var result = new LegacyAreaParser().Parse(text, "proto.are");

        Assert.Equal("Prototype", result.Area.Name);
        Assert.Equal(0, result.Area.LowVnum);
        Assert.Equal(0, result.Area.HighVnum);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("not numeric", warning.Message, StringComparison.Ordinal);
        Assert.Equal(5, warning.Position.Line);
    }

    [Fact]
    public void UnknownFlagBitsAreDroppedWithWarning()
    {
        const string text = """
            #MOBILES
            #1
            guard~
            the guard~
            A guard.
            ~
            A guard.
            ~
            human~
            Z 0 0 0
            1 0 1d1+0 1d1+0 1d1+0 none
            0 0 0 0
            0 0 0 0
            stand stand male 0
            0 0 medium none
            #0
            """;

        var result = new LegacyAreaParser().Parse(text, "bits.are");

        var warning = Assert.Single(result.Warnings);
        Assert.Contains("Unknown act bits 0x2000000", warning.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.Area.Mobiles[0].ActFlags);
    }

    [Fact]
    public void UnknownSectorWarnsAndUsesInside()
    {
        const string text = """
            #ROOMS
            #1
            A Room~
            A room.
            ~
            0 0 -1
            S
            #0
            """;

        var result = new LegacyAreaParser().Parse(text, "sector.are");

        Assert.Equal(Sector.Inside, result.Area.Rooms[0].Sector);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("Unknown sector -1", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateVnumReportsSourcePosition()
    {
        const string text = """
            #ROOMS
            #1
            A Room~
            A room.
            ~
            0 0 0
            S
            #1
            """;

        var exception = Assert.Throws<LegacyFormatException>(
            () => new LegacyAreaParser().Parse(text, "duplicate.are"));

        Assert.Contains("Duplicate room vnum 1.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(8, exception.Line);
    }

    [Fact]
    public void UnknownSectionReportsSourcePosition()
    {
        var exception = Assert.Throws<LegacyFormatException>(
            () => new LegacyAreaParser().Parse("#BOGUS\n", "bogus.are"));

        Assert.Contains("Unknown section '#BOGUS'.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, exception.Line);
        Assert.Equal(7, exception.Column);
    }

    [Fact]
    public void UnknownRaceReportsSourcePosition()
    {
        const string text = """
            #MOBILES
            #1
            guard~
            the guard~
            A guard.
            ~
            A guard.
            ~
            notarace~
            """;

        var exception = Assert.Throws<LegacyFormatException>(
            () => new LegacyAreaParser().Parse(text, "race.are"));

        Assert.Contains("Unknown race 'notarace'.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(9, exception.Line);
    }

    [Fact]
    public void UnknownResetCommandReportsSourcePosition()
    {
        var exception = Assert.Throws<LegacyFormatException>(
            () => new LegacyAreaParser().Parse("#RESETS\nX 0 0\n", "reset.are"));

        Assert.Contains("Unknown reset command 'X'.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, exception.Line);
    }

    [Fact]
    public void MalformedDiceReportsExpectedLetter()
    {
        const string text = """
            #MOBILES
            #1
            guard~
            the guard~
            A guard.
            ~
            A guard.
            ~
            human~
            0 0 0 0
            1 0 1x1+0
            """;

        var exception = Assert.Throws<LegacyFormatException>(
            () => new LegacyAreaParser().Parse(text, "dice.are"));

        Assert.Contains("Expected 'd', found 'x'.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(11, exception.Line);
    }

    [Fact]
    public void AreaDataFieldNamesAreCaseInsensitive()
    {
        const string text = """
            #AREADATA
            name        Lower~
            vnums       10 20
            End
            #$
            """;

        var area = new LegacyAreaParser().Parse(text, "case.are").Area;

        Assert.Equal("Lower", area.Name);
        Assert.Equal(10, area.LowVnum);
        Assert.Equal(20, area.HighVnum);
        Assert.Equal(9, area.Security);
    }

    [RomAreaFact]
    public void ParsesEveryRomAreaFileWithoutErrors()
    {
        var directory = RomAreaFixture.Directory!;
        var parser = new LegacyAreaParser();
        var failures = new List<string>();

        foreach (var file in Directory.GetFiles(directory, "*.are").Order(StringComparer.Ordinal))
        {
            try
            {
                var result = parser.Parse(File.ReadAllText(file), file);
                var errors = result.Area.Validate();
                if (errors.Count > 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: {string.Join("; ", errors.Take(3))}");
                }
            }
            catch (LegacyFormatException exception)
            {
                failures.Add($"{Path.GetFileName(file)}: {exception.Message}");
            }
        }

        Assert.Empty(failures);
    }

    [RomAreaFact]
    public void ParsesProtoRomHelpAndSocialWithAssertedCounts()
    {
        var directory = RomAreaFixture.Directory!;
        var parser = new LegacyAreaParser();

        var proto = parser.Parse(File.ReadAllText(Path.Combine(directory, "proto.are")), "proto.are");
        Assert.Empty(proto.Area.Validate());
        Assert.Empty(proto.Area.Mobiles);
        Assert.Empty(proto.Area.Objects);
        Assert.Empty(proto.Area.Rooms);
        Assert.Empty(proto.Area.Resets);
        Assert.Single(proto.Warnings);
        Assert.Contains("not numeric", proto.Warnings[0].Message, StringComparison.Ordinal);

        var rom = parser.Parse(File.ReadAllText(Path.Combine(directory, "rom.are")), "rom.are");
        Assert.Empty(rom.Area.Validate());
        Assert.Equal(27, rom.Area.Helps.Count);
        Assert.Empty(rom.Area.Mobiles);
        Assert.Empty(rom.Area.Socials);

        var help = parser.Parse(File.ReadAllText(Path.Combine(directory, "help.are")), "help.are");
        Assert.Empty(help.Area.Validate());
        Assert.Equal(141, help.Area.Helps.Count);
        Assert.Equal("Help", help.Area.Name);

        var social = parser.Parse(File.ReadAllText(Path.Combine(directory, "social.are")), "social.are");
        Assert.Empty(social.Area.Validate());
        Assert.Equal(244, social.Area.Socials.Count);
        Assert.Equal("Social", social.Area.Name);
        Assert.Equal("gack", social.Area.Socials[0].Name);
    }

    [Fact]
    public void ValidatesDanglingReferencesAcrossImports()
    {
        const string referring = """
            #ROOMS
            #1
            Room~
            Description.
            ~
            0 0 0
            D0
            Door.
            ~
            ~
            0 -1 999
            S
            #0

            #RESETS
            M 0 1 1 999 1
            S
            """;

        const string definition = """
            #MOBILES
            #1
            guard~
            the guard~
            A guard.
            ~
            A guard.
            ~
            human~
            0 0 0 0
            1 0 1d1+0 1d1+0 1d1+0 none
            0 0 0 0
            0 0 0 0
            stand stand male 0
            0 0 medium none
            #0
            """;

        var parser = new LegacyAreaParser();
        var first = parser.Parse(referring, "missing.are");
        var second = parser.Parse(definition, "defined.are");

        var warnings = LegacyAreaParser.ValidateReferences([first, second]);

        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, warning =>
            warning.Message.Contains("missing room vnum 999", StringComparison.Ordinal)
            && warning.Message.StartsWith("missing.are:", StringComparison.Ordinal));
        Assert.Contains(warnings, warning =>
            warning.Message.Contains("mobile reset room references missing room vnum 999", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateReferencesAcceptsTargetsInAnotherArea()
    {
        const string referring = """
            #ROOMS
            #1
            Room~
            Description.
            ~
            0 0 0
            D0
            Door.
            ~
            ~
            0 -1 2
            S
            #0
            """;

        const string definition = """
            #ROOMS
            #2
            Other~
            Other room.
            ~
            0 0 0
            S
            #0
            """;

        var parser = new LegacyAreaParser();
        var warnings = LegacyAreaParser.ValidateReferences(
            [parser.Parse(referring, "referring.are"), parser.Parse(definition, "defined.are")]);

        Assert.Empty(warnings);
    }

    private static LegacyAreaParseResult ParseFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        return new LegacyAreaParser().Parse(File.ReadAllText(path), name);
    }

    private static void AssertWeapon(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Weapon, obj.ItemType);
        var data = Assert.IsType<WeaponData>(obj.Data);
        Assert.Equal(WeaponClass.Sword, data.Class);
        Assert.Equal(2, data.DiceCount);
        Assert.Equal(6, data.DiceSize);
        Assert.Equal("Slash", data.AttackType);
        Assert.Equal(0, data.SpecialFlags);
        Assert.Equal(100, obj.Condition);
    }

    private static void AssertArmor(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Armor, obj.ItemType);
        var data = Assert.IsType<ArmorData>(obj.Data);
        Assert.Equal(new ArmorValues(6, 6, 6, 3), data.Values);
    }

    private static void AssertContainer(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Container, obj.ItemType);
        var data = Assert.IsType<ContainerData>(obj.Data);
        Assert.Equal(50, data.Capacity);
        Assert.Equal(0, data.Flags);
        Assert.Equal(1003, data.KeyVnum);
    }

    private static void AssertDrink(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.DrinkContainer, obj.ItemType);
        var data = Assert.IsType<DrinkData>(obj.Data);
        Assert.Equal(20, data.Capacity);
        Assert.Equal(20, data.Remaining);
        Assert.Equal("Beer", data.Liquid);
        Assert.False(data.Poisoned);
    }

    private static void AssertWand(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Wand, obj.ItemType);
        var data = Assert.IsType<ChargesData>(obj.Data);
        Assert.Equal(5, data.Level);
        Assert.Equal(3, data.Total);
        Assert.Equal(2, data.Remaining);
        Assert.Equal("magic missile", data.Spell);
    }

    private static void AssertScroll(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Scroll, obj.ItemType);
        var data = Assert.IsType<SpellsData>(obj.Data);
        Assert.Equal(5, data.Level);
        Assert.Equal(["cure light", "bless"], data.Names);
    }

    private static void AssertLight(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Light, obj.ItemType);
        var data = Assert.IsType<LightData>(obj.Data);
        Assert.Equal(-1, data.Duration);
    }

    private static void AssertFood(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Food, obj.ItemType);
        var data = Assert.IsType<FoodData>(obj.Data);
        Assert.Equal(10, data.Hours);
        Assert.False(data.Poisoned);
    }

    private static void AssertMoney(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Money, obj.ItemType);
        var data = Assert.IsType<MoneyData>(obj.Data);
        Assert.Equal(25, data.Gold);
        Assert.Equal(0, data.Silver);
    }

    private static void AssertFurniture(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Furniture, obj.ItemType);
        var data = Assert.IsType<FurnitureData>(obj.Data);
        Assert.Equal(2, data.MaxPeople);
        Assert.Equal(1, data.Flags);
    }

    private static void AssertPortal(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Portal, obj.ItemType);
        var data = Assert.IsType<PortalData>(obj.Data);
        Assert.Equal(3, data.ExitFlags);
        Assert.Equal(0, data.GateFlags);
        Assert.Equal(1000, data.ToRoomVnum);
    }

    private static void AssertMap(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Map, obj.ItemType);
        var data = Assert.IsType<MapData>(obj.Data);
        Assert.Equal(1000, data.RoomVnum);
    }

    private static void AssertPlainTreasure(ObjectPrototype obj)
    {
        Assert.Equal(ItemType.Treasure, obj.ItemType);
        var data = Assert.IsType<PlainObjectData>(obj.Data);
        Assert.NotNull(data.RawValues);
        Assert.Equal(new[] { 0, 0, 0, 0, 0 }, data.RawValues);
    }
}
