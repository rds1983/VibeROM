using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

public sealed record MobilePrototype(
    int Vnum,
    string Keywords,
    string ShortDescription,
    string LongDescription,
    string Description,
    Race Race,
    int ActFlags,
    int AffectedBy,
    int OffensiveFlags,
    int Immunities,
    int Resistances,
    int Vulnerabilities,
    int Form,
    int Parts,
    int Alignment,
    int Group,
    int Level,
    int HitRoll,
    Dice HitDice,
    Dice ManaDice,
    DamageDice Damage,
    ArmorValues Armor,
    Position StartPosition,
    Position DefaultPosition,
    Sex Sex,
    int Gold,
    Size Size,
    string Material)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        DomainValidation.ValidateVnum(errors, "vnum", Vnum);
        DomainValidation.ValidateKeywords(errors, "keywords", Keywords);

        if (string.IsNullOrWhiteSpace(ShortDescription))
        {
            errors.Add("shortDescription must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(Material))
        {
            errors.Add("material must not be empty.");
        }

        if (!Enum.IsDefined(Race)) errors.Add("race is invalid.");
        if (!Enum.IsDefined(StartPosition)) errors.Add("startPosition is invalid.");
        if (!Enum.IsDefined(DefaultPosition)) errors.Add("defaultPosition is invalid.");
        if (!Enum.IsDefined(Sex)) errors.Add("sex is invalid.");
        if (!Enum.IsDefined(Size)) errors.Add("size is invalid.");

        DomainValidation.ValidateMask(errors, "actFlags", ActFlags, RomFlags.Act);
        DomainValidation.ValidateMask(errors, "affectedBy", AffectedBy, RomFlags.Affect);
        DomainValidation.ValidateMask(errors, "offensiveFlags", OffensiveFlags, RomFlags.Offensive);
        DomainValidation.ValidateMask(errors, "immunities", Immunities, RomFlags.Immunity);
        DomainValidation.ValidateMask(errors, "resistances", Resistances, RomFlags.Immunity);
        DomainValidation.ValidateMask(errors, "vulnerabilities", Vulnerabilities, RomFlags.Immunity);
        DomainValidation.ValidateMask(errors, "form", Form, RomFlags.Form);
        DomainValidation.ValidateMask(errors, "parts", Parts, RomFlags.Part);

        if (Level < 0) errors.Add("level must not be negative.");
        if (Group < 0) errors.Add("group must not be negative.");
        if (Gold < 0) errors.Add("gold must not be negative.");

        errors.AddRange(HitDice.Validate("hitDice"));
        errors.AddRange(ManaDice.Validate("manaDice"));
        errors.AddRange(Damage.Validate("damage"));

        return errors;
    }
}
