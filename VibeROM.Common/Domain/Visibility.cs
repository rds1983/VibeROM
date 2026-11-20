using VibeROM.Common.Text;

namespace VibeROM.Common.Domain;

/// <summary>Visibility rules supplied by T027 and consumed by act formatting.</summary>
public interface IVisibilityService
{
    bool CanSee(Character viewer, Character target);

    bool CanSeeObject(Character viewer, GameObject target);
}

public sealed class AllVisibleVisibilityService : IVisibilityService
{
    public static AllVisibleVisibilityService Instance { get; } = new();

    public bool CanSee(Character viewer, Character target) => true;

    public bool CanSeeObject(Character viewer, GameObject target) => true;
}

public static class VisibilityExtensions
{
    public static ActVisibility ToActVisibility(
        this IVisibilityService visibility,
        Character viewer) =>
        new(
            (_, target) => target is Character character && visibility.CanSee(viewer, character),
            (_, obj) => obj is GameObject gameObject && visibility.CanSeeObject(viewer, gameObject));
}
