using System.Net;

namespace VibeROM.Common.Configuration;

/// <summary>Root configuration document. Every value has a safe default.</summary>
public sealed record GameOptions
{
    public ServerOptions Server { get; init; } = new();

    public DataOptions Data { get; init; } = new();

    public LoggingOptions Logging { get; init; } = new();

    public GameplayOptions Gameplay { get; init; } = new();

    public static GameOptions Default { get; } = new();

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Server.BindAddress))
        {
            errors.Add("server.bindAddress must not be empty.");
        }
        else if (Server.BindAddress is not "*" and not "localhost"
                 && !IPAddress.TryParse(Server.BindAddress, out _))
        {
            errors.Add("server.bindAddress must be '*', 'localhost' or an IP address.");
        }

        if (Server.Port is < 1 or > 65535)
        {
            errors.Add($"server.port must be between 1 and 65535 (was {Server.Port}).");
        }

        if (Server.MaxConnections < 1)
        {
            errors.Add($"server.maxConnections must be at least 1 (was {Server.MaxConnections}).");
        }

        if (Server.IdleTimeoutSeconds is < 1 or > 86400)
        {
            errors.Add(
                $"server.idleTimeoutSeconds must be between 1 and 86400 (was {Server.IdleTimeoutSeconds}).");
        }

        ValidatePath("data.directory", Data.Directory, allowAbsolute: true, errors);
        ValidatePath("data.areas", Data.Areas, allowAbsolute: false, errors);
        ValidatePath("data.players", Data.Players, allowAbsolute: false, errors);
        ValidatePath("data.notes", Data.Notes, allowAbsolute: false, errors);
        ValidatePath("data.gods", Data.Gods, allowAbsolute: false, errors);
        ValidatePath("data.music", Data.Music, allowAbsolute: false, errors);
        ValidatePath("data.schema", Data.Schema, allowAbsolute: false, errors);

        if (!LoggingOptions.AllowedLevels.Contains(Logging.Level, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                $"logging.level must be one of {string.Join(", ", LoggingOptions.AllowedLevels)} (was '{Logging.Level}').");
        }

        ValidatePath("logging.directory", Logging.Directory, allowAbsolute: true, errors);

        if (string.IsNullOrWhiteSpace(Logging.FilePrefix))
        {
            errors.Add("logging.filePrefix must not be empty.");
        }
        else if (Logging.FilePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                 || Logging.FilePrefix.Contains('/')
                 || Logging.FilePrefix.Contains('\\'))
        {
            errors.Add("logging.filePrefix must be a simple file name prefix.");
        }

        if (Logging.MaxArchiveFiles < 1)
        {
            errors.Add($"logging.maxArchiveFiles must be at least 1 (was {Logging.MaxArchiveFiles}).");
        }

        if (Gameplay.AutosaveIntervalSeconds < 0)
        {
            errors.Add(
                $"gameplay.autosaveIntervalSeconds must be 0 or greater (was {Gameplay.AutosaveIntervalSeconds}).");
        }

        return errors;
    }

    public ResolvedGamePaths ResolvePaths(string baseDirectory)
    {
        var dataDirectory = ResolvePath(baseDirectory, Data.Directory);
        var logsDirectory = ResolvePath(baseDirectory, Logging.Directory);

        return new ResolvedGamePaths(
            DataDirectory: dataDirectory,
            AreasDirectory: Path.Combine(dataDirectory, Data.Areas),
            PlayersDirectory: Path.Combine(dataDirectory, Data.Players),
            NotesDirectory: Path.Combine(dataDirectory, Data.Notes),
            GodsDirectory: Path.Combine(dataDirectory, Data.Gods),
            MusicDirectory: Path.Combine(dataDirectory, Data.Music),
            SchemaDirectory: Path.Combine(dataDirectory, Data.Schema),
            LogsDirectory: logsDirectory);
    }

    private static string ResolvePath(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path));

    private static void ValidatePath(
        string field,
        string value,
        bool allowAbsolute,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} must not be empty.");
            return;
        }

        if (!allowAbsolute && Path.IsPathRooted(value))
        {
            errors.Add($"{field} must be a relative path.");
            return;
        }

        var segments = value.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".."))
        {
            errors.Add($"{field} must not contain '..'.");
        }
    }
}

public sealed record ServerOptions
{
    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 4000;

    public int MaxConnections { get; init; } = 100;

    public int IdleTimeoutSeconds { get; init; } = 600;
}

public sealed record DataOptions
{
    public string Directory { get; init; } = "Data";

    public string Areas { get; init; } = "areas";

    public string Players { get; init; } = "players";

    public string Notes { get; init; } = "notes";

    public string Gods { get; init; } = "gods";

    public string Music { get; init; } = "music";

    public string Schema { get; init; } = "schema";
}

public sealed record LoggingOptions
{
    public static IReadOnlyList<string> AllowedLevels { get; } =
        ["Trace", "Debug", "Info", "Warn", "Error", "Fatal", "Off"];

    public string Level { get; init; } = "Info";

    public string Directory { get; init; } = "logs";

    public string FilePrefix { get; init; } = "vibrom";

    public int MaxArchiveFiles { get; init; } = 14;

    public static LoggingOptions Default { get; } = new();
}

public sealed record GameplayOptions
{
    public int AutosaveIntervalSeconds { get; init; } = 300;
}

/// <summary>Absolute paths derived from configuration and the config file location.</summary>
public sealed record ResolvedGamePaths(
    string DataDirectory,
    string AreasDirectory,
    string PlayersDirectory,
    string NotesDirectory,
    string GodsDirectory,
    string MusicDirectory,
    string SchemaDirectory,
    string LogsDirectory);
