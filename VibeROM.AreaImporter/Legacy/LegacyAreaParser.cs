using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.AreaImporter.Legacy;

public sealed record LegacyParseWarning(string Message, LegacyPosition Position);

public enum LegacyReferenceTarget
{
    Mobile,
    Object,
    Room
}

/// <summary>A vnum used by a parsed entity, with the position that named it.</summary>
public sealed record LegacyReference(
    string SourceFile,
    LegacyPosition Position,
    LegacyReferenceTarget Target,
    int Vnum,
    string Context);

public sealed record LegacyAreaParseResult(
    AreaData Area,
    IReadOnlyList<LegacyParseWarning> Warnings,
    IReadOnlyList<LegacyReference> References);

/// <summary>Parses one ROM 2.4b6 area file into the shared domain model.</summary>
public sealed class LegacyAreaParser
{
    public LegacyAreaParseResult Parse(string text, string fileName)
    {
        var reader = new LegacyAreaReader(text, fileName);
        var builder = new AreaBuilder(fileName);
        var warnings = new List<LegacyParseWarning>();

        while (!reader.AtEnd)
        {
            if (reader.PeekNonWhitespaceChar() is null)
            {
                break;
            }

            var marker = reader.ReadLetter();
            if (marker != '#')
            {
                throw reader.CreateError("Expected '#' at the start of a section.");
            }

            var section = reader.ReadWord();
            switch (section.ToUpperInvariant())
            {
                case "AREA":
                    ParseArea(reader, builder, warnings);
                    break;
                case "AREADATA":
                    ParseAreaData(reader, builder);
                    break;
                case "HELPS":
                    ParseHelps(reader, builder);
                    break;
                case "MOBILES":
                    ParseMobiles(reader, builder, warnings, oldFormat: false);
                    break;
                case "MOBOLD":
                    ParseMobiles(reader, builder, warnings, oldFormat: true);
                    break;
                case "OBJECTS":
                    ParseObjects(reader, builder, warnings, oldFormat: false);
                    break;
                case "OBJOLD":
                    ParseObjects(reader, builder, warnings, oldFormat: true);
                    break;
                case "ROOMS":
                    ParseRooms(reader, builder, warnings);
                    break;
                case "RESETS":
                    ParseResets(reader, builder);
                    break;
                case "SHOPS":
                    ParseShops(reader, builder);
                    break;
                case "SPECIALS":
                    ParseSpecials(reader, builder);
                    break;
                case "SOCIALS":
                    ParseSocials(reader, builder);
                    break;
                case "$":
                    goto Done;
                default:
                    throw reader.CreateError($"Unknown section '#{section}'.");
            }
        }

    Done:
        if (!builder.HasHeader)
        {
            builder.Name = DefaultName(fileName);
            builder.LegacyFile = Path.GetFileName(fileName);
        }

        return new LegacyAreaParseResult(builder.Build(), warnings, builder.References);
    }

    /// <summary>
    /// Checks references across a whole import (vnums may point into another
    /// area) and returns one warning per missing target with its source
    /// position. Call after all files have been parsed.
    /// </summary>
    public static IReadOnlyList<LegacyParseWarning> ValidateReferences(
        IEnumerable<LegacyAreaParseResult> results)
    {
        var parsed = results.ToList();
        var mobiles = parsed.SelectMany(result => result.Area.Mobiles).Select(mobile => mobile.Vnum).ToHashSet();
        var objects = parsed.SelectMany(result => result.Area.Objects).Select(obj => obj.Vnum).ToHashSet();
        var rooms = parsed.SelectMany(result => result.Area.Rooms).Select(room => room.Vnum).ToHashSet();
        var warnings = new List<LegacyParseWarning>();

        foreach (var reference in parsed.SelectMany(result => result.References))
        {
            var exists = reference.Target switch
            {
                LegacyReferenceTarget.Mobile => mobiles.Contains(reference.Vnum),
                LegacyReferenceTarget.Object => objects.Contains(reference.Vnum),
                LegacyReferenceTarget.Room => rooms.Contains(reference.Vnum),
                _ => true
            };

            if (!exists)
            {
                warnings.Add(new LegacyParseWarning(
                    $"{Path.GetFileName(reference.SourceFile)}: {reference.Context} references missing " +
                    $"{reference.Target.ToString().ToLowerInvariant()} vnum {reference.Vnum}.",
                    reference.Position));
            }
        }

        return warnings;
    }

    private static void ParseArea(
        LegacyAreaReader reader,
        AreaBuilder builder,
        ICollection<LegacyParseWarning> warnings)
    {
        var legacyName = reader.ReadString();
        var name = reader.ReadString();
        var credits = reader.ReadString();

        builder.Name = string.IsNullOrWhiteSpace(name) ? DefaultName(reader.FileName) : name;
        builder.Credits = credits;
        builder.LegacyFile = string.IsNullOrWhiteSpace(legacyName)
            ? Path.GetFileName(reader.FileName)
            : Path.GetFileName(legacyName);
        builder.HasHeader = true;

        try
        {
            builder.LowVnum = reader.ReadNumber();
            builder.HighVnum = reader.ReadNumber();
        }
        catch (LegacyFormatException exception)
        {
            warnings.Add(new LegacyParseWarning(
                "Area vnum range is not numeric; using 0 0.",
                exception.Position));
            builder.LowVnum = 0;
            builder.HighVnum = 0;
            reader.ReadToEndOfLine();
        }
    }

    private static void ParseAreaData(LegacyAreaReader reader, AreaBuilder builder)
    {
        builder.HasHeader = true;
        builder.LegacyFile = Path.GetFileName(reader.FileName);

        while (!reader.AtEnd)
        {
            var key = reader.ReadWord();
            switch (key.ToUpperInvariant())
            {
                case "NAME":
                    builder.Name = reader.ReadString();
                    break;
                case "BUILDERS":
                    builder.Builders = reader.ReadString();
                    break;
                case "VNUMS":
                    builder.LowVnum = reader.ReadNumber();
                    builder.HighVnum = reader.ReadNumber();
                    break;
                case "SECURITY":
                    builder.Security = reader.ReadNumber();
                    break;
                case "CREDITS":
                    builder.Credits = reader.ReadString();
                    break;
                case "END":
                    return;
                default:
                    throw reader.CreateError($"Unknown #AREADATA field '{key}'.");
            }
        }
    }

