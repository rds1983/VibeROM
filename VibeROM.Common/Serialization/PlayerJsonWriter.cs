using System.Text.Json;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;

namespace VibeROM.Common.Serialization;

/// <summary>Writes a player character as canonical player-save JSON.</summary>
internal static class PlayerJsonWriter
{
    public static void Write(Utf8JsonWriter writer, Character character)
    {
        var player = character.Player
            ?? throw new DataFormatException("/", "Only player characters can be saved.");
        var errors = character.Validate();
        if (errors.Count > 0)
        {
            throw new DataFormatException(errors);
        }

        writer.WriteStartObject();
        writer.WriteNumber("formatVersion", GameInfo.DataFormatVersion);
        writer.WriteString("name", character.Name);
        writer.WriteString("passwordHash", player.PasswordHash);
        writer.WriteString("race", RomTables.PcRaces.Get((int)character.Race).Name);
        writer.WriteString("class", (character.Class ?? PlayerClass.Warrior).ToString());
        writer.WriteString("sex", character.Sex.ToString());
        if (player.TrueSex != character.Sex)
        {
            writer.WriteString("trueSex", player.TrueSex.ToString());
        }

        if (!string.IsNullOrEmpty(player.Clan))
        {
            writer.WriteString("clan", player.Clan);
        }

        writer.WriteNumber("level", character.Level);
        if (character.Trust != 0)
        {
            writer.WriteNumber("trust", character.Trust);
        }

        if (player.Security != 0)
        {
            writer.WriteNumber("security", player.Security);
        }

        if (!player.ColourEnabled)
        {
            writer.WriteBoolean("colourEnabled", false);
        }

        if (player.Rent != 0)
        {
            writer.WriteNumber("rent", player.Rent);
        }

        writer.WriteNumber("roomVnum", character.InRoom?.Vnum ?? 0);
        WriteVitals(writer, character);
        WriteAttributes(writer, character);
        WriteMoney(writer, character);

        if (character.Experience != 0) writer.WriteNumber("experience", character.Experience);
        if (character.Practice != 0) writer.WriteNumber("practice", character.Practice);
        if (character.Train != 0) writer.WriteNumber("train", character.Train);
        if (character.SavingThrow != 0) writer.WriteNumber("savingThrow", character.SavingThrow);
        if (character.Alignment != 0) writer.WriteNumber("alignment", character.Alignment);
        if (character.HitRoll != 0) writer.WriteNumber("hitRoll", character.HitRoll);
        if (character.DamageRoll != 0) writer.WriteNumber("damageRoll", character.DamageRoll);
        if (character.Armor != new ArmorValues(0, 0, 0, 0))
        {
            writer.WriteStartObject("armor");
            writer.WriteNumber("pierce", character.Armor.Pierce);
            writer.WriteNumber("bash", character.Armor.Bash);
            writer.WriteNumber("slash", character.Armor.Slash);
            writer.WriteNumber("exotic", character.Armor.Exotic);
            writer.WriteEndObject();
        }

        if (character.Wimpy != 0) writer.WriteNumber("wimpy", character.Wimpy);
        writer.WriteString("position", character.Position.ToString());
        WriteConditions(writer, player);
        if (character.InvisLevel != 0) writer.WriteNumber("invisLevel", character.InvisLevel);
        if (character.IncogLevel != 0) writer.WriteNumber("incogLevel", character.IncogLevel);
        if (player.LastLevel != 0) writer.WriteNumber("lastLevel", player.LastLevel);
        if (player.Points != 0) writer.WriteNumber("points", player.Points);
        if (character.Played != 0) writer.WriteNumber("played", character.Played);
        if (character.Lines != 24) writer.WriteNumber("lines", character.Lines);
        WriteStringIfNotEmpty(writer, "prompt", player.Prompt);
        WriteStringIfNotEmpty(writer, "title", player.Title);
        WriteStringIfNotEmpty(writer, "bamfin", player.Bamfin);
        WriteStringIfNotEmpty(writer, "bamfout", player.Bamfout);
        WriteFlags(writer, "playerFlags", RomFlags.PlayerAct, character.PlayerFlags);
        WriteFlags(writer, "communicationFlags", RomFlags.Communication, character.CommunicationFlags);
        WriteFlags(writer, "wiznetFlags", RomFlags.Wiznet, character.WiznetFlags);
        WriteFlags(writer, "affectedBy", RomFlags.Affect, character.AffectedBy);

        WriteAliases(writer, player.Aliases);
        WriteSkills(writer, player.LearnedSkills);
        WriteGroups(writer, player.KnownGroups);
        WriteAffects(writer, character.Affects);
        WriteInventory(writer, character);
        WriteEquipment(writer, character);
        WritePet(writer, character.Pet);

        if (player.LastLogoff is { } lastLogoff) writer.WriteString("lastLogoff", FormatTimestamp(lastLogoff));
        if (player.LastSaved is { } lastSaved) writer.WriteString("lastSaved", FormatTimestamp(lastSaved));
        if (player.LastNote != 0) writer.WriteNumber("lastNote", player.LastNote);
        if (player.LastIdea != 0) writer.WriteNumber("lastIdea", player.LastIdea);
        if (player.LastPenalty != 0) writer.WriteNumber("lastPenalty", player.LastPenalty);
        if (player.LastNews != 0) writer.WriteNumber("lastNews", player.LastNews);
        if (player.LastChanges != 0) writer.WriteNumber("lastChanges", player.LastChanges);
        if (player.ConfirmDelete) writer.WriteBoolean("confirmDelete", true);

        writer.WriteEndObject();
    }

