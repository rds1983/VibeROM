using VibeROM.Common.Domain;
using VibeROM.Server.Communication;

namespace VibeROM.Server.Commands;

/// <summary>Handles one line typed by a playing character.</summary>
public interface IPlayerCommandHandler
{
    void Execute(Character character, string input);
}

/// <summary>Services available to a command handler for one input line.</summary>
public sealed record CommandContext(
    Character Character,
    string Arguments,
    World World,
    OnlinePlayers Online,
    IPlayerOutput Output,
    ActDelivery Acts,
    WiznetService Wiznet,
    Random Random);
