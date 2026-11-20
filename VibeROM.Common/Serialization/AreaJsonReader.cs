using System.Text.Json;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Serialization;

/// <summary>
/// Reads canonical area JSON into the domain model. Every name is resolved
/// through <see cref="RomFlags"/> or <see cref="RomTables"/>; failures carry a
/// JSON Pointer path.
/// </summary>
internal static class AreaJsonReader
{
    private static readonly string[] VariantProperties =
    [
        "weapon", "armor", "container", "drink", "fountain", "charges",
        "spells", "food", "money", "furniture", "portal", "map", "light"
    ];

    private static readonly HashSet<string> AreaProperties = new(StringComparer.Ordinal)
    {
        "formatVersion", "legacyFile", "name", "credits", "vnumRange", "security",
        "builders", "helps", "mobiles", "objects", "rooms", "resets", "shops",
        "specials", "socials"
    };

    private static readonly HashSet<string> VnumRangeProperties = new(StringComparer.Ordinal)
    {
        "low", "high"
    };

    private static readonly HashSet<string> HelpProperties = new(StringComparer.Ordinal)
    {
        "keywords", "level", "text"
    };

    private static readonly HashSet<string> MobileProperties = new(StringComparer.Ordinal)
    {
        "vnum", "keywords", "shortDescription", "longDescription", "description",
        "race", "actFlags", "affectedBy", "offensiveFlags", "immunities",
        "resistances", "vulnerabilities", "form", "parts", "alignment", "group",
        "level", "hitRoll", "hitDice", "manaDice", "damage", "armor",
        "startPosition", "defaultPosition", "sex", "gold", "size", "material"
    };

    private static readonly HashSet<string> DiceProperties = new(StringComparer.Ordinal)
    {
        "count", "size", "bonus"
    };

    private static readonly HashSet<string> DamageProperties = new(StringComparer.Ordinal)
    {
        "count", "size", "bonus", "attackType"
    };

    private static readonly HashSet<string> ArmorProperties = new(StringComparer.Ordinal)
    {
        "pierce", "bash", "slash", "exotic"
    };

    private static readonly HashSet<string> ObjectProperties = new(StringComparer.Ordinal)
    {
        "vnum", "keywords", "shortDescription", "longDescription", "material",
        "itemType", "extraFlags", "wearFlags", "level", "weightPounds",
        "costCopper", "condition", "affects", "extraDescriptions",
        "weapon", "armor", "container", "drink", "fountain", "charges",
        "spells", "food", "money", "furniture", "portal", "map", "light",
        "rawValues"
    };

    private static readonly HashSet<string> WeaponProperties = new(StringComparer.Ordinal)
    {
        "class", "diceCount", "diceSize", "attackType", "specialFlags"
    };

    private static readonly HashSet<string> ContainerProperties = new(StringComparer.Ordinal)
    {
        "capacity", "flags", "keyVnum"
    };

    private static readonly HashSet<string> DrinkProperties = new(StringComparer.Ordinal)
    {
        "capacity", "remaining", "liquid", "poisoned"
    };

    private static readonly HashSet<string> ChargesProperties = new(StringComparer.Ordinal)
    {
        "level", "total", "remaining", "spell"
    };

    private static readonly HashSet<string> SpellsProperties = new(StringComparer.Ordinal)
    {
        "level", "names"
    };

    private static readonly HashSet<string> FoodProperties = new(StringComparer.Ordinal)
    {
        "hours", "poisoned"
    };

    private static readonly HashSet<string> MoneyProperties = new(StringComparer.Ordinal)
    {
        "gold", "silver"
    };

    private static readonly HashSet<string> FurnitureProperties = new(StringComparer.Ordinal)
    {
        "maxPeople", "flags"
    };

    private static readonly HashSet<string> PortalProperties = new(StringComparer.Ordinal)
    {
        "exitFlags", "gateFlags", "toRoomVnum"
    };

    private static readonly HashSet<string> MapProperties = new(StringComparer.Ordinal)
    {
        "roomVnum"
    };

    private static readonly HashSet<string> LightProperties = new(StringComparer.Ordinal)
    {
        "duration"
    };

    private static readonly HashSet<string> AffectProperties = new(StringComparer.Ordinal)
    {
        "where", "location", "modifier", "flags"
    };

    private static readonly HashSet<string> ExtraDescriptionProperties = new(StringComparer.Ordinal)
    {
        "keywords", "description"
    };

