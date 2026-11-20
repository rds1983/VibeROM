using System.Text;

namespace VibeROM.Common.Text;

/// <summary>
/// Converts ROM-style brace colour codes to ANSI escapes. Unknown codes are
/// preserved so ordinary text containing braces is not lost.
/// </summary>
public static class Colour
{
    public const string Reset = "\u001b[0m";

    private static readonly Dictionary<char, string> AnsiCodes = new()
    {
        ['x'] = Reset,
        ['n'] = Reset,
        ['k'] = "\u001b[0;30m",
        ['K'] = "\u001b[1;30m",
        ['r'] = "\u001b[0;31m",
        ['R'] = "\u001b[1;31m",
        ['g'] = "\u001b[0;32m",
        ['G'] = "\u001b[1;32m",
        ['y'] = "\u001b[0;33m",
        ['Y'] = "\u001b[1;33m",
        ['b'] = "\u001b[0;34m",
        ['B'] = "\u001b[1;34m",
        ['m'] = "\u001b[0;35m",
        ['M'] = "\u001b[1;35m",
        ['c'] = "\u001b[0;36m",
        ['C'] = "\u001b[1;36m",
        ['w'] = "\u001b[0;37m",
        ['W'] = "\u001b[1;37m",
        ['d'] = "\u001b[1;30m",
        ['D'] = "\u001b[0;37m"
    };

    public static string ToAnsi(string text, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character != '{' || index + 1 >= text.Length)
            {
                builder.Append(character);
                continue;
            }

            var code = text[index + 1];
            if (code == '{')
            {
                builder.Append('{');
                index++;
                continue;
            }

            if (AnsiCodes.TryGetValue(code, out var ansi))
            {
                if (enabled)
                {
                    builder.Append(ansi);
                }

                index++;
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    public static string Strip(string text) => ToAnsi(text, enabled: false);
}
