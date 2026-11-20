namespace VibeROM.Common.Rom;

/// <summary>A canonical name and numeric value in a lookup table.</summary>
public sealed record ValueDefinition(int Value, string Name);

public sealed record SkillDefinition(
    string Name,
    IReadOnlyList<int> Levels,
    IReadOnlyList<int> Ratings,
    string? SpellFunction,
    SkillTarget Target,
    Position MinimumPosition,
    string? Gsn,
    int Slot,
    int MinMana,
    int Beats,
    string NounDamage,
    string MessageOff,
    string MessageObject);

public sealed record GroupDefinition(
    string Name,
    IReadOnlyList<int> Ratings,
    IReadOnlyList<string> Spells);

public sealed record RaceDefinition(
    Race Race,
    bool IsPc,
    int ActMask,
    int AffectMask,
    int OffensiveMask,
    int ImmunityMask,
    int ResistanceMask,
    int VulnerabilityMask,
    int FormMask,
    int PartsMask)
{
    public string Name => Race.ToString();

    public IReadOnlyList<string> ActFlags => RomFlags.Act.GetNames(ActMask);
    public IReadOnlyList<string> AffectedBy => RomFlags.Affect.GetNames(AffectMask);
    public IReadOnlyList<string> OffensiveFlags => RomFlags.Offensive.GetNames(OffensiveMask);
    public IReadOnlyList<string> Immunities => RomFlags.Immunity.GetNames(ImmunityMask);
    public IReadOnlyList<string> Resistances => RomFlags.Immunity.GetNames(ResistanceMask);
    public IReadOnlyList<string> Vulnerabilities => RomFlags.Immunity.GetNames(VulnerabilityMask);
    public IReadOnlyList<string> Form => RomFlags.Form.GetNames(FormMask);
    public IReadOnlyList<string> Parts => RomFlags.Part.GetNames(PartsMask);
}

public sealed record ClassDefinition(
    PlayerClass Class,
    string WhoName,
    string PrimeAttribute,
    int WeaponVnum,
    IReadOnlyList<int> GuildVnums,
    int SkillAdept,
    int Thac0Level0,
    int Thac0Level32,
    int HpMin,
    int HpMax,
    bool GainsMana,
    string BaseGroup,
    string DefaultGroup);

public sealed record PcRaceDefinition(
    Race Race,
    string ShortName,
    int Points,
    IReadOnlyList<int> ClassMultipliers,
    IReadOnlyList<string> BonusSkills,
    IReadOnlyList<int> BaseStats,
    IReadOnlyList<int> MaxStats,
    Size Size);

public sealed record WeaponDefinition(
    string Name,
    int Vnum,
    WeaponClass Class,
    string SkillName);

public sealed record LiquidDefinition(
    int Value,
    string Name,
    string Color,
    int Proof,
    int Full,
    int Thirst,
    int Food,
    int ServingSize);

public sealed record AttackTypeDefinition(
    int Value,
    string Name,
    string Noun,
    string DamageType);

public sealed record WiznetDefinition(
    string Name,
    int Bit,
    int MinLevel);

public sealed record SpecialDefinition(
    int Value,
    string Name,
    string LegacyName);
