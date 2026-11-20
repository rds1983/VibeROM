using VibeROM.Common.Configuration;

namespace VibeROM.Common.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var errors = GameOptions.Default.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void JsonFile_OverridesDefaults()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(
            configPath,
            """
            {
              "server": { "bindAddress": "127.0.0.1", "port": 5000, "maxConnections": 25 },
              "data": { "directory": "GameData" },
              "logging": { "level": "Debug", "directory": "log" },
              "gameplay": { "autosaveIntervalSeconds": 60 }
            }
            """);

        var loaded = GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty);

        Assert.Equal("127.0.0.1", loaded.Options.Server.BindAddress);
        Assert.Equal(5000, loaded.Options.Server.Port);
        Assert.Equal(25, loaded.Options.Server.MaxConnections);
        Assert.Equal("GameData", loaded.Options.Data.Directory);
        Assert.Equal("Debug", loaded.Options.Logging.Level);
        Assert.Equal(60, loaded.Options.Gameplay.AutosaveIntervalSeconds);
    }

    [Fact]
    public void Environment_OverridesJson()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(configPath, """{ "server": { "port": 5000 }, "logging": { "level": "Info" } }""");

        var environment = TestEnvironment.Of(
            (GameOptionsEnvironment.ServerPort, "6000"),
            (GameOptionsEnvironment.LoggingLevel, "Warn"),
            (GameOptionsEnvironment.DataAreas, "world"));

        var loaded = GameOptionsLoader.Load(configPath, requireFile: true, environment);

        Assert.Equal(6000, loaded.Options.Server.Port);
        Assert.Equal("Warn", loaded.Options.Logging.Level);
        Assert.Equal("world", loaded.Options.Data.Areas);
    }

    [Fact]
    public void RelativePaths_ResolveAgainstConfigDirectory()
    {
        using var temp = new TempDirectory();
        var configDirectory = temp.GetPath("config");
        Directory.CreateDirectory(configDirectory);
        var configPath = Path.Combine(configDirectory, "VibeROM.json");
        File.WriteAllText(
            configPath,
            """{ "data": { "directory": "GameData" }, "logging": { "directory": "log" } }""");

        var loaded = GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty);

        Assert.Equal(Path.Combine(configDirectory, "GameData"), loaded.Paths.DataDirectory);
        Assert.Equal(Path.Combine(configDirectory, "GameData", "areas"), loaded.Paths.AreasDirectory);
        Assert.Equal(Path.Combine(configDirectory, "log"), loaded.Paths.LogsDirectory);
    }

    [Theory]
    [InlineData("""{ "server": { "port": 70000 } }""", "server.port")]
    [InlineData("""{ "server": { "maxConnections": 0 } }""", "server.maxConnections")]
    [InlineData("""{ "loggging": { "level": "Info" } }""", "loggging")]
    [InlineData("""{ "logging": { "level": "Loud" } }""", "logging.level")]
    [InlineData("""{ "data": { "areas": "../areas" } }""", "data.areas")]
    [InlineData("""{ "data": { "areas": "/areas" } }""", "data.areas")]
    [InlineData("""{ "logging": { "filePrefix": "sub/dir" } }""", "logging.filePrefix")]
    [InlineData("""{ "gameplay": { "autosaveIntervalSeconds": -1 } }""", "gameplay.autosaveIntervalSeconds")]
    public void InvalidConfig_FailsWithFieldName(string json, string expectedField)
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(configPath, json);

        var exception = Assert.Throws<GameConfigurationException>(
            () => GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty));

        Assert.Contains(expectedField, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidJson_FailsWithTheConfigPath()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(configPath, """{ "server": """);

        var exception = Assert.Throws<GameConfigurationException>(
            () => GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty));

        Assert.Contains(configPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("not valid JSON", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownProperty_IsRejected()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(configPath, """{ "server": { "prot": 4000 } }""");

        var exception = Assert.Throws<GameConfigurationException>(
            () => GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty));

        Assert.Contains("prot", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingExplicitConfig_Fails()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("missing.json");

        var exception = Assert.Throws<GameConfigurationException>(
            () => GameOptionsLoader.Load(configPath, requireFile: true, TestEnvironment.Empty));

        Assert.Contains("was not found", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingOptionalConfig_UsesDefaults()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("missing.json");

        var loaded = GameOptionsLoader.Load(configPath, requireFile: false, TestEnvironment.Empty);

        Assert.Null(loaded.ConfigPath);
        Assert.Equal(GameOptions.Default.Server.Port, loaded.Options.Server.Port);
    }

    [Fact]
    public void InvalidEnvironmentInteger_FailsWithVariableName()
    {
        var environment = TestEnvironment.Of((GameOptionsEnvironment.ServerPort, "not-a-number"));

        var exception = Assert.Throws<GameConfigurationException>(
            () => GameOptionsLoader.Load(null, requireFile: false, environment));

        Assert.Contains(GameOptionsEnvironment.ServerPort, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindConfigFile_WalksParentDirectories()
    {
        using var temp = new TempDirectory();
        var configPath = temp.GetPath("VibeROM.json");
        File.WriteAllText(configPath, "{}");
        var nested = temp.GetPath("a", "b", "c");
        Directory.CreateDirectory(nested);

        using var appDirectory = new TempDirectory();
        var found = GameOptionsLoader.FindConfigFile(nested, appDirectory.Root);

        Assert.Equal(configPath, found);
    }

    [Fact]
    public void FindConfigFile_FallsBackToApplicationDirectory()
    {
        using var start = new TempDirectory();
        using var app = new TempDirectory();
        var configPath = app.GetPath("VibeROM.json");
        File.WriteAllText(configPath, "{}");

        var found = GameOptionsLoader.FindConfigFile(start.Root, app.Root);

        Assert.Equal(configPath, found);
    }
}
