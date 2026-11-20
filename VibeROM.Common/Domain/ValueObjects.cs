namespace VibeROM.Common.Domain;

/// <summary>ROM dice: <c>count</c> dice of <c>size</c> sides plus a flat bonus.</summary>
public readonly record struct Dice(int Count, int Size, int Bonus)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (Count < 0)
        {
            errors.Add($"{field}.count must not be negative.");
        }

        if (Size < 0)
        {
            errors.Add($"{field}.size must not be negative.");
        }

        return errors;
    }
}

public sealed record DamageDice(Dice Dice, string AttackType)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>(Dice.Validate(field));
        if (!Rom.RomTables.AttackTypes.TryGet(AttackType, out _))
        {
            errors.Add($"{field}.attackType '{AttackType}' is not a canonical attack type.");
        }

        return errors;
    }
}

/// <summary>The four ROM armor classes, in pierce/bash/slash/exotic order.</summary>
public readonly record struct ArmorValues(int Pierce, int Bash, int Slash, int Exotic);
