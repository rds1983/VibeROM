using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Text;

namespace VibeROM.Server.Communication;

public enum ActTarget
{
    ToRoom,
    ToChar,
    ToVictim,
    ToNotVictim
}

/// <summary>ROM <c>act_new</c> audience selection and delivery.</summary>
public sealed class ActDelivery
{
    private readonly IPlayerOutput _output;

    public ActDelivery(IPlayerOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Act(
        string format,
        ActMessageContext context,
        ActTarget target,
        Position minimumPosition = Position.Resting)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(context);

        if (format.Length == 0)
        {
            return;
        }

        if (context.Actor is not Character actor || actor.InRoom is not { } room)
        {
            return;
        }

        var victim = context.Victim as Character;
        var audience = target == ActTarget.ToVictim ? victim?.InRoom : room;
        if (audience is null)
        {
            return;
        }

        var people = audience.People.ToArray();

        foreach (var recipient in people)
        {
            if (recipient.Descriptor is null || recipient.Position < minimumPosition)
            {
                continue;
            }

            switch (target)
            {
                case ActTarget.ToChar when !ReferenceEquals(recipient, actor):
                case ActTarget.ToVictim when !ReferenceEquals(recipient, victim)
                                              || ReferenceEquals(recipient, actor):
                case ActTarget.ToRoom when ReferenceEquals(recipient, actor):
                case ActTarget.ToNotVictim when ReferenceEquals(recipient, actor)
                                                || ReferenceEquals(recipient, victim):
                    continue;
            }

            _output.Send(
                recipient,
                ActFormatter.FormatLine(format, context, recipient));
        }
    }
}
