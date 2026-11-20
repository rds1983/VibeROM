using VibeROM.Common.Rom;
using VibeROM.Common.Text;

namespace VibeROM.Common.Domain;

/// <summary>A live character. Prototypes are immutable; this object is mutable state.</summary>
public sealed class Character : IActActor
{
    private readonly int[] _permanentStats = new int[5];
    private readonly int[] _modifiedStats = new int[5];

    public Character(MobilePrototype prototype, string? name = null)
    {
        Prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
        Name = name ?? prototype.ShortDescription;
        Keywords = prototype.Keywords;
        ShortDescription = prototype.ShortDescription;
        LongDescription = prototype.LongDescription;
        Description = prototype.Description;
        Race = prototype.Race;
        Sex = prototype.Sex;
        Level = prototype.Level;
        Trust = prototype.Level;
        ActFlags = prototype.ActFlags;
        AffectedBy = prototype.AffectedBy;
        OffensiveFlags = prototype.OffensiveFlags;
        Immunities = prototype.Immunities;
        Resistances = prototype.Resistances;
        Vulnerabilities = prototype.Vulnerabilities;
        Form = prototype.Form;
        Parts = prototype.Parts;
        Size = prototype.Size;
        Material = prototype.Material;
        Position = prototype.StartPosition;
        StartPosition = prototype.StartPosition;
        DefaultPosition = prototype.DefaultPosition;
        HitRoll = prototype.HitRoll;
        Armor = prototype.Armor;
        Damage = prototype.Damage;
        Gold = prototype.Gold;
        MaxHit = Math.Max(1, prototype.Level * 8);
        Hit = MaxHit;
        MaxMana = Math.Max(1, prototype.Level * 10);
        Mana = MaxMana;
        MaxMove = 100;
        Move = 100;
        _permanentStats.AsSpan().Fill(13);
    }

