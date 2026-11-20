namespace VibeROM.Common.Text;

/// <summary>Human-readable money strings used by score, worth and money piles.</summary>
public static class MoneyFormatter
{
    /// <summary>Formats a wallet as "10 gold and 25 silver".</summary>
    public static string Format(long gold, long silver) =>
        $"{gold} gold and {silver} silver";

    /// <summary>Formats a money pile exactly like ROM's ITEM_MONEY look output.</summary>
    public static string FormatPile(long gold, long silver)
    {
        if (gold == 0 && silver == 0)
        {
            return "Odd...there's no coins in the pile.";
        }

        if (gold == 0)
        {
            return silver == 1
                ? "Wow. One silver coin."
                : $"There are {silver} silver coins in the pile.";
        }

        if (silver == 0)
        {
            return gold == 1
                ? "Wow. One gold coin."
                : $"There are {gold} gold coins in the pile.";
        }

        return $"There are {gold} gold and {silver} silver coins in the pile.";
    }
}
