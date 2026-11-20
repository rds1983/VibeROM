# Networking

The server listens for telnet connections on `server.bindAddress` and
`server.port` (`VibeROM.json`). `server.bindAddress` accepts `*`, `localhost`
or an IP address; `server.maxConnections` is enforced by the listener.

```sh
dotnet run --project VibeROM.Server
```

Use `Ctrl+C` for a clean shutdown.

## Protocol

`VibeROM.Server.Network.TelnetServer` accepts connections asynchronously and
creates one `Descriptor` per client:

- greets the client with the VibeROM/ROM banner
- negotiates `TTYPE` (terminal type) and `NAWS` (window size)
- supports `WILL`/`WONT`/`DO`/`DONT` and subnegotiation, including fragmented
  packets
- renders `{r}`-style colour codes with `Colour.ToAnsi` when ANSI colour is
  enabled; logs use plain text
- queues output per connection so one slow client cannot block another
- limits input lines, supports backspace/CR/LF line editing and emits a
  `LineReceived` event for later login/command handling
- times out idle clients after `server.idleTimeoutSeconds` (default 600) with
  a notice and clean disconnect
- contains malformed/telnet garbage input without taking down the listener
- removes link-dead descriptors and accepts reconnects immediately
- enforces `server.maxConnections`

Login, line editing and world commands are implemented by later tasks; the
current greeting is followed by an open connection waiting for input.

Use `--validate-data` to validate the JSON world without opening the listener:

```sh
dotnet run --project VibeROM.Server -- --validate-data
```
