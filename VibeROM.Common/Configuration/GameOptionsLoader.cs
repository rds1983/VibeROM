using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VibeROM.Common.Configuration;

/// <summary>Environment variable names supported by the loader.</summary>
public static class GameOptionsEnvironment
{
    public const string ConfigPath = "VIBEROM_CONFIG";
    public const string ServerBindAddress = "VIBEROM_SERVER__BIND_ADDRESS";
    public const string ServerPort = "VIBEROM_SERVER__PORT";
    public const string ServerMaxConnections = "VIBEROM_SERVER__MAX_CONNECTIONS";
    public const string ServerIdleTimeoutSeconds = "VIBEROM_SERVER__IDLE_TIMEOUT_SECONDS";
    public const string DataDirectory = "VIBEROM_DATA__DIRECTORY";
    public const string DataAreas = "VIBEROM_DATA__AREAS";
    public const string DataPlayers = "VIBEROM_DATA__PLAYERS";
    public const string DataNotes = "VIBEROM_DATA__NOTES";
    public const string DataGods = "VIBEROM_DATA__GODS";
    public const string DataMusic = "VIBEROM_DATA__MUSIC";
    public const string DataSchema = "VIBEROM_DATA__SCHEMA";
    public const string LoggingLevel = "VIBEROM_LOGGING__LEVEL";
    public const string LoggingDirectory = "VIBEROM_LOGGING__DIRECTORY";
    public const string LoggingFilePrefix = "VIBEROM_LOGGING__FILE_PREFIX";
    public const string LoggingMaxArchiveFiles = "VIBEROM_LOGGING__MAX_ARCHIVE_FILES";
    public const string GameAutosaveIntervalSeconds = "VIBEROM_GAMEPLAY__AUTOSAVE_INTERVAL_SECONDS";
    public const string GameWizlock = "VIBEROM_GAMEPLAY__WIZLOCK";
    public const string GameMaxOnline = "VIBEROM_GAMEPLAY__MAX_ONLINE";
    public const string GameStartingRoomVnum = "VIBEROM_GAMEPLAY__STARTING_ROOM_VNUM";
    public const string GameBannedSites = "VIBEROM_GAMEPLAY__BANNED_SITES";
    public const string GameBannedNames = "VIBEROM_GAMEPLAY__BANNED_NAMES";
}

/// <summary>A validated configuration plus the location it was loaded from.</summary>
public sealed record LoadedGameOptions(GameOptions Options, string? ConfigPath, string BaseDirectory)
{
    public ResolvedGamePaths Paths => Options.ResolvePaths(BaseDirectory);
}

/// <summary>Loads VibeROM.json, applies environment overrides and validates the result.</summary>
public static class GameOptionsLoader
{
    public const string DefaultFileName = "VibeROM.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static LoadedGameOptions Load(
        string? configPath,
        bool requireFile,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= ReadEnvironment();

        string? fullConfigPath;
        try
        {
            fullConfigPath = configPath is null ? null : Path.GetFullPath(configPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new GameConfigurationException(
                $"Configuration path '{configPath}' is not valid: {exception.Message}",
                exception);
        }

        GameOptions options;
        if (fullConfigPath is null)
        {
            options = GameOptions.Default;
        }
        else if (!File.Exists(fullConfigPath))
        {
            if (requireFile)
            {
                throw new GameConfigurationException($"Configuration file '{fullConfigPath}' was not found.");
            }

            fullConfigPath = null;
            options = GameOptions.Default;
        }
        else
        {
            try
            {
                options = JsonSerializer.Deserialize<GameOptions>(
                        File.ReadAllText(fullConfigPath),
                        JsonOptions)
                    ?? new GameOptions();
            }
            catch (JsonException exception)
            {
                throw new GameConfigurationException(
                    $"Configuration file '{fullConfigPath}' is not valid JSON: {exception.Message}",
                    exception);
            }
            catch (IOException exception)
            {
                throw new GameConfigurationException(
                    $"Configuration file '{fullConfigPath}' could not be read: {exception.Message}",
                    exception);
            }
        }

        var baseDirectory = fullConfigPath is null
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(fullConfigPath) ?? AppContext.BaseDirectory;

        options = ApplyEnvironment(options, environment, out var environmentErrors);
        var errors = environmentErrors.Concat(options.Validate()).ToArray();
        if (errors.Length > 0)
        {
            throw new GameConfigurationException(FormatErrors(fullConfigPath, errors));
        }

        return new LoadedGameOptions(options, fullConfigPath, baseDirectory);
    }

