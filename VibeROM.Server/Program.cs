using NLog;
using VibeROM.Common;
using VibeROM.Common.Configuration;
using VibeROM.Common.Logging;
using VibeROM.Common.Rom;
using VibeROM.Common.Serialization;
using VibeROM.Server.Network;

namespace VibeROM.Server;

public static class Program
{
    private const string Usage =
        """
        Usage: VibeROM.Server [--config <path>] [--validate-data]

        Options:
          --config <path>   Configuration file (default: search for VibeROM.json)
          --validate-data   Load and validate JSON data, then exit
          --check           Alias for --validate-data
          -h, --help        Show this help
        """;

    public static async Task<int> Main(string[] args)
    {
        if (args.Any(argument => argument is "--help" or "-h"))
        {
            Console.Out.WriteLine(Usage);
            return 0;
        }

        if (!TryParseArguments(args, out var serverArguments, out var argumentError))
        {
            Console.Error.WriteLine(argumentError);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var logger = LogManager.GetCurrentClassLogger();

        try
        {
            // Bootstrap logging first so configuration failures are recorded
            // even when the configured log directory is invalid.
            try
            {
                LoggingSetup.Configure(LoggingOptions.Default, AppContext.BaseDirectory);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Default logging could not be configured: {exception.Message}");
            }

            var environmentConfigPath = Environment.GetEnvironmentVariable(GameOptionsEnvironment.ConfigPath);
            var usesEnvironmentConfig = !string.IsNullOrWhiteSpace(environmentConfigPath);
            var configPath = serverArguments.ConfigPath
                ?? (usesEnvironmentConfig ? environmentConfigPath : null)
                ?? GameOptionsLoader.FindConfigFile(Environment.CurrentDirectory, AppContext.BaseDirectory);
            var loaded = GameOptionsLoader.Load(
                configPath,
                requireFile: serverArguments.ConfigPath is not null || usesEnvironmentConfig);

            LoggingSetup.Configure(loaded.Options.Logging, loaded.BaseDirectory);

            var paths = loaded.Paths;
            logger.Info("{Description}", GameInfo.Describe());
            logger.Info("Configuration: {ConfigPath}", loaded.ConfigPath ?? "defaults");
            logger.Info(
                "Listening address: {BindAddress}:{Port}; max connections: {MaxConnections}",
                loaded.Options.Server.BindAddress,
                loaded.Options.Server.Port,
                loaded.Options.Server.MaxConnections);
            logger.Info("Data directory: {DataDirectory}", paths.DataDirectory);
            logger.Info("Log directory: {LogsDirectory}", paths.LogsDirectory);

            RomTables.ValidateOrThrow();
            logger.Info(
                "ROM tables validated: {RaceCount} races, {SkillCount} skills, {GroupCount} groups.",
                RomTables.RaceDefinitions.Count,
                RomTables.Skills.Count,
                RomTables.Groups.Count);

            var bootstrap = WorldBootstrap.Load(paths.AreasDirectory, paths.SchemaDirectory);
            foreach (var warning in bootstrap.Warnings)
            {
                logger.Warn("{WorldWarning}", warning);
            }

            logger.Info("World loaded and reset: {Summary}", bootstrap.Inventory.Summary);
            logger.Info(
                "Runtime after resets: {Characters} characters, {Objects} objects.",
                bootstrap.World.Characters.Count,
                bootstrap.World.GameObjects.Count);
            foreach (var area in bootstrap.Inventory.Areas)
            {
                logger.Debug(
                    "Area {Area}: {Mobiles} mobiles, {Objects} objects, {Rooms} rooms, {Resets} resets.",
                    area.Name,
                    area.Mobiles,
                    area.Objects,
                    area.Rooms,
                    area.Resets);
            }

            if (serverArguments.ValidateData)
            {
                logger.Info("Data validation succeeded.");
                return 0;
            }

            using var shutdown = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;

            var server = new TelnetServer(
                new TelnetServerOptions(
                    TelnetServer.ResolveBindAddress(loaded.Options.Server.BindAddress),
                    loaded.Options.Server.Port,
                    loaded.Options.Server.MaxConnections,
                    IdleTimeout: TimeSpan.FromSeconds(loaded.Options.Server.IdleTimeoutSeconds)));
            try
            {
                await server.RunAsync(shutdown.Token);
                logger.Info("Server stopped.");
                return 0;
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
                await server.DisposeAsync();
            }
        }
        catch (GameConfigurationException exception)
        {
            logger.Fatal(exception, "Configuration error");
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Unhandled startup error");
            Console.Error.WriteLine($"Unhandled startup error: {exception.Message}");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    private static bool TryParseArguments(
        string[] args,
        out ServerArguments arguments,
        out string error)
    {
        arguments = new ServerArguments(null, false);
        error = string.Empty;

        string? configPath = null;
        var validateData = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "--validate-data" or "--check")
            {
                validateData = true;
                continue;
            }

            if (argument == "--config")
            {
                if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
                {
                    error = "--config requires a value.";
                    return false;
                }

                configPath = args[++index];
                continue;
            }

            if (argument.StartsWith("--config=", StringComparison.Ordinal))
            {
                var value = argument["--config=".Length..];
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "--config requires a value.";
                    return false;
                }

                configPath = value;
                continue;
            }

            error = $"Unknown or incomplete argument '{argument}'.";
            return false;
        }

        arguments = new ServerArguments(configPath, validateData);
        return true;
    }

    private sealed record ServerArguments(string? ConfigPath, bool ValidateData);
}
