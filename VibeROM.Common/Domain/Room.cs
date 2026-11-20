namespace VibeROM.Common.Domain;

/// <summary>A live room instance: immutable prototype data plus runtime contents.</summary>
public sealed class Room
{
    public Room(RoomPrototype prototype)
    {
        Prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
        _exits = new Dictionary<Direction, ExitPrototype>(prototype.Exits);
    }

    private readonly Dictionary<Direction, ExitPrototype> _exits;

    public RoomPrototype Prototype { get; }

    public int Vnum => Prototype.Vnum;

    public IReadOnlyDictionary<Direction, ExitPrototype> Exits => _exits;

    public int Light { get; set; }

    public List<Character> People { get; } = [];

    public List<GameObject> Contents { get; } = [];

    public List<Affect> Affects { get; } = [];

    public ExitPrototype? GetExit(Direction direction) =>
        _exits.TryGetValue(direction, out var exit) ? exit : null;

    public void ResetExits()
    {
        _exits.Clear();
        foreach (var (direction, exit) in Prototype.Exits)
        {
            _exits[direction] = exit;
        }
    }

    public void SetExit(Direction direction, ExitPrototype exit) =>
        _exits[direction] = exit;

    public void SetExits(IReadOnlyDictionary<Direction, ExitPrototype> exits)
    {
        _exits.Clear();
        foreach (var (direction, exit) in exits)
        {
            _exits[direction] = exit;
        }
    }

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
