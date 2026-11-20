using System.Net;
using System.Net.Sockets;
using NLog;
using VibeROM.Common.Domain;
using VibeROM.Common.Text;

namespace VibeROM.Server.Network;

/// <summary>
/// Async TCP listener and per-connection telnet sessions. Each accepted socket
/// gets one <see cref="Descriptor"/>; writes are queued per connection.
/// </summary>
public sealed class TelnetServer : IAsyncDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly TcpListener _listener;
    private readonly TelnetServerOptions _options;
    private readonly List<ClientSession> _sessions = [];
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private bool _stopped;

    public TelnetServer(TelnetServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _listener = new TcpListener(options.BindAddress, options.Port);
    }

    public int Port { get; private set; }

    public event Action<Descriptor, string>? LineReceived;

    public event Action<Descriptor>? ConnectionOpened;

    public event Action<Descriptor>? ConnectionClosed;

    public IReadOnlyCollection<Descriptor> Connections
    {
        get
        {
            lock (_gate)
            {
                return _sessions.Select(session => session.Descriptor).ToArray();
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            throw new InvalidOperationException("Server is already started.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptTask = AcceptLoopAsync(_cts.Token);
        Logger.Info("Listening on {Address}:{Port}", _options.BindAddress, Port);
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken);
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected shutdown.
        }
        finally
        {
            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        _cts?.Cancel();
        _listener.Stop();

        ClientSession[] sessions;
        lock (_gate)
        {
            sessions = _sessions.ToArray();
            _sessions.Clear();
        }

        foreach (var session in sessions)
        {
            session.Stop();
        }

        foreach (var session in sessions)
        {
            try
            {
                await session.Closed;
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
                // Connection already gone.
            }
        }

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask;
            }
            catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                // Listener stopped.
            }
        }

        _cts?.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            ClientSession session;
            lock (_gate)
            {
                if (_sessions.Count >= _options.MaxConnections)
                {
                    client.Dispose();
                    continue;
                }

                session = new ClientSession(client, _options, HandleLine, HandleOpened);
                _sessions.Add(session);
            }

            Logger.Info("Connection from {Host}", session.Descriptor.Host);
            _ = RunSessionAsync(session);
        }
    }

    public Task SendAsync(Descriptor descriptor, string text)
    {
        ClientSession? session;
        lock (_gate)
        {
            session = _sessions.FirstOrDefault(candidate => candidate.Descriptor == descriptor);
        }

        return session?.SendTextAsync(text) ?? Task.CompletedTask;
    }

    public void Disconnect(Descriptor descriptor)
    {
        ClientSession? session;
        lock (_gate)
        {
            session = _sessions.FirstOrDefault(candidate => candidate.Descriptor == descriptor);
        }

        session?.Stop();
    }

    public void DisconnectAfterFlush(Descriptor descriptor)
    {
        ClientSession? session;
        lock (_gate)
        {
            session = _sessions.FirstOrDefault(candidate => candidate.Descriptor == descriptor);
        }

        if (session is not null)
        {
            _ = session.CloseAfterOutputAsync();
        }
    }

    private void HandleOpened(Descriptor descriptor)
    {
        try
        {
            ConnectionOpened?.Invoke(descriptor);
        }
        catch (Exception exception)
        {
            Logger.Warn(exception, "Connection-open handler failed for {Host}.", descriptor.Host);
        }
    }

    private void HandleLine(Descriptor descriptor, string line)
    {
        Logger.Debug("Input from {Host}: {Line}", descriptor.Host, Colour.Strip(line));
        try
        {
            LineReceived?.Invoke(descriptor, line);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Line handler failed for {Host}.", descriptor.Host);
        }
    }

    private async Task RunSessionAsync(ClientSession session)
    {
        try
        {
            await session.RunAsync();
        }
        finally
        {
            lock (_gate)
            {
                _sessions.Remove(session);
            }

            Logger.Info("Disconnected {Host}", session.Descriptor.Host);
            try
            {
                ConnectionClosed?.Invoke(session.Descriptor);
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Connection-close handler failed for {Host}.", session.Descriptor.Host);
            }
        }
    }

    public static IPAddress ResolveBindAddress(string value) =>
        value switch
        {
            "*" => IPAddress.Any,
            "0.0.0.0" => IPAddress.Any,
            "localhost" => IPAddress.Loopback,
            _ => IPAddress.TryParse(value, out var address)
                ? address
                : throw new ArgumentException($"Invalid bind address '{value}'.", nameof(value))
        };
}
