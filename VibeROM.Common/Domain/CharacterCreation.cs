using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>
/// ROM 2.4b6 new-character rules, ported from <c>comm.c</c> (<c>nanny</c>,
/// <c>CON_GET_NEW_RACE</c> through <c>CON_READ_MOTD</c>), <c>skills.c</c>
/// (<c>group_add</c>/<c>gn_add</c>) and <c>act_wiz.c</c> (<c>do_outfit</c>).
/// </summary>
public static class CharacterCreation
{
    public const int SchoolRoomVnum = 3700;
    public const int SchoolMapVnum = 3162;
    public const int SchoolBannerVnum = 3716;
    public const int SchoolVestVnum = 3703;
    public const int SchoolShieldVnum = 3704;

    public const int WeaponProficiency = 40;
    public const int RecallProficiency = 50;

    private const int PrimeStatBonus = 3;
    private const int StartingHit = 20;
    private const int StartingMana = 100;
    private const int StartingMove = 100;
    private const int StartingTrain = 3;
    private const int StartingPractice = 5;

    /// <summary>Race names printed by the login prompt, in ROM table order.</summary>
    public static IReadOnlyList<string> RaceNames { get; } =
        RomTables.PcRaceDefinitions
            .Select(race => race.Race.ToString().ToLowerInvariant())
            .ToArray();

    /// <summary>Class names printed by the login prompt, in ROM table order.</summary>
    public static IReadOnlyList<string> ClassNames { get; } =
        RomTables.ClassDefinitions
            .Select(playerClass => playerClass.Class.ToString().ToLowerInvariant())
            .ToArray();

    public static PcRaceDefinition FindPcRace(Race race) =>
        RomTables.PcRaceDefinitions.FirstOrDefault(definition => definition.Race == race)
        ?? throw new ArgumentOutOfRangeException(nameof(race), race, "Race is not playable.");

    public static RaceDefinition FindRace(Race race) =>
        RomTables.RaceDefinitions.FirstOrDefault(definition => definition.Race == race)
        ?? throw new ArgumentOutOfRangeException(nameof(race), race, "Unknown race.");

    public static ClassDefinition FindClass(PlayerClass playerClass) =>
        RomTables.ClassDefinitions.FirstOrDefault(definition => definition.Class == playerClass)
        ?? throw new ArgumentOutOfRangeException(nameof(playerClass), playerClass, "Unknown class.");

    /// <summary>Prefix lookup matching ROM's <c>race_lookup</c>.</summary>
    public static bool TryLookupRace(string input, out Race race)
    {
        foreach (var definition in RomTables.PcRaceDefinitions)
        {
            var name = definition.Race.ToString();
            if (name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                race = definition.Race;
                return true;
            }
        }

        race = default;
        return false;
    }

    /// <summary>Prefix lookup matching ROM's <c>class_lookup</c>.</summary>
    public static bool TryLookupClass(string input, out PlayerClass playerClass)
    {
        foreach (var definition in RomTables.ClassDefinitions)
        {
            var name = definition.Class.ToString();
            if (name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                playerClass = definition.Class;
                return true;
            }
        }

        playerClass = default;
        return false;
    }

    /// <summary>Prefix lookup matching ROM's <c>weapon_lookup</c>.</summary>
    public static WeaponDefinition? FindWeapon(string input) =>
        RomTables.WeaponDefinitions.FirstOrDefault(
            weapon => weapon.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Applies the racial creation block from <c>CON_GET_NEW_RACE</c>: base
    /// stats, race affects, form/parts, size, creation points and bonus skills.
    /// </summary>
    public static void ApplyRace(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Player is null)
        {
            throw new ArgumentException("Only player characters can be created.", nameof(character));
        }

        var pcRace = FindPcRace(character.Race);
        var race = FindRace(character.Race);

        for (var index = 0; index < pcRace.BaseStats.Count; index++)
        {
            character.SetPermanentStat(index, pcRace.BaseStats[index]);
            character.SetModifiedStat(index, pcRace.BaseStats[index]);
        }

        character.AffectedBy |= race.AffectMask;
        character.Immunities |= race.ImmunityMask;
        character.Resistances |= race.ResistanceMask;
        character.Vulnerabilities |= race.VulnerabilityMask;
        character.Form = race.FormMask;
        character.Parts = race.PartsMask;
        character.Size = pcRace.Size;
        character.Player.Points = pcRace.Points;

        foreach (var bonus in pcRace.BonusSkills)
        {
            GrantSkillOrGroup(character, bonus);
        }
    }

