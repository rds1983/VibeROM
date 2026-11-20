using System.Net;
using System.Net.Sockets;
using System.Text;
using VibeROM.Common.Configuration;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Security;
using VibeROM.Server.Login;
using VibeROM.Server.Network;

namespace VibeROM.Server.Tests;

public sealed class LoginServiceTests
{
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public async Task NewPlayerCompletesLoginAndEntersWorld()
    {
        using var temp = new TempDirectory();
        await using var server = CreateServer();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        var screen = await ReadUntilAsync(client.GetStream(), "By what name");
        Assert.Contains("DikuMUD", screen, StringComparison.Ordinal);
        Assert.Contains("Merc", screen, StringComparison.Ordinal);
        Assert.Contains("ROM 2.4", screen, StringComparison.Ordinal);

        await SendLineAsync(client.GetStream(), "Alander");
        await ReadUntilAsync(client.GetStream(), "Did I get that right");
        await SendLineAsync(client.GetStream(), "y");
        await ReadUntilAsync(client.GetStream(), "Choose a password");
        await SendLineAsync(client.GetStream(), "secret");
        await ReadUntilAsync(client.GetStream(), "again");
        await SendLineAsync(client.GetStream(), "secret");
        await ReadUntilAsync(client.GetStream(), "Email address");
        await SendLineAsync(client.GetStream(), "a@example.com");
        await ReadUntilAsync(client.GetStream(), "correct");
        await SendLineAsync(client.GetStream(), "y");
        await ReadUntilAsync(client.GetStream(), "Welcome, Alander");

        var descriptor = Assert.Single(server.Connections);
        Assert.Equal(ConnectionState.Playing, descriptor.State);
        Assert.True(store.Exists("Alander"));
        var loaded = store.Load("Alander", CreateWorld());
        Assert.Equal("a@example.com", loaded.Player!.Email);
        Assert.True(PasswordHasher.IsHash(loaded.Player.PasswordHash));
    }

    [Fact]
    public async Task MismatchedPasswordRepromptsSafely()
    {
        using var temp = new TempDirectory();
        await using var server = CreateServer();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Alander");
        await ReadUntilAsync(client.GetStream(), "Did I get that right");
        await SendLineAsync(client.GetStream(), "y");
        await ReadUntilAsync(client.GetStream(), "Choose a password");
        await SendLineAsync(client.GetStream(), "secret");
        await ReadUntilAsync(client.GetStream(), "again");
        await SendLineAsync(client.GetStream(), "secret2");
        var reprompt = await ReadUntilAsync(client.GetStream(), "do not match");

        Assert.Contains("do not match", reprompt, StringComparison.Ordinal);
        await SendLineAsync(client.GetStream(), "correct");
        await ReadUntilAsync(client.GetStream(), "again");
        await SendLineAsync(client.GetStream(), "correct");
        await ReadUntilAsync(client.GetStream(), "Email address");
        await SendLineAsync(client.GetStream(), "a@example.com");
        await ReadUntilAsync(client.GetStream(), "correct");
        await SendLineAsync(client.GetStream(), "y");
        await ReadUntilAsync(client.GetStream(), "Welcome, Alander");

        var loaded = store.Load("Alander", CreateWorld());
        Assert.True(PasswordHasher.Verify("correct", loaded.Player!.PasswordHash));
        Assert.False(PasswordHasher.Verify("secret", loaded.Player.PasswordHash));
    }

    [Fact]
    public async Task ReturningPlayerVerifiesPassword()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        SavePlayer(store, "Alander", "secret");

        await using var server = CreateServer();
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Alander");
        await ReadUntilAsync(client.GetStream(), "Password");
        await SendLineAsync(client.GetStream(), "secret");
        await ReadUntilAsync(client.GetStream(), "Welcome, Alander");

