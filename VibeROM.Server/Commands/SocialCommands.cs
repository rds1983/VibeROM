using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;
using VibeROM.Server.Communication;

namespace VibeROM.Server.Commands;

/// <summary>ROM <c>check_social</c> behaviour for social.json entries.</summary>
internal sealed class SocialCommands
{
    public void Execute(CommandContext context, SocialData social)
    {
        var actor = context.Character;

        if ((actor.CommunicationFlags & RomFlags.Communication.Get("NoEmote").Bit) != 0)
        {
            Send(context, "You are anti-social!");
            return;
        }

        switch (actor.Position)
        {
            case Position.Dead:
                Send(context, "Lie still; you are DEAD.");
                return;

            case Position.Incap:
            case Position.Mortal:
                Send(context, "You are hurt far too bad for that.");
                return;

            case Position.Stunned:
                Send(context, "You are too stunned to do that.");
                return;

            case Position.Sleeping when !social.Name.Equals("snore", StringComparison.OrdinalIgnoreCase):
                Send(context, "In your dreams, or what?");
                return;
        }

        var token = ArgumentParser.OneArgument(context.Arguments);
        if (token.Value.Length == 0)
        {
            context.Acts.Act(
                social.OthersNoArgument,
                new ActMessageContext(actor),
                ActTarget.ToRoom);
            context.Acts.Act(
                social.CharNoArgument,
                new ActMessageContext(actor),
                ActTarget.ToChar,
                Position.Dead);
            return;
        }

        var victim = FindInRoom(actor, token.Value);
        if (victim is null)
        {
            Send(context, "They aren't here.");
            return;
        }

        if (ReferenceEquals(victim, actor))
        {
            context.Acts.Act(
                social.OthersAuto,
                new ActMessageContext(actor, Victim: actor),
                ActTarget.ToRoom);
            context.Acts.Act(
                social.CharAuto,
                new ActMessageContext(actor, Victim: actor),
                ActTarget.ToChar,
                Position.Dead);
            return;
        }

        var message = new ActMessageContext(actor, Victim: victim);
        context.Acts.Act(social.OthersFound, message, ActTarget.ToNotVictim);
        context.Acts.Act(social.CharFound, message, ActTarget.ToChar, Position.Dead);
        context.Acts.Act(social.VictimFound, message, ActTarget.ToVictim);
    }

    private static Character? FindInRoom(Character actor, string name)
    {
        if (actor.InRoom is not { } room)
        {
            return null;
        }

        return room.People.FirstOrDefault(candidate => Matches(candidate, name));
    }

    private static bool Matches(Character candidate, string name)
    {
        if (candidate.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return candidate.Keywords
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(keyword => keyword.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    private static void Send(CommandContext context, string text) =>
        context.Output.Send(context.Character, text + "\r\n");
}
