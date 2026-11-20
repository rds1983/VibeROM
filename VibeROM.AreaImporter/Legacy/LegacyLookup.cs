using VibeROM.Common.Rom;

namespace VibeROM.AreaImporter.Legacy;

/// <summary>
/// Importer-only mapping of legacy ROM words to canonical names. ROM lookups
/// use case-insensitive prefix matching in table order, which this class
/// preserves.
/// </summary>
public static class LegacyLookup
{
    private static readonly (string Legacy, Race Value)[] Races =
    [
        ("unique", Race.Unique),
        ("human", Race.Human),
        ("elf", Race.Elf),
        ("dwarf", Race.Dwarf),
        ("giant", Race.Giant),
        ("bat", Race.Bat),
        ("bear", Race.Bear),
        ("cat", Race.Cat),
        ("centipede", Race.Centipede),
        ("dog", Race.Dog),
        ("doll", Race.Doll),
        ("dragon", Race.Dragon),
        ("fido", Race.Fido),
        ("fox", Race.Fox),
        ("goblin", Race.Goblin),
        ("hobgoblin", Race.Hobgoblin),
        ("kobold", Race.Kobold),
        ("lizard", Race.Lizard),
        ("modron", Race.Modron),
        ("orc", Race.Orc),
        ("pig", Race.Pig),
        ("rabbit", Race.Rabbit),
        ("school monster", Race.SchoolMonster),
        ("snake", Race.Snake),
        ("song bird", Race.SongBird),
        ("troll", Race.Troll),
        ("water fowl", Race.WaterFowl),
        ("wolf", Race.Wolf),
        ("wyvern", Race.Wyvern)
    ];

    private static readonly (string Legacy, ItemType Value)[] ItemTypes =
    [
        ("light", ItemType.Light),
        ("scroll", ItemType.Scroll),
        ("wand", ItemType.Wand),
        ("staff", ItemType.Staff),
        ("weapon", ItemType.Weapon),
        ("treasure", ItemType.Treasure),
        ("armor", ItemType.Armor),
        ("potion", ItemType.Potion),
        ("clothing", ItemType.Clothing),
        ("furniture", ItemType.Furniture),
        ("trash", ItemType.Trash),
        ("container", ItemType.Container),
        ("drink", ItemType.DrinkContainer),
        ("key", ItemType.Key),
        ("food", ItemType.Food),
        ("money", ItemType.Money),
        ("boat", ItemType.Boat),
        ("npc_corpse", ItemType.NpcCorpse),
        ("pc_corpse", ItemType.PcCorpse),
        ("fountain", ItemType.Fountain),
        ("pill", ItemType.Pill),
        ("protect", ItemType.Protect),
        ("map", ItemType.Map),
        ("portal", ItemType.Portal),
        ("warp_stone", ItemType.WarpStone),
        ("room_key", ItemType.RoomKey),
        ("gem", ItemType.Gem),
        ("jewelry", ItemType.Jewelry),
        ("jukebox", ItemType.Jukebox)
    ];

    private static readonly (string Legacy, WeaponClass Value)[] WeaponClasses =
    [
        ("sword", WeaponClass.Sword),
        ("mace", WeaponClass.Mace),
        ("dagger", WeaponClass.Dagger),
        ("axe", WeaponClass.Axe),
        ("staff", WeaponClass.Spear),
        ("flail", WeaponClass.Flail),
        ("whip", WeaponClass.Whip),
        ("polearm", WeaponClass.Polearm),
        ("exotic", WeaponClass.Exotic)
    ];

    private static readonly (string Legacy, Position Value)[] Positions =
    [
        ("dead", Position.Dead),
        ("mortally wounded", Position.Mortal),
        ("incapacitated", Position.Incap),
        ("stunned", Position.Stunned),
        ("sleeping", Position.Sleeping),
        ("resting", Position.Resting),
        ("sitting", Position.Sitting),
        ("fighting", Position.Fighting),
        ("standing", Position.Standing)
    ];

    private static readonly (string Legacy, Sex Value)[] Sexes =
    [
        ("none", Sex.Neutral),
        ("male", Sex.Male),
        ("female", Sex.Female),
        ("either", Sex.Neutral)
    ];

    private static readonly (string Legacy, Size Value)[] Sizes =
    [
        ("tiny", Size.Tiny),
        ("small", Size.Small),
        ("medium", Size.Medium),
        ("large", Size.Large),
        ("huge", Size.Huge),
        ("giant", Size.Giant)
    ];

