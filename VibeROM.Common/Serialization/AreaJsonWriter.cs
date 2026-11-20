using System.Text.Json;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Serialization;

/// <summary>
/// Writes a domain <see cref="AreaData"/> as canonical JSON: camelCase,
/// schema property order, two-space indent, LF, sorted entities and no
/// default/empty optional values.
/// </summary>
internal static class AreaJsonWriter
{
    private static readonly Direction[] Directions = Enum.GetValues<Direction>();

    public static void Write(Utf8JsonWriter writer, AreaData area)
    {
        var errors = area.Validate();
        if (errors.Count > 0)
        {
            throw new DataFormatException(errors);
        }

        writer.WriteStartObject();
        writer.WriteNumber("formatVersion", GameInfo.DataFormatVersion);

        if (!string.IsNullOrEmpty(area.LegacyFile))
        {
            writer.WriteString("legacyFile", area.LegacyFile);
        }

        writer.WriteString("name", area.Name);

        if (!string.IsNullOrEmpty(area.Credits))
        {
            writer.WriteString("credits", area.Credits);
        }

        writer.WriteStartObject("vnumRange");
        writer.WriteNumber("low", area.LowVnum);
        writer.WriteNumber("high", area.HighVnum);
        writer.WriteEndObject();

        if (area.Security != 9)
        {
            writer.WriteNumber("security", area.Security);
        }

        if (!string.IsNullOrEmpty(area.Builders))
        {
            writer.WriteString("builders", area.Builders);
        }

        WriteHelps(writer, area.Helps);
        WriteMobiles(writer, area.Mobiles);
        WriteObjects(writer, area.Objects);
        WriteRooms(writer, area.Rooms);
        WriteResets(writer, area.Resets);
        WriteShops(writer, area.Shops);
        WriteSpecials(writer, area.Specials);
        WriteSocials(writer, area.Socials);

        writer.WriteEndObject();
    }

