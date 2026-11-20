namespace VibeROM.Common.Text;

/// <summary>The token returned by <see cref="ArgumentParser.OneArgument"/> and the unparsed rest.</summary>
public readonly record struct ArgumentToken(string Value, string Remainder);

/// <summary>Port of ROM's one_argument: quoted tokens, lowercasing and leading-space skipping.</summary>
public static class ArgumentParser
{
    public static ArgumentToken OneArgument(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        var index = 0;
        while (index < argument.Length && char.IsWhiteSpace(argument[index]))
        {
            index++;
        }

        var endCharacter = ' ';
        if (index < argument.Length && argument[index] is '\'' or '"')
        {
            endCharacter = argument[index];
            index++;
        }

        var start = index;
        while (index < argument.Length && argument[index] != endCharacter)
        {
            index++;
        }

        var value = argument[start..index].ToLowerInvariant();

        if (index < argument.Length)
        {
            index++;
        }

        while (index < argument.Length && char.IsWhiteSpace(argument[index]))
        {
            index++;
        }

        return new ArgumentToken(value, argument[index..]);
    }

    public static IReadOnlyList<string> Split(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        var result = new List<string>();
        var remainder = argument;

        while (remainder.Length > 0)
        {
            var token = OneArgument(remainder);
            if (token.Value.Length > 0)
            {
                result.Add(token.Value);
            }

            if (token.Remainder.Length == remainder.Length)
            {
                break;
            }

            remainder = token.Remainder;
        }

        return result;
    }
}