    public Character(
        PlayerData player,
        string name,
        Race race,
        PlayerClass playerClass,
        Sex sex)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Name = name;
        Keywords = name;
        ShortDescription = name;
        LongDescription = name;
        Description = string.Empty;
        Race = race;
        Class = playerClass;
        Sex = sex;
        Player.TrueSex = sex;
        Level = 1;
        Trust = 1;
        MaxHit = 10;
        Hit = 10;
        MaxMana = 100;
        Mana = 100;
        MaxMove = 100;
        Move = 100;
        _permanentStats.AsSpan().Fill(13);
    }

    public MobilePrototype? Prototype { get; }

    public PlayerData? Player { get; set; }

    public bool IsNpc => Prototype is not null;

    public bool IsPlayer => Player is not null;

    public string Name { get; set; }

    public string Keywords { get; set; }

    public string ShortDescription { get; set; }

    public string LongDescription { get; set; }

    public string Description { get; set; }

    public Race Race { get; set; }

    public Sex Sex { get; set; }

    public PlayerClass? Class { get; set; }

    public int Level { get; set; }

    public int Trust { get; set; }

    public int Played { get; set; }

    public int Lines { get; set; } = 24;

    public DateTimeOffset Logon { get; set; } = DateTimeOffset.UtcNow;

    public int ActFlags { get; set; }

    public int AffectedBy { get; set; }

    public int OffensiveFlags { get; set; }

    public int Immunities { get; set; }

    public int Resistances { get; set; }

    public int Vulnerabilities { get; set; }

    public int Form { get; set; }

    public int Parts { get; set; }

    public Size Size { get; set; } = Size.Medium;

    public string Material { get; set; } = "none";

    public Position Position { get; set; } = Position.Standing;

    public Position StartPosition { get; set; } = Position.Standing;

    public Position DefaultPosition { get; set; } = Position.Standing;

    public int Hit { get; set; }

    public int MaxHit { get; set; }

    public int Mana { get; set; }

    public int MaxMana { get; set; }

    public int Move { get; set; }

    public int MaxMove { get; set; }

    public long Gold { get; set; }

    public long Silver { get; set; }

    public int Experience { get; set; }

    public int Practice { get; set; }

    public int Train { get; set; }

    public int SavingThrow { get; set; }

    public int Alignment { get; set; }

    public int HitRoll { get; set; }

    public int DamageRoll { get; set; }

    public ArmorValues Armor { get; set; }

    public int Wimpy { get; set; }

    public int InvisLevel { get; set; }

    public int IncogLevel { get; set; }

    public int Wait { get; set; }

    public int Daze { get; set; }

    public int Timer { get; set; }

    public int PlayerFlags { get; set; }

    public int CommunicationFlags { get; set; }

    public int WiznetFlags { get; set; }

    public DamageDice? Damage { get; set; }

    public int CarryWeight { get; set; }

    public int CarryNumber { get; set; }

    public Room? InRoom { get; set; }

    public Room? WasInRoom { get; set; }

    public AreaData? Zone { get; set; }

    public Character? Master { get; set; }

    public Character? Leader { get; set; }

    public Character? Fighting { get; set; }

    public Character? Reply { get; set; }

    /// <summary>AFK tells waiting for <c>replay</c> (runtime only, not saved).</summary>
    public List<string> TellBuffer { get; } = [];

    public Character? Pet { get; set; }

    public GameObject? On { get; set; }

    public Descriptor? Descriptor { get; set; }

    public GenerationData? Generation { get; set; }

    public List<Character> Followers { get; } = [];

    public List<Affect> Affects { get; } = [];

    public List<MemoryData> Memory { get; } = [];

    public List<GameObject> Inventory { get; } = [];

    public Dictionary<WearSlot, GameObject> Equipment { get; } = [];

    public IReadOnlyList<int> PermanentStats => _permanentStats;

    public IReadOnlyList<int> ModifiedStats => _modifiedStats;

    public int GetPermanentStat(int index) => _permanentStats[index];

    public void SetPermanentStat(int index, int value) => _permanentStats[index] = value;

    public int GetModifiedStat(int index) => _modifiedStats[index];

    public void SetModifiedStat(int index, int value) => _modifiedStats[index] = value;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("name must not be empty.");
        }

        if (Level < 0) errors.Add("level must not be negative.");
        if (Trust < 0) errors.Add("trust must not be negative.");
        if (MaxHit < 0 || MaxMana < 0 || MaxMove < 0) errors.Add("maximum vitals must not be negative.");

        if (IsNpc && Player is not null)
        {
            errors.Add("an NPC must not have player data.");
        }

        if (!IsNpc && Player is null)
        {
            errors.Add("a player character must have player data.");
        }

        if (!IsNpc && Class is null)
        {
            errors.Add("a player character must have a class.");
        }

        if (!IsNpc && Race is not (Race.Human or Race.Elf or Race.Dwarf or Race.Giant))
        {
            errors.Add("a player character must use a PC race.");
        }

        foreach (var item in Inventory)
        {
            if (!ReferenceEquals(item.CarriedBy, this))
            {
                errors.Add($"inventory item {item.Vnum} is not carried by this character.");
            }
        }

        foreach (var (slot, item) in Equipment)
        {
            if (slot == WearSlot.None)
            {
                errors.Add($"equipment item {item.Vnum} has no wear slot.");
            }

            if (item.WearLocation != slot)
            {
                errors.Add($"equipment item {item.Vnum} wear slot {item.WearLocation} does not match {slot}.");
            }

            if (!Inventory.Contains(item))
            {
                errors.Add($"equipment item {item.Vnum} is not in the character inventory.");
            }
        }

        for (var index = 0; index < Affects.Count; index++)
        {
            errors.AddRange(Affects[index].Validate($"affects[{index}]"));
        }

        return errors;
    }

    string IActActor.Name => Name;

    string IActActor.ShortDescription => ShortDescription;

    bool IActActor.IsNpc => IsNpc;

    ActorSex IActActor.Sex =>
        Sex switch
        {
            Sex.Male => ActorSex.Male,
            Sex.Female => ActorSex.Female,
            _ => ActorSex.Neutral
        };
}
