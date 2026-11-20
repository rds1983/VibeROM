using System.Text;

namespace VibeROM.Common.Text;

/// <summary>Sex values used by the act pronoun table.</summary>
public enum ActorSex
{
    Neutral = 0,
    Male = 1,
    Female = 2
}

/// <summary>The parts of a character that act messages need.</summary>
public interface IActActor
{
    string Name { get; }

    string ShortDescription { get; }

    bool IsNpc { get; }

    ActorSex Sex { get; }
}

/// <summary>The parts of an object that act messages need.</summary>
public interface IActObject
{
    string ShortDescription { get; }
}

/// <summary>Visibility checks for one message recipient.</summary>
public sealed record ActVisibility(
    Func<IActActor, IActActor, bool> CanSeeActor,
    Func<IActActor, IActObject, bool> CanSeeObject)
{
    public static ActVisibility AllVisible { get; } =
        new((_, _) => true, (_, _) => true);
}

/// <summary>The actor, optional victim/objects/strings and door used by one act call.</summary>
public sealed record ActMessageContext(
    IActActor Actor,
    IActActor? Victim = null,
    IActObject? Object1 = null,
    IActObject? Object2 = null,
    string? Text1 = null,
    string? Text2 = null,
    string? Door = null);

/// <summary>
/// Port of ROM's <c>act_new</c> token expander. Formatting is pure; delivery to
/// an audience is handled by the server layer.
/// </summary>
public static class ActFormatter
{
    public const string Placeholder = " <@@@> ";

    public static string Format(
        string format,
        ActMessageContext context,
        IActActor viewer,
        ActVisibility? visibility = null)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(viewer);

        if (format.Length == 0)
        {
            return string.Empty;
        }

        visibility ??= ActVisibility.AllVisible;

        var builder = new StringBuilder(format.Length + 32);
        for (var index = 0; index < format.Length; index++)
        {
            var character = format[index];
            if (character != '$')
            {
                builder.Append(character);
                continue;
            }

            if (index + 1 >= format.Length)
            {
                builder.Append(Placeholder);
                break;
            }

            var code = format[++index];
            if (code == '$')
            {
                builder.Append('$');
                continue;
            }

            builder.Append(Expand(code, context, viewer, visibility));
        }

        if (builder.Length > 0)
        {
            builder[0] = char.ToUpperInvariant(builder[0]);
        }

        return builder.ToString();
    }

    public static string FormatLine(
        string format,
        ActMessageContext context,
        IActActor viewer,
        ActVisibility? visibility = null) =>
        Format(format, context, viewer, visibility) + "\r\n";

    private static string Expand(
        char code,
        ActMessageContext context,
        IActActor viewer,
        ActVisibility visibility) =>
        code switch
        {
            't' => context.Text1 ?? Placeholder,
            'T' => context.Text2 ?? Placeholder,
            'n' => Person(context.Actor, viewer, visibility),
            'N' => Person(context.Victim, viewer, visibility),
            'e' => Pronoun(context.Actor.Sex, PronounForm.Subjective),
            'E' => context.Victim is null ? Placeholder : Pronoun(context.Victim.Sex, PronounForm.Subjective),
            'm' => Pronoun(context.Actor.Sex, PronounForm.Objective),
            'M' => context.Victim is null ? Placeholder : Pronoun(context.Victim.Sex, PronounForm.Objective),
            's' => Pronoun(context.Actor.Sex, PronounForm.Possessive),
            'S' => context.Victim is null ? Placeholder : Pronoun(context.Victim.Sex, PronounForm.Possessive),
            'p' => DescribeObject(context.Object1, viewer, visibility),
            'P' => DescribeObject(context.Object2, viewer, visibility),
            'd' => string.IsNullOrEmpty(context.Door)
                ? "door"
                : ArgumentParser.OneArgument(context.Door).Value,
            _ => Placeholder
        };

    private static string Person(IActActor? target, IActActor viewer, ActVisibility visibility)
    {
        if (target is null)
        {
            return Placeholder;
        }

        if (!visibility.CanSeeActor(viewer, target))
        {
            return "someone";
        }

        return target.IsNpc ? target.ShortDescription : target.Name;
    }

    private static string DescribeObject(IActObject? obj, IActActor viewer, ActVisibility visibility)
    {
        if (obj is null)
        {
            return Placeholder;
        }

        return visibility.CanSeeObject(viewer, obj)
            ? obj.ShortDescription
            : "something";
    }

    private static string Pronoun(ActorSex sex, PronounForm form) =>
        form switch
        {
            PronounForm.Subjective => sex switch
            {
                ActorSex.Male => "he",
                ActorSex.Female => "she",
                _ => "it"
            },
            PronounForm.Objective => sex switch
            {
                ActorSex.Male => "him",
                ActorSex.Female => "her",
                _ => "it"
            },
            _ => sex switch
            {
                ActorSex.Male => "his",
                ActorSex.Female => "her",
                _ => "its"
            }
        };

    private enum PronounForm
    {
        Subjective,
        Objective,
        Possessive
    }
}
