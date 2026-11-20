namespace VibeROM.Common.Rom;

/// <summary>
/// Canonical ROM flag registries. Names are the JSON names; bit values match
/// the C bit vectors exactly. Engine-derived flags are marked non-persisted.
/// </summary>
public static class RomFlags
{
    public static FlagRegistry Act { get; } = new(
        "act",
        [
            new(Bit(0), "IsNpc", Persisted: false),
            new(Bit(1), "Sentinel"),
            new(Bit(2), "Scavenger"),
            new(Bit(5), "Aggressive"),
            new(Bit(6), "StayArea"),
            new(Bit(7), "Wimpy"),
            new(Bit(8), "Pet", Persisted: false),
            new(Bit(9), "Train"),
            new(Bit(10), "Practice"),
            new(Bit(14), "Undead"),
            new(Bit(16), "Cleric"),
            new(Bit(17), "Mage"),
            new(Bit(18), "Thief"),
            new(Bit(19), "Warrior"),
            new(Bit(20), "NoAlign"),
            new(Bit(21), "NoPurge"),
            new(Bit(22), "Outdoors"),
            new(Bit(24), "Indoors"),
            new(Bit(26), "IsHealer"),
            new(Bit(27), "Gain"),
            new(Bit(28), "UpdateAlways"),
            new(Bit(29), "IsChanger")
        ]);

    public static FlagRegistry PlayerAct { get; } = new(
        "playerAct",
        [
            new(Bit(0), "IsNpc", Persisted: false),
            new(Bit(2), "AutoAssist"),
            new(Bit(3), "AutoExit"),
            new(Bit(4), "AutoLoot"),
            new(Bit(5), "AutoSac"),
            new(Bit(6), "AutoGold"),
            new(Bit(7), "AutoSplit"),
            new(Bit(13), "HolyLight"),
            new(Bit(15), "CanLoot"),
            new(Bit(16), "NoSummon"),
            new(Bit(17), "NoFollow"),
            new(Bit(20), "Permit"),
            new(Bit(22), "Log"),
            new(Bit(23), "Deny"),
            new(Bit(24), "Freeze"),
            new(Bit(25), "Thief"),
            new(Bit(26), "Killer")
        ]);

    public static FlagRegistry Affect { get; } = new(
        "affect",
        [
            new(Bit(0), "Blind"),
            new(Bit(1), "Invisible"),
            new(Bit(2), "DetectEvil"),
            new(Bit(3), "DetectInvis"),
            new(Bit(4), "DetectMagic"),
            new(Bit(5), "DetectHidden"),
            new(Bit(6), "DetectGood"),
            new(Bit(7), "Sanctuary"),
            new(Bit(8), "FaerieFire"),
            new(Bit(9), "Infrared"),
            new(Bit(10), "Curse"),
            new(Bit(12), "Poison"),
            new(Bit(13), "ProtectEvil"),
            new(Bit(14), "ProtectGood"),
            new(Bit(15), "Sneak"),
            new(Bit(16), "Hide"),
            new(Bit(17), "Sleep"),
            new(Bit(18), "Charm"),
            new(Bit(19), "Flying"),
            new(Bit(20), "PassDoor"),
            new(Bit(21), "Haste"),
            new(Bit(22), "Calm"),
            new(Bit(23), "Plague"),
            new(Bit(24), "Weaken"),
            new(Bit(25), "DarkVision"),
            new(Bit(26), "Berserk"),
            new(Bit(27), "Swim"),
            new(Bit(28), "Regeneration"),
            new(Bit(29), "Slow")
        ]);

    public static FlagRegistry Offensive { get; } = new(
        "offensive",
        [
            new(Bit(0), "AreaAttack"),
            new(Bit(1), "Backstab"),
            new(Bit(2), "Bash"),
            new(Bit(3), "Berserk"),
            new(Bit(4), "Disarm"),
            new(Bit(5), "Dodge"),
            new(Bit(6), "Fade"),
            new(Bit(7), "Fast"),
            new(Bit(8), "Kick"),
            new(Bit(9), "KickDirt"),
            new(Bit(10), "Parry"),
            new(Bit(11), "Rescue"),
            new(Bit(12), "Tail"),
            new(Bit(13), "Trip"),
            new(Bit(14), "Crush"),
            new(Bit(15), "AssistAll"),
            new(Bit(16), "AssistAlign"),
            new(Bit(17), "AssistRace"),
            new(Bit(18), "AssistPlayers"),
            new(Bit(19), "AssistGuard"),
            new(Bit(20), "AssistVnum")
        ]);

