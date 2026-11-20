using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;
using VibeROM.Server.Communication;

namespace VibeROM.Server.Commands;

/// <summary>ROM act_comm.c communication commands.</summary>
internal sealed class CommunicationCommands
{
    private const int ImmortalTrust = WiznetService.ImmortalTrust;

    private static readonly int Quiet = Bit("Quiet");
    private static readonly int Deaf = Bit("Deaf");
    private static readonly int NoWiz = Bit("NoWiz");
    private static readonly int NoAuction = Bit("NoAuction");
    private static readonly int NoGossip = Bit("NoGossip");
    private static readonly int NoQuestion = Bit("NoQuestion");
    private static readonly int NoMusic = Bit("NoMusic");
    private static readonly int NoQuote = Bit("NoQuote");
    private static readonly int ShoutsOff = Bit("ShoutsOff");
    private static readonly int NoGrats = Bit("NoGrats");
    private static readonly int NoEmote = Bit("NoEmote");
    private static readonly int NoShout = Bit("NoShout");
    private static readonly int NoTell = Bit("NoTell");
    private static readonly int NoChannels = Bit("NoChannels");
    private static readonly int SnoopProof = Bit("SnoopProof");
    private static readonly int Afk = Bit("Afk");

    public void Say(CommandContext context)
    {
        if (context.Arguments.Length == 0)
        {
            Send(context, "Say what?");
            return;
        }

        var message = new ActMessageContext(context.Character, Text2: context.Arguments);
        context.Acts.Act("$n says '$T'", message, ActTarget.ToRoom);
        context.Acts.Act("You say '$T'", message, ActTarget.ToChar, Position.Dead);
    }

    public void Emote(CommandContext context)
    {
        if (Has(context.Character, NoEmote))
        {
            Send(context, "You can't show your emotions.");
            return;
        }

        if (context.Arguments.Length == 0)
        {
            Send(context, "Emote what?");
            return;
        }

        var message = new ActMessageContext(context.Character, Text2: context.Arguments);
        context.Acts.Act("$n $T", message, ActTarget.ToRoom);
        context.Acts.Act("$n $T", message, ActTarget.ToChar, Position.Dead);
    }

    public void Pmote(CommandContext context)
    {
        if (Has(context.Character, NoEmote))
        {
            Send(context, "You can't show your emotions.");
            return;
        }

        if (context.Arguments.Length == 0)
        {
            Send(context, "Emote what?");
            return;
        }

        var actor = context.Character;
        context.Output.Send(
            actor,
            ActFormatter.FormatLine(
                "$n $t",
                new ActMessageContext(actor, Text1: context.Arguments),
                actor));

        if (actor.InRoom is not { } room)
        {
            return;
        }

        foreach (var recipient in room.People.ToArray())
        {
            if (ReferenceEquals(recipient, actor) || recipient.Descriptor is null)
            {
                continue;
            }

            var personalized = Personalize(context.Arguments, recipient.Name);
            context.Output.Send(
                recipient,
                ActFormatter.FormatLine(
                    "$N $t",
                    new ActMessageContext(recipient, Victim: actor, Text1: personalized),
                    recipient));
        }
    }

    public void Smote(CommandContext context)
    {
        if (Has(context.Character, NoEmote))
        {
            Send(context, "You can't show your emotions.");
            return;
        }

        if (context.Arguments.Length == 0)
        {
            Send(context, "Emote what?");
            return;
        }

        var actor = context.Character;
        if (!context.Arguments.Contains(actor.Name, StringComparison.Ordinal))
        {
            Send(context, "You must include your name in an smote.");
            return;
        }

        Send(context, context.Arguments);

        if (actor.InRoom is not { } room)
        {
            return;
        }

        foreach (var recipient in room.People.ToArray())
        {
            if (ReferenceEquals(recipient, actor) || recipient.Descriptor is null)
            {
                continue;
            }

            context.Output.Send(
                recipient,
                Personalize(context.Arguments, recipient.Name) + "\r\n");
        }
    }