    /// <summary>
    /// Walks from <paramref name="startDirectory"/> towards the filesystem root looking
    /// for VibeROM.json, then checks <paramref name="appBaseDirectory"/>.
    /// </summary>
    public static string? FindConfigFile(string startDirectory, string appBaseDirectory)
    {
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            directory = null;
        }

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, DefaultFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        var appCandidate = Path.Combine(Path.GetFullPath(appBaseDirectory), DefaultFileName);
        return File.Exists(appCandidate) ? appCandidate : null;
    }

    private static GameOptions ApplyEnvironment(
        GameOptions options,
        IReadOnlyDictionary<string, string?> environment,
        out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();

        var server = options.Server;
        if (Get(environment, GameOptionsEnvironment.ServerBindAddress) is { } bindAddress)
        {
            server = server with { BindAddress = bindAddress };
        }

        if (GetInt(environment, GameOptionsEnvironment.ServerPort, problems) is { } port)
        {
            server = server with { Port = port };
        }

        if (GetInt(environment, GameOptionsEnvironment.ServerMaxConnections, problems) is { } maxConnections)
        {
            server = server with { MaxConnections = maxConnections };
        }

        if (GetInt(environment, GameOptionsEnvironment.ServerIdleTimeoutSeconds, problems) is { } idleTimeout)
        {
            server = server with { IdleTimeoutSeconds = idleTimeout };
        }

        var data = options.Data;
        if (Get(environment, GameOptionsEnvironment.DataDirectory) is { } dataDirectory)
        {
            data = data with { Directory = dataDirectory };
        }

        if (Get(environment, GameOptionsEnvironment.DataAreas) is { } areas)
        {
            data = data with { Areas = areas };
        }

        if (Get(environment, GameOptionsEnvironment.DataPlayers) is { } players)
        {
            data = data with { Players = players };
        }

        if (Get(environment, GameOptionsEnvironment.DataNotes) is { } notes)
        {
            data = data with { Notes = notes };
        }

        if (Get(environment, GameOptionsEnvironment.DataGods) is { } gods)
        {
            data = data with { Gods = gods };
        }

        if (Get(environment, GameOptionsEnvironment.DataMusic) is { } music)
        {
            data = data with { Music = music };
        }

        if (Get(environment, GameOptionsEnvironment.DataSchema) is { } schema)
        {
            data = data with { Schema = schema };
        }

        var logging = options.Logging;
        if (Get(environment, GameOptionsEnvironment.LoggingLevel) is { } level)
        {
            logging = logging with { Level = level };
        }

        if (Get(environment, GameOptionsEnvironment.LoggingDirectory) is { } logDirectory)
        {
            logging = logging with { Directory = logDirectory };
        }

        if (Get(environment, GameOptionsEnvironment.LoggingFilePrefix) is { } filePrefix)
        {
            logging = logging with { FilePrefix = filePrefix };
        }

        if (GetInt(environment, GameOptionsEnvironment.LoggingMaxArchiveFiles, problems) is { } maxArchiveFiles)
        {
            logging = logging with { MaxArchiveFiles = maxArchiveFiles };
        }

        var gameplay = options.Gameplay;
        if (GetInt(environment, GameOptionsEnvironment.GameAutosaveIntervalSeconds, problems) is { } autosave)
        {
            gameplay = gameplay with { AutosaveIntervalSeconds = autosave };
        }

        if (GetBool(environment, GameOptionsEnvironment.GameWizlock, problems) is { } wizlock)
        {
            gameplay = gameplay with { Wizlock = wizlock };
        }

        if (GetInt(environment, GameOptionsEnvironment.GameMaxOnline, problems) is { } maxOnline)
        {
            gameplay = gameplay with { MaxOnline = maxOnline };
        }

        if (GetInt(environment, GameOptionsEnvironment.GameStartingRoomVnum, problems) is { } startingRoom)
        {
            gameplay = gameplay with { StartingRoomVnum = startingRoom };
        }

        if (GetList(environment, GameOptionsEnvironment.GameBannedSites) is { } bannedSites)
        {
            gameplay = gameplay with { BannedSites = bannedSites };
        }

        if (GetList(environment, GameOptionsEnvironment.GameBannedNames) is { } bannedNames)
        {
            gameplay = gameplay with { BannedNames = bannedNames };
        }

        errors = problems;
        return options with
        {
            Server = server,
            Data = data,
            Logging = logging,
            Gameplay = gameplay
        };
    }

    private static string? Get(IReadOnlyDictionary<string, string?> environment, string name) =>
        environment.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static bool? GetBool(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        ICollection<string> errors)
    {
        if (Get(environment, name) is not { } text)
        {
            return null;
        }

        if (bool.TryParse(text, out var value))
        {
            return value;
        }

        errors.Add($"environment variable {name} must be true or false (was '{text}').");
        return null;
    }

    private static IReadOnlyList<string>? GetList(
        IReadOnlyDictionary<string, string?> environment,
        string name)
    {
        if (Get(environment, name) is not { } text)
        {
            return null;
        }

        return text
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int? GetInt(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        ICollection<string> errors)
    {
        if (Get(environment, name) is not { } text)
        {
            return null;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        errors.Add($"environment variable {name} must be an integer (was '{text}').");
        return null;
    }

    private static string FormatErrors(string? configPath, IReadOnlyList<string> errors)
    {
        var source = configPath is null ? "defaults and environment variables" : $"'{configPath}'";
        return $"Invalid VibeROM configuration ({source}):{Environment.NewLine}- "
            + string.Join($"{Environment.NewLine}- ", errors);
    }

    private static IReadOnlyDictionary<string, string?> ReadEnvironment()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key)
            {
                values[key] = entry.Value?.ToString();
            }
        }

        return values;
    }
}
