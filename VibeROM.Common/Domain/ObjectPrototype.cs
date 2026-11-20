using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

public sealed record ObjectPrototype(
    int Vnum,
    string Keywords,
    string ShortDescription,
    string LongDescription,
    string Material,
    ItemType ItemType,
    int ExtraFlags,
    int WearFlags,
    int Level,
    int WeightTenths,
    int CostCopper,
    int Condition,
    ObjectTypeData Data,
    IReadOnlyList<AffectData> Affects,
    IReadOnlyList<ExtraDescription> ExtraDescriptions)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        DomainValidation.ValidateVnum(errors, "vnum", Vnum);
        DomainValidation.ValidateKeywords(errors, "keywords", Keywords);

        // ROM allows an empty material on objects such as donation pits.

        if (!Enum.IsDefined(ItemType))
        {
            errors.Add("itemType is invalid.");
        }

        if (!DataMatchesItemType())
        {
            errors.Add($"data type '{Data.GetType().Name}' does not match itemType '{ItemType}'.");
        }

        DomainValidation.ValidateMask(errors, "extraFlags", ExtraFlags, RomFlags.Extra);
        DomainValidation.ValidateMask(errors, "wearFlags", WearFlags, RomFlags.Wear);

        if (Level < 0) errors.Add("level must not be negative.");
        if (WeightTenths < 0) errors.Add("weightTenths must not be negative.");
        if (CostCopper < 0) errors.Add("costCopper must not be negative.");
        if (Condition is < 0 or > 100) errors.Add("condition must be between 0 and 100.");

        ValidateData(errors);
        for (var index = 0; index < Affects.Count; index++)
        {
            errors.AddRange(Affects[index].Validate($"affects[{index}]"));
        }

        for (var index = 0; index < ExtraDescriptions.Count; index++)
        {
            errors.AddRange(ExtraDescriptions[index].Validate($"extraDescriptions[{index}]"));
        }

        return errors;
    }

    private bool DataMatchesItemType() =>
        ItemType switch
        {
            ItemType.Light => Data is LightData,
            ItemType.Scroll or ItemType.Potion or ItemType.Pill => Data is SpellsData,
            ItemType.Wand or ItemType.Staff => Data is ChargesData,
            ItemType.Weapon => Data is WeaponData,
            ItemType.Armor => Data is ArmorData,
            ItemType.Container => Data is ContainerData,
            ItemType.DrinkContainer or ItemType.Fountain => Data is DrinkData,
            ItemType.Food => Data is FoodData,
            ItemType.Money => Data is MoneyData,
            ItemType.Furniture => Data is FurnitureData,
            ItemType.Portal => Data is PortalData,
            ItemType.Map => Data is MapData,
            _ => Data is PlainObjectData
        };

    private void ValidateData(ICollection<string> errors)
    {
        switch (Data)
        {
            case LightData light when light.Duration < -1:
                errors.Add("light.duration must be -1 or greater.");
                break;

            case SpellsData spells:
                if (spells.Level < 0) errors.Add("spells.level must not be negative.");
                if (spells.Names.Count is < 1 or > 4) errors.Add("spells.names must contain 1 to 4 spells.");
                if (spells.Names.Any(string.IsNullOrWhiteSpace)) errors.Add("spells.names must not contain empty names.");
                break;

            case ChargesData charges:
                if (charges.Level < 0) errors.Add("charges.level must not be negative.");
                if (charges.Total < 0 || charges.Remaining < 0) errors.Add("charges values must not be negative.");
                if (charges.Remaining > charges.Total) errors.Add("charges.remaining must not exceed charges.total.");
                if (string.IsNullOrWhiteSpace(charges.Spell)) errors.Add("charges.spell must not be empty.");
                break;

            case WeaponData weapon:
                if (!Enum.IsDefined(weapon.Class)) errors.Add("weapon.class is invalid.");
                if (weapon.DiceCount < 0 || weapon.DiceSize < 0) errors.Add("weapon dice must not be negative.");
                if (!RomTables.AttackTypes.TryGet(weapon.AttackType, out _))
                {
                    errors.Add($"weapon.attackType '{weapon.AttackType}' is invalid.");
                }

                DomainValidation.ValidateMask(errors, "weapon.specialFlags", weapon.SpecialFlags, RomFlags.WeaponSpecial);
                break;

            case ContainerData container:
                if (container.Capacity < 0) errors.Add("container.capacity must not be negative.");
                if (container.KeyVnum < -1) errors.Add("container.keyVnum must be -1 or greater.");
                DomainValidation.ValidateMask(errors, "container.flags", container.Flags, RomFlags.Container);
                break;

            case DrinkData drink:
                if (drink.Capacity < 0 || drink.Remaining < 0) errors.Add("drink values must not be negative.");
                if (drink.Remaining > drink.Capacity) errors.Add("drink.remaining must not exceed drink.capacity.");
                if (!RomTables.Liquids.TryGet(drink.Liquid, out _))
                {
                    errors.Add($"drink.liquid '{drink.Liquid}' is invalid.");
                }

                break;

            case FoodData food:
                if (food.Hours < 0) errors.Add("food.hours must not be negative.");
                break;

            case MoneyData money:
                if (money.Gold < 0 || money.Silver < 0) errors.Add("money values must not be negative.");
                break;

            case FurnitureData furniture:
                if (furniture.MaxPeople < 0) errors.Add("furniture.maxPeople must not be negative.");
                DomainValidation.ValidateMask(errors, "furniture.flags", furniture.Flags, RomFlags.Furniture);
                break;

            case PortalData portal:
                if (portal.ToRoomVnum < 0) errors.Add("portal.toRoomVnum must not be negative.");
                DomainValidation.ValidateMask(errors, "portal.exitFlags", portal.ExitFlags, RomFlags.Exit);
                DomainValidation.ValidateMask(errors, "portal.gateFlags", portal.GateFlags, RomFlags.Gate);
                break;

            case MapData map:
                if (map.RoomVnum < 0) errors.Add("map.roomVnum must not be negative.");
                break;

            case PlainObjectData plain when plain.RawValues is { Count: not 5 }:
                errors.Add("plain.rawValues must contain exactly 5 values when present.");
                break;
        }
    }
}
