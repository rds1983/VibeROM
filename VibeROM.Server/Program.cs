using NLog;
using VibeROM.Common;
using VibeROM.Common.Configuration;
using VibeROM.Common.Logging;
using VibeROM.Common.Rom;
using VibeROM.Common.Serialization;

namespace VibeROM.Server;

public static class Program
{
    private const string Usage =
        """
        Usage: VibeROM.Server [--config <path>]

        Options:
          --config <path>  Configuration file (default: search for VibeROM.json)
          -h, --help       Show this help
        """;

    public static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            Console.Out.WriteLine(Usage);
            return 0;
        }

        if (!TryParseConfigArgument(args, out var explicitConfigPath, out var argumentError))
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
            var configPath = explicitConfigPath
                ?? (usesEnvironmentConfig ? environmentConfigPath : null)
                ?? GameOptionsLoader.FindConfigFile(Environment.CurrentDirectory, AppContext.BaseDirectory);
            var loaded = GameOptionsLoader.Load(
                configPath,
                requireFile: explicitConfigPath is not null || usesEnvironmentConfig);

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

            var loadedWorld = WorldLoader.Load(paths.AreasDirectory, paths.SchemaDirectory);
            foreach (var warning in loadedWorld.Warnings)
            {
                logger.Warn("{WorldWarning}", warning);
            }

            logger.Info(
                "World loaded: {Areas} areas, {Mobiles} mobiles, {Objects} objects, {Rooms} rooms, " +
                "{Resets} resets, {Shops} shops, {Specials} specials, {Helps} helps.",
                loadedWorld.World.TopArea,
                loadedWorld.World.TopMobIndex,
                loadedWorld.World.TopObjectIndex,
                loadedWorld.World.TopRoom,
                loadedWorld.World.Areas.Sum(area => area.Resets.Count),
                loadedWorld.World.Shops.Count,
                loadedWorld.World.Areas.Sum(area => area.Specials.Count),
                loadedWorld.World.TopHelp);

            logger.Info("Scaffold only: server bootstrap is implemented in T020 and T024.");
            return 0;
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

    private static bool TryParseConfigArgument(
        string[] args,
        out string? configPath,
        out string error)
    {
        configPath = null;
        error = string.Empty;

        if (args.Length == 0)
        {
            return true;
        }

        if (args.Length == 2 && args[0] == "--config")
        {
            configPath = args[1];
            return true;
        }

        if (args.Length == 1 && args[0].StartsWith("--config=", StringComparison.Ordinal))
        {
            var value = args[0]["--config=".Length..];
            if (!string.IsNullOrWhiteSpace(value))
            {
                configPath = value;
                return true;
            }
        }

        error = "Unknown or incomplete arguments.";
        return false;
    }
}
