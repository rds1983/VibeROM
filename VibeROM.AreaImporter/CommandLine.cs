namespace VibeROM.AreaImporter;

public sealed record ParsedCommandLine(
    ImportRequest? Request,
    IReadOnlyList<string> Errors,
    bool HelpRequested);

/// <summary>Parses the importer command line without touching the filesystem.</summary>
public static class CommandLine
{
    public static ParsedCommandLine Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new ParsedCommandLine(null, ["No arguments specified."], false);
        }

        string? source = null;
        string? output = null;
        string? file = null;
        string? report = null;
        var strict = false;
        var check = false;
        var help = false;
        var errors = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (argument is "--help" or "-h" or "-?" or "/?")
            {
                help = true;
                continue;
            }

            if (argument is "--strict")
            {
                strict = true;
                continue;
            }

            if (argument is "--check")
            {
                check = true;
                continue;
            }

            if (TryReadValue(args, ref index, argument, "--source", errors, out var sourceValue))
            {
                source = sourceValue;
                continue;
            }

            if (TryReadValue(args, ref index, argument, "--output", errors, out var outputValue))
            {
                output = outputValue;
                continue;
            }

            if (TryReadValue(args, ref index, argument, "--file", errors, out var fileValue))
            {
                file = fileValue;
                continue;
            }

            if (TryReadValue(args, ref index, argument, "--report", errors, out var reportValue))
            {
                report = reportValue;
                continue;
            }

            if (!argument.StartsWith('-'))
            {
                errors.Add($"Unexpected argument '{argument}'.");
            }
            else
            {
                errors.Add($"Unknown option '{argument}'.");
            }
        }

        if (help)
        {
            return new ParsedCommandLine(null, errors, true);
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            errors.Add("--source <dir> is required.");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            errors.Add("--output <dir> is required.");
        }

        if (file is not null && string.IsNullOrWhiteSpace(file))
        {
            errors.Add("--file requires a non-empty name.");
        }

        if (errors.Count > 0)
        {
            return new ParsedCommandLine(null, errors, false);
        }

        return new ParsedCommandLine(
            new ImportRequest(source!, output!, file, report, strict, check),
            [],
            false);
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        string option,
        ICollection<string> errors,
        out string? value)
    {
        value = null;
        if (argument == option)
        {
            if (index + 1 >= args.Count || args[index + 1].StartsWith('-'))
            {
                errors.Add($"{option} requires a value.");
                return true;
            }

            value = args[++index];
            return true;
        }

        var prefix = option + "=";
        if (argument.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = argument[prefix.Length..];
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{option} requires a value.");
            }

            return true;
        }

        return false;
    }
}
