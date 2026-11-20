using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>Item-type-specific prototype data. The concrete type must match the item type.</summary>
public abstract record ObjectTypeData;

public sealed record PlainObjectData(IReadOnlyList<int>? RawValues = null) : ObjectTypeData;

public sealed record LightData(int Duration) : ObjectTypeData;

public sealed record SpellsData(int Level, IReadOnlyList<string> Names) : ObjectTypeData;

public sealed record ChargesData(int Level, int Total, int Remaining, string Spell) : ObjectTypeData;

public sealed record WeaponData(
    WeaponClass Class,
    int DiceCount,
    int DiceSize,
    string AttackType,
    int SpecialFlags) : ObjectTypeData;

public sealed record ArmorData(ArmorValues Values) : ObjectTypeData;

public sealed record ContainerData(int Capacity, int Flags, int KeyVnum) : ObjectTypeData;

public sealed record DrinkData(int Capacity, int Remaining, string Liquid, bool Poisoned) : ObjectTypeData;

public sealed record FoodData(int Hours, bool Poisoned) : ObjectTypeData;

public sealed record MoneyData(int Gold, int Silver) : ObjectTypeData;

public sealed record FurnitureData(int MaxPeople, int Flags) : ObjectTypeData;

public sealed record PortalData(int ExitFlags, int GateFlags, int ToRoomVnum) : ObjectTypeData;

public sealed record MapData(int RoomVnum) : ObjectTypeData;