    public void Pose(CommandContext context)
    {
        var actor = context.Character;
        if (actor.Class is not { } playerClass)
        {
            return;
        }

        var level = Math.Min(actor.Level, PoseTable.MaxLevel);
        var pose = context.Random.Next(level + 1);
        var message = new ActMessageContext(actor);
        context.Acts.Act(
            PoseTable.CharacterMessage(playerClass, pose),
            message,
            ActTarget.ToChar,
            Position.Dead);
        context.Acts.Act(
            PoseTable.RoomMessage(playerClass, pose),
            message,
            ActTarget.ToRoom);
    }

    public void Tell(CommandContext context)
    {
        var actor = context.Character;
        if (Has(actor, NoTell) || Has(actor, Deaf))
        {
            Send(context, "Your message didn't get through.");
            return;
        }

        if (Has(actor, Quiet))
        {
            Send(context, "You must turn off quiet mode first.");
            return;
        }

        var token = ArgumentParser.OneArgument(context.Arguments);
        if (token.Value.Length == 0 || token.Remainder.Length == 0)
        {
            Send(context, "Tell whom what?");
            return;
        }

        var victim = context.Online.FindByPrefix(token.Value);
        if (victim is null)
        {
            Send(context, "They aren't here.");
            return;
        }

        DeliverTell(context, actor, victim, token.Remainder);
    }

    public void Reply(CommandContext context)
    {
        var actor = context.Character;
        if (Has(actor, NoTell))
        {
            Send(context, "Your message didn't get through.");
            return;
        }

        if (actor.Reply is not { } victim)
        {
            Send(context, "They aren't here.");
            return;
        }

        if (victim.Descriptor is null)
        {
            context.Output.Send(
                actor,
                ActFormatter.FormatLine(
                    "$N seems to have misplaced $S link...try again later.",
                    new ActMessageContext(actor, Victim: victim),
                    actor));
            BufferAfkTell(victim, actor, context.Arguments);
            return;
        }

        if (!IsImmortal(victim) && !IsAwake(actor))
        {
            Send(context, "In your dreams, or what?");
            return;
        }

        DeliverTell(context, actor, victim, context.Arguments, checkSenderAwake: false);
    }

    public void Gossip(CommandContext context) =>
        Channel(
            context,
            NoGossip,
            "Gossip",
            "$n gossips '$t'",
            "You gossip '" + context.Arguments + "'");

    public void Quote(CommandContext context) =>
        Channel(
            context,
            NoQuote,
            "Quote",
            "$n quotes '$t'",
            "You quote '" + context.Arguments + "'");

    public void Grats(CommandContext context) =>
        Channel(
            context,
            NoGrats,
            "Grats",
            "$n grats '$t'",
            "You grats '" + context.Arguments + "'");

    public void Auction(CommandContext context) =>
        Channel(
            context,
            NoAuction,
            "Auction",
            "$n auctions '$t'",
            "You auction '" + context.Arguments + "'");

    public void Question(CommandContext context) =>
        Channel(
            context,
            NoQuestion,
            "Q/A",
            "$n questions '$t'",
            "You question '" + context.Arguments + "'");

    public void Answer(CommandContext context) =>
        Channel(
            context,
            NoQuestion,
            "Q/A",
            "$n answers '$t'",
            "You answer '" + context.Arguments + "'");

    public void Music(CommandContext context) =>
        Channel(
            context,
            NoMusic,
            "Music",
            "$n MUSIC: '$t'",
            "You MUSIC: '" + context.Arguments + "'");

    public void Shout(CommandContext context)
    {
        var actor = context.Character;
        if (context.Arguments.Length == 0)
        {
            Toggle(context, ShoutsOff, "You can hear shouts again.", "You will no longer hear shouts.");
            return;
        }

        if (Has(actor, NoShout))
        {
            Send(context, "You can't shout.");
            return;
        }

        Clear(actor, ShoutsOff);
        Send(context, $"You shout '{context.Arguments}'");

        foreach (var target in PlayingCharacters(context))
        {
            if (ReferenceEquals(target, actor)
                || Has(target, ShoutsOff)
                || Has(target, Quiet)
                || target.Position < Position.Resting)
            {
                continue;
            }

            context.Output.Send(
                target,
                ActFormatter.FormatLine(
                    "$n shouts '$t'",
                    new ActMessageContext(actor, Text1: context.Arguments),
                    target));
        }
    }