        Assert.Equal(ConnectionState.Playing, Assert.Single(server.Connections).State);
    }

    [Fact]
    public async Task WrongPasswordDoesNotEnterWorld()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        SavePlayer(store, "Alander", "secret");

        await using var server = CreateServer();
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Alander");
        await ReadUntilAsync(client.GetStream(), "Password");
        await SendLineAsync(client.GetStream(), "wrong");
        var response = await ReadUntilAsync(client.GetStream(), "Wrong password");

        Assert.Contains("Wrong password", response, StringComparison.Ordinal);
        Assert.Equal(ConnectionState.GetOldPassword, Assert.Single(server.Connections).State);
    }

    [Fact]
    public async Task BannedNameIsRejected()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        await using var server = CreateServer();
        using var login = new LoginService(
            server,
            CreateWorld(),
            store,
            new GameplayOptions { BannedNames = ["Alander"] });
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Alander");
        var response = await ReadUntilAsync(client.GetStream(), "banned");

        Assert.Contains("banned", response, StringComparison.Ordinal);
        await WaitForNoConnectionsAsync(server);
    }

    [Fact]
    public async Task WizlockRejectsNewPlayer()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        await using var server = CreateServer();
        using var login = new LoginService(
            server,
            CreateWorld(),
            store,
            new GameplayOptions { Wizlock = true });
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Alander");
        var response = await ReadUntilAsync(client.GetStream(), "wizlocked");

        Assert.Contains("wizlocked", response, StringComparison.Ordinal);
        await WaitForNoConnectionsAsync(server);
    }

    [Fact]
    public async Task BannedSiteIsRejectedBeforeName()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        await using var server = CreateServer();
        using var login = new LoginService(
            server,
            CreateWorld(),
            store,
            new GameplayOptions { BannedSites = ["127.0.0.1"] });
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        var response = await ReadUntilAsync(client.GetStream(), "banned");

        Assert.Contains("banned", response, StringComparison.Ordinal);
        await WaitForNoConnectionsAsync(server);
    }

    [Fact]
    public async Task ReconnectReplacesExistingSession()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        SavePlayer(store, "Alander", "secret");

        await using var server = CreateServer();
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();

        using var first = new TcpClient();
        await first.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(first.GetStream(), "By what name");
        await SendLineAsync(first.GetStream(), "Alander");
        await ReadUntilAsync(first.GetStream(), "Password");
        await SendLineAsync(first.GetStream(), "secret");
        await ReadUntilAsync(first.GetStream(), "Welcome, Alander");

        using var second = new TcpClient();
        await second.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(second.GetStream(), "By what name");
        await SendLineAsync(second.GetStream(), "Alander");
        await ReadUntilAsync(second.GetStream(), "Password");
        await SendLineAsync(second.GetStream(), "secret");
        await ReadUntilAsync(second.GetStream(), "Welcome, Alander");
        await WaitForConnectionCountAsync(server, 1);

        Assert.Equal(ConnectionState.Playing, Assert.Single(server.Connections).State);
    }

    [Fact]
    public async Task MaxOnlineRejectsNewCharacter()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        SavePlayer(store, "First", "secret");

        await using var server = CreateServer();
        using var login = new LoginService(
            server,
            CreateWorld(),
            store,
            new GameplayOptions { MaxOnline = 1 });
        await server.StartAsync();

        using var first = new TcpClient();
        await first.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(first.GetStream(), "By what name");
        await SendLineAsync(first.GetStream(), "First");
        await ReadUntilAsync(first.GetStream(), "Password");
        await SendLineAsync(first.GetStream(), "secret");
        await ReadUntilAsync(first.GetStream(), "Welcome, First");

        using var second = new TcpClient();
        await second.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(second.GetStream(), "By what name");
        await SendLineAsync(second.GetStream(), "Second");
        var response = await ReadUntilAsync(second.GetStream(), "full");
        Assert.Contains("full", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorruptSaveDisconnectsCleanly()
    {
        using var temp = new TempDirectory();
        var store = new PlayerStore(temp.Path, SchemaDirectory);
        File.WriteAllText(Path.Combine(temp.Path, "Broken.json"), """{ "not valid """);

        await using var server = CreateServer();
        using var login = new LoginService(server, CreateWorld(), store, new GameplayOptions());
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "By what name");
        await SendLineAsync(client.GetStream(), "Broken");
        await ReadUntilAsync(client.GetStream(), "Password");
        await SendLineAsync(client.GetStream(), "secret");
        var response = await ReadUntilAsync(client.GetStream(), "could not be read");

        Assert.Contains("could not be read", response, StringComparison.Ordinal);
        await WaitForNoConnectionsAsync(server);
    }

    private static TelnetServer CreateServer() =>
        new(new TelnetServerOptions(IPAddress.Loopback, 0));

    private static World CreateWorld()
    {
        var room = new RoomPrototype(
            3001,
            "The Temple",
            "A quiet temple.",
            0,
            Sector.Inside,
            100,
            100,
            0,
            new Dictionary<Direction, ExitPrototype>(),
            []);
        var area = new AreaData(
            "Login Test",
            "tests",
            3001,
            3099,
            9,
            "tests",
            "login.are",
            [],
            [],
            [],
            [room],
            [],
            [],
            [],
            []);
        return new World([area]);
    }

    private static void SavePlayer(PlayerStore store, string name, string password)
    {
        var world = CreateWorld();
        var player = new PlayerData
        {
            PasswordHash = PasswordHasher.Hash(password),
            Email = "player@example.com"
        };
        var character = world.CreatePlayer(player, name, Race.Human, PlayerClass.Warrior, Sex.Neutral);
        world.MoveCharacterToRoom(character, world.GetRoom(3001));
        store.Save(character);
    }

    private static async Task SendLineAsync(NetworkStream stream, string line) =>
        await stream.WriteAsync(Encoding.UTF8.GetBytes(line + "\r\n"));

    private static async Task WaitForNoConnectionsAsync(TelnetServer server)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (server.Connections.Count > 0)
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private static async Task WaitForConnectionCountAsync(TelnetServer server, int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (server.Connections.Count != count)
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private static async Task<string> ReadUntilAsync(NetworkStream stream, string expected)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        while (!builder.ToString().Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            var read = await stream.ReadAsync(buffer, timeout.Token);
            if (read == 0)
            {
                break;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }

        return builder.ToString();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "viberom-login-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
