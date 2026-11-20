namespace VibeROM.Common.Rom;

/// <summary>
/// Canonical ROM value tables. The generated files contain races, skills,
/// groups and titles; this file adds the remaining lookup tables and validation.
/// </summary>
public static partial class RomTables
{
    private static readonly Lazy<ValueTable> RacesLazy = new(() =>
        new ValueTable(
            "race",
            RaceDefinitions!.Select(definition => new ValueDefinition((int)definition.Race, definition.Name))));

    private static readonly Lazy<ValueTable> PcRacesLazy = new(() =>
        new ValueTable(
            "pcRace",
            PcRaceDefinitions!.Select(definition => new ValueDefinition((int)definition.Race, definition.Race.ToString()))));

    private static readonly Lazy<ValueTable> LiquidsLazy = new(() =>
        new ValueTable(
            "liquid",
            LiquidDefinitions!.Select(definition => new ValueDefinition(definition.Value, definition.Name))));

    private static readonly Lazy<ValueTable> AttackTypesLazy = new(() =>
        new ValueTable(
            "attackType",
            AttackTypeDefinitions!.Select(definition => new ValueDefinition(definition.Value, definition.Name))));

    private static readonly Lazy<ValueTable> SpecialsLazy = new(() =>
        new ValueTable(
            "specialProcedure",
            SpecialDefinitions!.Select(definition => new ValueDefinition(definition.Value, definition.Name))));

    public static ValueTable Races => RacesLazy.Value;

    public static ValueTable PcRaces => PcRacesLazy.Value;

    public static ValueTable Classes { get; } = ValueTable.FromEnum<PlayerClass>("class");

    public static ValueTable Sexes { get; } = ValueTable.FromEnum<Sex>("sex");

    public static ValueTable Positions { get; } = ValueTable.FromEnum<Position>("position");

    public static ValueTable Sizes { get; } = ValueTable.FromEnum<Size>("size");

    public static ValueTable Sectors { get; } = ValueTable.FromEnum<Sector>("sector");

    public static ValueTable ItemTypes { get; } = ValueTable.FromEnum<ItemType>("itemType");

    public static ValueTable WeaponClasses { get; } = ValueTable.FromEnum<WeaponClass>("weaponClass");

    public static ValueTable ApplyLocations { get; } = ValueTable.FromEnum<ApplyLocation>("applyLocation");

    public static ValueTable SkillTargets { get; } = ValueTable.FromEnum<SkillTarget>("skillTarget");

    public static ValueTable DamageTypes { get; } = new(
        "damageType",
        [
            new(0, "None"),
            new(1, "Bash"),
            new(2, "Pierce"),
            new(3, "Slash"),
            new(4, "Fire"),
            new(5, "Cold"),
            new(6, "Lightning"),
            new(7, "Acid"),
            new(8, "Poison"),
            new(9, "Negative"),
            new(10, "Holy"),
            new(11, "Energy"),
            new(12, "Mental"),
            new(13, "Disease"),
            new(14, "Drowning"),
            new(15, "Light"),
            new(16, "Other"),
            new(17, "Harm"),
            new(18, "Charm"),
            new(19, "Sound")
        ]);

    public static ValueTable Liquids => LiquidsLazy.Value;

    public static ValueTable AttackTypes => AttackTypesLazy.Value;

    public static ValueTable Specials => SpecialsLazy.Value;

    public static IReadOnlyList<ClassDefinition> ClassDefinitions { get; } =
    [
        new(PlayerClass.Mage, "Mag", "Intelligence", 3701, [3018, 9618], 75, 20, 6, 6, 8, true, "mage basics", "mage default"),
        new(PlayerClass.Cleric, "Cle", "Wisdom", 3700, [3003, 9619], 75, 20, 2, 7, 10, true, "cleric basics", "cleric default"),
        new(PlayerClass.Thief, "Thi", "Dexterity", 3701, [3028, 9639], 75, 20, -4, 8, 13, false, "thief basics", "thief default"),
        new(PlayerClass.Warrior, "War", "Strength", 3702, [3022, 9633], 75, 20, -10, 11, 15, false, "warrior basics", "warrior default")
    ];