    /// <summary>
    /// Grants <c>rom basics</c>, the class base group and the class default
    /// group, then sets recall to 50 (<c>CON_GET_ALIGNMENT</c> and the
    /// customize-No branch).
    /// </summary>
    public static void ApplyDefaultPackage(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Class is not { } playerClass)
        {
            throw new ArgumentException("A player character must have a class.", nameof(character));
        }

        var definition = FindClass(playerClass);
        GrantSkillOrGroup(character, "rom basics");
        GrantSkillOrGroup(character, definition.BaseGroup);
        GrantSkillOrGroup(character, definition.DefaultGroup);

        if (character.Player is null)
        {
            throw new ArgumentException("Only player characters can be created.", nameof(character));
        }

        character.Player.SetSkill("recall", RecallProficiency);
    }

    /// <summary>
    /// Adds a skill or recursively a group without deducting creation points
    /// (ROM <c>group_add(ch, name, FALSE)</c>).
    /// </summary>
    public static void GrantSkillOrGroup(Character character, string name)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Player is null)
        {
            throw new ArgumentException("Only player characters have skills.", nameof(character));
        }

        if (RomTables.FindSkill(name) is { } skill)
        {
            if (character.Player.GetSkill(skill.Name) == 0)
            {
                character.Player.SetSkill(skill.Name, 1);
            }

            return;
        }

        var group = RomTables.FindGroup(name)
            ?? throw new InvalidOperationException($"Unknown ROM skill or group '{name}'.");

        if (!character.Player.KnownGroups.Add(group.Name))
        {
            return;
        }

        foreach (var entry in group.Spells)
        {
            GrantSkillOrGroup(character, entry);
        }
    }

    /// <summary>Weapons the character may choose, in ROM <c>weapon_table</c> order.</summary>
    public static IReadOnlyList<WeaponDefinition> AvailableWeapons(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Player is null)
        {
            return [];
        }

        return RomTables.WeaponDefinitions
            .Where(weapon => character.Player.GetSkill(weapon.SkillName) > 0)
            .ToArray();
    }

    /// <summary>
    /// The weapon choices a default (non-customized) character of this race and
    /// class will have. Used to build the login prompt before the character is
    /// registered in the world.
    /// </summary>
    public static IReadOnlyList<WeaponDefinition> DefaultWeaponChoices(
        Race race,
        PlayerClass playerClass)
    {
        var preview = new Character(new PlayerData(), "Preview", race, playerClass, Sex.Neutral);
        ApplyRace(preview);
        ApplyDefaultPackage(preview);
        return AvailableWeapons(preview);
    }

    /// <summary>Validates a weapon choice and sets its skill to 40 (<c>CON_PICK_WEAPON</c>).</summary>
    public static bool TryChooseWeapon(
        Character character,
        string input,
        out WeaponDefinition weapon)
    {
        weapon = null!;
        var candidate = FindWeapon(input);
        if (candidate is null || character.Player is null)
        {
            return false;
        }

        if (AvailableWeapons(character).All(choice => choice.Name != candidate.Name))
        {
            return false;
        }

        character.Player.SetSkill(candidate.SkillName, WeaponProficiency);
        weapon = candidate;
        return true;
    }

    /// <summary>
    /// Finalises the character the way <c>CON_READ_MOTD</c> does: prime
    /// attribute bonus, level/experience, vitals, train/practice, title,
    /// outfit, map and starting room.
    /// </summary>
    public static void Complete(World world, Character character, int startingRoomVnum)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(character);
        if (character.Player is null)
        {
            throw new ArgumentException("Only player characters can be created.", nameof(character));
        }

        if (character.Class is not { } playerClass)
        {
            throw new ArgumentException("A player character must have a class.", nameof(character));
        }

        var definition = FindClass(playerClass);
        var prime = PrimeStatIndex(definition);
        character.SetPermanentStat(prime, character.GetPermanentStat(prime) + PrimeStatBonus);
        character.SetModifiedStat(prime, character.GetPermanentStat(prime));

        character.Level = 1;
        character.Experience = ExpPerLevel(character, character.Player.Points);
        character.MaxHit = StartingHit;
        character.Hit = StartingHit;
        character.MaxMana = StartingMana;
        character.Mana = StartingMana;
        character.MaxMove = StartingMove;
        character.Move = StartingMove;
        character.Armor = new ArmorValues(100, 100, 100, 100);
        character.Train = StartingTrain;
        character.Practice = StartingPractice;
        character.Player.Title = $"the {RomTables.GetTitle(playerClass, 1, character.Sex)}";

        EquipStartingGear(world, character);
        AddSchoolMap(world, character);

        if (!world.TryGetRoom(startingRoomVnum, out var room))
        {
            throw new WorldDataException(
                [$"Starting room vnum {startingRoomVnum} was not found."]);
        }

        world.MoveCharacterToRoom(character, room);
    }

    /// <summary>ROM <c>exp_per_level()</c> from <c>skills.c</c>.</summary>
    public static int ExpPerLevel(Character character, int points)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Class is not { } playerClass)
        {
            throw new ArgumentException("A player character must have a class.", nameof(character));
        }

        var classMultiplier = FindPcRace(character.Race).ClassMultipliers[(int)playerClass];
        var experience = 1000;
        var increment = 500;

        if (points < 40)
        {
            return 1000 * (classMultiplier / 100 > 0 ? classMultiplier / 100 : 1);
        }

        points -= 40;
        while (points > 9)
        {
            experience += increment;
            points -= 10;
            if (points > 9)
            {
                experience += increment;
                increment *= 2;
                points -= 10;
            }
        }

        experience += points * increment / 10;
        return experience * classMultiplier / 100;
    }

    private static int PrimeStatIndex(ClassDefinition definition) =>
        definition.PrimeAttribute switch
        {
            "Strength" => 0,
            "Dexterity" => 1,
            "Intelligence" => 2,
            "Wisdom" => 3,
            "Constitution" => 4,
            _ => throw new InvalidOperationException(
                $"Unknown prime attribute '{definition.PrimeAttribute}'.")
        };

    private static void EquipStartingGear(World world, Character character)
    {
        EquipSchoolItem(world, character, SchoolBannerVnum, WearSlot.Light);
        EquipSchoolItem(world, character, SchoolVestVnum, WearSlot.Body);

        var weapon = BestWeapon(character);
        var wielded = EquipSchoolItem(world, character, weapon.Vnum, WearSlot.Wield);
        if (!IsTwoHanded(wielded))
        {
            EquipSchoolItem(world, character, SchoolShieldVnum, WearSlot.Shield);
        }
    }

    private static WeaponDefinition BestWeapon(Character character)
    {
        var best = RomTables.WeaponDefinitions[0];
        var learned = Learned(character, best);

        for (var index = 1; index < RomTables.WeaponDefinitions.Count; index++)
        {
            var candidate = RomTables.WeaponDefinitions[index];
            var candidateLearned = Learned(character, candidate);
            if (learned < candidateLearned)
            {
                best = candidate;
                learned = candidateLearned;
            }
        }

        return best;
    }

    private static int Learned(Character character, WeaponDefinition weapon) =>
        character.Player?.GetSkill(weapon.SkillName) ?? 0;

    private static bool IsTwoHanded(GameObject obj) =>
        obj.Data is WeaponData weapon
        && (weapon.SpecialFlags & RomFlags.WeaponSpecial.Get("TwoHands").Bit) != 0;

    private static GameObject EquipSchoolItem(
        World world,
        Character character,
        int vnum,
        WearSlot slot)
    {
        if (!world.ObjectPrototypes.TryGetValue(vnum, out var prototype))
        {
            throw new WorldDataException(
                [$"Starting outfit object vnum {vnum} was not found."]);
        }

        var obj = world.CreateObject(prototype, carrier: character);
        obj.CostCopper = 0;
        world.EquipObject(character, obj, slot);
        return obj;
    }

    private static void AddSchoolMap(World world, Character character)
    {
        if (!world.ObjectPrototypes.TryGetValue(SchoolMapVnum, out var prototype))
        {
            throw new WorldDataException(
                [$"Starting outfit object vnum {SchoolMapVnum} was not found."]);
        }

        world.CreateObject(prototype, carrier: character);
    }
}
