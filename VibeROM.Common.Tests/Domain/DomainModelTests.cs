using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Domain;

public sealed class DomainModelTests
{
    [Fact]
    public void ValidAreaWithAllEntityTypes_HasNoErrors()
    {
        var area = CreateValidArea();

        Assert.Empty(area.Validate());
    }

    [Fact]
    public void Area_RejectsDuplicateVnums()
    {
        var mobile = CreateMobile();
        var area = CreateValidArea() with
        {
            Mobiles = [mobile, mobile]
        };

        Assert.Contains(area.Validate(), error => error.Contains("duplicate vnum", StringComparison.Ordinal));
    }

    [Fact]
    public void ObjectPrototype_DataMustMatchItemType()
    {
        var obj = CreateWeapon() with
        {
            Data = new PlainObjectData()
        };

        Assert.Contains(obj.Validate(), error => error.Contains("does not match itemType", StringComparison.Ordinal));
    }

    [Fact]
    public void MobilePrototype_RejectsUnknownFlagBits()
    {
        var mobile = CreateMobile() with
        {
            ActFlags = 1 << 30
        };

        Assert.Contains(mobile.Validate(), error => error.Contains("unknown act bits", StringComparison.Ordinal));
    }

    [Fact]
    public void RoomPrototype_RejectsMismatchedExitKey()
    {
        var exit = new ExitPrototype(Direction.North, 1001, 0, 0, string.Empty);
        var room = CreateRoom(1000) with
        {
            Exits = new Dictionary<Direction, ExitPrototype>
            {
                [Direction.South] = exit
            }
        };

        Assert.Contains(room.Validate(), error => error.Contains("does not match exit direction", StringComparison.Ordinal));
    }

    [Fact]
    public void NpcCharacter_ImplementsActActor()
    {
        var character = new Character(CreateMobile());

        IActActor actor = character;

        Assert.True(actor.IsNpc);
        Assert.Equal("the guard", actor.Name);
        Assert.Equal("the guard", actor.ShortDescription);
        Assert.Equal(ActorSex.Male, actor.Sex);
    }

    [Fact]
    public void PlayerCharacter_ImplementsActActor()
    {
        var character = new Character(
            new PlayerData(),
            "Alander",
            Race.Human,
            PlayerClass.Warrior,
            Sex.Female);

        IActActor actor = character;

        Assert.False(actor.IsNpc);
        Assert.Equal("Alander", actor.Name);
        Assert.Equal("Alander", actor.ShortDescription);
        Assert.Equal(ActorSex.Female, actor.Sex);
        Assert.Empty(character.Validate());
    }

    [Fact]
    public void PlayerCharacter_RequiresPlayerData()
    {
        var character = new Character(
            new PlayerData(),
            "Alander",
            Race.Human,
            PlayerClass.Warrior,
            Sex.Male)
        {
            Player = null
        };

        Assert.Contains(character.Validate(), error => error.Contains("player data", StringComparison.Ordinal));
    }

    [Fact]
    public void GameObject_ImplementsActObject()
    {
        var obj = new GameObject(CreateWeapon());

        IActObject actObject = obj;

        Assert.Equal("a sword", actObject.ShortDescription);

        obj.ShortDescriptionOverride = "a glowing sword";
        Assert.Equal("a glowing sword", actObject.ShortDescription);
    }

    [Fact]
    public void GameObject_RejectsMultipleOwners()
    {
        var room = new Room(CreateRoom(1000));
        var character = new Character(CreateMobile());
        var obj = new GameObject(CreateWeapon())
        {
            InRoom = room,
            CarriedBy = character
        };

        Assert.Contains(obj.Validate(), error => error.Contains("more than one owner", StringComparison.Ordinal));
    }

    [Fact]
    public void Room_ValidatesBackReferences()
    {
        var room = new Room(CreateRoom(1000));
        var character = new Character(CreateMobile()) { InRoom = room };
        var obj = new GameObject(CreateWeapon()) { InRoom = room };
        room.People.Add(character);
        room.Contents.Add(obj);

        Assert.Empty(room.Validate());

        room.Contents.Add(new GameObject(CreateWeapon()));
        Assert.Contains(room.Validate(), error => error.Contains("does not point back", StringComparison.Ordinal));
    }

    [Fact]
    public void VisibilityExtensions_BuildActVisibility()
    {
        var player = new Character(
            new PlayerData(),
            "Alander",
            Race.Human,
            PlayerClass.Warrior,
            Sex.Male);
        var obj = new GameObject(CreateWeapon());
        var visibility = AllVisibleVisibilityService.Instance.ToActVisibility(player);

        Assert.Equal(
            "Alander drops a sword.",
            ActFormatter.Format("$n drops $p.", new ActMessageContext(player, Object1: obj), player, visibility));
    }