    public static IReadOnlyList<PcRaceDefinition> PcRaceDefinitions { get; } =
    [
        new(Race.Human, "Human", 0, [100, 100, 100, 100], [], [13, 13, 13, 13, 13], [18, 18, 18, 18, 18], Size.Medium),
        new(Race.Elf, " Elf ", 5, [100, 125, 100, 120], ["sneak", "hide"], [12, 14, 13, 15, 11], [16, 20, 18, 21, 15], Size.Small),
        new(Race.Dwarf, "Dwarf", 8, [150, 100, 125, 100], ["berserk"], [14, 12, 14, 10, 15], [20, 16, 19, 14, 21], Size.Medium),
        new(Race.Giant, "Giant", 6, [200, 150, 150, 105], ["bash", "fast healing"], [16, 11, 13, 11, 14], [22, 15, 18, 15, 20], Size.Large)
    ];

    public static IReadOnlyList<WeaponDefinition> WeaponDefinitions { get; } =
    [
        new("sword", 3702, WeaponClass.Sword, "sword"),
        new("mace", 3700, WeaponClass.Mace, "mace"),
        new("dagger", 3701, WeaponClass.Dagger, "dagger"),
        new("axe", 3719, WeaponClass.Axe, "axe"),
        new("staff", 3718, WeaponClass.Spear, "spear"),
        new("flail", 3720, WeaponClass.Flail, "flail"),
        new("whip", 3721, WeaponClass.Whip, "whip"),
        new("polearm", 3722, WeaponClass.Polearm, "polearm")
    ];

    public static IReadOnlyList<LiquidDefinition> LiquidDefinitions { get; } =
    [
        new(0, "Water", "clear", 0, 1, 10, 0, 16),
        new(1, "Beer", "amber", 12, 1, 8, 1, 12),
        new(2, "RedWine", "burgundy", 30, 1, 8, 1, 5),
        new(3, "Ale", "brown", 15, 1, 8, 1, 12),
        new(4, "DarkAle", "dark", 16, 1, 8, 1, 12),
        new(5, "Whisky", "golden", 120, 1, 5, 0, 2),
        new(6, "Lemonade", "pink", 0, 1, 9, 2, 12),
        new(7, "Firebreather", "boiling", 190, 0, 4, 0, 2),
        new(8, "LocalSpecialty", "clear", 151, 1, 3, 0, 2),
        new(9, "SlimeMoldJuice", "green", 0, 2, -8, 1, 2),
        new(10, "Milk", "white", 0, 2, 9, 3, 12),
        new(11, "Tea", "tan", 0, 1, 8, 0, 6),
        new(12, "Coffee", "black", 0, 1, 8, 0, 6),
        new(13, "Blood", "red", 0, 2, -1, 2, 6),
        new(14, "SaltWater", "clear", 0, 1, -2, 0, 1),
        new(15, "Coke", "brown", 0, 2, 9, 2, 12),
        new(16, "RootBeer", "brown", 0, 2, 9, 2, 12),
        new(17, "ElvishWine", "green", 35, 2, 8, 1, 5),
        new(18, "WhiteWine", "golden", 28, 1, 8, 1, 5),
        new(19, "Champagne", "golden", 32, 1, 8, 1, 5),
        new(20, "Mead", "honey-colored", 34, 2, 8, 2, 12),
        new(21, "RoseWine", "pink", 26, 1, 8, 1, 5),
        new(22, "BenedictineWine", "burgundy", 40, 1, 8, 1, 5),
        new(23, "Vodka", "clear", 130, 1, 5, 0, 2),
        new(24, "CranberryJuice", "red", 0, 1, 9, 2, 12),
        new(25, "OrangeJuice", "orange", 0, 2, 9, 3, 12),
        new(26, "Absinthe", "green", 200, 1, 4, 0, 2),
        new(27, "Brandy", "golden", 80, 1, 5, 0, 4),
        new(28, "Aquavit", "clear", 140, 1, 5, 0, 2),
        new(29, "Schnapps", "clear", 90, 1, 5, 0, 2),
        new(30, "Icewine", "purple", 50, 2, 6, 1, 5),
        new(31, "Amontillado", "burgundy", 35, 2, 8, 1, 5),
        new(32, "Sherry", "red", 38, 2, 7, 1, 5),
        new(33, "Framboise", "red", 50, 1, 7, 1, 5),
        new(34, "Rum", "amber", 151, 1, 4, 0, 2),
        new(35, "Cordial", "clear", 100, 1, 5, 0, 2)
    ];