    private static readonly (string Legacy, string Value)[] Liquids =
    [
        ("water", "Water"),
        ("beer", "Beer"),
        ("red wine", "RedWine"),
        ("ale", "Ale"),
        ("dark ale", "DarkAle"),
        ("whisky", "Whisky"),
        ("lemonade", "Lemonade"),
        ("firebreather", "Firebreather"),
        ("local specialty", "LocalSpecialty"),
        ("slime mold juice", "SlimeMoldJuice"),
        ("milk", "Milk"),
        ("tea", "Tea"),
        ("coffee", "Coffee"),
        ("blood", "Blood"),
        ("salt water", "SaltWater"),
        ("coke", "Coke"),
        ("root beer", "RootBeer"),
        ("elvish wine", "ElvishWine"),
        ("white wine", "WhiteWine"),
        ("champagne", "Champagne"),
        ("mead", "Mead"),
        ("rose wine", "RoseWine"),
        ("benedictine wine", "BenedictineWine"),
        ("vodka", "Vodka"),
        ("cranberry juice", "CranberryJuice"),
        ("orange juice", "OrangeJuice"),
        ("absinthe", "Absinthe"),
        ("brandy", "Brandy"),
        ("aquavit", "Aquavit"),
        ("schnapps", "Schnapps"),
        ("icewine", "Icewine"),
        ("amontillado", "Amontillado"),
        ("sherry", "Sherry"),
        ("framboise", "Framboise"),
        ("rum", "Rum"),
        ("cordial", "Cordial")
    ];

    private static readonly (string Legacy, string Value)[] Attacks =
    [
        ("none", "Hit"),
        ("slice", "Slice"),
        ("stab", "Stab"),
        ("slash", "Slash"),
        ("whip", "Whip"),
        ("claw", "Claw"),
        ("blast", "Blast"),
        ("pound", "Pound"),
        ("crush", "Crush"),
        ("grep", "Grep"),
        ("bite", "Bite"),
        ("pierce", "Pierce"),
        ("suction", "Suction"),
        ("beating", "Beating"),
        ("digestion", "Digestion"),
        ("charge", "Charge"),
        ("slap", "Slap"),
        ("punch", "Punch"),
        ("wrath", "Wrath"),
        ("magic", "Magic"),
        ("divine", "DivinePower"),
        ("cleave", "Cleave"),
        ("scratch", "Scratch"),
        ("peck", "PeckPierce"),
        ("peckb", "PeckBash"),
        ("chop", "Chop"),
        ("sting", "Sting"),
        ("smash", "Smash"),
        ("shbite", "ShockingBite"),
        ("flbite", "FlamingBite"),
        ("frbite", "FreezingBite"),
        ("acbite", "AcidicBite"),
        ("chomp", "Chomp"),
        ("drain", "Drain"),
        ("thrust", "Thrust"),
        ("slime", "Slime"),
        ("shock", "Shock"),
        ("thwack", "Thwack"),
        ("flame", "Flame"),
        ("chill", "Chill")
    ];

    private static readonly (string Legacy, int Value)[] Clans =
    [
        ("", 0),
        ("loner", 1),
        ("rom", 2)
    ];

    public static Race ToRace(string value, LegacyAreaReader reader) =>
        Lookup(Races, value, "race", reader);

    public static ItemType ToItemType(string value, LegacyAreaReader reader) =>
        Lookup(ItemTypes, value, "item type", reader);

    public static WeaponClass ToWeaponClass(string value, LegacyAreaReader reader) =>
        Lookup(WeaponClasses, value, "weapon class", reader);

    public static Position ToPosition(string value, LegacyAreaReader reader) =>
        Lookup(Positions, value, "position", reader);

    public static Sex ToSex(string value, LegacyAreaReader reader) =>
        Lookup(Sexes, value, "sex", reader);

    public static Size ToSize(string value, LegacyAreaReader reader) =>
        Lookup(Sizes, value, "size", reader);

    public static string ToLiquid(string value, LegacyAreaReader reader) =>
        Lookup(Liquids, value, "liquid", reader);

    public static string ToAttackType(string value, LegacyAreaReader reader) =>
        Lookup(Attacks, value, "attack type", reader);

    public static int ToClan(string value, LegacyAreaReader reader) =>
        Lookup(Clans, value, "clan", reader);

    public static string ToSpecial(string value, LegacyAreaReader reader)
    {
        foreach (var definition in RomTables.SpecialDefinitions)
        {
            if (definition.LegacyName.Equals(value, StringComparison.OrdinalIgnoreCase)
                || definition.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return definition.Name;
            }
        }

        throw reader.CreateError($"Unknown special procedure '{value}'.");
    }

    private static T Lookup<T>(
        IReadOnlyList<(string Legacy, T Value)> table,
        string value,
        string category,
        LegacyAreaReader reader)
    {
        foreach (var (legacy, result) in table)
        {
            if (legacy.StartsWith(value, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }
        }

        throw reader.CreateError($"Unknown {category} '{value}'.");
    }
}