    private static void WriteHelps(Utf8JsonWriter writer, IReadOnlyList<HelpEntry> helps)
    {
        if (helps.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("helps");
        foreach (var help in helps.OrderBy(entry => entry.Keywords, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("keywords", help.Keywords);
            writer.WriteNumber("level", help.Level);
            writer.WriteString("text", help.Text);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteMobiles(Utf8JsonWriter writer, IReadOnlyList<MobilePrototype> mobiles)
    {
        if (mobiles.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("mobiles");
        var index = 0;
        foreach (var mobile in mobiles.OrderBy(entry => entry.Vnum))
        {
            WriteMobile(writer, mobile, $"/mobiles/{index++}");
        }

        writer.WriteEndArray();
    }

    private static void WriteMobile(Utf8JsonWriter writer, MobilePrototype mobile, string path)
    {
        writer.WriteStartObject();
        writer.WriteNumber("vnum", mobile.Vnum);
        writer.WriteString("keywords", mobile.Keywords);
        writer.WriteString("shortDescription", mobile.ShortDescription);
        writer.WriteString("longDescription", mobile.LongDescription);
        writer.WriteString("description", mobile.Description);
        writer.WriteString("race", RomTables.Races.Get((int)mobile.Race).Name);
        WriteFlags(writer, "actFlags", RomFlags.Act, mobile.ActFlags);
        WriteFlags(writer, "affectedBy", RomFlags.Affect, mobile.AffectedBy);
        WriteFlags(writer, "offensiveFlags", RomFlags.Offensive, mobile.OffensiveFlags);
        WriteFlags(writer, "immunities", RomFlags.Immunity, mobile.Immunities);
        WriteFlags(writer, "resistances", RomFlags.Immunity, mobile.Resistances);
        WriteFlags(writer, "vulnerabilities", RomFlags.Immunity, mobile.Vulnerabilities);
        WriteFlags(writer, "form", RomFlags.Form, mobile.Form);
        WriteFlags(writer, "parts", RomFlags.Part, mobile.Parts);
        writer.WriteNumber("alignment", mobile.Alignment);
        writer.WriteNumber("group", mobile.Group);
        writer.WriteNumber("level", mobile.Level);
        writer.WriteNumber("hitRoll", mobile.HitRoll);
        WriteDice(writer, "hitDice", mobile.HitDice);
        WriteDice(writer, "manaDice", mobile.ManaDice);
        WriteDamage(writer, mobile.Damage);
        WriteMobileArmor(writer, mobile.Armor);
        writer.WriteString("startPosition", mobile.StartPosition.ToString());
        writer.WriteString("defaultPosition", mobile.DefaultPosition.ToString());
        writer.WriteString("sex", mobile.Sex.ToString());
        writer.WriteNumber("gold", mobile.Gold);
        writer.WriteString("size", mobile.Size.ToString());
        writer.WriteString("material", mobile.Material);
        writer.WriteEndObject();
    }

    private static void WriteMobileArmor(Utf8JsonWriter writer, ArmorValues armor)
    {
        writer.WriteStartObject("armor");
        WriteMobileArmorValue(writer, "pierce", armor.Pierce);
        WriteMobileArmorValue(writer, "bash", armor.Bash);
        WriteMobileArmorValue(writer, "slash", armor.Slash);
        WriteMobileArmorValue(writer, "exotic", armor.Exotic);
        writer.WriteEndObject();
    }

    private static void WriteMobileArmorValue(
        Utf8JsonWriter writer,
        string name,
        int internalValue)
    {
        if (internalValue % 10 == 0)
        {
            writer.WriteNumber(name, internalValue / 10);
            return;
        }

        writer.WriteNumber(name, internalValue / 10m);
    }

    private static void WriteObjects(Utf8JsonWriter writer, IReadOnlyList<ObjectPrototype> objects)
    {
        if (objects.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("objects");
        var index = 0;
        foreach (var obj in objects.OrderBy(entry => entry.Vnum))
        {
            WriteObject(writer, obj, $"/objects/{index++}");
        }

        writer.WriteEndArray();
    }

    private static void WriteObject(Utf8JsonWriter writer, ObjectPrototype obj, string path)
    {
        writer.WriteStartObject();
        writer.WriteNumber("vnum", obj.Vnum);
        writer.WriteString("keywords", obj.Keywords);
        writer.WriteString("shortDescription", obj.ShortDescription);
        writer.WriteString("longDescription", obj.LongDescription);
        writer.WriteString("material", obj.Material);
        writer.WriteString("itemType", obj.ItemType.ToString());
        WriteFlags(writer, "extraFlags", RomFlags.Extra, obj.ExtraFlags);
        WriteFlags(writer, "wearFlags", RomFlags.Wear, obj.WearFlags);
        writer.WriteNumber("level", obj.Level);
        WriteWeight(writer, obj.WeightTenths, path);
        writer.WriteNumber("costCopper", obj.CostCopper);
        writer.WriteNumber("condition", obj.Condition);
        WriteObjectData(writer, obj, path);
        WriteAffects(writer, obj.Affects, path);
        WriteExtraDescriptions(writer, obj.ExtraDescriptions);
        writer.WriteEndObject();
    }

    private static void WriteWeight(Utf8JsonWriter writer, int weightTenths, string path)
    {
        if (weightTenths % 10 == 0)
        {
            writer.WriteNumber("weightPounds", weightTenths / 10);
            return;
        }

        writer.WriteNumber("weightPounds", weightTenths / 10m);
    }

    private static void WriteObjectData(Utf8JsonWriter writer, ObjectPrototype obj, string path)
    {
        switch (obj.Data)
        {
            case WeaponData weapon:
                writer.WriteStartObject("weapon");
                writer.WriteString("class", weapon.Class.ToString());
                writer.WriteNumber("diceCount", weapon.DiceCount);
                writer.WriteNumber("diceSize", weapon.DiceSize);
                writer.WriteString("attackType", weapon.AttackType);
                WriteFlags(writer, "specialFlags", RomFlags.WeaponSpecial, weapon.SpecialFlags);
                writer.WriteEndObject();
                break;

            case ArmorData armor:
                writer.WriteStartObject("armor");
                writer.WriteNumber("pierce", armor.Values.Pierce);
                writer.WriteNumber("bash", armor.Values.Bash);
                writer.WriteNumber("slash", armor.Values.Slash);
                writer.WriteNumber("exotic", armor.Values.Exotic);
                writer.WriteEndObject();
                break;

            case ContainerData container:
                writer.WriteStartObject("container");
                writer.WriteNumber("capacity", container.Capacity);
                WriteFlags(writer, "flags", RomFlags.Container, container.Flags, required: true);
                writer.WriteNumber("keyVnum", container.KeyVnum);
                writer.WriteEndObject();
                break;

            case DrinkData drink:
                writer.WriteStartObject(obj.ItemType == ItemType.Fountain ? "fountain" : "drink");
                writer.WriteNumber("capacity", drink.Capacity);
                writer.WriteNumber("remaining", drink.Remaining);
                writer.WriteString("liquid", drink.Liquid);
                writer.WriteBoolean("poisoned", drink.Poisoned);
                writer.WriteEndObject();
                break;

            case ChargesData charges:
                writer.WriteStartObject("charges");
                writer.WriteNumber("level", charges.Level);
                writer.WriteNumber("total", charges.Total);
                writer.WriteNumber("remaining", charges.Remaining);
                writer.WriteString("spell", charges.Spell);
                writer.WriteEndObject();
                break;

            case SpellsData spells:
                writer.WriteStartObject("spells");
                writer.WriteNumber("level", spells.Level);
                writer.WriteStartArray("names");
                foreach (var name in spells.Names)
                {
                    writer.WriteStringValue(name);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                break;

            case FoodData food:
                writer.WriteStartObject("food");
                writer.WriteNumber("hours", food.Hours);
                writer.WriteBoolean("poisoned", food.Poisoned);
                writer.WriteEndObject();
                break;

            case MoneyData money:
                writer.WriteStartObject("money");
                writer.WriteNumber("gold", money.Gold);
                writer.WriteNumber("silver", money.Silver);
                writer.WriteEndObject();
                break;

            case FurnitureData furniture:
                writer.WriteStartObject("furniture");
                writer.WriteNumber("maxPeople", furniture.MaxPeople);
                WriteFlags(writer, "flags", RomFlags.Furniture, furniture.Flags, required: true);
                writer.WriteEndObject();
                break;

            case PortalData portal:
                writer.WriteStartObject("portal");
                WriteFlags(writer, "exitFlags", RomFlags.Exit, portal.ExitFlags, required: true);
                WriteFlags(writer, "gateFlags", RomFlags.Gate, portal.GateFlags, required: true);
                writer.WriteNumber("toRoomVnum", portal.ToRoomVnum);
                writer.WriteEndObject();
                break;

            case MapData map:
                writer.WriteStartObject("map");
                writer.WriteNumber("roomVnum", map.RoomVnum);
                writer.WriteEndObject();
                break;

            case LightData light:
                writer.WriteStartObject("light");
                writer.WriteNumber("duration", light.Duration);
                writer.WriteEndObject();
                break;

            case PlainObjectData plain:
                if (plain.RawValues is not null)
                {
                    writer.WriteStartArray("rawValues");
                    foreach (var value in plain.RawValues)
                    {
                        writer.WriteNumberValue(value);
                    }

                    writer.WriteEndArray();
                }

                break;

            default:
                throw new DataFormatException(path, $"Unsupported object data type '{obj.Data.GetType().Name}'.");
        }
    }

    private static void WriteAffects(
        Utf8JsonWriter writer,
        IReadOnlyList<AffectData> affects,
        string path)
    {
        if (affects.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("affects");
        var index = 0;
        foreach (var affect in affects)
        {
            var affectPath = $"{path}/affects/{index++}";
            if (affect.Where == AffectWhere.Weapon)
            {
                throw new DataFormatException(affectPath, "Weapon affects are not representable in area JSON.");
            }

            writer.WriteStartObject();
            if (affect.Where != AffectWhere.Object)
            {
                writer.WriteString("where", affect.Where.ToString());
            }

            writer.WriteString("location", affect.Location.ToString());
            writer.WriteNumber("modifier", affect.Modifier);
            if (affect.Bits != 0)
            {
                var registry = AffectData.RegistryFor(affect.Where)
                    ?? throw new DataFormatException(affectPath, "Flags are not allowed for this affect location.");
                WriteFlags(writer, "flags", registry, affect.Bits);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteExtraDescriptions(
        Utf8JsonWriter writer,
        IReadOnlyList<ExtraDescription> descriptions)
    {
        if (descriptions.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("extraDescriptions");
        foreach (var description in descriptions)
        {
            writer.WriteStartObject();
            writer.WriteString("keywords", description.Keywords);
            writer.WriteString("description", description.Description);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteRooms(Utf8JsonWriter writer, IReadOnlyList<RoomPrototype> rooms)
    {
        if (rooms.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("rooms");
        foreach (var room in rooms.OrderBy(entry => entry.Vnum))
        {
            writer.WriteStartObject();
            writer.WriteNumber("vnum", room.Vnum);
            writer.WriteString("name", room.Name);
            writer.WriteString("description", room.Description);
            WriteFlags(writer, "roomFlags", RomFlags.Room, room.RoomFlags);
            writer.WriteString("sector", room.Sector.ToString());
            writer.WriteNumber("healRate", room.HealRate);
            writer.WriteNumber("manaRate", room.ManaRate);
            writer.WriteNumber("clan", room.Clan);
            WriteExits(writer, room.Exits);
            WriteExtraDescriptions(writer, room.ExtraDescriptions);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteExits(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<Direction, ExitPrototype> exits)
    {
        if (exits.Count == 0)
        {
            return;
        }

        writer.WriteStartObject("exits");
        foreach (var direction in Directions)
        {
            if (!exits.TryGetValue(direction, out var exit))
            {
                continue;
            }

            writer.WritePropertyName(direction.ToString().ToLowerInvariant());
            writer.WriteStartObject();
            writer.WriteNumber("toRoom", exit.ToRoomVnum);
            WriteFlags(writer, "flags", RomFlags.Exit, exit.Flags);
            if (exit.KeyVnum > 0)
            {
                writer.WriteNumber("keyVnum", exit.KeyVnum);
            }

            if (!string.IsNullOrEmpty(exit.Description))
            {
                writer.WriteString("description", exit.Description);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteResets(Utf8JsonWriter writer, IReadOnlyList<Reset> resets)
    {
        if (resets.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("resets");
        foreach (var reset in resets)
        {
            writer.WriteStartObject();
            switch (reset)
            {
                case MobileReset mobile:
                    WriteResetHeader(writer, "Mobile", mobile.IfFlag);
                    writer.WriteNumber("mobileVnum", mobile.MobileVnum);
                    writer.WriteNumber("roomVnum", mobile.RoomVnum);
                    writer.WriteNumber("maxInWorld", mobile.MaxInWorld);
                    writer.WriteNumber("maxInRoom", mobile.MaxInRoom);
                    break;

                case ObjectReset obj:
                    WriteResetHeader(writer, "Object", obj.IfFlag);
                    writer.WriteNumber("objectVnum", obj.ObjectVnum);
                    writer.WriteNumber("roomVnum", obj.RoomVnum);
                    writer.WriteNumber("maxInWorld", obj.MaxInWorld);
                    break;

                case PutReset put:
                    WriteResetHeader(writer, "Put", put.IfFlag);
                    writer.WriteNumber("objectVnum", put.ObjectVnum);
                    writer.WriteNumber("containerVnum", put.ContainerVnum);
                    writer.WriteNumber("maxInWorld", put.MaxInWorld);
                    break;

                case GiveReset give:
                    WriteResetHeader(writer, "Give", give.IfFlag);
                    writer.WriteNumber("objectVnum", give.ObjectVnum);
                    writer.WriteNumber("maxInWorld", give.MaxInWorld);
                    break;

                case EquipReset equip:
                    WriteResetHeader(writer, "Equip", equip.IfFlag);
                    writer.WriteNumber("objectVnum", equip.ObjectVnum);
                    writer.WriteString("wearLocation", equip.WearLocation.ToString());
                    writer.WriteNumber("maxInWorld", equip.MaxInWorld);
                    break;

                case DoorReset door:
                    WriteResetHeader(writer, "Door", door.IfFlag);
                    writer.WriteNumber("roomVnum", door.RoomVnum);
                    writer.WriteString("direction", door.Direction.ToString());
                    writer.WriteString("state", door.State.ToString());
                    break;

                case RandomizeReset randomize:
                    WriteResetHeader(writer, "Randomize", randomize.IfFlag);
                    writer.WriteNumber("roomVnum", randomize.RoomVnum);
                    writer.WriteNumber("door", randomize.Door);
                    break;

                default:
                    throw new DataFormatException("/resets", $"Unsupported reset type '{reset.GetType().Name}'.");
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteResetHeader(Utf8JsonWriter writer, string type, bool ifFlag)
    {
        writer.WriteString("type", type);
        if (ifFlag)
        {
            writer.WriteBoolean("ifFlag", true);
        }
    }

    private static void WriteShops(Utf8JsonWriter writer, IReadOnlyList<ShopData> shops)
    {
        if (shops.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("shops");
        foreach (var shop in shops)
        {
            writer.WriteStartObject();
            writer.WriteNumber("keeperVnum", shop.KeeperVnum);
            writer.WriteStartArray("buyTypes");
            foreach (var itemType in shop.BuyTypes)
            {
                writer.WriteStringValue(itemType.ToString());
            }

            writer.WriteEndArray();
            writer.WriteNumber("profitBuy", shop.ProfitBuy);
            writer.WriteNumber("profitSell", shop.ProfitSell);
            writer.WriteNumber("openHour", shop.OpenHour);
            writer.WriteNumber("closeHour", shop.CloseHour);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteSpecials(Utf8JsonWriter writer, IReadOnlyList<SpecialData> specials)
    {
        if (specials.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("specials");
        foreach (var special in specials)
        {
            writer.WriteStartObject();
            writer.WriteNumber("mobileVnum", special.MobileVnum);
            writer.WriteString("procedure", special.Procedure);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteSocials(Utf8JsonWriter writer, IReadOnlyList<SocialData> socials)
    {
        if (socials.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("socials");
        foreach (var social in socials.OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("name", social.Name);
            writer.WriteString("charNoArgument", social.CharNoArgument);
            writer.WriteString("othersNoArgument", social.OthersNoArgument);
            writer.WriteString("charFound", social.CharFound);
            writer.WriteString("othersFound", social.OthersFound);
            writer.WriteString("victimFound", social.VictimFound);
            writer.WriteString("charNotFound", social.CharNotFound);
            writer.WriteString("charAuto", social.CharAuto);
            writer.WriteString("othersAuto", social.OthersAuto);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteDice(Utf8JsonWriter writer, string name, Dice dice)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("count", dice.Count);
        writer.WriteNumber("size", dice.Size);
        writer.WriteNumber("bonus", dice.Bonus);
        writer.WriteEndObject();
    }

    private static void WriteDamage(Utf8JsonWriter writer, DamageDice damage)
    {
        writer.WriteStartObject("damage");
        writer.WriteNumber("count", damage.Dice.Count);
        writer.WriteNumber("size", damage.Dice.Size);
        writer.WriteNumber("bonus", damage.Dice.Bonus);
        writer.WriteString("attackType", damage.AttackType);
        writer.WriteEndObject();
    }

    private static void WriteFlags(
        Utf8JsonWriter writer,
        string name,
        FlagRegistry registry,
        int mask,
        bool required = false)
    {
        var names = registry.GetNames(mask);
        if (names.Count == 0)
        {
            if (required)
            {
                writer.WriteStartArray(name);
                writer.WriteEndArray();
            }

            return;
        }

        writer.WriteStartArray(name);
        foreach (var flag in names)
        {
            writer.WriteStringValue(flag);
        }

        writer.WriteEndArray();
    }
}