    public static IReadOnlyList<AttackTypeDefinition> AttackTypeDefinitions { get; } =
    [
        new(0, "Hit", "hit", "None"),
        new(1, "Slice", "slice", "Slash"),
        new(2, "Stab", "stab", "Pierce"),
        new(3, "Slash", "slash", "Slash"),
        new(4, "Whip", "whip", "Slash"),
        new(5, "Claw", "claw", "Slash"),
        new(6, "Blast", "blast", "Bash"),
        new(7, "Pound", "pound", "Bash"),
        new(8, "Crush", "crush", "Bash"),
        new(9, "Grep", "grep", "Slash"),
        new(10, "Bite", "bite", "Pierce"),
        new(11, "Pierce", "pierce", "Pierce"),
        new(12, "Suction", "suction", "Bash"),
        new(13, "Beating", "beating", "Bash"),
        new(14, "Digestion", "digestion", "Acid"),
        new(15, "Charge", "charge", "Bash"),
        new(16, "Slap", "slap", "Bash"),
        new(17, "Punch", "punch", "Bash"),
        new(18, "Wrath", "wrath", "Energy"),
        new(19, "Magic", "magic", "Energy"),
        new(20, "DivinePower", "divine power", "Holy"),
        new(21, "Cleave", "cleave", "Slash"),
        new(22, "Scratch", "scratch", "Pierce"),
        new(23, "PeckPierce", "peck", "Pierce"),
        new(24, "PeckBash", "peck", "Bash"),
        new(25, "Chop", "chop", "Slash"),
        new(26, "Sting", "sting", "Pierce"),
        new(27, "Smash", "smash", "Bash"),
        new(28, "ShockingBite", "shocking bite", "Lightning"),
        new(29, "FlamingBite", "flaming bite", "Fire"),
        new(30, "FreezingBite", "freezing bite", "Cold"),
        new(31, "AcidicBite", "acidic bite", "Acid"),
        new(32, "Chomp", "chomp", "Pierce"),
        new(33, "Drain", "life drain", "Negative"),
        new(34, "Thrust", "thrust", "Pierce"),
        new(35, "Slime", "slime", "Acid"),
        new(36, "Shock", "shock", "Lightning"),
        new(37, "Thwack", "thwack", "Bash"),
        new(38, "Flame", "flame", "Fire"),
        new(39, "Chill", "chill", "Cold")
    ];

    public static IReadOnlyList<WiznetDefinition> WiznetDefinitions { get; } =
    [
        new("On", 1 << 0, 52),
        new("Prefix", 1 << 18, 52),
        new("Ticks", 1 << 1, 52),
        new("Logins", 1 << 2, 52),
        new("Sites", 1 << 3, 56),
        new("Links", 1 << 4, 53),
        new("Newbie", 1 << 17, 52),
        new("Spam", 1 << 19, 55),
        new("Deaths", 1 << 5, 52),
        new("Resets", 1 << 6, 56),
        new("MobDeaths", 1 << 7, 56),
        new("Flags", 1 << 8, 55),
        new("Penalties", 1 << 9, 55),
        new("Saccing", 1 << 10, 55),
        new("Levels", 1 << 11, 52),
        new("Load", 1 << 16, 58),
        new("Restore", 1 << 15, 58),
        new("Snoops", 1 << 14, 58),
        new("Switches", 1 << 13, 58),
        new("Secure", 1 << 12, 59)
    ];

