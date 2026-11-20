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

    private static TelnetServer CreateServer() =>
        new(new TelnetServerOptions(IPAddress.Loopback, 0, MaxConnections: 8));

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
