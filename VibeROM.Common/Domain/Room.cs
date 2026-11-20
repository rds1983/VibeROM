namespace VibeROM.Common.Domain;

/// <summary>A live room instance: immutable prototype data plus runtime contents.</summary>
public sealed class Room
{
    public Room(RoomPrototype prototype)
    {
        Prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
    }

    public RoomPrototype Prototype { get; }

    public int Vnum => Prototype.Vnum;

    public int Light { get; set; }

    public List<Character> People { get; } = [];

    public List<GameObject> Contents { get; } = [];

    public List<Affect> Affects { get; } = [];

    public ExitPrototype? GetExit(Direction direction) =>
        Prototype.Exits.TryGetValue(direction, out var exit) ? exit : null;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        var people = new HashSet<Character>(ReferenceEqualityComparer.Instance);
        foreach (var character in People)
        {
            if (!people.Add(character))
            {
                errors.Add($"duplicate character {character.Name} in room {Vnum}.");
            }

            if (!ReferenceEquals(character.InRoom, this))
            {
                errors.Add($"character {character.Name} does not point back to room {Vnum}.");
            }
        }

        var objects = new HashSet<GameObject>(ReferenceEqualityComparer.Instance);
        foreach (var obj in Contents)
        {
            if (!objects.Add(obj))
            {
                errors.Add($"duplicate object {obj.Vnum} in room {Vnum}.");
            }

            if (!ReferenceEquals(obj.InRoom, this))
            {
                errors.Add($"object {obj.Vnum} does not point back to room {Vnum}.");
            }
        }

        for (var index = 0; index < Affects.Count; index++)
        {
            errors.AddRange(Affects[index].Validate($"affects[{index}]"));
        }

        return errors;
    }
}
