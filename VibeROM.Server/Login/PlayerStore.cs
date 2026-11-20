using System.Text.Json;
using Json.Schema;
using VibeROM.Common.Domain;
using VibeROM.Common.Serialization;

namespace VibeROM.Server.Login;

/// <summary>Reads and writes one JSON save per player under Data/players.</summary>
public sealed class PlayerStore
{
    private readonly string _directory;
    private readonly JsonSchema _schema;

    public PlayerStore(string directory, string schemaDirectory)
    {
        _directory = Path.GetFullPath(directory);
        _schema = JsonSchemaValidator.Load(Path.Combine(schemaDirectory, "player.schema.json"));
    }

    public bool Exists(string name) => FindPath(name) is not null;

    public string? GetPasswordHash(string name)
    {
        var path = FindPath(name);
        if (path is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("passwordHash", out var hash)
               && hash.ValueKind == JsonValueKind.String
            ? hash.GetString()
            : null;
    }

    public Character Load(string name, World world)
    {
        var path = FindPath(name)
            ?? throw new FileNotFoundException($"Player save '{name}' was not found.", name);
        return PlayerJson.Load(path, world, _schema);
    }

    public void Save(Character character)
    {
        if (character.Player is null)
        {
            throw new ArgumentException("Only player characters can be saved.", nameof(character));
        }

        var path = Path.Combine(_directory, $"{character.Name}.json");
        PlayerJson.Save(path, character, _schema);
    }

    public IReadOnlyList<string> ListNames()
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string? FindPath(string name)
    {
        if (!Directory.Exists(_directory))
        {
            return null;
        }

        return Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => string.Equals(
                Path.GetFileNameWithoutExtension(path),
                name,
                StringComparison.OrdinalIgnoreCase));
    }
}