    private static void WriteVitals(Utf8JsonWriter writer, Character character)
    {
        writer.WriteStartObject("vitals");
        writer.WriteNumber("hit", character.Hit);
        writer.WriteNumber("mana", character.Mana);
        writer.WriteNumber("move", character.Move);
        writer.WriteNumber("maxHit", character.MaxHit);
        writer.WriteNumber("maxMana", character.MaxMana);
        writer.WriteNumber("maxMove", character.MaxMove);
        writer.WriteEndObject();
    }

    private static void WriteAttributes(Utf8JsonWriter writer, Character character)
    {
        writer.WriteStartObject("attributes");
        writer.WriteNumber("strength", character.GetPermanentStat(0));
        writer.WriteNumber("dexterity", character.GetPermanentStat(1));
        writer.WriteNumber("intelligence", character.GetPermanentStat(2));
        writer.WriteNumber("wisdom", character.GetPermanentStat(3));
        writer.WriteNumber("constitution", character.GetPermanentStat(4));
        writer.WriteEndObject();
    }

    private static void WriteMoney(Utf8JsonWriter writer, Character character)
    {
        writer.WriteStartObject("money");
        writer.WriteNumber("gold", character.Gold);
        writer.WriteNumber("silver", character.Silver);
        writer.WriteEndObject();
    }

    private static void WriteConditions(Utf8JsonWriter writer, PlayerData player)
    {
        writer.WriteStartObject("conditions");
        writer.WriteNumber("drunk", player.Drunk);
        writer.WriteNumber("full", player.Full);
        writer.WriteNumber("thirst", player.Thirst);
        writer.WriteNumber("hunger", player.Hunger);
        writer.WriteEndObject();
    }