    private static readonly HashSet<string> RoomProperties = new(StringComparer.Ordinal)
    {
        "vnum", "name", "description", "roomFlags", "sector", "healRate",
        "manaRate", "clan", "exits", "extraDescriptions"
    };

    private static readonly HashSet<string> ExitProperties = new(StringComparer.Ordinal)
    {
        "toRoom", "flags", "keyVnum", "description"
    };

    private static readonly HashSet<string> ShopProperties = new(StringComparer.Ordinal)
    {
        "keeperVnum", "buyTypes", "profitBuy", "profitSell", "openHour", "closeHour"
    };

    private static readonly HashSet<string> SpecialProperties = new(StringComparer.Ordinal)
    {
        "mobileVnum", "procedure"
    };

    private static readonly HashSet<string> SocialProperties = new(StringComparer.Ordinal)
    {
        "name", "charNoArgument", "othersNoArgument", "charFound", "othersFound",
        "victimFound", "charNotFound", "charAuto", "othersAuto"
    };

    public static AreaData Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Fail(string.Empty, "Area must be a JSON object.");
        }

        EnsureKnown(root, string.Empty, AreaProperties);

        var formatVersion = ReadInt(root, "formatVersion", string.Empty);
        if (formatVersion != GameInfo.DataFormatVersion)
        {
            throw Fail(
                PropertyPath(string.Empty, "formatVersion"),
                $"Unsupported formatVersion {formatVersion}; expected {GameInfo.DataFormatVersion}.");
        }

        var legacyFile = ReadOptionalString(root, "legacyFile", string.Empty);
        if (string.IsNullOrEmpty(legacyFile))
        {
            legacyFile = null;
        }

        var name = ReadString(root, "name", string.Empty);
        var credits = ReadOptionalString(root, "credits", string.Empty) ?? string.Empty;
        var builders = ReadOptionalString(root, "builders", string.Empty) ?? string.Empty;
        var security = ReadOptionalInt(root, "security", 9, string.Empty);

        var vnumRangePath = PropertyPath(string.Empty, "vnumRange");
        var vnumRange = RequireObject(root, "vnumRange", string.Empty);
        EnsureKnown(vnumRange, vnumRangePath, VnumRangeProperties);
        var lowVnum = ReadInt(vnumRange, "low", vnumRangePath);
        var highVnum = ReadInt(vnumRange, "high", vnumRangePath);

        var area = new AreaData(
            name,
            credits,
            lowVnum,
            highVnum,
            security,
            builders,
            legacyFile,
            ReadHelps(root),
            ReadMobiles(root),
            ReadObjects(root),
            ReadRooms(root),
            ReadResets(root),
            ReadShops(root),
            ReadSpecials(root),
            ReadSocials(root));

        var errors = area.Validate();
        if (errors.Count > 0)
        {
            throw new DataFormatException(errors);
        }

        return area;
    }

    private static IReadOnlyList<HelpEntry> ReadHelps(JsonElement root)
    {
        var helps = new List<HelpEntry>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "helps", string.Empty))
        {
            var path = $"/helps/{index++}";
            EnsureKnown(element, path, HelpProperties);
            helps.Add(new HelpEntry(
                ReadString(element, "keywords", path),
                ReadInt(element, "level", path),
                ReadString(element, "text", path)));
        }

        return helps;
    }

    private static IReadOnlyList<MobilePrototype> ReadMobiles(JsonElement root)
    {
        var mobiles = new List<MobilePrototype>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "mobiles", string.Empty))
        {
            mobiles.Add(ReadMobile(element, $"/mobiles/{index++}"));
        }

        return mobiles;
    }

    private static MobilePrototype ReadMobile(JsonElement element, string path)
    {
        EnsureKnown(element, path, MobileProperties);

        var armorPath = PropertyPath(path, "armor");
        var armorElement = RequireObject(element, "armor", path);
        EnsureKnown(armorElement, armorPath, ArmorProperties);
        var armor = new ArmorValues(
            ReadMobileArmor(armorElement, "pierce", armorPath),
            ReadMobileArmor(armorElement, "bash", armorPath),
            ReadMobileArmor(armorElement, "slash", armorPath),
            ReadMobileArmor(armorElement, "exotic", armorPath));

        return new MobilePrototype(
            ReadInt(element, "vnum", path),
            ReadString(element, "keywords", path),
            ReadString(element, "shortDescription", path),
            ReadString(element, "longDescription", path),
            ReadString(element, "description", path),
            ReadEnumTable<Race>(element, "race", RomTables.Races, path),
            ReadFlags(element, "actFlags", RomFlags.Act, path),
            ReadFlags(element, "affectedBy", RomFlags.Affect, path),
            ReadFlags(element, "offensiveFlags", RomFlags.Offensive, path),
            ReadFlags(element, "immunities", RomFlags.Immunity, path),
            ReadFlags(element, "resistances", RomFlags.Immunity, path),
            ReadFlags(element, "vulnerabilities", RomFlags.Immunity, path),
            ReadFlags(element, "form", RomFlags.Form, path),
            ReadFlags(element, "parts", RomFlags.Part, path),
            ReadInt(element, "alignment", path),
            ReadInt(element, "group", path),
            ReadInt(element, "level", path),
            ReadInt(element, "hitRoll", path),
            ReadDice(element, "hitDice", path),
            ReadDice(element, "manaDice", path),
            ReadDamage(element, path),
            armor,
            ReadEnumName<Position>(element, "startPosition", path),
            ReadEnumName<Position>(element, "defaultPosition", path),
            ReadEnumName<Sex>(element, "sex", path),
            ReadInt(element, "gold", path),
            ReadEnumName<Size>(element, "size", path),
            ReadString(element, "material", path));
    }

    private static int ReadMobileArmor(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            throw Fail(valuePath, $"Mobile armor '{name}' must be a number.");
        }

        if (!value.TryGetDecimal(out var fileValue))
        {
            throw Fail(valuePath, "Mobile armor value is not a valid number.");
        }

        var internalValue = fileValue * 10m;
        if (internalValue != decimal.Truncate(internalValue)
            || internalValue < int.MinValue
            || internalValue > int.MaxValue)
        {
            throw Fail(valuePath, "Mobile armor must convert to a whole internal AC value.");
        }

        return (int)internalValue;
    }

    private static IReadOnlyList<ObjectPrototype> ReadObjects(JsonElement root)
    {
        var objects = new List<ObjectPrototype>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "objects", string.Empty))
        {
            objects.Add(ReadObject(element, $"/objects/{index++}"));
        }

        return objects;
    }

    private static ObjectPrototype ReadObject(JsonElement element, string path)
    {
        EnsureKnown(element, path, ObjectProperties);

        var itemType = ReadEnumTable<ItemType>(element, "itemType", RomTables.ItemTypes, path);
        var data = ReadObjectData(element, itemType, path);

        return new ObjectPrototype(
            ReadInt(element, "vnum", path),
            ReadString(element, "keywords", path),
            ReadString(element, "shortDescription", path),
            ReadString(element, "longDescription", path),
            ReadString(element, "material", path),
            itemType,
            ReadFlags(element, "extraFlags", RomFlags.Extra, path),
            ReadFlags(element, "wearFlags", RomFlags.Wear, path),
            ReadInt(element, "level", path),
            ReadWeightTenths(element, path),
            ReadInt(element, "costCopper", path),
            ReadInt(element, "condition", path),
            data,
            ReadAffects(element, path),
            ReadExtraDescriptions(element, path));
    }

    private static ObjectTypeData ReadObjectData(JsonElement element, ItemType itemType, string path)
    {
        var expected = ExpectedVariant(itemType);
        if (expected is null)
        {
            foreach (var variant in VariantProperties)
            {
                if (element.TryGetProperty(variant, out _))
                {
                    throw Fail(
                        PropertyPath(path, variant),
                        $"Property '{variant}' is not valid for plain item type '{itemType}'.");
                }
            }

            if (element.TryGetProperty("rawValues", out _))
            {
                return new PlainObjectData(ReadRawValues(element, path));
            }

            return new PlainObjectData();
        }

        foreach (var variant in VariantProperties)
        {
            if (variant != expected && element.TryGetProperty(variant, out _))
            {
                throw Fail(
                    PropertyPath(path, variant),
                    $"Property '{variant}' does not match itemType '{itemType}' (expected '{expected}').");
            }
        }

        if (element.TryGetProperty("rawValues", out _))
        {
            throw Fail(
                PropertyPath(path, "rawValues"),
                $"Property 'rawValues' is only valid for plain item types, not '{itemType}'.");
        }

        var dataPath = PropertyPath(path, expected);
        var data = RequireObject(element, expected, path);
        return itemType switch
        {
            ItemType.Weapon => ReadWeapon(data, dataPath),
            ItemType.Armor => ReadArmor(data, dataPath),
            ItemType.Container => ReadContainer(data, dataPath),
            ItemType.DrinkContainer or ItemType.Fountain => ReadDrink(data, dataPath),
            ItemType.Wand or ItemType.Staff => ReadCharges(data, dataPath),
            ItemType.Scroll or ItemType.Potion or ItemType.Pill => ReadSpells(data, dataPath),
            ItemType.Food => ReadFood(data, dataPath),
            ItemType.Money => ReadMoney(data, dataPath),
            ItemType.Furniture => ReadFurniture(data, dataPath),
            ItemType.Portal => ReadPortal(data, dataPath),
            ItemType.Map => ReadMap(data, dataPath),
            ItemType.Light => ReadLight(data, dataPath),
            _ => throw Fail(dataPath, $"Unsupported item type '{itemType}'.")
        };
    }

    private static string? ExpectedVariant(ItemType itemType) =>
        itemType switch
        {
            ItemType.Weapon => "weapon",
            ItemType.Armor => "armor",
            ItemType.Container => "container",
            ItemType.DrinkContainer => "drink",
            ItemType.Fountain => "fountain",
            ItemType.Wand or ItemType.Staff => "charges",
            ItemType.Scroll or ItemType.Potion or ItemType.Pill => "spells",
            ItemType.Food => "food",
            ItemType.Money => "money",
            ItemType.Furniture => "furniture",
            ItemType.Portal => "portal",
            ItemType.Map => "map",
            ItemType.Light => "light",
            _ => null
        };

    private static ObjectTypeData ReadWeapon(JsonElement element, string path)
    {
        EnsureKnown(element, path, WeaponProperties);
        return new WeaponData(
            ReadEnumTable<WeaponClass>(element, "class", RomTables.WeaponClasses, path),
            ReadInt(element, "diceCount", path),
            ReadInt(element, "diceSize", path),
            ReadAttackType(element, "attackType", path),
            ReadFlags(element, "specialFlags", RomFlags.WeaponSpecial, path));
    }

    private static ObjectTypeData ReadArmor(JsonElement element, string path)
    {
        EnsureKnown(element, path, ArmorProperties);
        return new ArmorData(new ArmorValues(
            ReadInt(element, "pierce", path),
            ReadInt(element, "bash", path),
            ReadInt(element, "slash", path),
            ReadInt(element, "exotic", path)));
    }

    private static ObjectTypeData ReadContainer(JsonElement element, string path)
    {
        EnsureKnown(element, path, ContainerProperties);
        return new ContainerData(
            ReadInt(element, "capacity", path),
            ReadFlags(element, "flags", RomFlags.Container, path, required: true),
            ReadInt(element, "keyVnum", path));
    }

    private static ObjectTypeData ReadDrink(JsonElement element, string path)
    {
        EnsureKnown(element, path, DrinkProperties);
        return new DrinkData(
            ReadInt(element, "capacity", path),
            ReadInt(element, "remaining", path),
            ReadLiquid(element, "liquid", path),
            ReadBool(element, "poisoned", path));
    }

    private static ObjectTypeData ReadCharges(JsonElement element, string path)
    {
        EnsureKnown(element, path, ChargesProperties);
        var spell = ReadString(element, "spell", path);
        if (string.IsNullOrWhiteSpace(spell))
        {
            throw Fail(PropertyPath(path, "spell"), "spell must not be empty.");
        }

        return new ChargesData(
            ReadInt(element, "level", path),
            ReadInt(element, "total", path),
            ReadInt(element, "remaining", path),
            spell);
    }

    private static ObjectTypeData ReadSpells(JsonElement element, string path)
    {
        EnsureKnown(element, path, SpellsProperties);
        var namesPath = PropertyPath(path, "names");
        var names = RequireArray(element, "names", path).EnumerateArray()
            .Select((name, index) =>
            {
                var namePath = $"{namesPath}/{index}";
                var value = ReadStringValue(name, namePath);
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw Fail(namePath, "Spell name must not be empty.");
                }

                return value;
            })
            .ToArray();
        if (names.Length is < 1 or > 4)
        {
            throw Fail(namesPath, "names must contain one to four spell names.");
        }

        return new SpellsData(ReadInt(element, "level", path), names);
    }

    private static ObjectTypeData ReadFood(JsonElement element, string path)
    {
        EnsureKnown(element, path, FoodProperties);
        return new FoodData(
            ReadInt(element, "hours", path),
            ReadBool(element, "poisoned", path));
    }

    private static ObjectTypeData ReadMoney(JsonElement element, string path)
    {
        EnsureKnown(element, path, MoneyProperties);
        return new MoneyData(
            ReadInt(element, "gold", path),
            ReadInt(element, "silver", path));
    }

    private static ObjectTypeData ReadFurniture(JsonElement element, string path)
    {
        EnsureKnown(element, path, FurnitureProperties);
        return new FurnitureData(
            ReadInt(element, "maxPeople", path),
            ReadFlags(element, "flags", RomFlags.Furniture, path, required: true));
    }

    private static ObjectTypeData ReadPortal(JsonElement element, string path)
    {
        EnsureKnown(element, path, PortalProperties);
        return new PortalData(
            ReadFlags(element, "exitFlags", RomFlags.Exit, path, required: true),
            ReadFlags(element, "gateFlags", RomFlags.Gate, path, required: true),
            ReadInt(element, "toRoomVnum", path));
    }

    private static ObjectTypeData ReadMap(JsonElement element, string path)
    {
        EnsureKnown(element, path, MapProperties);
        return new MapData(ReadInt(element, "roomVnum", path));
    }

    private static ObjectTypeData ReadLight(JsonElement element, string path)
    {
        EnsureKnown(element, path, LightProperties);
        return new LightData(ReadInt(element, "duration", path));
    }

    private static IReadOnlyList<int> ReadRawValues(JsonElement element, string path)
    {
        var valuesPath = PropertyPath(path, "rawValues");
        var array = RequireArray(element, "rawValues", path);
        var values = array.EnumerateArray().Select((value, index) => ReadIntValue(value, $"{valuesPath}/{index}")).ToArray();
        if (values.Length != 5)
        {
            throw Fail(valuesPath, "rawValues must contain exactly five integers.");
        }

        return values;
    }

    private static int ReadWeightTenths(JsonElement element, string path)
    {
        var valuePath = PropertyPath(path, "weightPounds");
        if (!element.TryGetProperty("weightPounds", out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(valuePath, "Required property 'weightPounds' is missing.");
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var pounds))
        {
            throw Fail(valuePath, "weightPounds must be a number.");
        }

        var tenths = pounds * 10m;
        if (tenths != decimal.Truncate(tenths) || tenths < 0 || tenths > int.MaxValue)
        {
            throw Fail(valuePath, "weightPounds must be a non-negative multiple of 0.1.");
        }

        return (int)tenths;
    }

    private static IReadOnlyList<AffectData> ReadAffects(JsonElement element, string path)
    {
        var affects = new List<AffectData>();
        var index = 0;
        foreach (var affect in ReadOptionalArray(element, "affects", path))
        {
            var affectPath = $"{PropertyPath(path, "affects")}/{index++}";
            EnsureKnown(affect, affectPath, AffectProperties);

            var where = affect.TryGetProperty("where", out _)
                ? ReadEnumName<AffectWhere>(affect, "where", affectPath)
                : AffectWhere.Object;
            if (where == AffectWhere.Weapon)
            {
                throw Fail(PropertyPath(affectPath, "where"), "Weapon affects are not representable in area JSON.");
            }

            var location = ReadEnumTable<ApplyLocation>(
                affect,
                "location",
                RomTables.ApplyLocations,
                affectPath);
            var modifier = ReadInt(affect, "modifier", affectPath);
            var registry = AffectData.RegistryFor(where);
            if (registry is null && affect.TryGetProperty("flags", out _))
            {
                throw Fail(PropertyPath(affectPath, "flags"), "Flags are not allowed for an Object affect.");
            }

            var bits = registry is null
                ? 0
                : ReadFlags(affect, "flags", registry, affectPath);
            affects.Add(new AffectData(where, location, modifier, bits));
        }

        return affects;
    }

    private static IReadOnlyList<ExtraDescription> ReadExtraDescriptions(JsonElement element, string path)
    {
        var descriptions = new List<ExtraDescription>();
        var index = 0;
        foreach (var description in ReadOptionalArray(element, "extraDescriptions", path))
        {
            var descriptionPath = $"{PropertyPath(path, "extraDescriptions")}/{index++}";
            EnsureKnown(description, descriptionPath, ExtraDescriptionProperties);
            descriptions.Add(new ExtraDescription(
                ReadString(description, "keywords", descriptionPath),
                ReadString(description, "description", descriptionPath)));
        }

        return descriptions;
    }

    private static IReadOnlyList<RoomPrototype> ReadRooms(JsonElement root)
    {
        var rooms = new List<RoomPrototype>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "rooms", string.Empty))
        {
            rooms.Add(ReadRoom(element, $"/rooms/{index++}"));
        }

        return rooms;
    }

    private static RoomPrototype ReadRoom(JsonElement element, string path)
    {
        EnsureKnown(element, path, RoomProperties);

        var exits = new Dictionary<Direction, ExitPrototype>();
        if (element.TryGetProperty("exits", out var exitsElement) && exitsElement.ValueKind != JsonValueKind.Null)
        {
            if (exitsElement.ValueKind != JsonValueKind.Object)
            {
                throw Fail(PropertyPath(path, "exits"), "exits must be an object.");
            }

            foreach (var exitProperty in exitsElement.EnumerateObject())
            {
                var direction = ParseDirection(exitProperty.Name, PropertyPath(path, "exits"));
                if (!exits.TryAdd(direction, ReadExit(exitProperty.Value, direction, path)))
                {
                    throw Fail(
                        PropertyPath(PropertyPath(path, "exits"), exitProperty.Name),
                        $"Duplicate exit direction '{exitProperty.Name}'.");
                }
            }
        }

        return new RoomPrototype(
            ReadInt(element, "vnum", path),
            ReadString(element, "name", path),
            ReadString(element, "description", path),
            ReadFlags(element, "roomFlags", RomFlags.Room, path),
            ReadEnumTable<Sector>(element, "sector", RomTables.Sectors, path),
            ReadInt(element, "healRate", path),
            ReadInt(element, "manaRate", path),
            ReadInt(element, "clan", path),
            exits,
            ReadExtraDescriptions(element, path));
    }

    private static ExitPrototype ReadExit(JsonElement element, Direction direction, string roomPath)
    {
        var path = PropertyPath(PropertyPath(roomPath, "exits"), direction.ToString().ToLowerInvariant());
        EnsureKnown(element, path, ExitProperties);
        return new ExitPrototype(
            direction,
            ReadInt(element, "toRoom", path),
            ReadFlags(element, "flags", RomFlags.Exit, path),
            ReadOptionalInt(element, "keyVnum", 0, path),
            ReadOptionalString(element, "description", path) ?? string.Empty);
    }

    private static Direction ParseDirection(string name, string path) =>
        name switch
        {
            "north" => Direction.North,
            "east" => Direction.East,
            "south" => Direction.South,
            "west" => Direction.West,
            "up" => Direction.Up,
            "down" => Direction.Down,
            _ => throw Fail(PropertyPath(path, name), $"Unknown exit direction '{name}'.")
        };

    private static IReadOnlyList<Reset> ReadResets(JsonElement root)
    {
        var resets = new List<Reset>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "resets", string.Empty))
        {
            resets.Add(ReadReset(element, $"/resets/{index++}"));
        }

        return resets;
    }

    private static Reset ReadReset(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Fail(path, "Reset must be an object.");
        }

        var type = ReadString(element, "type", path);
        var ifFlag = ReadOptionalBool(element, "ifFlag", false, path);

        switch (type)
        {
            case "Mobile":
                EnsureKnown(element, path, ["type", "ifFlag", "mobileVnum", "roomVnum", "maxInWorld", "maxInRoom"]);
                return new MobileReset(
                    ifFlag,
                    ReadInt(element, "mobileVnum", path),
                    ReadInt(element, "roomVnum", path),
                    ReadInt(element, "maxInWorld", path),
                    ReadInt(element, "maxInRoom", path));

            case "Object":
                EnsureKnown(element, path, ["type", "ifFlag", "objectVnum", "roomVnum", "maxInWorld"]);
                return new ObjectReset(
                    ifFlag,
                    ReadInt(element, "objectVnum", path),
                    ReadInt(element, "roomVnum", path),
                    ReadInt(element, "maxInWorld", path));

            case "Put":
                EnsureKnown(element, path, ["type", "ifFlag", "objectVnum", "containerVnum", "maxInWorld"]);
                return new PutReset(
                    ifFlag,
                    ReadInt(element, "objectVnum", path),
                    ReadInt(element, "containerVnum", path),
                    ReadInt(element, "maxInWorld", path));

            case "Give":
                EnsureKnown(element, path, ["type", "ifFlag", "objectVnum", "maxInWorld"]);
                return new GiveReset(
                    ifFlag,
                    ReadInt(element, "objectVnum", path),
                    ReadInt(element, "maxInWorld", path));

            case "Equip":
                EnsureKnown(element, path, ["type", "ifFlag", "objectVnum", "wearLocation", "maxInWorld"]);
                return new EquipReset(
                    ifFlag,
                    ReadInt(element, "objectVnum", path),
                    ReadEnumName<WearSlot>(element, "wearLocation", path),
                    ReadInt(element, "maxInWorld", path));

            case "Door":
                EnsureKnown(element, path, ["type", "ifFlag", "roomVnum", "direction", "state"]);
                return new DoorReset(
                    ifFlag,
                    ReadInt(element, "roomVnum", path),
                    ReadEnumName<Direction>(element, "direction", path),
                    ReadEnumName<DoorState>(element, "state", path));

            case "Randomize":
                EnsureKnown(element, path, ["type", "ifFlag", "roomVnum", "door"]);
                return new RandomizeReset(
                    ifFlag,
                    ReadInt(element, "roomVnum", path),
                    ReadInt(element, "door", path));

            default:
                throw Fail(PropertyPath(path, "type"), $"Unknown reset type '{type}'.");
        }
    }

    private static IReadOnlyList<ShopData> ReadShops(JsonElement root)
    {
        var shops = new List<ShopData>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "shops", string.Empty))
        {
            var path = $"/shops/{index++}";
            EnsureKnown(element, path, ShopProperties);
            var buyTypesPath = PropertyPath(path, "buyTypes");
            var buyTypes = RequireArray(element, "buyTypes", path).EnumerateArray()
                .Select((itemType, itemIndex) => ReadEnumTableValue<ItemType>(
                    itemType,
                    RomTables.ItemTypes,
                    $"{buyTypesPath}/{itemIndex}"))
                .ToArray();
            shops.Add(new ShopData(
                ReadInt(element, "keeperVnum", path),
                buyTypes,
                ReadInt(element, "profitBuy", path),
                ReadInt(element, "profitSell", path),
                ReadInt(element, "openHour", path),
                ReadInt(element, "closeHour", path)));
        }

        return shops;
    }

    private static IReadOnlyList<SpecialData> ReadSpecials(JsonElement root)
    {
        var specials = new List<SpecialData>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "specials", string.Empty))
        {
            var path = $"/specials/{index++}";
            EnsureKnown(element, path, SpecialProperties);
            specials.Add(new SpecialData(
                ReadInt(element, "mobileVnum", path),
                ReadSpecial(element, "procedure", path)));
        }

        return specials;
    }

    private static IReadOnlyList<SocialData> ReadSocials(JsonElement root)
    {
        var socials = new List<SocialData>();
        var index = 0;
        foreach (var element in ReadOptionalArray(root, "socials", string.Empty))
        {
            var path = $"/socials/{index++}";
            EnsureKnown(element, path, SocialProperties);
            socials.Add(new SocialData(
                ReadString(element, "name", path),
                ReadString(element, "charNoArgument", path),
                ReadString(element, "othersNoArgument", path),
                ReadString(element, "charFound", path),
                ReadString(element, "othersFound", path),
                ReadString(element, "victimFound", path),
                ReadString(element, "charNotFound", path),
                ReadString(element, "charAuto", path),
                ReadString(element, "othersAuto", path)));
        }

        return socials;
    }

    private static Dice ReadDice(JsonElement element, string name, string path)
    {
        var dicePath = PropertyPath(path, name);
        var dice = RequireObject(element, name, path);
        EnsureKnown(dice, dicePath, DiceProperties);
        return new Dice(
            ReadInt(dice, "count", dicePath),
            ReadInt(dice, "size", dicePath),
            ReadInt(dice, "bonus", dicePath));
    }

    private static DamageDice ReadDamage(JsonElement element, string path)
    {
        var damagePath = PropertyPath(path, "damage");
        var damage = RequireObject(element, "damage", path);
        EnsureKnown(damage, damagePath, DamageProperties);
        return new DamageDice(
            new Dice(
                ReadInt(damage, "count", damagePath),
                ReadInt(damage, "size", damagePath),
                ReadInt(damage, "bonus", damagePath)),
            ReadAttackType(damage, "attackType", damagePath));
    }

    private static T ReadEnumTable<T>(
        JsonElement element,
        string name,
        ValueTable table,
        string path)
        where T : struct, Enum
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!table.TryGet(text, out var entry))
        {
            throw Fail(
                valuePath,
                $"Unknown {table.Category} name '{text}'. Expected one of: {string.Join(", ", table.Names)}.");
        }

        return (T)Enum.ToObject(typeof(T), entry.Value);
    }

    private static T ReadEnumTableValue<T>(JsonElement value, ValueTable table, string path)
        where T : struct, Enum
    {
        var text = ReadStringValue(value, path);
        if (!table.TryGet(text, out var entry))
        {
            throw Fail(
                path,
                $"Unknown {table.Category} name '{text}'. Expected one of: {string.Join(", ", table.Names)}.");
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

    private static int ReadFlags(
        JsonElement element,
        string name,
        FlagRegistry registry,
        string path,
        bool required = false)
    {
        var flagsPath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            if (required)
            {
                throw Fail(flagsPath, $"Required property '{name}' is missing.");
            }

            return 0;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Fail(flagsPath, $"{name} must be an array of {registry.Category} flag names.");
        }

        var mask = 0;
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var itemPath = $"{flagsPath}/{index++}";
            if (item.ValueKind != JsonValueKind.String)
            {
                throw Fail(itemPath, "Flag name must be a string.");
            }

            var nameValue = item.GetString()!;
            if (!registry.TryGet(nameValue, out var definition) || !definition.Persisted)
            {
                throw Fail(
                    itemPath,
                    $"Unknown or non-persisted {registry.Category} flag '{nameValue}'. Expected one of: " +
                    string.Join(", ", registry.PersistedNames) + ".");
            }

            if ((mask & definition.Bit) != 0)
            {
                throw Fail(itemPath, $"Duplicate {registry.Category} flag '{nameValue}'.");
            }

            mask |= definition.Bit;
        }

        return mask;
    }


    private static string ReadAttackType(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!RomTables.AttackTypes.TryGet(text, out var entry))
        {
            throw Fail(
                valuePath,
                $"Unknown attackType '{text}'. Expected one of: {string.Join(", ", RomTables.AttackTypes.Names)}.");
        }

        return entry.Name;
    }

    private static string ReadLiquid(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!RomTables.Liquids.TryGet(text, out var entry))
        {
            throw Fail(
                valuePath,
                $"Unknown liquid '{text}'. Expected one of: {string.Join(", ", RomTables.Liquids.Names)}.");
        }

        return entry.Name;
    }

    private static string ReadSpecial(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        var text = ReadString(element, name, path);
        if (!RomTables.Specials.TryGet(text, out var entry))
        {
            throw Fail(
                valuePath,
                $"Unknown special procedure '{text}'. Expected one of: {string.Join(", ", RomTables.Specials.Names)}.");
        }

        return entry.Name;
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

    private static bool ReadBool(JsonElement element, string name, string path)
    {
        var valuePath = PropertyPath(path, name);
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(valuePath, $"Required property '{name}' is missing.");
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw Fail(valuePath, "Expected a boolean.");
        }

        return value.GetBoolean();
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

    private static JsonElement RequireArray(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw Fail(PropertyPath(path, name), $"Required property '{name}' is missing.");
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Fail(PropertyPath(path, name), $"Property '{name}' must be an array.");
        }

        return value;
    }

    private static IEnumerable<JsonElement> ReadOptionalArray(
        JsonElement element,
        string name,
        string path)
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

    private static void EnsureKnown(
        JsonElement element,
        string path,
        IReadOnlyCollection<string> known)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Fail(path, "Expected an object.");
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!known.Contains(property.Name))
            {
                throw Fail(
                    PropertyPath(path, property.Name),
                    $"Unknown property '{property.Name}'.");
            }
        }
    }

    private static string PropertyPath(string path, string name) =>
        string.IsNullOrEmpty(path) ? "/" + name : path + "/" + name;

    private static string PropertyPath(string path, int index) =>
        string.IsNullOrEmpty(path) ? "/" + index : path + "/" + index;

    private static DataFormatException Fail(string path, string message)
    {
        var effectivePath = string.IsNullOrEmpty(path) ? "/" : path;
        return new DataFormatException(effectivePath, message);
    }
}
