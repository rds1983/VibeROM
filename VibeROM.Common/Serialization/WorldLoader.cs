using Json.Schema;
using VibeROM.Common.Domain;

namespace VibeROM.Common.Serialization;

public sealed record WorldLoadResult(World World, IReadOnlyList<string> Warnings);

/// <summary>
/// Loads Data/areas/index.json and the area files it lists, validates the
/// schemas and cross-references, applies prototype fix-ups and builds a world.
/// </summary>
public static class WorldLoader
{
    public static WorldLoadResult Load(string areasDirectory, string schemaDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(areasDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaDirectory);

        var areasPath = Path.GetFullPath(areasDirectory);
        var schemaPath = Path.GetFullPath(schemaDirectory);
        if (!Directory.Exists(areasPath))
        {
            throw new WorldDataException([$"Areas directory '{areasPath}' does not exist."]);
        }

        if (!Directory.Exists(schemaPath))
        {
            throw new WorldDataException([$"Schema directory '{schemaPath}' does not exist."]);
        }

        var indexSchema = JsonSchemaValidator.Load(Path.Combine(schemaPath, "index.schema.json"));
        var areaSchema = JsonSchemaValidator.Load(Path.Combine(schemaPath, "area.schema.json"));
        var indexFile = Path.Combine(areasPath, "index.json");
        if (!File.Exists(indexFile))
        {
            throw new WorldDataException([$"Area index '{indexFile}' does not exist."]);
        }

        var index = AreaIndexJson.Deserialize(File.ReadAllText(indexFile), indexSchema);
        ValidateNames(index.Areas);

        var areas = new List<AreaData>(index.Areas.Count);
        foreach (var name in index.Areas)
        {
            var path = Path.Combine(areasPath, name);
            if (!File.Exists(path))
            {
                throw new WorldDataException([$"Area file '{name}' listed in index.json does not exist."]);
            }

            try
            {
                areas.Add(AreaJson.Deserialize(File.ReadAllText(path), areaSchema));
            }
            catch (DataFormatException exception)
            {
                throw new DataFormatException(exception.Path ?? "/", $"{name}: {exception.Message}");
            }
        }

        var warnings = new List<string>();
        var normalized = ApplyFixups(areas, warnings);

        World world;
        try
        {
            world = new World(normalized);
        }
        catch (WorldDataException exception)
        {
            throw new WorldDataException(exception.Errors);
        }

        var errors = WorldReferenceValidator.Validate(world).Concat(world.Validate()).ToArray();
        if (errors.Length > 0)
        {
            throw new WorldDataException(errors);
        }

        return new WorldLoadResult(world, warnings);
    }

    private static IReadOnlyList<AreaData> ApplyFixups(
        IReadOnlyList<AreaData> areas,
        ICollection<string> warnings)
    {
        var result = new List<AreaData>(areas.Count);
        foreach (var area in areas)
        {
            var objects = new List<ObjectPrototype>(area.Objects.Count);
            var changed = false;
            foreach (var obj in area.Objects)
            {
                if (string.IsNullOrEmpty(obj.Material))
                {
                    warnings.Add(
                        $"Area '{area.Name}': object {obj.Vnum} had no material; using 'none'.");
                    objects.Add(obj with { Material = "none" });
                    changed = true;
                }
                else
                {
                    objects.Add(obj);
                }
            }

            // ROM's convert_objects() only rewrites old-format object levels.
            // JSON contains final prototype levels; the importer performs that
            // conversion before writing (the 2.4b6 corpus has no old objects).
            result.Add(changed ? area with { Objects = objects } : area);
        }

        return result;
    }

    private static void ValidateNames(IReadOnlyList<string> names)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        foreach (var name in names)
        {
            if (Path.GetFileName(name) != name
                || name.Contains('/')
                || name.Contains('\\')
                || !name.EndsWith(".json", StringComparison.Ordinal))
            {
                errors.Add($"Area index entry '{name}' is not a plain .json file name.");
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add($"Area index entry '{name}' is duplicated.");
            }
        }

        if (errors.Count > 0)
        {
            throw new WorldDataException(errors);
        }
    }
}
