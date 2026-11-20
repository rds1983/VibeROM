using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using NLog;
using VibeROM.Common.Domain;
using VibeROM.Common.Text;

namespace VibeROM.Server.Network;

internal sealed class ClientSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly TelnetServerOptions _options;
    private readonly Channel<byte[]> _output = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(64)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TelnetState _state = TelnetState.Data;
    private byte _subOption;
    private bool _subOptionRead;
    private readonly List<byte> _subData = [];

    public ClientSession(TcpClient client, TelnetServerOptions options)
    {
        _client = client;
        _stream = client.GetStream();
        _options = options;
        Descriptor = new Descriptor
        {
            Host = client.Client.RemoteEndPoint?.ToString() ?? "unknown",
            ColourEnabled = options.ColourEnabled
        };
    }

    public Descriptor Descriptor { get; }

    public Task Closed => _closed.Task;

    public async Task RunAsync()
    {
        var writer = WriteLoopAsync();
        try
        {
            await SendNegotiationAsync();
            await SendTextAsync(_options.Greeting);
            await ReadLoopAsync(_cts.Token);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
        {
            // A client can disappear at any time; this is a normal disconnect.
        }
        catch (Exception exception)
        {
            Logger.Warn(exception, "Connection {Host} failed.", Descriptor.Host);
        }
        finally
        {
            _cts.Cancel();
            _output.Writer.TryComplete();
            try
            {
                await writer;
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
            {
                // Expected when the socket is gone.
            }

            _client.Dispose();
            _closed.TrySetResult();
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        try
        {
            _client.Close();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // Already closed.
        }
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await _stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return;
            }

            for (var index = 0; index < read; index++)
            {
                ProcessByte(buffer[index]);
            }
        }
    }

    private void ProcessByte(byte value)
    {
        switch (_state)
        {
            case TelnetState.Data:
                if (value == TelnetConstants.Iac)
                {
                    _state = TelnetState.Iac;
                }
                else
                {
                    ProcessTextByte(value);
                }

                break;

            case TelnetState.Iac:
                switch (value)
                {
                    case TelnetConstants.Iac:
                        ProcessTextByte(value);
                        _state = TelnetState.Data;
                        break;

                    case TelnetConstants.Will:
                    case TelnetConstants.Wont:
                    case TelnetConstants.Do:
                    case TelnetConstants.Dont:
                        _state = TelnetState.Negotiation;
                        break;

                    case TelnetConstants.Sb:
                        _state = TelnetState.SubNegotiation;
                        _subOptionRead = false;
                        _subData.Clear();
                        break;

                    default:
                        _state = TelnetState.Data;
                        break;
                }

                break;

            case TelnetState.Negotiation:
                HandleNegotiation(value);
                _state = TelnetState.Data;
                break;

            case TelnetState.SubNegotiation:
                if (value == TelnetConstants.Iac)
                {
                    _state = TelnetState.SubNegotiationIac;
                }
                else if (!_subOptionRead)
                {
                    _subOption = value;
                    _subOptionRead = true;
                }
                else
                {
                    _subData.Add(value);
                }

                break;

            case TelnetState.SubNegotiationIac:
                if (value == TelnetConstants.Se)
                {
                    HandleSubNegotiation();
                    _state = TelnetState.Data;
                }
                else if (value == TelnetConstants.Iac)
                {
                    _subData.Add(value);
                    _state = TelnetState.SubNegotiation;
                }
                else
                {
                    _state = TelnetState.Data;
                }

                break;
        }
    }

    private void ProcessTextByte(byte value)
    {
        if (value == (byte)'\r')
        {
            return;
        }

        if (value == (byte)'\n')
        {
            Descriptor.InputBuffer = string.Empty;
            return;
        }

        if (value is >= 32 and < 127 && Descriptor.InputBuffer.Length < 4096)
        {
            Descriptor.InputBuffer += (char)value;
        }
        else if (Descriptor.InputBuffer.Length >= 4096)
        {
            Descriptor.InputBuffer = string.Empty;
        }
    }

    private void HandleNegotiation(byte option)
    {
        // The server does not require any option to function. Answer the
        // options needed for terminal type and window size.
        if (option == TelnetConstants.TerminalType)
        {
            SendRaw([TelnetConstants.Iac, TelnetConstants.Sb, TelnetConstants.TerminalType,
                TelnetConstants.TerminalTypeSend, TelnetConstants.Iac, TelnetConstants.Se]);
        }
        else if (option == TelnetConstants.SuppressGoAhead)
        {
            SendRaw([TelnetConstants.Iac, TelnetConstants.Will, TelnetConstants.SuppressGoAhead]);
        }
    }

    private void HandleSubNegotiation()
    {
        if (_subOption == TelnetConstants.TerminalType
            && _subData.Count > 1
            && _subData[0] == TelnetConstants.TerminalTypeIs)
        {
            Descriptor.TerminalType = Encoding.Latin1.GetString(
                _subData.Skip(1).ToArray()).TrimEnd('\0', ' ');
        }
        else if (_subOption == TelnetConstants.Naws && _subData.Count >= 4)
        {
            var width = (_subData[0] << 8) | _subData[1];
            var height = (_subData[2] << 8) | _subData[3];
            if (width is >= 20 and <= 500)
            {
                Descriptor.TerminalWidth = width;
            }

            if (height is >= 5 and <= 500)
            {
                Descriptor.TerminalHeight = height;
            }
        }
    }

    private Task SendNegotiationAsync()
    {
        SendRaw(
        [
            TelnetConstants.Iac, TelnetConstants.Do, TelnetConstants.TerminalType,
            TelnetConstants.Iac, TelnetConstants.Do, TelnetConstants.Naws,
            TelnetConstants.Iac, TelnetConstants.Will, TelnetConstants.SuppressGoAhead
        ]);
        return Task.CompletedTask;
    }

    private Task SendTextAsync(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
        var rendered = Colour.ToAnsi(normalized, Descriptor.ColourEnabled);
        SendRaw(Encoding.UTF8.GetBytes(rendered));
        return Task.CompletedTask;
    }

    private void SendRaw(byte[] bytes)
    {
        if (!_output.Writer.TryWrite(bytes))
        {
            // A client that cannot accept small greeting/negotiation output is
            // treated as dead rather than blocking the whole server.
            _cts.Cancel();
        }
    }

    private async Task WriteLoopAsync()
    {
        await foreach (var bytes in _output.Reader.ReadAllAsync(_cts.Token))
        {
            await _stream.WriteAsync(bytes, _cts.Token);
        }
    }

    private enum TelnetState
    {
        Data,
        Iac,
        Negotiation,
        SubNegotiation,
        SubNegotiationIac
    }
}
