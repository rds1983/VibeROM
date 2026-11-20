using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;
using VibeROM.Server.Communication;

namespace VibeROM.Server.Commands;

/// <summary>
/// ROM's <c>interpret</c>: command prefix lookup, trust/position checks,
/// social fallback and snoop forwarding.
/// </summary>
public sealed class CommandDispatcher : IPlayerCommandHandler
{
    private readonly List<CommandDefinition> _commands = [];
    private readonly World _world;
    private readonly OnlinePlayers _online;
    private readonly IPlayerOutput _output;
    private readonly WiznetService _wiznet;
    private readonly Random _random;
    private readonly CommunicationCommands _communication = new();
    private readonly SocialCommands _socials = new();
    private readonly ActDelivery _acts;

    public CommandDispatcher(
        World world,
        OnlinePlayers online,
        IPlayerOutput output,
        WiznetService wiznet,
        Random? random = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _online = online ?? throw new ArgumentNullException(nameof(online));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _wiznet = wiznet ?? throw new ArgumentNullException(nameof(wiznet));
        _random = random ?? Random.Shared;
        _acts = new ActDelivery(output);

        RegisterCommands();
    }

    public void Execute(Character character, string input)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(input);

        var trimmed = input.TrimStart();
        if (trimmed.Length == 0)
        {
            return;
        }

        var (command, argument) = Parse(trimmed);
        if (command.Length == 0)
        {
            return;
        }

        ForwardSnoop(character, trimmed);

        var definition = _commands.FirstOrDefault(candidate =>
            char.ToLowerInvariant(candidate.Name[0]) == command[0]
            && candidate.Name.StartsWith(command, StringComparison.OrdinalIgnoreCase)
            && candidate.MinimumTrust <= character.Trust);

        if (definition is not null)
        {
            if (character.Position < definition.MinimumPosition)
            {
                SendPositionError(character);
                return;
            }

            definition.Handler(Context(character, argument));
            return;
        }

        if (_world.FindSocial(command) is { } social)
        {
            _socials.Execute(Context(character, argument), social);
            return;
        }

        _output.Send(character, "Huh?\r\n");
    }

    private CommandContext Context(Character character, string argument) =>
        new(character, argument, _world, _online, _output, _acts, _wiznet, _random);

    private void ForwardSnoop(Character character, string input)
    {
        if (character.Descriptor?.SnoopBy is { } snooper)
        {
            _output.Send(snooper, "% " + input + "\r\n");
        }
    }

    private void SendPositionError(Character character)
    {
        var message = character.Position switch
        {
            Position.Dead => "Lie still; you are DEAD.",
            Position.Mortal or Position.Incap => "You are hurt far too bad for that.",
            Position.Stunned => "You are too stunned to do that.",
            Position.Sleeping => "In your dreams, or what?",
            Position.Resting => "Nah... You feel too relaxed...",
            Position.Sitting => "Better stand up first.",
            Position.Fighting => "No way!  You are still fighting!",
            _ => "You can't do that now."
        };

        _output.Send(character, message + "\r\n");
    }

    private static (string Command, string Argument) Parse(string input)
    {
        if (!char.IsLetterOrDigit(input[0]))
        {
            return (input[..1].ToLowerInvariant(), input[1..].TrimStart());
        }

        var token = ArgumentParser.OneArgument(input);
        return (token.Value, token.Remainder);
    }

    private void RegisterCommands()
    {
        Add("auction", Position.Sleeping, c => _communication.Auction(c));
        Add("channels", Position.Dead, c => _communication.Channels(c));
        Add("music", Position.Sleeping, c => _communication.Music(c));
        Add("tell", Position.Resting, c => _communication.Tell(c));
        Add("socials", Position.Dead, c => _communication.Socials(c));
        Add("afk", Position.Sleeping, c => _communication.ToggleAfk(c));
        Add("answer", Position.Sleeping, c => _communication.Answer(c));
        Add("deaf", Position.Dead, c => _communication.ToggleDeaf(c));
        Add("emote", Position.Resting, c => _communication.Emote(c));
        Add("pmote", Position.Resting, c => _communication.Pmote(c));
        Add("gossip", Position.Sleeping, c => _communication.Gossip(c));
        Add("grats", Position.Sleeping, c => _communication.Grats(c));
        Add("gtell", Position.Dead, c => _communication.GroupTell(c));
        Add("pose", Position.Resting, c => _communication.Pose(c));
        Add("question", Position.Sleeping, c => _communication.Question(c));
        Add("quote", Position.Sleeping, c => _communication.Quote(c));
        Add("quiet", Position.Sleeping, c => _communication.ToggleQuiet(c));
        Add("reply", Position.Sleeping, c => _communication.Reply(c));
        Add("replay", Position.Sleeping, c => _communication.Replay(c));
        Add("say", Position.Resting, c => _communication.Say(c));
        Add("'", Position.Resting, c => _communication.Say(c));
        Add("shout", Position.Resting, c => _communication.Shout(c), minimumTrust: 3);
        Add("yell", Position.Resting, c => _communication.Yell(c));
        Add("nochannels", Position.Dead, c => _communication.ToggleNoChannels(c), minimumTrust: 55);
        Add("snoop", Position.Dead, c => _communication.Snoop(c), minimumTrust: 55);
        Add("wiznet", Position.Dead, c => _communication.Wiznet(c), minimumTrust: 52);
        Add("immtalk", Position.Dead, c => _communication.ImmortalTalk(c), minimumTrust: 52);
        Add("smote", Position.Dead, c => _communication.Smote(c), minimumTrust: 52);
    }

    private void Add(
        string name,
        Position minimumPosition,
        Action<CommandContext> handler,
        int minimumTrust = 0) =>
        _commands.Add(new CommandDefinition(name, minimumPosition, minimumTrust, handler));

    private sealed record CommandDefinition(
        string Name,
        Position MinimumPosition,
        int MinimumTrust,
        Action<CommandContext> Handler);
}