    public static IReadOnlyList<SpecialDefinition> SpecialDefinitions { get; } =
    [
        new(0, "BreathAny", "spec_breath_any"),
        new(1, "BreathAcid", "spec_breath_acid"),
        new(2, "BreathFire", "spec_breath_fire"),
        new(3, "BreathFrost", "spec_breath_frost"),
        new(4, "BreathGas", "spec_breath_gas"),
        new(5, "BreathLightning", "spec_breath_lightning"),
        new(6, "CastAdept", "spec_cast_adept"),
        new(7, "CastCleric", "spec_cast_cleric"),
        new(8, "CastJudge", "spec_cast_judge"),
        new(9, "CastMage", "spec_cast_mage"),
        new(10, "CastUndead", "spec_cast_undead"),
        new(11, "Executioner", "spec_executioner"),
        new(12, "Fido", "spec_fido"),
        new(13, "Guard", "spec_guard"),
        new(14, "Janitor", "spec_janitor"),
        new(15, "Mayor", "spec_mayor"),
        new(16, "Poison", "spec_poison"),
        new(17, "Thief", "spec_thief"),
        new(18, "Nasty", "spec_nasty"),
        new(19, "TrollMember", "spec_troll_member"),
        new(20, "OgreMember", "spec_ogre_member"),
        new(21, "Patrolman", "spec_patrolman")
    ];