    /// <summary>Shared by immunities, resistances and vulnerabilities.</summary>
    public static FlagRegistry Immunity { get; } = new(
        "immunity",
        [
            new(Bit(0), "Summon"),
            new(Bit(1), "Charm"),
            new(Bit(2), "Magic"),
            new(Bit(3), "Weapon"),
            new(Bit(4), "Bash"),
            new(Bit(5), "Pierce"),
            new(Bit(6), "Slash"),
            new(Bit(7), "Fire"),
            new(Bit(8), "Cold"),
            new(Bit(9), "Lightning"),
            new(Bit(10), "Acid"),
            new(Bit(11), "Poison"),
            new(Bit(12), "Negative"),
            new(Bit(13), "Holy"),
            new(Bit(14), "Energy"),
            new(Bit(15), "Mental"),
            new(Bit(16), "Disease"),
            new(Bit(17), "Drowning"),
            new(Bit(18), "Light"),
            new(Bit(19), "Sound"),
            new(Bit(23), "Wood"),
            new(Bit(24), "Silver"),
            new(Bit(25), "Iron")
        ]);

    public static FlagRegistry Form { get; } = new(
        "form",
        [
            new(Bit(0), "Edible"),
            new(Bit(1), "Poison"),
            new(Bit(2), "Magical"),
            new(Bit(3), "InstantDecay"),
            new(Bit(4), "Other"),
            new(Bit(6), "Animal"),
            new(Bit(7), "Sentient"),
            new(Bit(8), "Undead"),
            new(Bit(9), "Construct"),
            new(Bit(10), "Mist"),
            new(Bit(11), "Intangible"),
            new(Bit(12), "Biped"),
            new(Bit(13), "Centaur"),
            new(Bit(14), "Insect"),
            new(Bit(15), "Spider"),
            new(Bit(16), "Crustacean"),
            new(Bit(17), "Worm"),
            new(Bit(18), "Blob"),
            new(Bit(21), "Mammal"),
            new(Bit(22), "Bird"),
            new(Bit(23), "Reptile"),
            new(Bit(24), "Snake"),
            new(Bit(25), "Dragon"),
            new(Bit(26), "Amphibian"),
            new(Bit(27), "Fish"),
            new(Bit(28), "ColdBlood")
        ]);

    public static FlagRegistry Part { get; } = new(
        "part",
        [
            new(Bit(0), "Head"),
            new(Bit(1), "Arms"),
            new(Bit(2), "Legs"),
            new(Bit(3), "Heart"),
            new(Bit(4), "Brains"),
            new(Bit(5), "Guts"),
            new(Bit(6), "Hands"),
            new(Bit(7), "Feet"),
            new(Bit(8), "Fingers"),
            new(Bit(9), "Ear"),
            new(Bit(10), "Eye"),
            new(Bit(11), "LongTongue"),
            new(Bit(12), "Eyestalks"),
            new(Bit(13), "Tentacles"),
            new(Bit(14), "Fins"),
            new(Bit(15), "Wings"),
            new(Bit(16), "Tail"),
            new(Bit(20), "Claws"),
            new(Bit(21), "Fangs"),
            new(Bit(22), "Horns"),
            new(Bit(23), "Scales"),
            new(Bit(24), "Tusks")
        ]);

    public static FlagRegistry Room { get; } = new(
        "room",
        [
            new(Bit(0), "Dark"),
            new(Bit(2), "NoMob"),
            new(Bit(3), "Indoors"),
            new(Bit(9), "Private"),
            new(Bit(10), "Safe"),
            new(Bit(11), "Solitary"),
            new(Bit(12), "PetShop"),
            new(Bit(13), "NoRecall"),
            new(Bit(14), "ImpOnly"),
            new(Bit(15), "GodOnly"),
            new(Bit(16), "HeroesOnly"),
            new(Bit(17), "NewbiesOnly"),
            new(Bit(18), "Law"),
            new(Bit(19), "NoWhere")
        ]);

    public static FlagRegistry Exit { get; } = new(
        "exit",
        [
            new(Bit(0), "IsDoor"),
            new(Bit(1), "Closed"),
            new(Bit(2), "Locked"),
            new(Bit(5), "PickProof"),
            new(Bit(6), "NoPass"),
            new(Bit(7), "Easy"),
            new(Bit(8), "Hard"),
            new(Bit(9), "Infuriating"),
            new(Bit(10), "NoClose"),
            new(Bit(11), "NoLock")
        ]);

    public static FlagRegistry Extra { get; } = new(
        "extra",
        [
            new(Bit(0), "Glow"),
            new(Bit(1), "Hum"),
            new(Bit(2), "Dark"),
            new(Bit(3), "Lock"),
            new(Bit(4), "Evil"),
            new(Bit(5), "Invis"),
            new(Bit(6), "Magic"),
            new(Bit(7), "NoDrop"),
            new(Bit(8), "Bless"),
            new(Bit(9), "AntiGood"),
            new(Bit(10), "AntiEvil"),
            new(Bit(11), "AntiNeutral"),
            new(Bit(12), "NoRemove"),
            new(Bit(13), "Inventory"),
            new(Bit(14), "NoPurge"),
            new(Bit(15), "RotDeath"),
            new(Bit(16), "VisDeath"),
            new(Bit(18), "NonMetal"),
            new(Bit(19), "NoLocate"),
            new(Bit(20), "MeltDrop"),
            new(Bit(21), "HadTimer"),
            new(Bit(22), "SellExtract"),
            new(Bit(24), "BurnProof"),
            new(Bit(25), "NoUncurse")
        ]);