    public void Yell(CommandContext context)
    {
        var actor = context.Character;
        if (Has(actor, NoShout))
        {
            Send(context, "You can't yell.");
            return;
        }

        if (context.Arguments.Length == 0)
        {
            Send(context, "Yell what?");
            return;
        }

        Send(context, $"You yell '{context.Arguments}'");

        var area = actor.InRoom is { } room
            ? context.World.GetAreaForRoom(room.Vnum)
            : null;

        foreach (var target in PlayingCharacters(context))
        {
            if (ReferenceEquals(target, actor)
                || Has(target, Quiet)
                || target.Position < Position.Resting
                || target.InRoom is null
                || area is null
                || !ReferenceEquals(context.World.GetAreaForRoom(target.InRoom.Vnum), area))
            {
                continue;
            }

            context.Output.Send(
                target,
                ActFormatter.FormatLine(
                    "$n yells '$t'",
                    new ActMessageContext(actor, Text1: context.Arguments),
                    target));
        }
    }

    public void GroupTell(CommandContext context)
    {
        if (context.Arguments.Length == 0)
        {
            Send(context, "Tell your group what?");
            return;
        }

        if (Has(context.Character, NoTell))
        {
            Send(context, "Your message didn't get through!");
            return;
        }

        foreach (var target in PlayingCharacters(context))
        {
            if (!IsSameGroup(target, context.Character))
            {
                continue;
            }

            context.Output.Send(
                target,
                ActFormatter.FormatLine(
                    "$n tells the group '$t'",
                    new ActMessageContext(context.Character, Text1: context.Arguments),
                    target));
        }
    }

    public void ImmortalTalk(CommandContext context)
    {
        var actor = context.Character;
        if (context.Arguments.Length == 0)
        {
            Toggle(
                context,
                NoWiz,
                "Immortal channel is now ON",
                "Immortal channel is now OFF");
            return;
        }

        Clear(actor, NoWiz);
        foreach (var target in PlayingCharacters(context))
        {
            if (!IsImmortal(target) || Has(target, NoWiz))
            {
                continue;
            }

            context.Output.Send(
                target,
                ActFormatter.FormatLine(
                    "$n: $t",
                    new ActMessageContext(actor, Text1: context.Arguments),
                    target));
        }
    }

    public void Channels(CommandContext context)
    {
        var actor = context.Character;
        var builder = new System.Text.StringBuilder();
        builder.Append("   channel     status\r\n---------------------\r\n");
        AppendStatus(builder, "gossip", !Has(actor, NoGossip));
        AppendStatus(builder, "auction", !Has(actor, NoAuction));
        AppendStatus(builder, "music", !Has(actor, NoMusic));
        AppendStatus(builder, "Q/A", !Has(actor, NoQuestion));
        AppendStatus(builder, "Quote", !Has(actor, NoQuote));
        AppendStatus(builder, "grats", !Has(actor, NoGrats));
        if (IsImmortal(actor))
        {
            AppendStatus(builder, "god channel", !Has(actor, NoWiz));
        }

        AppendStatus(builder, "shouts", !Has(actor, ShoutsOff));
        AppendStatus(builder, "tells", !Has(actor, Deaf));
        AppendStatus(builder, "quiet mode", Has(actor, Quiet));

        if (Has(actor, Afk))
        {
            builder.Append("You are AFK.\r\n");
        }

        if (Has(actor, SnoopProof))
        {
            builder.Append("You are immune to snooping.\r\n");
        }

        if (Has(actor, NoShout))
        {
            builder.Append("You cannot shout.\r\n");
        }

        if (Has(actor, NoTell))
        {
            builder.Append("You cannot use tell.\r\n");
        }

        if (Has(actor, NoChannels))
        {
            builder.Append("You cannot use channels.\r\n");
        }

        if (Has(actor, NoEmote))
        {
            builder.Append("You cannot show emotions.\r\n");
        }

        context.Output.Send(actor, builder.ToString());
    }