    /// <summary>Prefix lookup matching ROM's <c>skill_lookup</c> table order.</summary>
    public static SkillDefinition? FindSkill(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return Skills.FirstOrDefault(
            skill => skill.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Prefix lookup matching ROM's <c>group_lookup</c> table order.</summary>
    public static GroupDefinition? FindGroup(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return Groups.FirstOrDefault(
            group => group.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetTitle(PlayerClass playerClass, int level, Sex sex)
    {
        var classIndex = (int)playerClass;
        if (classIndex is < 0 or >= 4)
        {
            throw new ArgumentOutOfRangeException(nameof(playerClass));
        }

        var levelIndex = Math.Clamp(level, 0, 60);
        var sexIndex = sex == Sex.Female ? 1 : 0;
        return TitleNames[classIndex][levelIndex][sexIndex];
    }

    public static IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        AddCountError(errors, "races", RaceDefinitions.Count, 29);
        AddCountError(errors, "classes", ClassDefinitions.Count, 4);
        AddCountError(errors, "pc races", PcRaceDefinitions.Count, 4);
        AddCountError(errors, "weapons", WeaponDefinitions.Count, 8);
        AddCountError(errors, "liquids", LiquidDefinitions.Count, 36);
        AddCountError(errors, "attack types", AttackTypeDefinitions.Count, 40);
        AddCountError(errors, "wiznet", WiznetDefinitions.Count, 20);
        AddCountError(errors, "specials", SpecialDefinitions.Count, 22);
        AddCountError(errors, "skills", Skills.Count, 135);
        AddCountError(errors, "groups", Groups.Count, 27);

        var raceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var race in RaceDefinitions)
        {
            if (!raceNames.Add(race.Name))
            {
                errors.Add($"Duplicate race name '{race.Name}'.");
            }
        }

        var skillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in Skills)
        {
            if (!skillNames.Add(skill.Name))
            {
                errors.Add($"Duplicate skill name '{skill.Name}'.");
            }

            if (skill.Levels.Count != 4 || skill.Ratings.Count != 4)
            {
                errors.Add($"Skill '{skill.Name}' must have four class levels and ratings.");
            }

            if (!Enum.IsDefined(skill.Target))
            {
                errors.Add($"Skill '{skill.Name}' has an invalid target.");
            }

            if (!Enum.IsDefined(skill.MinimumPosition))
            {
                errors.Add($"Skill '{skill.Name}' has an invalid minimum position.");
            }
        }

        var groupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in Groups)
        {
            if (!groupNames.Add(group.Name))
            {
                errors.Add($"Duplicate group name '{group.Name}'.");
            }

            if (group.Ratings.Count != 4)
            {
                errors.Add($"Group '{group.Name}' must have four class ratings.");
            }
        }

        foreach (var group in Groups)
        {
            foreach (var spell in group.Spells)
            {
                if (FindSkill(spell) is null && FindGroup(spell) is null)
                {
                    errors.Add($"Group '{group.Name}' references unknown skill/group '{spell}'.");
                }
            }
        }

        foreach (var cls in ClassDefinitions)
        {
            if (!groupNames.Contains(cls.BaseGroup))
            {
                errors.Add($"Class '{cls.Class}' references unknown base group '{cls.BaseGroup}'.");
            }

            if (!groupNames.Contains(cls.DefaultGroup))
            {
                errors.Add($"Class '{cls.Class}' references unknown default group '{cls.DefaultGroup}'.");
            }
        }

        foreach (var pcRace in PcRaceDefinitions)
        {
            if (!RaceDefinitions.Any(race => race.Race == pcRace.Race))
            {
                errors.Add($"PC race '{pcRace.Race}' is not present in the race table.");
            }

            if (pcRace.ClassMultipliers.Count != 4)
            {
                errors.Add($"PC race '{pcRace.Race}' must have four class multipliers.");
            }

            if (pcRace.BaseStats.Count != 5 || pcRace.MaxStats.Count != 5)
            {
                errors.Add($"PC race '{pcRace.Race}' must have five base and maximum stats.");
            }

            foreach (var skill in pcRace.BonusSkills)
            {
                if (!skillNames.Contains(skill))
                {
                    errors.Add($"PC race '{pcRace.Race}' references unknown skill '{skill}'.");
                }
            }
        }

        foreach (var weapon in WeaponDefinitions)
        {
            if (!skillNames.Contains(weapon.SkillName))
            {
                errors.Add($"Weapon '{weapon.Name}' references unknown skill '{weapon.SkillName}'.");
            }

            if (!Enum.IsDefined(weapon.Class))
            {
                errors.Add($"Weapon '{weapon.Name}' has an invalid class.");
            }
        }

        foreach (var wiznet in WiznetDefinitions)
        {
            if (!RomFlags.Wiznet.TryGet(wiznet.Name, out var flag))
            {
                errors.Add($"Wiznet '{wiznet.Name}' has no flag definition.");
            }
            else if (flag.Bit != wiznet.Bit)
            {
                errors.Add($"Wiznet '{wiznet.Name}' bit 0x{wiznet.Bit:X} does not match 0x{flag.Bit:X}.");
            }
        }

        if (SpecialDefinitions.Select(definition => definition.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != SpecialDefinitions.Count)
        {
            errors.Add("Special procedure names are not unique.");
        }

        if (TitleNames.Length != 4 || TitleNames.Any(levels => levels.Length != 61) ||
            TitleNames.Any(levels => levels.Any(sexes => sexes.Length != 2)))
        {
            errors.Add("Title table must be 4 classes x 61 levels x 2 sexes.");
        }

        foreach (var registry in RomFlags.All)
        {
            if (registry.PersistedNames.Count == 0)
            {
                errors.Add($"Flag registry '{registry.Category}' has no persisted names.");
            }
        }

        return errors;
    }

    public static void ValidateOrThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "ROM table validation failed:" + Environment.NewLine + "- " +
                string.Join($"{Environment.NewLine}- ", errors));
        }
    }

    private static void AddCountError(ICollection<string> errors, string name, int actual, int expected)
    {
        if (actual != expected)
        {
            errors.Add($"{name} count is {actual}, expected {expected}.");
        }
    }
}
