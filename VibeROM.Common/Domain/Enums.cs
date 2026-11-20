namespace VibeROM.Common.Domain;

public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
    Up = 4,
    Down = 5
}

public enum DoorState
{
    Open = 0,
    Closed = 1,
    Locked = 2
}

public enum WearSlot
{
    None = 0,
    Light,
    FingerLeft,
    FingerRight,
    Neck1,
    Neck2,
    Body,
    Head,
    Legs,
    Feet,
    Hands,
    Arms,
    Shield,
    About,
    Waist,
    WristLeft,
    WristRight,
    Wield,
    Hold,
    Float
}

/// <summary>ROM's TO_* affect locations.</summary>
public enum AffectWhere
{
    Affect = 0,
    Object = 1,
    Immune = 2,
    Resist = 3,
    Vulnerable = 4,
    Weapon = 5
}

public enum ConnectionState
{
    Playing = 0,
    GetName,
    GetOldPassword,
    ConfirmNewName,
    GetNewPassword,
    ConfirmNewPassword,
    GetNewRace,
    GetNewSex,
    GetNewClass,
    GetAlignment,
    DefaultChoice,
    GenerateGroups,
    PickWeapon,
    ReadImotd,
    ReadMotd,
    BreakConnect
}

public enum SkyState
{
    Cloudless = 0,
    Cloudy = 1,
    Raining = 2,
    Lightning = 3
}

public enum SunState
{
    Dark = 0,
    Rise = 1,
    Light = 2,
    Set = 3
}