    public void ToggleDeaf(CommandContext context) =>
        Toggle(
            context,
            Deaf,
            "You can now hear tells again.",
            "From now on, you won't hear tells.");

    public void ToggleQuiet(CommandContext context) =>
        Toggle(
            context,
            Quiet,
            "Quiet mode removed.",
            "From now on, you will only hear says and emotes.");

    public void ToggleAfk(CommandContext context) =>
        Toggle(
            context,
            Afk,
            "AFK mode removed. Type 'replay' to see tells.",
            "You are now in AFK mode.");

    public void Replay(CommandContext context)
    {
        var actor = context.Character;
        if (actor.TellBuffer.Count == 0)
        {
            Send(context, "You have no tells to replay.");
            return;
        }

        foreach (var line in actor.TellBuffer)
        {
            context.Output.Send(actor, line.EndsWith("\r\n", StringComparison.Ordinal) ? line : line + "\r\n");
        }

        actor.TellBuffer.Clear();
    }

    public void ToggleNoChannels(CommandContext context)
    {
        var actor = context.Character;
        var token = ArgumentParser.OneArgument(context.Arguments);
        if (token.Value.Length == 0)
        {
            Send(context, "Nochannel whom?");
            return;
        }

        var victim = context.Online.FindByPrefix(token.Value);
        if (victim is null)
        {
            Send(context, "They aren't here.");
            return;
        }

        if (victim.Trust >= actor.Trust)
        {
            Send(context, "You failed.");
            return;
        }

        if (Has(victim, NoChannels))
        {
            Clear(victim, NoChannels);
            context.Output.Send(
                victim,
                "The gods have restored your channel priviliges.\r\n");
            Send(context, "NOCHANNELS removed.");
            context.Wiznet.Send(
                "$N restores channels to " + victim.Name,
                actor,
                RomFlags.Wiznet.Get("Penalties").Bit,
                RomFlags.Wiznet.Get("Secure").Bit);
        }
        else
        {
            Set(victim, NoChannels);
            context.Output.Send(
                victim,
                "The gods have revoked your channel priviliges.\r\n");
            Send(context, "NOCHANNELS set.");
            context.Wiznet.Send(
                "$N revokes " + victim.Name + "'s channels.",
                actor,
                RomFlags.Wiznet.Get("Penalties").Bit,
                RomFlags.Wiznet.Get("Secure").Bit);
        }
    }

    public void Wiznet(CommandContext context)
    {
        var actor = context.Character;
        var arguments = context.Arguments;
        var onBit = RomFlags.Wiznet.Get("On").Bit;

        if (arguments.Length == 0)
        {
            if ((actor.WiznetFlags & onBit) != 0)
            {
                actor.WiznetFlags &= ~onBit;
                Send(context, "Signing off of Wiznet.");
            }
            else
            {
                actor.WiznetFlags |= onBit;
                Send(context, "Welcome to Wiznet!");
            }

            return;
        }

        if (arguments.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            actor.WiznetFlags |= onBit;
            Send(context, "Welcome to Wiznet!");
            return;
        }

        if (arguments.StartsWith("off", StringComparison.OrdinalIgnoreCase))
        {
            actor.WiznetFlags &= ~onBit;
            Send(context, "Signing off of Wiznet.");
            return;
        }

        if (arguments.StartsWith("status", StringComparison.OrdinalIgnoreCase))
        {
            var names = new List<string>();
            if ((actor.WiznetFlags & onBit) == 0)
            {
                names.Add("off");
            }

            foreach (var definition in RomTables.WiznetDefinitions)
            {
                if ((actor.WiznetFlags & definition.Bit) != 0)
                {
                    names.Add(definition.Name.ToLowerInvariant());
                }
            }

            Send(context, "Wiznet status:\r\n" + string.Join(' ', names));
            return;
        }

        if (arguments.StartsWith("show", StringComparison.OrdinalIgnoreCase))
        {
            var names = RomTables.WiznetDefinitions
                .Where(definition => definition.MinLevel <= actor.Trust)
                .Select(definition => definition.Name.ToLowerInvariant());
            Send(
                context,
                "Wiznet options available to you are:\r\n" + string.Join(' ', names));
            return;
        }

        var option = RomTables.WiznetDefinitions.FirstOrDefault(
            definition => definition.Name.StartsWith(arguments, StringComparison.OrdinalIgnoreCase));
        if (option is null || actor.Trust < option.MinLevel)
        {
            Send(context, "No such option.");
            return;
        }

        if ((actor.WiznetFlags & option.Bit) != 0)
        {
            actor.WiznetFlags &= ~option.Bit;
            Send(context, $"You will no longer see {option.Name.ToLowerInvariant()} on wiznet.");
        }
        else
        {
            actor.WiznetFlags |= option.Bit;
            Send(context, $"You will now see {option.Name.ToLowerInvariant()} on wiznet.");
        }
    }