    private static void ParseHelps(LegacyAreaReader reader, AreaBuilder builder)
    {
        while (true)
        {
            var level = reader.ReadNumber();
            var keywords = reader.ReadString();
            if (keywords.StartsWith('$'))
            {
                return;
            }

            var text = reader.ReadString();
            builder.Helps.Add(new HelpEntry(keywords, level, text));
        }
    }

    private static void ParseMobiles(
        LegacyAreaReader reader,
        AreaBuilder builder,
        ICollection<LegacyParseWarning> warnings,
        bool oldFormat)
    {
        while (true)
        {
            var marker = reader.ReadLetter();
            if (marker != '#')
            {
                throw reader.CreateError("Expected '#' before a mobile vnum.");
            }

            var vnum = reader.ReadNumber();
            if (vnum == 0)
            {
                return;
            }

            if (!builder.MobileVnums.Add(vnum))
            {
                throw reader.CreateError($"Duplicate mobile vnum {vnum}.");
            }

            builder.Mobiles.Add(oldFormat
                ? ParseOldMobile(reader, vnum, warnings)
                : ParseNewMobile(reader, vnum, warnings));
        }
    }

    private static MobilePrototype ParseNewMobile(
        LegacyAreaReader reader,
        int vnum,
        ICollection<LegacyParseWarning> warnings)
    {
        var keywords = reader.ReadString();
        var shortDescription = reader.ReadString();
        var longDescription = UpperFirst(reader.ReadString());
        var description = UpperFirst(reader.ReadString());
        var race = LegacyLookup.ToRace(reader.ReadString(), reader);
        var actFlags = ReadMask(reader, RomFlags.Act, "actFlags", warnings);
        var affectedBy = ReadMask(reader, RomFlags.Affect, "affectedBy", warnings);
        var alignment = reader.ReadNumber();
        var group = reader.ReadNumber();
        var level = reader.ReadNumber();
        var hitRoll = reader.ReadNumber();
        var hitDice = ReadDice(reader);
        var manaDice = ReadDice(reader);
        var damageDice = ReadDice(reader);
        var attackType = LegacyLookup.ToAttackType(reader.ReadWord(), reader);
        // ROM stores mobile AC internally at ten times the area-file value;
        // JSON keeps the area-file value and the runtime model stores the
        // internal value (see DataFormats.md).
        var armor = new ArmorValues(
            reader.ReadNumber() * 10,
            reader.ReadNumber() * 10,
            reader.ReadNumber() * 10,
            reader.ReadNumber() * 10);
        var offensiveFlags = ReadMask(reader, RomFlags.Offensive, "offensiveFlags", warnings);
        var immunities = ReadMask(reader, RomFlags.Immunity, "immunities", warnings);
        var resistances = ReadMask(reader, RomFlags.Immunity, "resistances", warnings);
        var vulnerabilities = ReadMask(reader, RomFlags.Immunity, "vulnerabilities", warnings);
        var startPosition = LegacyLookup.ToPosition(reader.ReadWord(), reader);
        var defaultPosition = LegacyLookup.ToPosition(reader.ReadWord(), reader);
        var sex = LegacyLookup.ToSex(reader.ReadWord(), reader);
        var gold = reader.ReadNumber();
        var form = ReadMask(reader, RomFlags.Form, "form", warnings);
        var parts = ReadMask(reader, RomFlags.Part, "parts", warnings);
        var size = LegacyLookup.ToSize(reader.ReadWord(), reader);
        var material = reader.ReadWord();

        while (reader.PeekNonWhitespaceChar() == 'F')
        {
            reader.ReadLetter();
            var field = reader.ReadWord();
            switch (field.ToLowerInvariant())
            {
                case "act":
                    actFlags &= ~ReadMask(reader, RomFlags.Act, field, warnings);
                    break;
                case "aff":
                    affectedBy &= ~ReadMask(reader, RomFlags.Affect, field, warnings);
                    break;
                case "off":
                    offensiveFlags &= ~ReadMask(reader, RomFlags.Offensive, field, warnings);
                    break;
                case "imm":
                    immunities &= ~ReadMask(reader, RomFlags.Immunity, field, warnings);
                    break;
                case "res":
                    resistances &= ~ReadMask(reader, RomFlags.Immunity, field, warnings);
                    break;
                case "vul":
                    vulnerabilities &= ~ReadMask(reader, RomFlags.Immunity, field, warnings);
                    break;
                case "for":
                    form &= ~ReadMask(reader, RomFlags.Form, field, warnings);
                    break;
                case "par":
                    parts &= ~ReadMask(reader, RomFlags.Part, field, warnings);
                    break;
                default:
                    throw reader.CreateError($"Unknown mobile flag removal '{field}'.");
            }
        }

        return new MobilePrototype(
            vnum,
            keywords,
            shortDescription,
            longDescription,
            description,
            race,
            actFlags,
            affectedBy,
            offensiveFlags,
            immunities,
            resistances,
            vulnerabilities,
            form,
            parts,
            alignment,
            group,
            level,
            hitRoll,
            hitDice,
            manaDice,
            new DamageDice(damageDice, attackType),
            armor,
            startPosition,
            defaultPosition,
            sex,
            gold,
            size,
            material);
    }

