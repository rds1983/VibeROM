using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Tests.Domain;

internal static class WorldFixtures
{
    public static AreaData CreateArea(
        string name = "Test Area",
        int mobileVnum = 2000,
        int weaponVnum = 3000,
        int containerVnum = 3001,
        int room1Vnum = 1000,
        int room2Vnum = 1001,
        string helpKeywords = "rom diku")
    {
        return new AreaData(
            Name: name,
            Credits: "Tests",
            LowVnum: 1000,
            HighVnum: 4000,
            Security: 9,
            Builders: "Tests",
            LegacyFile: null,
            Helps: [new HelpEntry(helpKeywords, 0, "Test help.")],
            Mobiles: [CreateMobile(mobileVnum)],
            Objects: [CreateWeapon(weaponVnum), CreateContainer(containerVnum)],
            Rooms: [CreateRoom(room1Vnum), CreateRoom(room2Vnum)],
            Resets: [],
            Shops: [],
            Specials: [],
            Socials: []);
    }

    public static MobilePrototype CreateMobile(int vnum = 2000) =>
        new(
            Vnum: vnum,
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

    public static ObjectPrototype CreateWeapon(int vnum = 3000) =>
        new(
            Vnum: vnum,
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

    public static ObjectPrototype CreateContainer(int vnum = 3001) =>
        new(
            Vnum: vnum,
            Keywords: "bag test",
            ShortDescription: "a bag",
            LongDescription: "A bag lies here.",
            Material: "leather",
            ItemType: ItemType.Container,
            ExtraFlags: 0,
            WearFlags: 0,
            Level: 1,
            WeightTenths: 10,
            CostCopper: 1,
            Condition: 100,
            Data: new ContainerData(50, 0, 0),
            Affects: [],
            ExtraDescriptions: []);

    public static RoomPrototype CreateRoom(int vnum) =>
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
