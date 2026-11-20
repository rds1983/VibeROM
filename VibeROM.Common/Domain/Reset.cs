namespace VibeROM.Common.Domain;

public abstract record Reset(bool IfFlag)
{
    public abstract IReadOnlyList<string> Validate(string field);
}

public sealed record MobileReset(bool IfFlag, int MobileVnum, int RoomVnum, int MaxInWorld)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (MobileVnum < 0) errors.Add($"{field}.mobileVnum must not be negative.");
        if (RoomVnum < 0) errors.Add($"{field}.roomVnum must not be negative.");
        if (MaxInWorld < 0) errors.Add($"{field}.maxInWorld must not be negative.");
        return errors;
    }
}

public sealed record ObjectReset(bool IfFlag, int ObjectVnum, int RoomVnum)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (ObjectVnum < 0) errors.Add($"{field}.objectVnum must not be negative.");
        if (RoomVnum < 0) errors.Add($"{field}.roomVnum must not be negative.");
        return errors;
    }
}

public sealed record PutReset(bool IfFlag, int ObjectVnum, int ContainerVnum)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (ObjectVnum < 0) errors.Add($"{field}.objectVnum must not be negative.");
        if (ContainerVnum < 0) errors.Add($"{field}.containerVnum must not be negative.");
        return errors;
    }
}

public sealed record GiveReset(bool IfFlag, int ObjectVnum, int MobileVnum)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (ObjectVnum < 0) errors.Add($"{field}.objectVnum must not be negative.");
        if (MobileVnum < 0) errors.Add($"{field}.mobileVnum must not be negative.");
        return errors;
    }
}

public sealed record EquipReset(bool IfFlag, int ObjectVnum, int MobileVnum, WearSlot WearLocation)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (ObjectVnum < 0) errors.Add($"{field}.objectVnum must not be negative.");
        if (MobileVnum < 0) errors.Add($"{field}.mobileVnum must not be negative.");
        if (WearLocation == WearSlot.None) errors.Add($"{field}.wearLocation must not be None.");
        return errors;
    }
}

public sealed record DoorReset(bool IfFlag, int RoomVnum, Direction Direction, DoorState State)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (RoomVnum < 0) errors.Add($"{field}.roomVnum must not be negative.");
        if (!Enum.IsDefined(Direction)) errors.Add($"{field}.direction is invalid.");
        if (!Enum.IsDefined(State)) errors.Add($"{field}.state is invalid.");
        return errors;
    }
}

public sealed record RandomizeReset(bool IfFlag, int RoomVnum, int Door)
    : Reset(IfFlag)
{
    public override IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (RoomVnum < 0) errors.Add($"{field}.roomVnum must not be negative.");
        if (Door is < 0 or > 6) errors.Add($"{field}.door must be between 0 and 6.");
        return errors;
    }
}