    public void Snoop(CommandContext context)
    {
        var actor = context.Character;
        var token = ArgumentParser.OneArgument(context.Arguments);
        if (token.Value.Length == 0)
        {
            Send(context, "Snoop whom?");
            return;
        }

        var victim = context.Online.FindByPrefix(token.Value);
        if (victim is null)
        {
            Send(context, "They aren't here.");
            return;
        }

        if (victim.Descriptor is null)
        {
            Send(context, "No descriptor to snoop.");
            return;
        }

        if (ReferenceEquals(victim, actor))
        {
            Send(context, "Cancelling all snoops.");
            foreach (var descriptor in context.Online.Snapshot())
            {
                if (descriptor.SnoopBy == actor.Descriptor)
                {
                    descriptor.SnoopBy = null;
                }
            }

            return;
        }

        if (victim.Descriptor.SnoopBy is not null)
        {
            Send(context, "Busy already.");
            return;
        }

        if (victim.Trust >= actor.Trust || Has(victim, SnoopProof))
        {
            Send(context, "You failed.");
            return;
        }

        for (var current = actor.Descriptor?.SnoopBy; current is not null; current = current.SnoopBy)
        {
            if (ReferenceEquals(current, victim.Descriptor))
            {
                Send(context, "No snoop loops.");
                return;
            }
        }

        victim.Descriptor.SnoopBy = actor.Descriptor;
        context.Wiznet.Send(
            "$N starts snooping on " + victim.Name,
            actor,
            RomFlags.Wiznet.Get("Snoops").Bit,
            RomFlags.Wiznet.Get("Secure").Bit,
            actor.Trust);
        Send(context, "Ok.");
    }

    public void Socials(CommandContext context)
    {
        var builder = new System.Text.StringBuilder();
        var column = 0;
        foreach (var social in context.World.Socials)
        {
            builder.Append(social.Name.PadRight(12));
            if (++column % 6 == 0)
            {
                builder.Append("\r\n");
            }
        }

        if (column % 6 != 0)
        {
            builder.Append("\r\n");
        }

        context.Output.Send(context.Character, builder.ToString());
    }

    private void Channel(
        CommandContext context,
        int flag,
        string channelName,
        string roomFormat,
        string senderLine)
    {
        var actor = context.Character;
        if (context.Arguments.Length == 0)
        {
            Toggle(
                context,
                flag,
                $"{channelName} channel is now ON.",
                $"{channelName} channel is now OFF.");
            return;
        }

        if (Has(actor, Quiet))
        {
            Send(context, "You must turn off quiet mode first.");
            return;
        }

        if (Has(actor, NoChannels))
        {
            Send(context, "The gods have revoked your channel priviliges.");
            return;
        }

        Clear(actor, flag);
        Send(context, senderLine);

        foreach (var target in PlayingCharacters(context))
        {
            if (ReferenceEquals(target, actor)
                || Has(target, flag)
                || Has(target, Quiet))
            {
                continue;
            }

            context.Output.Send(
                target,
                ActFormatter.FormatLine(
                    roomFormat,
                    new ActMessageContext(actor, Text1: context.Arguments),
                    target));
        }
    }

