using NLog;
using NLog.Targets;
using VibeROM.Common.Configuration;
using VibeROM.Common.Logging;

namespace VibeROM.Common.Tests;

public sealed class LoggingTests
{
    [Fact]
    public void CreateConfiguration_BuildsConsoleAndRollingFileTargets()
    {
        using var temp = new TempDirectory();
        var options = new LoggingOptions
        {
            Level = "Debug",
            Directory = "log",
            FilePrefix = "test",
            MaxArchiveFiles = 7
        };

        var configuration = LoggingSetup.CreateConfiguration(options, temp.Root);

        Assert.Single(configuration.AllTargets.OfType<ConsoleTarget>());
        var file = Assert.Single(configuration.AllTargets.OfType<FileTarget>());
        Assert.Contains("test.log", file.FileName?.ToString() ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(FileArchivePeriod.Day, file.ArchiveEvery);
        Assert.Equal(7, file.MaxArchiveFiles);
        Assert.Equal("_{0:000}", file.ArchiveSuffixFormat);
        Assert.Contains(configuration.LoggingRules, rule => rule.MinLevel == LogLevel.Debug);
    }

    [Fact]
    public void CreateConfiguration_WritesLogFile()
    {
        using var temp = new TempDirectory();
        var options = new LoggingOptions
        {
            Level = "Info",
            Directory = "log",
            FilePrefix = "test"
        };

        var configuration = LoggingSetup.CreateConfiguration(options, temp.Root);
        using var factory = new LogFactory();
        factory.Configuration = configuration;
        var logger = factory.GetLogger("VibeROM.Tests.Logging");

        logger.Info("hello from the test");
        factory.Flush();
        factory.Shutdown();

        var file = Assert.Single(Directory.GetFiles(Path.Combine(temp.Root, "log"), "*.log"));
        Assert.Contains("hello from the test", File.ReadAllText(file), StringComparison.Ordinal);
    }
}
