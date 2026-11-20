using NLog;
using NLog.Config;
using NLog.Targets;
using VibeROM.Common.Configuration;

namespace VibeROM.Common.Logging;

/// <summary>Builds and applies the shared console + rolling-file NLog configuration.</summary>
public static class LoggingSetup
{
    public const string Layout =
        "${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}";

    public static void Configure(LoggingOptions options, string baseDirectory)
    {
        var configuration = CreateConfiguration(options, baseDirectory);
        LogManager.Configuration = configuration;
        LogManager.ReconfigExistingLoggers();
    }

    public static LoggingConfiguration CreateConfiguration(LoggingOptions options, string baseDirectory)
    {
        var logDirectory = ResolveLogDirectory(options, baseDirectory);
        try
        {
            Directory.CreateDirectory(logDirectory);
        }
        catch (Exception exception)
        {
            throw new GameConfigurationException(
                $"Log directory '{logDirectory}' could not be created: {exception.Message}",
                exception);
        }

        var level = LogLevel.FromString(options.Level);

        var console = new ConsoleTarget("console")
        {
            Layout = Layout
        };

        var file = new FileTarget("file")
        {
            FileName = Path.Combine(logDirectory, $"{options.FilePrefix}.log"),
            ArchiveSuffixFormat = "_{0:000}",
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = options.MaxArchiveFiles,
            Layout = Layout,
            KeepFileOpen = true
        };

        var configuration = new LoggingConfiguration();
        configuration.AddTarget(console);
        configuration.AddTarget(file);
        configuration.AddRule(level, LogLevel.Fatal, console);
        configuration.AddRule(level, LogLevel.Fatal, file);
        return configuration;
    }

    public static string ResolveLogDirectory(LoggingOptions options, string baseDirectory) =>
        Path.GetFullPath(
            Path.IsPathRooted(options.Directory)
                ? options.Directory
                : Path.Combine(baseDirectory, options.Directory));
}