    private static MobilePrototype ParseOldMobile(
        LegacyAreaReader reader,
        int vnum,
        ICollection<LegacyParseWarning> warnings)
    {
        var keywords = reader.ReadString();
        var shortDescription = reader.ReadString();
        var longDescription = UpperFirst(reader.ReadString());
        var description = UpperFirst(reader.ReadString());
        var actFlags = ReadMask(reader, RomFlags.Act, "actFlags", warnings);
        var affectedBy = ReadMask(reader, RomFlags.Affect, "affectedBy", warnings);
        var alignment = reader.ReadNumber();

        var marker = reader.ReadLetter();
        if (marker != 'S')
        {
            throw reader.CreateError($"Old mobile expects 'S', found '{marker}'.");
        }

        var level = reader.ReadNumber();

        // Old-style hit/mana statistics are discarded by ROM and recomputed
        // from level in convert_mobile(). After level come two obsolete
        // numbers, then the hit-dice count and dice.
        ReadNumber(reader);
        ReadNumber(reader);
        ReadNumber(reader); // hit-dice count
        ExpectLetter(reader, 'd');
        ReadNumber(reader);
        ExpectLetter(reader, '+');
        ReadNumber(reader);
        ReadNumber(reader);
        ExpectLetter(reader, 'd');
        ReadNumber(reader);
        ExpectLetter(reader, '+');
        ReadNumber(reader);

        var wealth = reader.ReadNumber() / 20 / 100;
        ReadNumber(reader); // experience cannot be used
        var startPosition = ToOldPosition(reader.ReadNumber());
        var defaultPosition = ToOldPosition(reader.ReadNumber());
        var sex = ToSex(reader.ReadNumber());

        var firstKeyword = keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        Race race;
        int offensiveFlags;
        int immunities;
        int resistances;
        int vulnerabilities;
        int form;
        int parts;

        if (!TryLookupRace(firstKeyword, reader, out race))
        {
            // ROM: race.c, load_old_mob() fallback when race_lookup() returns 0.
            race = Race.Human;
            offensiveFlags = RomFlags.Offensive.GetMask(["Dodge", "Disarm", "Trip", "AssistVnum"]);
            immunities = 0;
            resistances = 0;
            vulnerabilities = 0;
            form = RomFlags.Form.GetMask(["Edible", "Sentient", "Biped", "Mammal"]);
            parts = RomFlags.Part.GetMask(["Head", "Arms", "Legs", "Heart", "Brains", "Guts"]);
        }
        else
        {
            var raceDefinition = RomTables.RaceDefinitions.First(definition => definition.Race == race);
            offensiveFlags = raceDefinition.OffensiveMask
                | RomFlags.Offensive.GetMask(["Dodge", "Disarm", "Trip", "AssistRace"]);
            immunities = raceDefinition.ImmunityMask;
            resistances = raceDefinition.ResistanceMask;
            vulnerabilities = raceDefinition.VulnerabilityMask;
            form = raceDefinition.FormMask;
            parts = raceDefinition.PartsMask;
        }

        // ROM: db2.c, convert_mobile().
        var hitDice = ConvertHitDice(level);
        var manaDice = new Dice(level, 10, 100);

        var damageType = level * 7 / 4;
        var damageNumber = Math.Min(damageType / 8 + 1, 5);
        damageType = Math.Max(2, damageType / damageNumber);
        var damageBonus = Math.Max(0, level * 9 / 4 - damageNumber * damageType);
        var damageDice = new Dice(damageNumber, damageType, damageBonus);

        var armor = new ArmorValues(
            Interpolate(level, 100, -100),
            Interpolate(level, 100, -100),
            Interpolate(level, 100, -100),
            Interpolate(level, 100, 0));

        return new MobilePrototype(
            vnum,
            keywords,
            shortDescription,
            longDescription,
            description,
            race,
            actFlags | RomFlags.Act.GetMask(["Warrior"]),
            affectedBy,
            offensiveFlags,
            immunities,
            resistances,
            vulnerabilities,
            form,
            parts,
            alignment,
            0,
            level,
            0,
            hitDice,
            manaDice,
            new DamageDice(damageDice, ConvertedDamageType(vnum)),
            armor,
            startPosition,
            defaultPosition,
            sex,
            wealth,
            Size.Medium,
            "none");
    }

