using System.Text.Json;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Serialization;

/// <summary>Reads canonical player-save JSON into a mutable character.</summary>
internal static class PlayerJsonReader
{
    private static readonly HashSet<string> RootProperties = new(StringComparer.Ordinal)
    {
        "formatVersion", "name", "passwordHash", "email", "race", "class", "sex", "trueSex",
        "clan", "level", "trust", "security", "colourEnabled", "rent", "roomVnum",
        "vitals", "attributes", "money", "experience", "practice", "train",
        "savingThrow", "alignment", "hitRoll", "damageRoll", "armor", "wimpy",
        "position", "conditions", "invisLevel", "incogLevel", "lastLevel", "points",
        "played", "lines", "prompt", "title", "bamfin", "bamfout", "playerFlags",
        "communicationFlags", "wiznetFlags", "affectedBy", "aliases", "skills",
        "groups", "affects", "inventory", "equipment", "pet", "lastLogoff",
        "lastSaved", "lastNote", "lastIdea", "lastPenalty", "lastNews",
        "lastChanges", "confirmDelete"
    };

    private static readonly HashSet<string> VitalsProperties = new(StringComparer.Ordinal)
    {
        "hit", "mana", "move", "maxHit", "maxMana", "maxMove"
    };

    private static readonly HashSet<string> AttributeProperties = new(StringComparer.Ordinal)
    {
        "strength", "dexterity", "intelligence", "wisdom", "constitution"
    };

    private static readonly HashSet<string> MoneyProperties = new(StringComparer.Ordinal)
    {
        "gold", "silver"
    };

    private static readonly HashSet<string> ConditionProperties = new(StringComparer.Ordinal)
    {
        "drunk", "full", "thirst", "hunger"
    };

    private static readonly HashSet<string> ArmorProperties = new(StringComparer.Ordinal)
    {
        "pierce", "bash", "slash", "exotic"
    };

    private static readonly HashSet<string> AliasProperties = new(StringComparer.Ordinal)
    {
        "name", "replacement"
    };

    private static readonly HashSet<string> SkillProperties = new(StringComparer.Ordinal)
    {
        "name", "proficiency"
    };

    private static readonly HashSet<string> AffectProperties = new(StringComparer.Ordinal)
    {
        "type", "skill", "flags", "where", "level", "duration", "location", "modifier"
    };

    private static readonly HashSet<string> ItemProperties = new(StringComparer.Ordinal)
    {
        "vnum", "condition", "timer", "contents", "affects", "extraDescriptions"
    };

    private static readonly HashSet<string> EquippedProperties = new(StringComparer.Ordinal)
    {
        "wearLocation", "item"
    };

    private static readonly HashSet<string> ExtraDescriptionProperties = new(StringComparer.Ordinal)
    {
        "keywords", "description"
    };

    private static readonly HashSet<string> PetProperties = new(StringComparer.Ordinal)
    {
        "vnum", "name", "shortDescription", "longDescription", "description",
        "level", "sex", "race"
    };