    public static FlagRegistry Wear { get; } = new(
        "wear",
        [
            new(Bit(0), "Take"),
            new(Bit(1), "Finger"),
            new(Bit(2), "Neck"),
            new(Bit(3), "Body"),
            new(Bit(4), "Head"),
            new(Bit(5), "Legs"),
            new(Bit(6), "Feet"),
            new(Bit(7), "Hands"),
            new(Bit(8), "Arms"),
            new(Bit(9), "Shield"),
            new(Bit(10), "About"),
            new(Bit(11), "Waist"),
            new(Bit(12), "Wrist"),
            new(Bit(13), "Wield"),
            new(Bit(14), "Hold"),
            new(Bit(15), "NoSac"),
            new(Bit(16), "Float")
        ]);

    public static FlagRegistry Container { get; } = new(
        "container",
        [
            new(1, "Closeable"),
            new(2, "PickProof"),
            new(4, "Closed"),
            new(8, "Locked"),
            new(16, "PutOn")
        ]);

    public static FlagRegistry WeaponSpecial { get; } = new(
        "weaponSpecial",
        [
            new(Bit(0), "Flaming"),
            new(Bit(1), "Frost"),
            new(Bit(2), "Vampiric"),
            new(Bit(3), "Sharp"),
            new(Bit(4), "Vorpal"),
            new(Bit(5), "TwoHands"),
            new(Bit(6), "Shocking"),
            new(Bit(7), "Poison")
        ]);

    public static FlagRegistry Communication { get; } = new(
        "communication",
        [
            new(Bit(0), "Quiet"),
            new(Bit(1), "Deaf"),
            new(Bit(2), "NoWiz"),
            new(Bit(3), "NoAuction"),
            new(Bit(4), "NoGossip"),
            new(Bit(5), "NoQuestion"),
            new(Bit(6), "NoMusic"),
            new(Bit(7), "NoClan"),
            new(Bit(8), "NoQuote"),
            new(Bit(9), "ShoutsOff"),
            new(Bit(11), "Compact"),
            new(Bit(12), "Brief"),
            new(Bit(13), "Prompt"),
            new(Bit(14), "Combine"),
            new(Bit(15), "TelnetGa"),
            new(Bit(16), "ShowAffects"),
            new(Bit(17), "NoGrats"),
            new(Bit(19), "NoEmote"),
            new(Bit(20), "NoShout"),
            new(Bit(21), "NoTell"),
            new(Bit(22), "NoChannels"),
            new(Bit(24), "SnoopProof"),
            new(Bit(25), "Afk")
        ]);

    public static FlagRegistry Wiznet { get; } = new(
        "wiznet",
        [
            new(Bit(0), "On"),
            new(Bit(1), "Ticks"),
            new(Bit(2), "Logins"),
            new(Bit(3), "Sites"),
            new(Bit(4), "Links"),
            new(Bit(5), "Deaths"),
            new(Bit(6), "Resets"),
            new(Bit(7), "MobDeaths"),
            new(Bit(8), "Flags"),
            new(Bit(9), "Penalties"),
            new(Bit(10), "Saccing"),
            new(Bit(11), "Levels"),
            new(Bit(12), "Secure"),
            new(Bit(13), "Switches"),
            new(Bit(14), "Snoops"),
            new(Bit(15), "Restore"),
            new(Bit(16), "Load"),
            new(Bit(17), "Newbie"),
            new(Bit(18), "Prefix"),
            new(Bit(19), "Spam")
        ]);

    public static FlagRegistry Furniture { get; } = new(
        "furniture",
        [
            new(Bit(0), "StandAt"),
            new(Bit(1), "StandOn"),
            new(Bit(2), "StandIn")
        ]);

    public static FlagRegistry Gate { get; } = new(
        "gate",
        [
            new(Bit(0), "NormalExit"),
            new(Bit(1), "NoCurse"),
            new(Bit(2), "GoWith"),
            new(Bit(3), "Buggy"),
            new(Bit(4), "Random")
        ]);

    public static IReadOnlyList<FlagRegistry> All { get; } =
    [
        Act,
        PlayerAct,
        Affect,
        Offensive,
        Immunity,
        Form,
        Part,
        Room,
        Exit,
        Extra,
        Wear,
        Container,
        WeaponSpecial,
        Communication,
        Wiznet,
        Furniture,
        Gate
    ];

    private static int Bit(int index) => 1 << index;
}
