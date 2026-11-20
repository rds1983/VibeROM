using NLog;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>
/// Immutable prototype indexes plus the mutable runtime world. All ownership
/// changes go through this class so back-references stay consistent.
/// </summary>
public sealed class World
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly HashSet<Character> _characters = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<GameObject> _objects = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, List<HelpEntry>> _helpIndex =
        new(StringComparer.OrdinalIgnoreCase);

    public World(IEnumerable<AreaData> areas)
    {
        ArgumentNullException.ThrowIfNull(areas);

        var areaList = areas.ToArray();
        var errors = new List<string>();
        var mobiles = new Dictionary<int, MobilePrototype>();
        var objects = new Dictionary<int, ObjectPrototype>();
        var rooms = new Dictionary<int, RoomPrototype>();
        var shops = new Dictionary<int, ShopData>();
        var helps = new List<HelpEntry>();

        foreach (var area in areaList)
        {
            errors.AddRange(area.Validate().Select(error => $"{area.Name}: {error}"));

            foreach (var mobile in area.Mobiles)
            {
                if (!mobiles.TryAdd(mobile.Vnum, mobile))
                {
                    errors.Add($"Duplicate mobile vnum {mobile.Vnum} in area '{area.Name}'.");
                }
            }

            foreach (var obj in area.Objects)
            {
                if (!objects.TryAdd(obj.Vnum, obj))
                {
                    errors.Add($"Duplicate object vnum {obj.Vnum} in area '{area.Name}'.");
                }
            }

            foreach (var room in area.Rooms)
            {
                if (!rooms.TryAdd(room.Vnum, room))
                {
                    errors.Add($"Duplicate room vnum {room.Vnum} in area '{area.Name}'.");
                }
            }

            foreach (var shop in area.Shops)
            {
                if (!shops.TryAdd(shop.KeeperVnum, shop))
                {
                    errors.Add($"Duplicate shop keeper vnum {shop.KeeperVnum} in area '{area.Name}'.");
                }
            }

            helps.AddRange(area.Helps);
        }

        if (errors.Count > 0)
        {
            throw new WorldDataException(errors);
        }

        Areas = areaList;
        Mobiles = mobiles;
        ObjectPrototypes = objects;
        RoomPrototypes = rooms;
        Shops = shops;
        Helps = helps;
        Rooms = rooms.ToDictionary(pair => pair.Key, pair => new Room(pair.Value));

        foreach (var help in helps)
        {
            foreach (var token in TokenizeKeywords(help.Keywords))
            {
                if (!_helpIndex.TryGetValue(token, out var entries))
                {
                    entries = [];
                    _helpIndex[token] = entries;
                }

                entries.Add(help);
            }
        }
    }

    public IReadOnlyList<AreaData> Areas { get; }

    public IReadOnlyDictionary<int, MobilePrototype> Mobiles { get; }

    public IReadOnlyDictionary<int, ObjectPrototype> ObjectPrototypes { get; }

    public IReadOnlyDictionary<int, RoomPrototype> RoomPrototypes { get; }

    /// <summary>Shop definitions indexed by keeper mobile vnum.</summary>
    public IReadOnlyDictionary<int, ShopData> Shops { get; }

    /// <summary>Runtime room instances keyed by vnum.</summary>
    public IReadOnlyDictionary<int, Room> Rooms { get; }

    public IReadOnlyList<HelpEntry> Helps { get; }

    public IReadOnlyCollection<Character> Characters => _characters;

    public IReadOnlyCollection<GameObject> GameObjects => _objects;

    public int TopArea => Areas.Count;

    public int TopMobIndex => Mobiles.Count;

    public int TopObjectIndex => ObjectPrototypes.Count;

    public int TopRoom => Rooms.Count;

    public int TopHelp => Helps.Count;

    public int TopVnumMob => Mobiles.Count == 0 ? 0 : Mobiles.Keys.Max();

    public int TopVnumObj => ObjectPrototypes.Count == 0 ? 0 : ObjectPrototypes.Keys.Max();

    public int TopVnumRoom => RoomPrototypes.Count == 0 ? 0 : RoomPrototypes.Keys.Max();

    public MobilePrototype GetMobile(int vnum)
    {
        if (Mobiles.TryGetValue(vnum, out var mobile))
        {
            return mobile;
        }

        Logger.Warn("Unknown mobile vnum {Vnum}", vnum);
        throw new KeyNotFoundException($"Unknown mobile vnum {vnum}.");
    }

    public ObjectPrototype GetObjectPrototype(int vnum)
    {
        if (ObjectPrototypes.TryGetValue(vnum, out var obj))
        {
            return obj;
        }

        Logger.Warn("Unknown object vnum {Vnum}", vnum);
        throw new KeyNotFoundException($"Unknown object vnum {vnum}.");
    }

    public RoomPrototype GetRoomPrototype(int vnum)
    {
        if (RoomPrototypes.TryGetValue(vnum, out var room))
        {
            return room;
        }

        Logger.Warn("Unknown room vnum {Vnum}", vnum);
        throw new KeyNotFoundException($"Unknown room vnum {vnum}.");
    }

    public Room GetRoom(int vnum)
    {
        if (Rooms.TryGetValue(vnum, out var room))
        {
            return room;
        }

        Logger.Warn("Unknown room vnum {Vnum}", vnum);
        throw new KeyNotFoundException($"Unknown room vnum {vnum}.");
    }

    public ShopData GetShop(int keeperVnum)
    {
        if (Shops.TryGetValue(keeperVnum, out var shop))
        {
            return shop;
        }

        Logger.Warn("Unknown shop keeper vnum {Vnum}", keeperVnum);
        throw new KeyNotFoundException($"Unknown shop keeper vnum {keeperVnum}.");
    }

    public bool TryGetRoom(int vnum, out Room room) => Rooms.TryGetValue(vnum, out room!);

    public IReadOnlyList<HelpEntry> FindHelp(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        return _helpIndex.TryGetValue(keyword, out var entries) ? entries : [];
    }

    public Character CreateMobile(MobilePrototype prototype, Room? room = null)
    {
        ArgumentNullException.ThrowIfNull(prototype);
        if (room is not null)
        {
            EnsureRoom(room);
        }

        var character = new Character(prototype);
        _characters.Add(character);

        if (room is not null)
        {
            MoveCharacterToRoom(character, room);
        }

        return character;
    }

    public Character CreatePlayer(
        PlayerData player,
        string name,
        Race race,
        PlayerClass playerClass,
        Sex sex)
    {
        var character = new Character(player, name, race, playerClass, sex);
        _characters.Add(character);
        return character;
    }

    public GameObject CreateObject(
        ObjectPrototype prototype,
        Room? room = null,
        GameObject? container = null,
        Character? carrier = null)
    {
        ArgumentNullException.ThrowIfNull(prototype);

        var destinations = (room is null ? 0 : 1)
            + (container is null ? 0 : 1)
            + (carrier is null ? 0 : 1);

        if (destinations > 1)
        {
            throw new ArgumentException("An object can start in only one place.");
        }

        if (room is not null) EnsureRoom(room);
        if (container is not null) EnsureObject(container);
        if (carrier is not null) EnsureCharacter(carrier);

        var obj = new GameObject(prototype);
        _objects.Add(obj);

        if (room is not null)
        {
            MoveObjectToRoom(obj, room);
        }
        else if (container is not null)
        {
            MoveObjectToContainer(obj, container);
        }
        else if (carrier is not null)
        {
            MoveObjectToCharacter(obj, carrier);
        }

        return obj;
    }

    public void MoveCharacterToRoom(Character character, Room room)
    {
        EnsureCharacter(character);
        EnsureRoom(room);

        if (ReferenceEquals(character.InRoom, room))
        {
            return;
        }

        if (character.InRoom is { } oldRoom)
        {
            oldRoom.People.Remove(character);
        }

        character.InRoom = room;
        if (!room.People.Contains(character))
        {
            room.People.Add(character);
        }
    }

    public void MoveObjectToRoom(GameObject obj, Room room)
    {
        EnsureObject(obj);
        EnsureRoom(room);

        DetachObject(obj);
        obj.InRoom = room;
        if (!room.Contents.Contains(obj))
        {
            room.Contents.Add(obj);
        }
    }

    public void MoveObjectToCharacter(GameObject obj, Character character)
    {
        EnsureObject(obj);
        EnsureCharacter(character);

        if (ReferenceEquals(obj.CarriedBy, character))
        {
            return;
        }

        DetachObject(obj);
        obj.CarriedBy = character;
        if (!character.Inventory.Contains(obj))
        {
            character.Inventory.Add(obj);
        }
    }

    public void MoveObjectToContainer(GameObject obj, GameObject container)
    {
        EnsureObject(obj);
        EnsureObject(container);

        if (ReferenceEquals(obj, container))
        {
            throw new InvalidOperationException("An object cannot contain itself.");
        }

        for (var current = container; current is not null; current = current.Container)
        {
            if (ReferenceEquals(current, obj))
            {
                throw new InvalidOperationException("An object cannot be placed inside its own contents.");
            }
        }

        DetachObject(obj);
        obj.Container = container;
        if (!container.Contents.Contains(obj))
        {
            container.Contents.Add(obj);
        }
    }

    public void EquipObject(Character character, GameObject obj, WearSlot slot)
    {
        EnsureCharacter(character);
        EnsureObject(obj);

        if (!ReferenceEquals(obj.CarriedBy, character))
        {
            throw new InvalidOperationException("An object must be carried before it can be equipped.");
        }

        if (slot == WearSlot.None)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "A wear slot is required.");
        }

        if (character.Equipment.TryGetValue(slot, out var existing) && !ReferenceEquals(existing, obj))
        {
            UnequipObject(character, slot);
        }

        obj.WearLocation = slot;
        character.Equipment[slot] = obj;
    }

    public void UnequipObject(Character character, WearSlot slot)
    {
        EnsureCharacter(character);

        if (character.Equipment.Remove(slot, out var obj))
        {
            obj.WearLocation = WearSlot.None;
        }
    }

    public void DestroyObject(GameObject obj)
    {
        EnsureObject(obj);

        DetachObject(obj);

        foreach (var child in obj.Contents.ToArray())
        {
            DestroyObject(child);
        }

        obj.Affects.Clear();
        obj.ExtraDescriptions.Clear();
        _objects.Remove(obj);
    }

    public void DestroyCharacter(Character character)
    {
        EnsureCharacter(character);

        if (character.InRoom is { } room)
        {
            room.People.Remove(character);
            character.InRoom = null;
        }

        if (character.Master is { } master)
        {
            master.Followers.Remove(character);
            if (ReferenceEquals(master.Pet, character))
            {
                master.Pet = null;
            }
        }

        foreach (var follower in character.Followers.ToArray())
        {
            follower.Master = null;
        }

        character.Followers.Clear();

        if (character.Pet is { } pet)
        {
            pet.Master = null;
        }

        foreach (var other in _characters)
        {
            if (ReferenceEquals(other.Fighting, character))
            {
                other.Fighting = null;
            }

            if (ReferenceEquals(other.Reply, character))
            {
                other.Reply = null;
            }
        }

        character.Fighting = null;
        character.Reply = null;
        character.Pet = null;
        character.Master = null;
        character.On = null;

        foreach (var item in character.Inventory.ToArray())
        {
            DestroyObject(item);
        }

        character.Inventory.Clear();
        character.Equipment.Clear();
        character.Affects.Clear();
        _characters.Remove(character);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var area in Areas)
        {
            errors.AddRange(area.Validate().Select(error => $"{area.Name}: {error}"));
        }

        foreach (var (vnum, prototype) in Mobiles)
        {
            if (prototype.Vnum != vnum)
            {
                errors.Add($"mobile index {vnum} points at vnum {prototype.Vnum}.");
            }
        }

        foreach (var (vnum, prototype) in ObjectPrototypes)
        {
            if (prototype.Vnum != vnum)
            {
                errors.Add($"object index {vnum} points at vnum {prototype.Vnum}.");
            }
        }

        foreach (var (vnum, prototype) in RoomPrototypes)
        {
            if (prototype.Vnum != vnum)
            {
                errors.Add($"room index {vnum} points at vnum {prototype.Vnum}.");
            }

            if (!Rooms.TryGetValue(vnum, out var runtime) || !ReferenceEquals(runtime.Prototype, prototype))
            {
                errors.Add($"room {vnum} has no matching runtime room.");
            }
        }

        foreach (var (keeperVnum, shop) in Shops)
        {
            if (!Mobiles.ContainsKey(keeperVnum))
            {
                errors.Add($"shop keeper {keeperVnum} is not a known mobile.");
            }

            errors.AddRange(shop.Validate($"shop keeper {keeperVnum}"));
        }

        foreach (var room in Rooms.Values)
        {
            errors.AddRange(room.Validate().Select(error => $"room {room.Vnum}: {error}"));
        }

        foreach (var character in _characters)
        {
            errors.AddRange(character.Validate().Select(error => $"character {character.Name}: {error}"));

            if (character.InRoom is { } inRoom &&
                (!Rooms.TryGetValue(inRoom.Vnum, out var knownRoom) || !ReferenceEquals(knownRoom, inRoom)))
            {
                errors.Add($"character {character.Name} is in an unknown room {inRoom.Vnum}.");
            }

            foreach (var item in character.Inventory)
            {
                if (!_objects.Contains(item))
                {
                    errors.Add($"character {character.Name} carries untracked object {item.Vnum}.");
                }
            }
        }

        foreach (var obj in _objects)
        {
            errors.AddRange(obj.Validate().Select(error => $"object {obj.Vnum}: {error}"));

            if (!ObjectPrototypes.TryGetValue(obj.Prototype.Vnum, out var prototype) ||
                !ReferenceEquals(prototype, obj.Prototype))
            {
                errors.Add($"object {obj.Vnum} has an unknown prototype.");
            }

            if (obj.CarriedBy is { } carrier && !_characters.Contains(carrier))
            {
                errors.Add($"object {obj.Vnum} is carried by an unknown character.");
            }

            if (obj.InRoom is { } room &&
                (!Rooms.TryGetValue(room.Vnum, out var knownRoom) || !ReferenceEquals(knownRoom, room)))
            {
                errors.Add($"object {obj.Vnum} is in an unknown room {room.Vnum}.");
            }

            if (obj.Container is { } container && !_objects.Contains(container))
            {
                errors.Add($"object {obj.Vnum} is inside an untracked container.");
            }

            foreach (var child in obj.Contents)
            {
                if (!_objects.Contains(child))
                {
                    errors.Add($"object {obj.Vnum} contains untracked object {child.Vnum}.");
                }
            }
        }

        return errors;
    }

    private void DetachObject(GameObject obj)
    {
        if (obj.Container is { } container)
        {
            container.Contents.Remove(obj);
            obj.Container = null;
        }

        if (obj.CarriedBy is { } carrier)
        {
            foreach (var slot in carrier.Equipment
                         .Where(pair => ReferenceEquals(pair.Value, obj))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                carrier.Equipment.Remove(slot);
            }

            carrier.Inventory.Remove(obj);
            obj.CarriedBy = null;
        }

        if (obj.InRoom is { } room)
        {
            room.Contents.Remove(obj);
            obj.InRoom = null;
        }

        obj.WearLocation = WearSlot.None;
    }

    private void EnsureObject(GameObject obj)
    {
        if (!_objects.Contains(obj))
        {
            throw new InvalidOperationException("Object is not part of this world.");
        }
    }

    private void EnsureCharacter(Character character)
    {
        if (!_characters.Contains(character))
        {
            throw new InvalidOperationException("Character is not part of this world.");
        }
    }

    private void EnsureRoom(Room room)
    {
        if (!Rooms.TryGetValue(room.Vnum, out var known) || !ReferenceEquals(known, room))
        {
            throw new InvalidOperationException("Room is not part of this world.");
        }
    }

    private static IEnumerable<string> TokenizeKeywords(string keywords) =>
        keywords.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