    private void DeliverTell(
        CommandContext context,
        Character actor,
        Character victim,
        string arguments,
        bool checkSenderAwake = true)
    {
        if (victim.Descriptor is null)
        {
            Send(context, "They aren't here.");
            return;
        }

        if (checkSenderAwake && actor.Trust < 53 && !IsAwake(victim))
        {
            context.Output.Send(
                actor,
                ActFormatter.FormatLine(
                    "$E can't hear you.",
                    new ActMessageContext(actor, Victim: victim),
                    actor));
            return;
        }

        if ((Has(victim, Quiet) || Has(victim, Deaf)) && !IsImmortal(actor))
        {
            context.Output.Send(
                actor,
                ActFormatter.FormatLine(
                    "$E is not receiving tells.",
                    new ActMessageContext(actor, Victim: victim),
                    actor));
            return;
        }

        if (Has(victim, Afk))
        {
            context.Output.Send(
                actor,
                ActFormatter.FormatLine(
                    "$E is AFK, but your tell will go through when $E returns.",
                    new ActMessageContext(actor, Victim: victim),
                    actor));
            BufferAfkTell(victim, actor, arguments);
            return;
        }

        context.Output.Send(
            actor,
            ActFormatter.FormatLine(
                "You tell $N '$t'",
                new ActMessageContext(actor, Victim: victim, Text1: arguments),
                actor));
        context.Output.Send(
            victim,
            ActFormatter.FormatLine(
                "$n tells you '$t'",
                new ActMessageContext(actor, Victim: victim, Text1: arguments),
                victim));
        victim.Reply = actor;
    }

    private static void BufferAfkTell(Character victim, Character actor, string arguments)
    {
        var line = $"{actor.Name} tells you '{arguments}'";
        victim.TellBuffer.Add(char.ToUpperInvariant(line[0]) + line[1..]);
    }

    private static void AppendStatus(System.Text.StringBuilder builder, string name, bool enabled) =>
        builder.Append(name.PadRight(15))
            .Append(enabled ? "ON\r\n" : "OFF\r\n");

    private void Toggle(CommandContext context, int bit, string onMessage, string offMessage)
    {
        var enabled = Has(context.Character, bit);
        if (enabled)
        {
            Clear(context.Character, bit);
            Send(context, onMessage);
        }
        else
        {
            Set(context.Character, bit);
            Send(context, offMessage);
        }
    }

    private void Send(CommandContext context, string text) =>
        context.Output.Send(context.Character, text + "\r\n");

    private IEnumerable<Character> PlayingCharacters(CommandContext context) =>
        context.Online.Snapshot()
            .Where(descriptor => descriptor.IsPlaying && descriptor.Character is not null)
            .Select(descriptor => descriptor.Character!);

    private static bool IsSameGroup(Character first, Character second)
    {
        var firstLeader = first.Leader ?? first;
        var secondLeader = second.Leader ?? second;
        return ReferenceEquals(firstLeader, secondLeader);
    }

    private static bool IsImmortal(Character character) => character.Trust >= ImmortalTrust;

    private static bool IsAwake(Character character) => character.Position > Position.Sleeping;

    private static bool Has(Character character, int bit) =>
        (character.CommunicationFlags & bit) != 0;

    private static void Set(Character character, int bit) =>
        character.CommunicationFlags |= bit;

    private static void Clear(Character character, int bit) =>
        character.CommunicationFlags &= ~bit;

    private static int Bit(string name) => RomFlags.Communication.Get(name).Bit;

    /// <summary>
    /// Replaces a viewer's name with "you" in pmote/smote text, matching the
    /// intent of ROM's do_pmote/do_smote walker (including <c>Name's</c>).
    /// </summary>
    internal static string Personalize(string text, string name)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(name))
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length)
        {
            var match = text.IndexOf(name, index, StringComparison.Ordinal);
            if (match < 0)
            {
                builder.Append(text, index, text.Length - index);
                break;
            }

            builder.Append(text, index, match - index);
            var after = match + name.Length;
            if (after + 1 < text.Length && text[after] == '\'' && text[after + 1] == 's')
            {
                builder.Append("your");
                index = after + 2;
            }
            else if (after < text.Length && text[after] == 's')
            {
                builder.Append("you");
                index = after + 1;
            }
            else
            {
                builder.Append("you");
                index = after;
            }
        }

        return builder.ToString();
    }
}
