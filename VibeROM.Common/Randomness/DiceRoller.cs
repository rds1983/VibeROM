namespace VibeROM.Common.Randomness;

/// <summary>Port of ROM's dice and random-number helpers.</summary>
public sealed class DiceRoller
{
    private readonly Random _random;

    public DiceRoller(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>Returns 1..100, matching ROM's <c>number_percent</c>.</summary>
    public int NumberPercent() => _random.Next(1, 101);

    /// <summary>
    /// Returns an inclusive range. Matches ROM for <c>(0,0)</c> and for
    /// <paramref name="to"/> less than <paramref name="from"/>.
    /// </summary>
    public int NumberRange(int from, int to)
    {
        if (from == 0 && to == 0)
        {
            return 0;
        }

        var range = (long)to - from + 1;
        if (range <= 1)
        {
            return from;
        }

        return from + _random.Next(0, (int)range);
    }

    /// <summary>Returns the low <paramref name="width"/> random bits (0..30).</summary>
    public int NumberBits(int width)
    {
        if (width is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 0 and 30.");
        }

        return _random.Next(0, 1 << width);
    }

    /// <summary>Randomly adjusts a number by -1, 0 or +1, clamped to at least 1.</summary>
    public int NumberFuzzy(int number)
    {
        switch (NumberBits(2))
        {
            case 0:
                number--;
                break;
            case 3:
                number++;
                break;
        }

        return Math.Max(1, number);
    }

    /// <summary>Rolls <paramref name="number"/> dice of <paramref name="size"/> sides.</summary>
    public int Dice(int number, int size)
    {
        if (size <= 0)
        {
            return 0;
        }

        if (size == 1)
        {
            return number;
        }

        var sum = 0;
        for (var index = 0; index < number; index++)
        {
            sum += NumberRange(1, size);
        }

        return sum;
    }
}