    [Fact]
    public void ResetVariants_ValidateTheirFields()
    {
        Assert.Contains(
            new EquipReset(false, 1, WearSlot.None, 1).Validate("equip"),
            error => error.Contains("wearLocation", StringComparison.Ordinal));

        Assert.Contains(
            new RandomizeReset(false, 1, 7).Validate("random"),
            error => error.Contains("door", StringComparison.Ordinal));

        Assert.Empty(new MobileReset(false, 1, 2, 1, 1).Validate("mobile"));
    }

    [Fact]
    public void PlayerData_ClampsSkills()
    {
        var player = new PlayerData();
        player.SetSkill("sword", 150);

        Assert.Equal(100, player.GetSkill("SWORD"));
    }

    [Fact]
    public void Descriptor_DefaultsToNameEntry()
    {
        var descriptor = new Descriptor();

        Assert.NotEqual(Guid.Empty, descriptor.Id);
        Assert.Equal(ConnectionState.GetName, descriptor.State);
        Assert.False(descriptor.IsPlaying);
        Assert.Equal(80, descriptor.TerminalWidth);
        Assert.Equal(24, descriptor.TerminalHeight);
    }

    private static AreaData CreateValidArea()
    {
        var mobile = CreateMobile();
        var obj = CreateWeapon();
        var room = CreateRoom(1000) with
        {
            Exits = new Dictionary<Direction, ExitPrototype>
            {
                [Direction.North] = new(Direction.North, 1001, 0, 0, string.Empty)
            }
        };
        var room2 = CreateRoom(1001);

        return new AreaData(
            Name: "Test Area",
            Credits: "Tests",
            LowVnum: 1000,
            HighVnum: 3000,
            Security: 9,
            Builders: "Tests",
            LegacyFile: "test.are",
            Helps: [new HelpEntry("test", 0, "Test help.")],
            Mobiles: [mobile],
            Objects: [obj],
            Rooms: [room, room2],
            Resets:
            [
                new MobileReset(false, mobile.Vnum, room.Vnum, 1, 1),
                new ObjectReset(false, obj.Vnum, room.Vnum, 1),
                new EquipReset(false, obj.Vnum, WearSlot.Wield, 1),
                new DoorReset(false, room.Vnum, Direction.North, DoorState.Locked),
                new RandomizeReset(false, room.Vnum, 1),
                new PutReset(false, obj.Vnum, obj.Vnum, 1),
                new GiveReset(false, obj.Vnum, 1)
            ],
            Shops: [new ShopData(mobile.Vnum, [ItemType.Weapon], 120, 120, 0, 23)],
            Specials: [new SpecialData(mobile.Vnum, "Guard")],
            Socials:
            [
                new SocialData(
                    "smile",
                    "You smile.",
                    "$n smiles.",
                    "You smile at $N.",
                    "$n smiles at $N.",
                    "$n smiles at you.",
                    "You don't see them.",
                    "You smile at yourself.",
                    "$n smiles at $mself.")
            ]);
    }

    private static MobilePrototype CreateMobile() =>
        new(
            Vnum: 2000,
            Keywords: "guard test",
            ShortDescription: "the guard",
            LongDescription: "A guard stands here.",
            Description: "The guard looks alert.",
            Race: Race.Human,
            ActFlags: 0,
            AffectedBy: 0,
            OffensiveFlags: 0,
            Immunities: 0,
            Resistances: 0,
            Vulnerabilities: 0,
            Form: 0,
            Parts: 0,
            Alignment: 0,
            Group: 0,
            Level: 10,
            HitRoll: 1,
            HitDice: new Dice(2, 6, 0),
            ManaDice: new Dice(1, 4, 0),
            Damage: new DamageDice(new Dice(1, 4, 0), "Slash"),
            Armor: new ArmorValues(0, 0, 0, 0),
            StartPosition: Position.Standing,
            DefaultPosition: Position.Standing,
            Sex: Sex.Male,
            Gold: 0,
            Size: Size.Medium,
            Material: "human");

    private static ObjectPrototype CreateWeapon() =>
        new(
            Vnum: 3000,
            Keywords: "sword test",
            ShortDescription: "a sword",
            LongDescription: "A sword lies here.",
            Material: "iron",
            ItemType: ItemType.Weapon,
            ExtraFlags: 0,
            WearFlags: 0,
            Level: 1,
            WeightTenths: 10,
            CostCopper: 1,
            Condition: 100,
            Data: new WeaponData(WeaponClass.Sword, 1, 4, "Slash", 0),
            Affects: [],
            ExtraDescriptions: []);

    private static RoomPrototype CreateRoom(int vnum) =>
        new(
            Vnum: vnum,
            Name: $"Room {vnum}",
            Description: "A test room.",
            RoomFlags: 0,
            Sector: Sector.Inside,
            HealRate: 100,
            ManaRate: 100,
            Clan: 0,
            Exits: new Dictionary<Direction, ExitPrototype>(),
            ExtraDescriptions: []);
}