    private static bool TryLookupRace(string keyword, LegacyAreaReader reader, out Race race)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            race = Race.Human;
            return false;
        }

        try
        {
            race = LegacyLookup.ToRace(keyword, reader);
            return true;
        }
        catch (LegacyFormatException)
        {
            race = Race.Human;
            return false;
        }
    }

    private static Dice ConvertHitDice(int level)
    {
        var type = level * level * 27 / 40;
        var number = Math.Min(type / 40 + 1, 10);
        type = Math.Max(2, type / number);
        var bonus = Math.Max(0, (int)(level * (8 + level) * 0.9) - number * type);
        return new Dice(number, type, bonus);
    }

    private static string ConvertedDamageType(int vnum) =>
        (vnum % 3) switch
        {
            0 => "Slash",
            1 => "Pound",
            _ => "Pierce"
        };

    private static int Interpolate(int level, int from, int to) =>
        from + level * (to - from) / 32;

    private static void RecordReference(
        ICollection<LegacyReference> references,
        LegacyAreaReader reader,
        LegacyPosition position,
        LegacyReferenceTarget target,
        int vnum,
        string context)
    {
        if (vnum <= 0)
        {
            return;
        }

        references.Add(new LegacyReference(reader.FileName, position, target, vnum, context));
    }

    private static int ClampNonNegative(
        int value,
        string field,
        LegacyAreaReader reader,
        ICollection<LegacyParseWarning> warnings)
    {
        if (value >= 0)
        {
            return value;
        }

        warnings.Add(new LegacyParseWarning(
            $"Negative {field} value {value} was clamped to 0.",
            reader.Position));
        return 0;
    }

    private static void ParseObjects(
        LegacyAreaReader reader,
        AreaBuilder builder,
        ICollection<LegacyParseWarning> warnings,
        bool oldFormat)
    {
        while (true)
        {
            var marker = reader.ReadLetter();
            if (marker != '#')
            {
                throw reader.CreateError("Expected '#' before an object vnum.");
            }

            var vnum = reader.ReadNumber();
            if (vnum == 0)
            {
                return;
            }

            if (!builder.ObjectVnums.Add(vnum))
            {
                throw reader.CreateError($"Duplicate object vnum {vnum}.");
            }

            builder.Objects.Add(oldFormat
                ? ParseOldObject(reader, vnum, warnings, builder.References)
                : ParseNewObject(reader, vnum, warnings, builder.References));
        }
    }

    private static ObjectPrototype ParseNewObject(
        LegacyAreaReader reader,
        int vnum,
        ICollection<LegacyParseWarning> warnings,
        ICollection<LegacyReference> references)
    {
        var keywords = reader.ReadString();
        var shortDescription = reader.ReadString();
        var longDescription = reader.ReadString();
        var material = reader.ReadString();
        var itemType = LegacyLookup.ToItemType(reader.ReadWord(), reader);
        var extraFlags = ReadMask(reader, RomFlags.Extra, "extraFlags", warnings);
        var wearFlags = ReadMask(reader, RomFlags.Wear, "wearFlags", warnings);

        var data = ReadNewObjectData(reader, vnum, itemType, warnings, references);
        var level = reader.ReadNumber();
        var weight = reader.ReadNumber();
        var cost = reader.ReadNumber();
        var condition = ReadCondition(reader);

        var affects = new List<AffectData>();
        var extraDescriptions = new List<ExtraDescription>();
        ReadObjectExtras(reader, affects, extraDescriptions, warnings);

        return new ObjectPrototype(
            vnum,
            keywords,
            shortDescription,
            longDescription,
            material,
            itemType,
            extraFlags,
            wearFlags,
            level,
            weight,
            cost,
            condition,
            data,
            affects,
            extraDescriptions);
    }

    private static ObjectTypeData ReadNewObjectData(
        LegacyAreaReader reader,
        int objVnum,
        ItemType itemType,
        ICollection<LegacyParseWarning> warnings,
        ICollection<LegacyReference> references)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                var weaponClass = LegacyLookup.ToWeaponClass(reader.ReadWord(), reader);
                var diceCount = reader.ReadNumber();
                var diceSize = reader.ReadNumber();
                var attackType = LegacyLookup.ToAttackType(reader.ReadWord(), reader);
                var specialFlags = ReadMask(reader, RomFlags.WeaponSpecial, "weapon.specialFlags", warnings);
                return new WeaponData(weaponClass, diceCount, diceSize, attackType, specialFlags);

            case ItemType.Container:
                var capacity = reader.ReadNumber();
                var containerFlags = ReadMask(reader, RomFlags.Container, "container.flags", warnings);
                var keyPosition = reader.Position;
                var keyVnum = reader.ReadNumber();
                RecordReference(references, reader, keyPosition,
                    LegacyReferenceTarget.Object, keyVnum, $"object {objVnum} container key");
                ReadNumber(reader);
                ReadNumber(reader);
                return new ContainerData(capacity, containerFlags, keyVnum);

            case ItemType.DrinkContainer:
            case ItemType.Fountain:
                var drinkCapacity = reader.ReadNumber();
                var remaining = reader.ReadNumber();
                var liquid = LegacyLookup.ToLiquid(reader.ReadWord(), reader);
                var poisoned = reader.ReadNumber() != 0;
                ReadNumber(reader);
                return new DrinkData(drinkCapacity, remaining, liquid, poisoned);

            case ItemType.Wand:
            case ItemType.Staff:
                var chargeLevel = reader.ReadNumber();
                var total = reader.ReadNumber();
                var left = reader.ReadNumber();
                var spell = reader.ReadWord();
                ReadNumber(reader);
                return new ChargesData(
                    chargeLevel,
                    total,
                    left,
                    string.IsNullOrWhiteSpace(spell) ? "None" : spell);

            case ItemType.Scroll:
            case ItemType.Potion:
            case ItemType.Pill:
                var spellLevel = reader.ReadNumber();
                var names = new List<string>();
                for (var index = 0; index < 4; index++)
                {
                    var name = reader.ReadWord();
                    if (!string.IsNullOrWhiteSpace(name) && !name.Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        names.Add(name);
                    }
                }

                if (names.Count == 0)
                {
                    names.Add("None");
                }

                return new SpellsData(spellLevel, names);

            case ItemType.Armor:
                var armorValues = new ArmorValues(
                    (int)reader.ReadFlag(),
                    (int)reader.ReadFlag(),
                    (int)reader.ReadFlag(),
                    (int)reader.ReadFlag());
                reader.ReadFlag(); // ROM reads five values for the default branch
                return new ArmorData(armorValues);

            default:
                var values = new int[5];
                for (var index = 0; index < values.Length; index++)
                {
                    values[index] = (int)reader.ReadFlag();
                }

                return BuildDefaultObjectData(objVnum, itemType, values, reader, warnings, references);
        }
    }

    private static ObjectPrototype ParseOldObject(
        LegacyAreaReader reader,
        int vnum,
        ICollection<LegacyParseWarning> warnings,
        ICollection<LegacyReference> references)
    {
        var keywords = reader.ReadString();
        var shortDescription = LowerFirst(reader.ReadString());
        var longDescription = UpperFirst(reader.ReadString());
        reader.ReadString(); // action description
        var itemTypeValue = reader.ReadNumber();
        if (!RomTables.ItemTypes.TryGet(itemTypeValue, out var itemTypeEntry))
        {
            throw reader.CreateError($"Unknown item type {itemTypeValue}.");
        }

        var itemType = (ItemType)itemTypeEntry.Value;
        var extraFlags = ReadMask(reader, RomFlags.Extra, "extraFlags", warnings);
        var wearFlags = ReadMask(reader, RomFlags.Wear, "wearFlags", warnings);
        var values = new int[5];
        for (var index = 0; index < 4; index++)
        {
            values[index] = reader.ReadNumber();
        }

        var weight = reader.ReadNumber();
        var cost = reader.ReadNumber();
        reader.ReadNumber(); // cost per day

        var data = BuildOldObjectData(vnum, itemType, values, reader, warnings, references);
        var affects = new List<AffectData>();
        var extraDescriptions = new List<ExtraDescription>();
        ReadObjectExtras(reader, affects, extraDescriptions, warnings);

        return new ObjectPrototype(
            vnum,
            keywords,
            shortDescription,
            longDescription,
            "none",
            itemType,
            extraFlags,
            wearFlags,
            0,
            weight,
            cost,
            100,
            data,
            affects,
            extraDescriptions);
    }

    private static ObjectTypeData BuildOldObjectData(
        int objVnum,
        ItemType itemType,
        IReadOnlyList<int> values,
        LegacyAreaReader reader,
        ICollection<LegacyParseWarning> warnings,
        ICollection<LegacyReference> references)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                var weaponClass = RomTables.WeaponClasses.TryGet(values[0], out var classEntry)
                    ? (WeaponClass)classEntry.Value
                    : WeaponClass.Exotic;
                var attackType = RomTables.AttackTypes.TryGet(values[3], out var attackEntry)
                    ? attackEntry.Name
                    : "Hit";
                return new WeaponData(weaponClass, values[1], values[2], attackType, 0);

            case ItemType.Container:
                RecordReference(references, reader, reader.Position,
                    LegacyReferenceTarget.Object, values[2], $"object {objVnum} container key");
                return new ContainerData(values[0], values[1], values[2]);

            case ItemType.DrinkContainer:
            case ItemType.Fountain:
                return new DrinkData(
                    values[0],
                    values[1],
                    RomTables.Liquids.TryGet(values[2], out var liquid) ? liquid.Name : "Water",
                    values[3] != 0);

            case ItemType.Wand:
            case ItemType.Staff:
                return new ChargesData(
                    values[0],
                    values[1],
                    values[1],
                    SkillName(values[3]));

            case ItemType.Scroll:
            case ItemType.Potion:
            case ItemType.Pill:
                var names = new List<string>();
                for (var index = 1; index <= 3; index++)
                {
                    if (values[index] >= 0)
                    {
                        names.Add(SkillName(values[index]));
                    }
                }

                if (names.Count == 0)
                {
                    names.Add("None");
                }

                return new SpellsData(values[0], names);

            case ItemType.Money:
                return new MoneyData(
                    ClampNonNegative(values[1], "money.gold", reader, warnings),
                    ClampNonNegative(values[0], "money.silver", reader, warnings));

            case ItemType.Armor:
                // ROM: db.c, load_old_obj() copies value[0] into values 1 and 2.
                return new ArmorData(new ArmorValues(values[0], values[0], values[0], values[3]));

            case ItemType.Portal:
                RecordReference(references, reader, reader.Position,
                    LegacyReferenceTarget.Room, values[3], $"object {objVnum} portal");
                return new PortalData(values[1], values[2], values[3]);

            case ItemType.Map:
                RecordReference(references, reader, reader.Position,
                    LegacyReferenceTarget.Room, values[0], $"object {objVnum} map");
                return new MapData(values[0]);

            default:
                return BuildDefaultObjectData(objVnum, itemType, values, reader, warnings, references);
        }
    }

    private static ObjectTypeData BuildDefaultObjectData(
        int objVnum,
        ItemType itemType,
        IReadOnlyList<int> values,
        LegacyAreaReader reader,
        ICollection<LegacyParseWarning> warnings,
        ICollection<LegacyReference> references)
    {
        switch (itemType)
        {
            case ItemType.Light:
                return new LightData(values[2]);

            case ItemType.Food:
                return new FoodData(ClampNonNegative(values[0], "food.hours", reader, warnings), values[3] != 0);

            case ItemType.Money:
                return new MoneyData(
                    ClampNonNegative(values[1], "money.gold", reader, warnings),
                    ClampNonNegative(values[0], "money.silver", reader, warnings));

            case ItemType.Furniture:
                return new FurnitureData(values[0], values[2]);

            case ItemType.Portal:
                RecordReference(references, reader, reader.Position,
                    LegacyReferenceTarget.Room, values[3], $"object {objVnum} portal");
                return new PortalData(values[1], values[2], values[3]);

            case ItemType.Map:
                RecordReference(references, reader, reader.Position,
                    LegacyReferenceTarget.Room, values[0], $"object {objVnum} map");
                return new MapData(values[0]);

            default:
                return new PlainObjectData(values.ToArray());
        }
    }

    private static void ReadObjectExtras(
        LegacyAreaReader reader,
        ICollection<AffectData> affects,
        ICollection<ExtraDescription> extraDescriptions,
        ICollection<LegacyParseWarning> warnings)
    {
        while (reader.PeekNonWhitespaceChar() is 'A' or 'F' or 'E')
        {
            var letter = reader.ReadLetter();
            switch (letter)
            {
                case 'A':
                    affects.Add(new AffectData(
                        AffectWhere.Object,
                        ToApplyLocation(reader, reader.ReadNumber()),
                        reader.ReadNumber()));
                    break;

                case 'F':
                    var where = reader.ReadLetter();
                    var affectWhere = where switch
                    {
                        'A' => AffectWhere.Affect,
                        'I' => AffectWhere.Immune,
                        'R' => AffectWhere.Resist,
                        'V' => AffectWhere.Vulnerable,
                        _ => throw reader.CreateError($"Unknown object affect location '{where}'.")
                    };
                    var location = ToApplyLocation(reader, reader.ReadNumber());
                    var modifier = reader.ReadNumber();
                    var registry = affectWhere switch
                    {
                        AffectWhere.Affect => RomFlags.Affect,
                        AffectWhere.Immune or AffectWhere.Resist or AffectWhere.Vulnerable => RomFlags.Immunity,
                        _ => RomFlags.Affect
                    };
                    var flags = ReadMask(reader, registry, "object affect flags", warnings);
                    affects.Add(new AffectData(affectWhere, location, modifier, flags));
                    break;

                case 'E':
                    var keyword = reader.ReadString();
                    var description = reader.ReadString();
                    extraDescriptions.Add(new ExtraDescription(keyword, description));
                    break;
            }
        }
    }

    private static void ParseRooms(
        LegacyAreaReader reader,
        AreaBuilder builder,
        ICollection<LegacyParseWarning> warnings)
    {
        while (true)
        {
            var marker = reader.ReadLetter();
            if (marker != '#')
            {
                throw reader.CreateError("Expected '#' before a room vnum.");
            }

            var vnum = reader.ReadNumber();
            if (vnum == 0)
            {
                return;
            }

            if (!builder.RoomVnums.Add(vnum))
            {
                throw reader.CreateError($"Duplicate room vnum {vnum}.");
            }

            var name = reader.ReadString();
            var description = reader.ReadString();
            reader.ReadNumber(); // area number
            var roomFlags = ReadMask(reader, RomFlags.Room, "roomFlags", warnings);
            var sectorValue = reader.ReadNumber();
            Sector sector;
            if (RomTables.Sectors.TryGet(sectorValue, out var sectorEntry))
            {
                sector = (Sector)sectorEntry.Value;
            }
            else
            {
                warnings.Add(new LegacyParseWarning(
                    $"Unknown sector {sectorValue} in room {vnum}; using Inside.",
                    reader.Position));
                sector = Sector.Inside;
            }

            var healRate = 100;
            var manaRate = 100;
            var clan = 0;
            var exits = new Dictionary<Direction, ExitPrototype>();
            var extraDescriptions = new List<ExtraDescription>();

            while (true)
            {
                var letter = reader.ReadLetter();
                switch (letter)
                {
                    case 'S':
                        goto RoomDone;
                    case 'H':
                        healRate = reader.ReadNumber();
                        break;
                    case 'M':
                        manaRate = reader.ReadNumber();
                        break;
                    case 'C':
                        clan = LegacyLookup.ToClan(reader.ReadString(), reader);
                        break;
                    case 'O':
                        reader.ReadString(); // room owner, not part of the JSON model
                        break;
                    case 'D':
                        var door = reader.ReadNumber();
                        if (door is < 0 or > 5)
                        {
                            throw reader.CreateError($"Bad door number {door}.");
                        }

                        var direction = (Direction)door;
                        var exitDescription = reader.ReadString();
                        reader.ReadString(); // keyword, not part of the model
                        var locks = reader.ReadNumber();
                        var keyPosition = reader.Position;
                        var keyVnum = reader.ReadNumber();
                        var toRoomPosition = reader.Position;
                        var toRoom = reader.ReadNumber();
                        RecordReference(
                            builder.References,
                            reader,
                            keyPosition,
                            LegacyReferenceTarget.Object,
                            keyVnum,
                            $"room {vnum} {direction.ToString().ToLowerInvariant()} exit key");
                        RecordReference(
                            builder.References,
                            reader,
                            toRoomPosition,
                            LegacyReferenceTarget.Room,
                            toRoom,
                            $"room {vnum} {direction.ToString().ToLowerInvariant()} exit");
                        var exitFlags = locks switch
                        {
                            0 => 0,
                            1 => RomFlags.Exit.GetMask(["IsDoor", "Closed"]),
                            2 => RomFlags.Exit.GetMask(["IsDoor", "Closed", "Locked"]),
                            3 => RomFlags.Exit.GetMask(["IsDoor", "Closed", "Locked", "NoPass"]),
                            4 => RomFlags.Exit.GetMask(["IsDoor", "Closed", "Locked", "PickProof"]),
                            5 => RomFlags.Exit.GetMask(["IsDoor", "Closed", "Locked", "PickProof", "NoPass"]),
                            _ => throw reader.CreateError($"Bad door lock state {locks}.")
                        };
                        exits[direction] = new ExitPrototype(direction, toRoom, exitFlags, keyVnum, exitDescription);
                        break;
                    case 'E':
                        extraDescriptions.Add(new ExtraDescription(reader.ReadString(), reader.ReadString()));
                        break;
                    default:
                        throw reader.CreateError($"Unknown room field '{letter}'.");
                }
            }

        RoomDone:
            builder.Rooms.Add(new RoomPrototype(
                vnum,
                name,
                description,
                roomFlags,
                sector,
                healRate,
                manaRate,
                clan,
                exits,
                extraDescriptions));
        }
    }

    private static void ParseResets(LegacyAreaReader reader, AreaBuilder builder)
    {
        while (true)
        {
            var letter = reader.ReadLetter();
            switch (letter)
            {
                case 'S':
                    return;
                case '*':
                    reader.ReadToEndOfLine();
                    continue;
            }

            var ifFlag = reader.ReadNumber() != 0;
            switch (letter)
            {
                case 'M':
                    var mobilePosition = reader.Position;
                    var mobileVnum = reader.ReadNumber();
                    var maxInWorld = reader.ReadNumber();
                    var mobileRoomPosition = reader.Position;
                    var mobileRoom = reader.ReadNumber();
                    var maxInRoom = reader.ReadNumber();
                    RecordReference(builder.References, reader, mobilePosition,
                        LegacyReferenceTarget.Mobile, mobileVnum, "mobile reset");
                    RecordReference(builder.References, reader, mobileRoomPosition,
                        LegacyReferenceTarget.Room, mobileRoom, "mobile reset room");
                    builder.Resets.Add(new MobileReset(
                        ifFlag,
                        mobileVnum,
                        mobileRoom,
                        maxInWorld,
                        maxInRoom));
                    break;

                case 'O':
                    var objectPosition = reader.Position;
                    var resetObject = reader.ReadNumber();
                    var objectMax = reader.ReadNumber();
                    var objectRoomPosition = reader.Position;
                    var objectRoom = reader.ReadNumber();
                    RecordReference(builder.References, reader, objectPosition,
                        LegacyReferenceTarget.Object, resetObject, "object reset");
                    RecordReference(builder.References, reader, objectRoomPosition,
                        LegacyReferenceTarget.Room, objectRoom, "object reset room");
                    builder.Resets.Add(new ObjectReset(
                        ifFlag,
                        resetObject,
                        objectRoom,
                        objectMax));
                    break;

                case 'P':
                    var putObjectPosition = reader.Position;
                    var objectVnum = reader.ReadNumber();
                    reader.ReadNumber(); // legacy max/field
                    var containerPosition = reader.Position;
                    var containerVnum = reader.ReadNumber();
                    var putMax = reader.ReadNumber();
                    RecordReference(builder.References, reader, putObjectPosition,
                        LegacyReferenceTarget.Object, objectVnum, "put reset");
                    RecordReference(builder.References, reader, containerPosition,
                        LegacyReferenceTarget.Object, containerVnum, "put reset container");
                    builder.Resets.Add(new PutReset(ifFlag, objectVnum, containerVnum, putMax));
                    break;

                case 'G':
                    var givePosition = reader.Position;
                    var giveObject = reader.ReadNumber();
                    var giveMax = reader.ReadNumber();
                    RecordReference(builder.References, reader, givePosition,
                        LegacyReferenceTarget.Object, giveObject, "give reset");
                    builder.Resets.Add(new GiveReset(ifFlag, giveObject, giveMax));
                    break;

                case 'E':
                    var equipPosition = reader.Position;
                    var equipObject = reader.ReadNumber();
                    var equipMax = reader.ReadNumber();
                    var wearValue = reader.ReadNumber();
                    RecordReference(builder.References, reader, equipPosition,
                        LegacyReferenceTarget.Object, equipObject, "equip reset");
                    builder.Resets.Add(new EquipReset(
                        ifFlag,
                        equipObject,
                        ToWearSlot(wearValue, reader),
                        equipMax));
                    break;

                case 'D':
                    var doorRoomPosition = reader.Position;
                    var roomVnum = reader.ReadNumber();
                    var door = reader.ReadNumber();
                    var stateValue = reader.ReadNumber();
                    if (door is < 0 or > 5) throw reader.CreateError($"Bad reset door {door}.");
                    if (stateValue is < 0 or > 2) throw reader.CreateError($"Bad reset door state {stateValue}.");
                    RecordReference(builder.References, reader, doorRoomPosition,
                        LegacyReferenceTarget.Room, roomVnum, "door reset");
                    builder.Resets.Add(new DoorReset(
                        ifFlag,
                        roomVnum,
                        (Direction)door,
                        (DoorState)stateValue));
                    break;

                case 'R':
                    var randomRoomPosition = reader.Position;
                    var randomRoom = reader.ReadNumber();
                    var randomDoor = reader.ReadNumber();
                    RecordReference(builder.References, reader, randomRoomPosition,
                        LegacyReferenceTarget.Room, randomRoom, "randomize reset");
                    builder.Resets.Add(new RandomizeReset(
                        ifFlag,
                        randomRoom,
                        randomDoor));
                    break;

                default:
                    throw reader.CreateError($"Unknown reset command '{letter}'.");
            }

            reader.ReadToEndOfLine();
        }
    }

    private static void ParseShops(LegacyAreaReader reader, AreaBuilder builder)
    {
        while (true)
        {
            var keeperPosition = reader.Position;
            var keeper = reader.ReadNumber();
            if (keeper == 0)
            {
                return;
            }

            RecordReference(builder.References, reader, keeperPosition,
                LegacyReferenceTarget.Mobile, keeper, "shop keeper");

            var buyTypes = new List<ItemType>();
            for (var index = 0; index < 5; index++)
            {
                var itemTypeValue = reader.ReadNumber();
                if (itemTypeValue != 0 && RomTables.ItemTypes.TryGet(itemTypeValue, out var entry))
                {
                    buyTypes.Add((ItemType)entry.Value);
                }
            }

            var profitBuy = reader.ReadNumber();
            var profitSell = reader.ReadNumber();
            var openHour = reader.ReadNumber();
            var closeHour = reader.ReadNumber();
            reader.ReadToEndOfLine();

            builder.Shops.Add(new ShopData(
                keeper,
                buyTypes,
                profitBuy,
                profitSell,
                openHour,
                closeHour));
        }
    }

    private static void ParseSpecials(LegacyAreaReader reader, AreaBuilder builder)
    {
        while (true)
        {
            var letter = reader.ReadLetter();
            switch (letter)
            {
                case 'S':
                    return;
                case '*':
                    reader.ReadToEndOfLine();
                    break;
                case 'M':
                    var mobilePosition = reader.Position;
                    var mobileVnum = reader.ReadNumber();
                    var procedure = LegacyLookup.ToSpecial(reader.ReadWord(), reader);
                    RecordReference(builder.References, reader, mobilePosition,
                        LegacyReferenceTarget.Mobile, mobileVnum, "special procedure");
                    builder.Specials.Add(new SpecialData(mobileVnum, procedure));
                    reader.ReadToEndOfLine();
                    break;
                default:
                    throw reader.CreateError($"Unknown special field '{letter}'.");
            }
        }
    }

    private static void ParseSocials(LegacyAreaReader reader, AreaBuilder builder)
    {
        while (true)
        {
            var name = reader.ReadWord();
            if (name == "#0")
            {
                return;
            }

            reader.ReadToEndOfLine();
            var fields = new string[8];
            var ended = false;

            for (var index = 0; index < fields.Length; index++)
            {
                if (ended)
                {
                    fields[index] = string.Empty;
                    continue;
                }

                var value = reader.ReadStringEol();
                if (value == "#")
                {
                    ended = true;
                    fields[index] = string.Empty;
                }
                else
                {
                    fields[index] = value == "$" ? string.Empty : value;
                }
            }

            builder.Socials.Add(new SocialData(
                name,
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                fields[4],
                fields[5],
                fields[6],
                fields[7]));
        }
    }

    private static Dice ReadDice(LegacyAreaReader reader)
    {
        var count = reader.ReadNumber();
        ExpectLetter(reader, 'd');
        var size = reader.ReadNumber();
        ExpectLetter(reader, '+');
        var bonus = reader.ReadNumber();
        return new Dice(count, size, bonus);
    }

    private static void ExpectLetter(LegacyAreaReader reader, char expected)
    {
        var actual = reader.ReadLetter();
        if (char.ToLowerInvariant(actual) != expected)
        {
            throw reader.CreateError($"Expected '{expected}', found '{actual}'.");
        }
    }

    private static int ReadMask(
        LegacyAreaReader reader,
        FlagRegistry registry,
        string field,
        ICollection<LegacyParseWarning> warnings)
    {
        var start = reader.Position;
        var value = reader.ReadFlag();
        if (value < 0 || value > int.MaxValue)
        {
            throw reader.CreateError($"Invalid {field} value.", start);
        }

        var mask = (int)value;
        var known = registry.Definitions.Aggregate(0, (current, definition) => current | definition.Bit);
        var persisted = registry.PersistedDefinitions.Aggregate(
            0,
            (current, definition) => current | definition.Bit);
        var unknown = mask & ~known;
        if (unknown != 0)
        {
            // Real ROM data contains bits that the flag table does not name
            // (for example act bit 4). ROM keeps the raw bit but no code
            // inspects it, so the importer drops it and reports the loss.
            warnings.Add(new LegacyParseWarning(
                $"Unknown {registry.Category} bits 0x{unknown:X} in {field} were dropped.",
                start));
        }

        // Non-persisted bits (IsNpc, Pet, …) are derived by the runtime and
        // are not written to the canonical JSON.
        return mask & persisted;
    }

    private static int ReadCondition(LegacyAreaReader reader) =>
        reader.ReadLetter() switch
        {
            'P' => 100,
            'G' => 90,
            'A' => 75,
            'W' => 50,
            'D' => 25,
            'B' => 10,
            'R' => 0,
            _ => 100
        };

    private static ApplyLocation ToApplyLocation(LegacyAreaReader reader, int value)
    {
        if (!RomTables.ApplyLocations.TryGet(value, out var entry))
        {
            throw reader.CreateError($"Unknown apply location {value}.");
        }

        return (ApplyLocation)entry.Value;
    }

    private static WearSlot ToWearSlot(int value, LegacyAreaReader reader) =>
        value switch
        {
            -1 => WearSlot.None,
            0 => WearSlot.Light,
            1 => WearSlot.FingerLeft,
            2 => WearSlot.FingerRight,
            3 => WearSlot.Neck1,
            4 => WearSlot.Neck2,
            5 => WearSlot.Body,
            6 => WearSlot.Head,
            7 => WearSlot.Legs,
            8 => WearSlot.Feet,
            9 => WearSlot.Hands,
            10 => WearSlot.Arms,
            11 => WearSlot.Shield,
            12 => WearSlot.About,
            13 => WearSlot.Waist,
            14 => WearSlot.WristLeft,
            15 => WearSlot.WristRight,
            16 => WearSlot.Wield,
            17 => WearSlot.Hold,
            18 => WearSlot.Float,
            _ => throw reader.CreateError($"Unknown wear slot {value}.")
        };

    private static Position ToPosition(int value) =>
        value is >= 0 and <= 8 ? (Position)value : Position.Standing;

    private static Position ToOldPosition(int value) =>
        value is >= (int)Position.Sleeping and <= (int)Position.Standing
            ? (Position)value
            : Position.Standing;

    private static Sex ToSex(int value) =>
        value is >= 0 and <= 2 ? (Sex)value : Sex.Neutral;

    private static string SkillName(int value) =>
        value >= 0 && value < RomTables.Skills.Count ? RomTables.Skills[value].Name : "None";

    private static int ReadNumber(LegacyAreaReader reader) => reader.ReadNumber();

    private static string UpperFirst(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static string LowerFirst(string value) =>
        value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static string DefaultName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(name) ? "Area" : UpperFirst(name);
    }

    private sealed class AreaBuilder
    {
        public AreaBuilder(string fileName)
        {
            Name = DefaultName(fileName);
            LegacyFile = Path.GetFileName(fileName);
        }

        public string Name { get; set; }

        public string Credits { get; set; } = string.Empty;

        public int LowVnum { get; set; }

        public int HighVnum { get; set; }

        public int Security { get; set; } = 9;

        public string Builders { get; set; } = string.Empty;

        public string? LegacyFile { get; set; }

        public bool HasHeader { get; set; }

        public List<HelpEntry> Helps { get; } = [];

        public List<MobilePrototype> Mobiles { get; } = [];

        public List<ObjectPrototype> Objects { get; } = [];

        public List<RoomPrototype> Rooms { get; } = [];

        public List<Reset> Resets { get; } = [];

        public List<ShopData> Shops { get; } = [];

        public List<SpecialData> Specials { get; } = [];

        public List<SocialData> Socials { get; } = [];

        public List<LegacyReference> References { get; } = [];

        public HashSet<int> MobileVnums { get; } = [];

        public HashSet<int> ObjectVnums { get; } = [];

        public HashSet<int> RoomVnums { get; } = [];

        public AreaData Build() =>
            new(
                Name,
                Credits,
                LowVnum,
                HighVnum,
                Security,
                Builders,
                LegacyFile,
                Helps,
                Mobiles,
                Objects,
                Rooms,
                Resets,
                Shops,
                Specials,
                Socials);
    }
}