    public static Character Read(JsonElement root, World world)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Fail(string.Empty, "Player save must be a JSON object.");
        }

        EnsureKnown(root, string.Empty, RootProperties);
        var formatVersion = ReadInt(root, "formatVersion", string.Empty);
        if (formatVersion != GameInfo.DataFormatVersion)
        {
            throw Fail("/formatVersion", $"Unsupported formatVersion {formatVersion}; expected {GameInfo.DataFormatVersion}.");
        }

        var name = ReadString(root, "name", string.Empty);
        var player = new PlayerData
        {
            PasswordHash = ReadString(root, "passwordHash", string.Empty),
            Email = ReadOptionalString(root, "email", string.Empty) ?? string.Empty,
            Clan = ReadOptionalString(root, "clan", string.Empty) ?? "none",
            Security = ReadOptionalInt(root, "security", 0, string.Empty),
            ColourEnabled = ReadOptionalBool(root, "colourEnabled", true, string.Empty),
            Rent = ReadOptionalInt(root, "rent", 0, string.Empty),
            Prompt = ReadOptionalString(root, "prompt", string.Empty) ?? string.Empty,
            Title = ReadOptionalString(root, "title", string.Empty) ?? string.Empty,
            Bamfin = ReadOptionalString(root, "bamfin", string.Empty) ?? string.Empty,
            Bamfout = ReadOptionalString(root, "bamfout", string.Empty) ?? string.Empty
        };

        var race = ReadTable<Race>(root, "race", RomTables.PcRaces, string.Empty);
        var playerClass = ReadTable<PlayerClass>(root, "class", RomTables.Classes, string.Empty);
        var sex = ReadEnumName<Sex>(root, "sex", string.Empty);
        var character = world.CreatePlayer(player, name, race, playerClass, sex);

        player.TrueSex = ReadOptionalEnumName(root, "trueSex", sex, string.Empty);
        character.Level = ReadInt(root, "level", string.Empty);
        character.Trust = ReadOptionalInt(root, "trust", 0, string.Empty);
        ReadVitals(root, character);
        ReadAttributes(root, character);
        ReadMoney(root, character);
        character.Experience = ReadOptionalInt(root, "experience", 0, string.Empty);
        character.Practice = ReadOptionalInt(root, "practice", 0, string.Empty);
        character.Train = ReadOptionalInt(root, "train", 0, string.Empty);
        character.SavingThrow = ReadOptionalInt(root, "savingThrow", 0, string.Empty);
        character.Alignment = ReadOptionalInt(root, "alignment", 0, string.Empty);
        character.HitRoll = ReadOptionalInt(root, "hitRoll", 0, string.Empty);
        character.DamageRoll = ReadOptionalInt(root, "damageRoll", 0, string.Empty);
        character.Armor = ReadArmor(root);
        character.Wimpy = ReadOptionalInt(root, "wimpy", 0, string.Empty);
        character.Position = ReadEnumName<Position>(root, "position", string.Empty);
        ReadConditions(root, player);
        character.InvisLevel = ReadOptionalInt(root, "invisLevel", 0, string.Empty);
        character.IncogLevel = ReadOptionalInt(root, "incogLevel", 0, string.Empty);
        player.LastLevel = ReadOptionalInt(root, "lastLevel", 0, string.Empty);
        player.Points = ReadOptionalInt(root, "points", 0, string.Empty);
        character.Played = ReadOptionalInt(root, "played", 0, string.Empty);
        character.Lines = ReadOptionalInt(root, "lines", 24, string.Empty);
        character.PlayerFlags = ReadFlags(root, "playerFlags", RomFlags.PlayerAct, string.Empty);
        character.CommunicationFlags = ReadFlags(root, "communicationFlags", RomFlags.Communication, string.Empty);
        character.WiznetFlags = ReadFlags(root, "wiznetFlags", RomFlags.Wiznet, string.Empty);
        character.AffectedBy = ReadFlags(root, "affectedBy", RomFlags.Affect, string.Empty);
        ReadAliases(root, player);
        ReadSkills(root, player);
        ReadGroups(root, player);

        var roomVnum = ReadInt(root, "roomVnum", string.Empty);
        if (roomVnum > 0)
        {
            if (!world.TryGetRoom(roomVnum, out var room))
            {
                throw Fail("/roomVnum", $"Unknown room vnum {roomVnum}.");
            }

            world.MoveCharacterToRoom(character, room);
        }

        ReadAffects(root, character.Affects, AffectWhere.Affect);
        ReadInventory(root, world, character);
        ReadEquipment(root, world, character);
        ReadPet(root, world, character);

        if (root.TryGetProperty("lastLogoff", out _))
        {
            player.LastLogoff = ReadTimestamp(root, "lastLogoff", string.Empty);
        }

        if (root.TryGetProperty("lastSaved", out _))
        {
            player.LastSaved = ReadTimestamp(root, "lastSaved", string.Empty);
        }

        player.LastNote = ReadOptionalInt(root, "lastNote", 0, string.Empty);
        player.LastIdea = ReadOptionalInt(root, "lastIdea", 0, string.Empty);
        player.LastPenalty = ReadOptionalInt(root, "lastPenalty", 0, string.Empty);
        player.LastNews = ReadOptionalInt(root, "lastNews", 0, string.Empty);
        player.LastChanges = ReadOptionalInt(root, "lastChanges", 0, string.Empty);
        player.ConfirmDelete = ReadOptionalBool(root, "confirmDelete", false, string.Empty);

        var errors = character.Validate();
        if (errors.Count > 0)
        {
            throw new DataFormatException(errors);
        }

        return character;
    }

    private static void ReadVitals(JsonElement root, Character character)
    {
        var vitals = RequireObject(root, "vitals", string.Empty);
        EnsureKnown(vitals, "/vitals", VitalsProperties);
        character.Hit = ReadInt(vitals, "hit", "/vitals");
        character.Mana = ReadInt(vitals, "mana", "/vitals");
        character.Move = ReadInt(vitals, "move", "/vitals");
        character.MaxHit = ReadInt(vitals, "maxHit", "/vitals");
        character.MaxMana = ReadInt(vitals, "maxMana", "/vitals");
        character.MaxMove = ReadInt(vitals, "maxMove", "/vitals");
    }

    private static void ReadAttributes(JsonElement root, Character character)
    {
        var attributes = RequireObject(root, "attributes", string.Empty);
        EnsureKnown(attributes, "/attributes", AttributeProperties);
        character.SetPermanentStat(0, ReadInt(attributes, "strength", "/attributes"));
        character.SetPermanentStat(1, ReadInt(attributes, "dexterity", "/attributes"));
        character.SetPermanentStat(2, ReadInt(attributes, "intelligence", "/attributes"));
        character.SetPermanentStat(3, ReadInt(attributes, "wisdom", "/attributes"));
        character.SetPermanentStat(4, ReadInt(attributes, "constitution", "/attributes"));
        for (var index = 0; index < 5; index++)
        {
            character.SetModifiedStat(index, character.GetPermanentStat(index));
        }
    }

    private static void ReadMoney(JsonElement root, Character character)
    {
        var money = RequireObject(root, "money", string.Empty);
        EnsureKnown(money, "/money", MoneyProperties);
        character.Gold = ReadInt(money, "gold", "/money");
        character.Silver = ReadInt(money, "silver", "/money");
    }

    private static ArmorValues ReadArmor(JsonElement root)
    {
        if (!root.TryGetProperty("armor", out var armor) || armor.ValueKind == JsonValueKind.Null)
        {
            return new ArmorValues(0, 0, 0, 0);
        }

        if (armor.ValueKind != JsonValueKind.Object)
        {
            throw Fail("/armor", "armor must be an object.");
        }

        EnsureKnown(armor, "/armor", ArmorProperties);
        return new ArmorValues(
            ReadInt(armor, "pierce", "/armor"),
            ReadInt(armor, "bash", "/armor"),
            ReadInt(armor, "slash", "/armor"),
            ReadInt(armor, "exotic", "/armor"));
    }

    private static void ReadConditions(JsonElement root, PlayerData player)
    {
        var conditions = RequireObject(root, "conditions", string.Empty);
        EnsureKnown(conditions, "/conditions", ConditionProperties);
        player.Drunk = ReadInt(conditions, "drunk", "/conditions");
        player.Full = ReadInt(conditions, "full", "/conditions");
        player.Thirst = ReadInt(conditions, "thirst", "/conditions");
        player.Hunger = ReadInt(conditions, "hunger", "/conditions");
    }

    private static void ReadAliases(JsonElement root, PlayerData player)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "aliases", string.Empty))
        {
            var path = $"/aliases/{index++}";
            EnsureKnown(element, path, AliasProperties);
            player.Aliases.Add(new Alias(
                ReadString(element, "name", path),
                ReadString(element, "replacement", path)));
        }
    }

    private static void ReadSkills(JsonElement root, PlayerData player)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "skills", string.Empty))
        {
            var path = $"/skills/{index++}";
            EnsureKnown(element, path, SkillProperties);
            player.SetSkill(
                ReadString(element, "name", path),
                ReadInt(element, "proficiency", path));
        }
    }

    private static void ReadGroups(JsonElement root, PlayerData player)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "groups", string.Empty))
        {
            player.KnownGroups.Add(ReadStringValue(element, $"/groups/{index++}"));
        }
    }

    private static void ReadAffects(JsonElement root, ICollection<Affect> affects, AffectWhere defaultWhere)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "affects", string.Empty))
        {
            var path = $"/affects/{index++}";
            EnsureKnown(element, path, AffectProperties);
            affects.Add(ReadAffect(element, path, defaultWhere));
        }
    }

    private static Affect ReadAffect(JsonElement element, string path, AffectWhere defaultWhere)
    {
        var where = element.TryGetProperty("where", out _)
            ? ReadEnumName<AffectWhere>(element, "where", path)
            : defaultWhere;
        var bits = ReadAffectBits(element, path);
        var skill = ReadOptionalString(element, "skill", path);
        return new Affect
        {
            Where = where,
            Type = SkillIndex(skill, path),
            Bits = bits,
            Level = ReadInt(element, "level", path),
            Duration = ReadInt(element, "duration", path),
            Location = ReadTable<ApplyLocation>(element, "location", RomTables.ApplyLocations, path),
            Modifier = ReadInt(element, "modifier", path)
        };
    }

    private static int ReadAffectBits(JsonElement element, string path)
    {
        var bits = 0;
        if (element.TryGetProperty("type", out _))
        {
            var type = ReadString(element, "type", path);
            if (!RomFlags.Affect.TryGet(type, out var definition))
            {
                throw Fail(PropertyPath(path, "type"), $"Unknown affect flag '{type}'.");
            }

            bits |= definition.Bit;
        }

        bits |= ReadFlags(element, "flags", RomFlags.Affect, path);
        return bits;
    }

    private static int SkillIndex(string? skill, string path)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return -1;
        }

        for (var index = 0; index < RomTables.Skills.Count; index++)
        {
            if (string.Equals(RomTables.Skills[index].Name, skill, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        throw Fail(PropertyPath(path, "skill"), $"Unknown skill '{skill}'.");
    }

    private static void ReadInventory(JsonElement root, World world, Character character)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "inventory", string.Empty))
        {
            ReadItem(element, world, character, container: null, path: $"/inventory/{index++}", requireCarrier: true);
        }
    }

    private static void ReadEquipment(JsonElement root, World world, Character character)
    {
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "equipment", string.Empty))
        {
            var path = $"/equipment/{index++}";
            EnsureKnown(element, path, EquippedProperties);
            var slot = ReadEnumName<WearSlot>(element, "wearLocation", path);
            var item = RequireObject(element, "item", path);
            var obj = ReadItem(item, world, character, container: null, path: PropertyPath(path, "item"), requireCarrier: true);
            world.EquipObject(character, obj, slot);
        }
    }

    private static GameObject ReadItem(
        JsonElement element,
        World world,
        Character? character,
        GameObject? container,
        string path,
        bool requireCarrier)
    {
        EnsureKnown(element, path, ItemProperties);
        var vnum = ReadInt(element, "vnum", path);
        if (!world.ObjectPrototypes.TryGetValue(vnum, out var prototype))
        {
            throw Fail(PropertyPath(path, "vnum"), $"Unknown object vnum {vnum}.");
        }

        var obj = world.CreateObject(
            prototype,
            container: container,
            carrier: requireCarrier ? character : null);
        obj.Condition = ReadOptionalInt(element, "condition", prototype.Condition, path);
        obj.Timer = ReadOptionalInt(element, "timer", 0, path);
        ReadItemAffects(element, obj, path);

        var childIndex = 0;
        foreach (var child in ReadOptionalArray(element, "contents", path))
        {
            ReadItem(
                child,
                world,
                character: null,
                container: obj,
                $"{PropertyPath(path, "contents")}/{childIndex++}",
                requireCarrier: false);
        }

        ReadExtraDescriptions(element, obj, path);
        return obj;
    }

    private static void ReadItemAffects(JsonElement element, GameObject obj, string path)
    {
        var index = 0;
        foreach (var affect in ReadOptionalArray(element, "affects", path))
        {
            var affectPath = $"{PropertyPath(path, "affects")}/{index++}";
            EnsureKnown(affect, affectPath, AffectProperties);
            obj.Affects.Add(ReadAffect(affect, affectPath, AffectWhere.Object));
        }
    }

    private static void ReadExtraDescriptions(JsonElement element, GameObject obj, string path)
    {
        var index = 0;
        foreach (var description in ReadOptionalArray(element, "extraDescriptions", path))
        {
            var descriptionPath = $"{PropertyPath(path, "extraDescriptions")}/{index++}";
            EnsureKnown(description, descriptionPath, ExtraDescriptionProperties);
            obj.ExtraDescriptions.Add(new ExtraDescription(
                ReadString(description, "keywords", descriptionPath),
                ReadString(description, "description", descriptionPath)));
        }
    }

    private static void ReadPet(JsonElement root, World world, Character character)
    {
        if (!root.TryGetProperty("pet", out var petElement) || petElement.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (petElement.ValueKind != JsonValueKind.Object)
        {
            throw Fail("/pet", "pet must be an object.");
        }

        EnsureKnown(petElement, "/pet", PetProperties);
        var vnum = ReadInt(petElement, "vnum", "/pet");
        if (!world.Mobiles.TryGetValue(vnum, out var prototype))
        {
            throw Fail("/pet/vnum", $"Unknown mobile vnum {vnum}.");
        }

        var pet = world.CreateMobile(prototype, character.InRoom);
        pet.Name = ReadString(petElement, "name", "/pet");
        pet.ShortDescription = ReadOptionalString(petElement, "shortDescription", "/pet") ?? pet.ShortDescription;
        pet.LongDescription = ReadOptionalString(petElement, "longDescription", "/pet") ?? pet.LongDescription;
        pet.Description = ReadOptionalString(petElement, "description", "/pet") ?? pet.Description;
        pet.Level = ReadInt(petElement, "level", "/pet");
        pet.Trust = pet.Level;
        pet.Sex = ReadOptionalEnumName(petElement, "sex", pet.Sex, "/pet");
        pet.Race = ReadTable<Race>(petElement, "race", RomTables.Races, "/pet");
        pet.Master = character;
        character.Pet = pet;
    }

    private static T ReadTable<T>(JsonElement element, string name, ValueTable table, string path)
        where T : struct, Enum
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!table.TryGet(text, out var entry))
        {
            throw Fail(valuePath, $"Unknown {table.Category} name '{text}'.");
        }

        return (T)Enum.ToObject(typeof(T), entry.Value);
    }

    private static T ReadEnumName<T>(JsonElement element, string name, string path)
        where T : struct, Enum
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!Enum.TryParse<T>(text, ignoreCase: false, out var value)
            || !Enum.IsDefined(value)
            || Enum.GetName(value) != text)
        {
            throw Fail(valuePath, $"Unknown {typeof(T).Name} name '{text}'.");
        }

        return value;
    }

    private static T ReadOptionalEnumName<T>(JsonElement element, string name, T defaultValue, string path)
        where T : struct, Enum =>
        element.TryGetProperty(name, out _) ? ReadEnumName<T>(element, name, path) : defaultValue;

    private static int ReadFlags(JsonElement element, string name, FlagRegistry registry, string path)
    {
        var flagsPath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return 0;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Fail(flagsPath, $"{name} must be an array.");
        }

        var mask = 0;
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var itemPath = $"{flagsPath}/{index++}";
            var text = ReadStringValue(item, itemPath);
            if (!registry.TryGet(text, out var definition) || !definition.Persisted)
            {
                throw Fail(itemPath, $"Unknown or non-persisted {registry.Category} flag '{text}'.");
            }

            if ((mask & definition.Bit) != 0)
            {
                throw Fail(itemPath, $"Duplicate {registry.Category} flag '{text}'.");
            }

            mask |= definition.Bit;
        }

        return mask;
    }

    private static DateTimeOffset ReadTimestamp(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw Fail(valuePath, $"{name} must be a date-time string.");
        }

        if (!value.TryGetDateTimeOffset(out var timestamp))
        {
            throw Fail(valuePath, $"{name} is not a valid date-time.");
        }

        return timestamp;
    }

    private static string ReadString(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(valuePath, $"Required property '{name}' is missing.");
        }

        return ReadStringValue(value, valuePath);
    }

    private static string ReadStringValue(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw Fail(path, "Expected a string.");
        }

        return value.GetString()!;
    }

    private static string? ReadOptionalString(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ReadStringValue(value, PropertyPath(path, name));
    }

    private static int ReadInt(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(valuePath, $"Required property '{name}' is missing.");
        }

        return ReadIntValue(value, valuePath);
    }

    private static int ReadIntValue(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw Fail(path, "Expected a 32-bit integer.");
        }

        return result;
    }

    private static int ReadOptionalInt(JsonElement element, string name, int defaultValue, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        return ReadIntValue(value, PropertyPath(path, name));
    }

    private static bool ReadOptionalBool(JsonElement element, string name, bool defaultValue, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw Fail(PropertyPath(path, name), "Expected a boolean.");
        }

        return value.GetBoolean();
    }

    private static JsonElement RequireObject(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(PropertyPath(path, name), $"Required property '{name}' is missing.");
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw Fail(PropertyPath(path, name), $"Property '{name}' must be an object.");
        }

        return value;
    }

    private static IEnumerable<JsonElement> ReadOptionalArray(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Fail(PropertyPath(path, name), $"Property '{name}' must be an array.");
        }

        return value.EnumerateArray().ToArray();
    }

    private static void EnsureKnown(JsonElement element, string path, IReadOnlyCollection<string> known)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Fail(path, "Expected an object.");
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!known.Contains(property.Name))
            {
                throw Fail(PropertyPath(path, property.Name), $"Unknown property '{property.Name}'.");
            }
        }
    }

    private static string PropertyPath(string path, string name) =>
        string.IsNullOrEmpty(path) ? "/" + name : path + "/" + name;

    private static DataFormatException Fail(string path, string message)
    {
        var effectivePath = string.IsNullOrEmpty(path) ? "/" : path;
        return new DataFormatException(effectivePath, message);
    }
}
