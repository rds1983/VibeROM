namespace VibeROM.Common.Rom;

public static partial class RomTables
{
    public static IReadOnlyList<GroupDefinition> Groups { get; } =
    [
        new("rom basics", [0, 0, 0, 0], ["scrolls", "staves", "wands", "recall"]),
        new("mage basics", [0, -1, -1, -1], ["dagger"]),
        new("cleric basics", [-1, 0, -1, -1], ["mace"]),
        new("thief basics", [-1, -1, 0, -1], ["dagger", "steal"]),
        new("warrior basics", [-1, -1, -1, 0], ["sword", "second attack"]),
        new("mage default", [40, -1, -1, -1], ["lore", "beguiling", "combat", "detection", "enhancement", "illusion", "maladictions", "protective", "transportation", "weather"]),
        new("cleric default", [-1, 40, -1, -1], ["flail", "attack", "creation", "curative", "benedictions", "detection", "healing", "maladictions", "protective", "shield block", "transportation", "weather"]),
        new("thief default", [-1, -1, 40, -1], ["mace", "sword", "backstab", "disarm", "dodge", "second attack", "trip", "hide", "peek", "pick lock", "sneak"]),
        new("warrior default", [-1, -1, -1, 40], ["weaponsmaster", "shield block", "bash", "disarm", "enhanced damage", "parry", "rescue", "third attack"]),
        new("weaponsmaster", [40, 40, 40, 20], ["axe", "dagger", "flail", "mace", "polearm", "spear", "sword", "whip"]),
        new("attack", [-1, 5, -1, 8], ["demonfire", "dispel evil", "dispel good", "earthquake", "flamestrike", "heat metal", "ray of truth"]),
        new("beguiling", [4, -1, 6, -1], ["calm", "charm person", "sleep"]),
        new("benedictions", [-1, 4, -1, 8], ["bless", "calm", "frenzy", "holy word", "remove curse"]),
        new("combat", [6, -1, 10, 9], ["acid blast", "burning hands", "chain lightning", "chill touch", "colour spray", "fireball", "lightning bolt", "magic missile", "shocking grasp"]),
        new("creation", [4, 4, 8, 8], ["continual light", "create food", "create spring", "create water", "create rose", "floating disc"]),
        new("curative", [-1, 4, -1, 8], ["cure blindness", "cure disease", "cure poison"]),
        new("detection", [4, 3, 6, -1], ["detect evil", "detect good", "detect hidden", "detect invis", "detect magic", "detect poison", "farsight", "identify", "know alignment", "locate object"]),
        new("draconian", [8, -1, -1, -1], ["acid breath", "fire breath", "frost breath", "gas breath", "lightning breath"]),
        new("enchantment", [6, -1, -1, -1], ["enchant armor", "enchant weapon", "fireproof", "recharge"]),
        new("enhancement", [5, -1, 9, 9], ["giant strength", "haste", "infravision", "refresh"]),
        new("harmful", [-1, 3, -1, 6], ["cause critical", "cause light", "cause serious", "harm"]),
        new("healing", [-1, 3, -1, 6], ["cure critical", "cure light", "cure serious", "heal", "mass healing", "refresh"]),
        new("illusion", [4, -1, 7, -1], ["invis", "mass invis", "ventriloquate"]),
        new("maladictions", [5, 5, 9, 9], ["blindness", "change sex", "curse", "energy drain", "plague", "poison", "slow", "weaken"]),
        new("protective", [4, 4, 7, 8], ["armor", "cancellation", "dispel magic", "fireproof", "protection evil", "protection good", "sanctuary", "shield", "stone skin"]),
        new("transportation", [4, 4, 8, 9], ["fly", "gate", "nexus", "pass door", "portal", "summon", "teleport", "word of recall"]),
        new("weather", [4, 4, 8, 8], ["call lightning", "control weather", "faerie fire", "faerie fog", "lightning bolt"]),
    ];
}
