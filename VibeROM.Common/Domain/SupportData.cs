using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

public sealed record HelpEntry(string Keywords, int Level, string Text)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Keywords))
        {
            errors.Add($"{field}.keywords must not be empty.");
        }

        if (Level is < -1 or > 100)
        {
            errors.Add($"{field}.level must be between -1 and 100.");
        }

        return errors;
    }
}

public sealed record ExtraDescription(string Keywords, string Description)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Keywords))
        {
            errors.Add($"{field}.keywords must not be empty.");
        }

        return errors;
    }
}

public sealed record SocialData(
    string Name,
    string CharNoArgument,
    string OthersNoArgument,
    string CharFound,
    string OthersFound,
    string VictimFound,
    string CharNotFound,
    string CharAuto,
    string OthersAuto)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add($"{field}.name must not be empty.");
        }

        return errors;
    }
}

public sealed record ShopData(
    int KeeperVnum,
    IReadOnlyList<ItemType> BuyTypes,
    int ProfitBuy,
    int ProfitSell,
    int OpenHour,
    int CloseHour)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (KeeperVnum < 0)
        {
            errors.Add($"{field}.keeperVnum must not be negative.");
        }

        if (BuyTypes.Any(itemType => !Enum.IsDefined(itemType)))
        {
            errors.Add($"{field}.buyTypes contains an invalid item type.");
        }

        if (ProfitBuy is < 0 or > 1000 || ProfitSell is < 0 or > 1000)
        {
            errors.Add($"{field} profit percentages must be between 0 and 1000.");
        }

        if (OpenHour is < 0 or > 23 || CloseHour is < 0 or > 23)
        {
            errors.Add($"{field} hours must be between 0 and 23.");
        }

        return errors;
    }
}

public sealed record SpecialData(int MobileVnum, string Procedure)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (MobileVnum < 0)
        {
            errors.Add($"{field}.mobileVnum must not be negative.");
        }

        if (!RomTables.Specials.TryGet(Procedure, out _))
        {
            errors.Add($"{field}.procedure '{Procedure}' is not a canonical special procedure.");
        }

        return errors;
    }
}
