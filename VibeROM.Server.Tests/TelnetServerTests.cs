using System.Net;
using System.Net.Sockets;
using System.Text;
using VibeROM.Server.Network;

namespace VibeROM.Server.Tests;

public sealed class TelnetServerTests
{
    [Fact]
    public async Task ClientReceivesGreetingAndNegotiatesTerminal()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        var response = await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");
        Assert.Contains("Welcome to VibeROM", response, StringComparison.Ordinal);
        Assert.Contains("\u001b[1;37m", response, StringComparison.Ordinal);

        await SendAsync(client.GetStream(), [Iac, Will, TerminalType]);
        var terminalRequest = await ReadUntilBytesAsync(
            client.GetStream(),
            [Iac, Sb, TerminalType, TerminalTypeSend, Iac, Se]);
        AssertContains([Iac, Sb, TerminalType, TerminalTypeSend, Iac, Se], terminalRequest);

        await SendAsync(
            client.GetStream(),
            [Iac, Sb, TerminalType, 0, (byte)'X', (byte)'T', (byte)'E', (byte)'R', (byte)'M', Iac, Se]);
        await SendAsync(
            client.GetStream(),
            [Iac, Sb, Naws, 0, 100, 0, 40, Iac, Se]);
        await Task.Delay(50);

        var descriptor = Assert.Single(server.Connections);
        Assert.Equal("XTERM", descriptor.TerminalType);
        Assert.Equal(100, descriptor.TerminalWidth);
        Assert.Equal(40, descriptor.TerminalHeight);
    }

    [Fact]
    public async Task GarbageInputDoesNotKillTheListener()
    {
        await using var server = CreateServer();
        await server.StartAsync();

        using (var garbage = new TcpClient())
        {
            await garbage.ConnectAsync(IPAddress.Loopback, server.Port);
            await garbage.GetStream().WriteAsync(
                Enumerable.Range(0, 512).Select(index => (byte)(index * 37)).ToArray());
            garbage.Close();
        }

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        var response = await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");
        Assert.Contains("Welcome to VibeROM", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SlowClientDoesNotBlockAnotherConnection()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        using var slow = new TcpClient();
        await slow.ConnectAsync(IPAddress.Loopback, server.Port);

        using var fast = new TcpClient();
        await fast.ConnectAsync(IPAddress.Loopback, server.Port);
        var response = await ReadUntilAsync(fast.GetStream(), "Welcome to VibeROM");
        Assert.Contains("Welcome to VibeROM", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisconnectRemovesTheDescriptor()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");
        Assert.Single(server.Connections);

        client.Close();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (server.Connections.Count > 0)
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    [Fact]
    public async Task InputLineEditingHandlesBackspaceAndCarriageReturn()
    {
        await using var server = CreateServer();
        var line = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.LineReceived += (_, value) => line.TrySetResult(value);
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");

        await SendAsync(
            client.GetStream(),
            Encoding.Latin1.GetBytes("ab\bcd\r\n"));

        var received = await line.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("acd", received);
    }

    [Fact]
    public async Task SimultaneousConnectionsAllReceiveGreetings()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        var clients = Enumerable.Range(0, 3).Select(_ => new TcpClient()).ToArray();
        try
        {
            foreach (var client in clients)
            {
                await client.ConnectAsync(IPAddress.Loopback, server.Port);
            }

            foreach (var client in clients)
            {
                var greeting = await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");
                Assert.Contains("Welcome to VibeROM", greeting, StringComparison.Ordinal);
            }

            Assert.Equal(3, server.Connections.Count);
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task IdleTimeoutSendsNoticeAndClosesTheConnection()
    {
        await using var server = CreateServer(idleTimeout: TimeSpan.FromMilliseconds(200));
        await server.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(client.GetStream(), "Welcome to VibeROM");

        var response = await ReadUntilAsync(client.GetStream(), "Idle timeout");
        Assert.Contains("Idle timeout", response, StringComparison.Ordinal);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (server.Connections.Count > 0)
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    [Fact]
    public async Task MaxConnectionsRejectsAdditionalClients()
    {
        await using var server = CreateServer(maxConnections: 1);
        await server.StartAsync();
        using var first = new TcpClient();
        await first.ConnectAsync(IPAddress.Loopback, server.Port);
        await ReadUntilAsync(first.GetStream(), "Welcome to VibeROM");

        using var second = new TcpClient();
        await second.ConnectAsync(IPAddress.Loopback, server.Port);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var buffer = new byte[64];
        var read = await second.GetStream().ReadAsync(buffer, timeout.Token);
        Assert.Equal(0, read);
        Assert.Single(server.Connections);
    }

    [Fact]
    public async Task ReconnectAfterDisconnectIsAccepted()
    {
        await using var server = CreateServer();
        await server.StartAsync();

        using (var first = new TcpClient())
        {
            await first.ConnectAsync(IPAddress.Loopback, server.Port);
            await ReadUntilAsync(first.GetStream(), "Welcome to VibeROM");
        }

        using var second = new TcpClient();
        await second.ConnectAsync(IPAddress.Loopback, server.Port);
        var greeting = await ReadUntilAsync(second.GetStream(), "Welcome to VibeROM");
        Assert.Contains("Welcome to VibeROM", greeting, StringComparison.Ordinal);
        Assert.Contains(server.Connections, descriptor => descriptor.Host == "127.0.0.1");
    }

    private static TelnetServer CreateServer(
        int maxConnections = 8,
        TimeSpan? idleTimeout = null) =>
        new(new TelnetServerOptions(
            IPAddress.Loopback,
            0,
            maxConnections,
            IdleTimeout: idleTimeout));

    private static async Task<string> ReadUntilAsync(
        NetworkStream stream,
        string expected,
        int timeoutSeconds = 5)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        while (!builder.ToString().Contains(expected, StringComparison.Ordinal))
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

    private static async Task<byte[]> ReadUntilBytesAsync(
        NetworkStream stream,
        byte[] expected,
        int timeoutSeconds = 5)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var buffer = new byte[4096];
        using var memory = new MemoryStream();
        while (true)
        {
            var read = await stream.ReadAsync(buffer, timeout.Token);
            if (read == 0)
            {
                break;
            }

            memory.Write(buffer, 0, read);
            var actual = memory.ToArray();
            if (Contains(actual, expected))
            {
                return actual;
            }
        }

        return memory.ToArray();
    }

    private static bool Contains(byte[] actual, byte[] expected) =>
        actual.AsSpan().IndexOf(expected) >= 0;

    private static Task SendAsync(NetworkStream stream, byte[] bytes) =>
        stream.WriteAsync(bytes).AsTask();

    private static void AssertContains(byte[] expected, byte[] actual)
    {
        for (var index = 0; index <= actual.Length - expected.Length; index++)
        {
            if (actual.AsSpan(index, expected.Length).SequenceEqual(expected))
            {
                return;
            }
        }

        Assert.Fail($"Expected sequence {Convert.ToHexString(expected)} in {Convert.ToHexString(actual)}.");
    }

    private const byte Iac = 255;
    private const byte Will = 251;
    private const byte Sb = 250;
    private const byte Se = 240;
    private const byte TerminalType = 24;
    private const byte TerminalTypeSend = 1;
    private const byte Naws = 31;
}
