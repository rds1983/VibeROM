using VibeROM.Common.Rom;
using VibeROM.Common.Text;

namespace VibeROM.Common.Domain;

/// <summary>A live object. Prototypes are immutable; this object is mutable state.</summary>
public sealed class GameObject : IActObject
{
    public GameObject(ObjectPrototype prototype)
    {
        Prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
        ExtraFlags = prototype.ExtraFlags;
        WearFlags = prototype.WearFlags;
        Level = prototype.Level;
        Condition = prototype.Condition;
        CostCopper = prototype.CostCopper;
    }

    public ObjectPrototype Prototype { get; }

    public int Vnum => Prototype.Vnum;

    public string Keywords => Prototype.Keywords;

    public string ShortDescription => ShortDescriptionOverride ?? Prototype.ShortDescription;

    public string? ShortDescriptionOverride { get; set; }

    public string Description => Prototype.LongDescription;

    public string Material => Prototype.Material;

    public ItemType ItemType => Prototype.ItemType;

    public ObjectTypeData Data => Prototype.Data;

    public int ExtraFlags { get; set; }

    public int WearFlags { get; set; }

    public int Level { get; set; }

    public int WeightTenths => Prototype.WeightTenths;

    public int CostCopper { get; set; }

    public int Condition { get; set; }

    public int Timer { get; set; }

    public WearSlot WearLocation { get; set; } = WearSlot.None;

    public List<Affect> Affects { get; } = [];

    public List<ExtraDescription> ExtraDescriptions { get; } = [];

    public GameObject? Container { get; set; }

    public List<GameObject> Contents { get; } = [];

    public Character? CarriedBy { get; set; }

    public Room? InRoom { get; set; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (Condition is < 0 or > 100) errors.Add("condition must be between 0 and 100.");
        if (Level < 0) errors.Add("level must not be negative.");
        if (string.IsNullOrWhiteSpace(ShortDescription)) errors.Add("shortDescription must not be empty.");

        DomainValidation.ValidateMask(errors, "extraFlags", ExtraFlags, RomFlags.Extra);
        DomainValidation.ValidateMask(errors, "wearFlags", WearFlags, RomFlags.Wear);

        var owners = (Container is not null ? 1 : 0)
            + (CarriedBy is not null ? 1 : 0)
            + (InRoom is not null ? 1 : 0);

        if (owners > 1)
        {
            errors.Add("object has more than one owner (container, character or room).");
        }

        for (var index = 0; index < Affects.Count; index++)
        {
            errors.AddRange(Affects[index].Validate($"affects[{index}]"));
        }

        for (var index = 0; index < ExtraDescriptions.Count; index++)
        {
            errors.AddRange(ExtraDescriptions[index].Validate($"extraDescriptions[{index}]"));
        }

        var seen = new HashSet<GameObject>(ReferenceEqualityComparer.Instance);
        foreach (var child in Contents)
        {
            if (ReferenceEquals(child, this))
            {
                errors.Add("object cannot contain itself.");
                continue;
            }

            if (!seen.Add(child))
            {
                errors.Add($"object contains duplicate item {child.Vnum}.");
            }

            if (!ReferenceEquals(child.Container, this))
            {
                errors.Add($"contained item {child.Vnum} does not point back to this container.");
            }
        }

        return errors;
    }

    string IActObject.ShortDescription => ShortDescription;
}