    private static void WriteAliases(Utf8JsonWriter writer, IReadOnlyList<Alias> aliases)
    {
        if (aliases.Count == 0) return;
        writer.WriteStartArray("aliases");
        foreach (var alias in aliases)
        {
            writer.WriteStartObject();
            writer.WriteString("name", alias.Name);
            writer.WriteString("replacement", alias.Replacement);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteSkills(Utf8JsonWriter writer, IReadOnlyDictionary<string, int> skills)
    {
        if (skills.Count == 0) return;
        writer.WriteStartArray("skills");
        foreach (var skill in skills.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteStartObject();
            writer.WriteString("name", skill.Key);
            writer.WriteNumber("proficiency", skill.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteGroups(Utf8JsonWriter writer, IReadOnlySet<string> groups)
    {
        if (groups.Count == 0) return;
        writer.WriteStartArray("groups");
        foreach (var group in groups.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteStringValue(group);
        }

        writer.WriteEndArray();
    }

    private static void WriteAffects(Utf8JsonWriter writer, IReadOnlyList<Affect> affects)
    {
        if (affects.Count == 0) return;
        writer.WriteStartArray("affects");
        foreach (var affect in affects)
        {
            writer.WriteStartObject();
            var names = RomFlags.Affect.GetNames(affect.Bits);
            if (names.Count == 1)
            {
                writer.WriteString("type", names[0]);
            }
            else if (names.Count > 1)
            {
                writer.WriteStartArray("flags");
                foreach (var name in names) writer.WriteStringValue(name);
                writer.WriteEndArray();
            }

            if (affect.Type >= 0 && affect.Type < RomTables.Skills.Count)
            {
                writer.WriteString("skill", RomTables.Skills[affect.Type].Name);
            }

            if (affect.Where != AffectWhere.Affect)
            {
                writer.WriteString("where", affect.Where.ToString());
            }

            writer.WriteNumber("level", affect.Level);
            writer.WriteNumber("duration", affect.Duration);
            writer.WriteString("location", affect.Location.ToString());
            writer.WriteNumber("modifier", affect.Modifier);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteInventory(Utf8JsonWriter writer, Character character)
    {
        var equipped = new HashSet<GameObject>(character.Equipment.Values, ReferenceEqualityComparer.Instance);
        var inventory = character.Inventory.Where(obj => !equipped.Contains(obj)).ToArray();
        if (inventory.Length == 0) return;
        writer.WriteStartArray("inventory");
        foreach (var obj in inventory) WriteItem(writer, obj);
        writer.WriteEndArray();
    }

    private static void WriteEquipment(Utf8JsonWriter writer, Character character)
    {
        if (character.Equipment.Count == 0) return;
        writer.WriteStartArray("equipment");
        foreach (var (slot, obj) in character.Equipment.OrderBy(entry => entry.Key))
        {
            writer.WriteStartObject();
            writer.WriteString("wearLocation", slot.ToString());
            writer.WritePropertyName("item");
            WriteItem(writer, obj);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteItem(Utf8JsonWriter writer, GameObject obj)
    {
        writer.WriteStartObject();
        writer.WriteNumber("vnum", obj.Vnum);
        if (obj.Condition != obj.Prototype.Condition) writer.WriteNumber("condition", obj.Condition);
        if (obj.Timer != 0) writer.WriteNumber("timer", obj.Timer);
        if (obj.Contents.Count > 0)
        {
            writer.WriteStartArray("contents");
            foreach (var child in obj.Contents) WriteItem(writer, child);
            writer.WriteEndArray();
        }

        if (obj.Affects.Count > 0)
        {
            writer.WriteStartArray("affects");
            foreach (var affect in obj.Affects) WriteItemAffect(writer, affect);
            writer.WriteEndArray();
        }

        if (obj.ExtraDescriptions.Count > 0)
        {
            writer.WriteStartArray("extraDescriptions");
            foreach (var description in obj.ExtraDescriptions)
            {
                writer.WriteStartObject();
                writer.WriteString("keywords", description.Keywords);
                writer.WriteString("description", description.Description);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteItemAffect(Utf8JsonWriter writer, Affect affect)
    {
        writer.WriteStartObject();
        var names = RomFlags.Affect.GetNames(affect.Bits);
        if (names.Count == 1)
        {
            writer.WriteString("type", names[0]);
        }
        else if (names.Count > 1)
        {
            writer.WriteStartArray("flags");
            foreach (var name in names) writer.WriteStringValue(name);
            writer.WriteEndArray();
        }

        if (affect.Type >= 0 && affect.Type < RomTables.Skills.Count)
        {
            writer.WriteString("skill", RomTables.Skills[affect.Type].Name);
        }

        if (affect.Where != AffectWhere.Object)
        {
            writer.WriteString("where", affect.Where.ToString());
        }

        writer.WriteNumber("level", affect.Level);
        writer.WriteNumber("duration", affect.Duration);
        writer.WriteString("location", affect.Location.ToString());
        writer.WriteNumber("modifier", affect.Modifier);
        writer.WriteEndObject();
    }

    private static void WritePet(Utf8JsonWriter writer, Character? pet)
    {
        if (pet?.Prototype is null) return;
        writer.WriteStartObject("pet");
        writer.WriteNumber("vnum", pet.Prototype.Vnum);
        writer.WriteString("name", pet.Name);
        WriteStringIfNotEmpty(writer, "shortDescription", pet.ShortDescription);
        WriteStringIfNotEmpty(writer, "longDescription", pet.LongDescription);
        WriteStringIfNotEmpty(writer, "description", pet.Description);
        writer.WriteNumber("level", pet.Level);
        writer.WriteString("sex", pet.Sex.ToString());
        writer.WriteString("race", RomTables.Races.Get((int)pet.Race).Name);
        writer.WriteEndObject();
    }

    private static void WriteFlags(Utf8JsonWriter writer, string name, FlagRegistry registry, int mask)
    {
        var names = registry.GetNames(mask);
        if (names.Count == 0) return;
        writer.WriteStartArray(name);
        foreach (var value in names) writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteStringIfNotEmpty(Utf8JsonWriter writer, string name, string value)
    {
        if (!string.IsNullOrEmpty(value)) writer.WriteString(name, value);
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
