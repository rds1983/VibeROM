using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

public sealed record Alias(string Name, string Replacement);

/// <summary>Data that only player characters have.</summary>
public sealed class PlayerData
{
    public string PasswordHash { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Clan { get; set; } = "none";

    public int Rent { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Bamfin { get; set; } = string.Empty;

    public string Bamfout { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public int Security { get; set; }

    public Sex TrueSex { get; set; } = Sex.Neutral;

    public int LastLevel { get; set; }

    public int Points { get; set; }

    public int LastNote { get; set; }

    public int LastIdea { get; set; }

    public int LastPenalty { get; set; }

    public int LastNews { get; set; }

    public int LastChanges { get; set; }

    public bool ConfirmDelete { get; set; }

    public bool ColourEnabled { get; set; } = true;

    public int Drunk { get; set; }

    public int Full { get; set; }

    public int Thirst { get; set; }

    public int Hunger { get; set; }

    public Dictionary<string, int> LearnedSkills { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> KnownGroups { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<Alias> Aliases { get; } = [];

    public DateTimeOffset? LastLogoff { get; set; }

    public DateTimeOffset? LastSaved { get; set; }

    public int GetSkill(string name) =>
        LearnedSkills.TryGetValue(name, out var value) ? value : 0;

    public void SetSkill(string name, int proficiency)
    {
        LearnedSkills[name] = Math.Clamp(proficiency, 0, 100);
    }
}
