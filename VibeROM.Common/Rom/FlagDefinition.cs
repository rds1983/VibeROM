namespace VibeROM.Common.Rom;

/// <summary>One named bit in a ROM flag vector.</summary>
/// <param name="Bit">The bit mask, not the bit index.</param>
/// <param name="Name">Canonical JSON name.</param>
/// <param name="Persisted">
/// False for engine-derived flags (for example <c>ACT_IS_NPC</c>) that are not
/// written to JSON.
/// </param>
public sealed record FlagDefinition(int Bit, string Name, bool Persisted = true);
