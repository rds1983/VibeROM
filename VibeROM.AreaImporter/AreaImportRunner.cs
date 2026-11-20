using System.Text;
using System.Text.Json;
using Json.Schema;
using NLog;
using VibeROM.AreaImporter.Legacy;
using VibeROM.Common;
using VibeROM.Common.Serialization;

namespace VibeROM.AreaImporter;

/// <summary>
/// Imports legacy area files into canonical JSON, writes an index, and
/// optionally checks the committed output for drift. Never writes a partial
/// data set.
/// </summary>
public static class AreaImportRunner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static ImportResult Run(ImportRequest request)
    {
        var messages = new List<string>();
        var warnings = new List<ImportMessage>();
        var errors = new List<ImportMessage>();
        var areas = new List<ImportedArea>();
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        var parsed = new List<LegacyAreaParseResult>();

        string sourceDirectory;
        string outputDirectory;
        try
        {
            sourceDirectory = Path.GetFullPath(request.SourceDirectory);
            outputDirectory = Path.GetFullPath(request.OutputDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add(new ImportMessage(request.SourceDirectory, 0, 0, exception.Message));
            return Finish(request, request.SourceDirectory, messages, areas, warnings, errors);
        }

        if (!Directory.Exists(sourceDirectory))
        {
            errors.Add(new ImportMessage(sourceDirectory, 0, 0, "Source directory does not exist."));
        }

        if (File.Exists(outputDirectory))
        {
            errors.Add(new ImportMessage(outputDirectory, 0, 0, "Output path is a file, not a directory."));
        }

        JsonSchema? areaSchema = null;
        JsonSchema? indexSchema = null;
        var schemaDirectory = FindSchemaDirectory();
        if (schemaDirectory is null)
        {
            errors.Add(new ImportMessage(
                sourceDirectory,
                0,
                0,
                "Schema directory not found. Expected Data/schema/area.schema.json."));
        }
        else
        {
            try
            {
                areaSchema = JsonSchemaValidator.Load(Path.Combine(schemaDirectory, "area.schema.json"));
                indexSchema = JsonSchemaValidator.Load(Path.Combine(schemaDirectory, "index.schema.json"));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                errors.Add(new ImportMessage(schemaDirectory, 0, 0, $"Could not load schemas: {exception.Message}"));
            }
        }

        var areaNames = new List<string>();
        if (errors.Count == 0)
        {
            areaNames = LoadAreaNames(request, sourceDirectory, errors);
        }

        if (errors.Count == 0 && areaSchema is not null)
        {
            ImportAreas(
                sourceDirectory,
                areaNames,
                areaSchema,
                parsed,
                areas,
                documents,
                warnings,
                errors);
        }

        if (errors.Count == 0)
        {
            foreach (var warning in LegacyAreaParser.ValidateReferences(parsed))
            {
                var (file, message) = SplitReferenceMessage(warning.Message);
                warnings.Add(new ImportMessage(file, warning.Position.Line, warning.Position.Column, message));
            }
        }

        if (request.Strict)
        {
            errors.AddRange(warnings);
        }

        if (errors.Count == 0 && indexSchema is not null)
        {
            var indexJson = ImportJson.SerializeIndex(areas.Select(area => area.JsonFile).ToArray());
            var indexErrors = JsonSchemaValidator.Validate(indexSchema, indexJson);
            foreach (var error in indexErrors)
            {
                errors.Add(new ImportMessage("index.json", 0, 0, error));
            }

            if (indexErrors.Count == 0)
            {
                documents["index.json"] = indexJson;
            }
        }

        if (errors.Count == 0)
        {
            if (request.Check)
            {
                var drift = CompareOutput(outputDirectory, documents);
                if (drift.Count == 0)
                {
                    messages.Add($"Check passed: {areas.Count} area files match '{outputDirectory}'.");
                    Logger.Info("Check passed for {OutputDirectory}: {AreaCount} areas.", outputDirectory, areas.Count);
                }
                else
                {
                    foreach (var entry in drift)
                    {
                        errors.Add(new ImportMessage(entry.File, 0, 0, entry.Message));
                    }

                    messages.Add($"Drift detected: {drift.Count} file(s) differ from '{outputDirectory}'.");
                    Logger.Warn("Drift detected for {OutputDirectory}: {DriftCount} files.", outputDirectory, drift.Count);
                }
            }
            else
            {
                try
                {
                    WriteOutput(outputDirectory, documents);
                    messages.Add($"Imported {areas.Count} area files to '{outputDirectory}'.");
                    Logger.Info("Imported {AreaCount} areas to {OutputDirectory}.", areas.Count, outputDirectory);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    errors.Add(new ImportMessage(outputDirectory, 0, 0, $"Could not write output: {exception.Message}"));
                    messages.Add("Import failed while writing output; the previous data set was left unchanged.");
                    Logger.Error(exception, "Could not write output to {OutputDirectory}.", outputDirectory);
                }
            }
        }
        else
        {
            messages.Add($"Import failed with {errors.Count} error(s); no output was written.");
        }

        return Finish(request, sourceDirectory, messages, areas, warnings, errors);
    }

    private static void ImportAreas(
        string sourceDirectory,
        IReadOnlyList<string> areaNames,
        JsonSchema areaSchema,
        ICollection<LegacyAreaParseResult> parsed,
        ICollection<ImportedArea> areas,
        IDictionary<string, string> documents,
        ICollection<ImportMessage> warnings,
        ICollection<ImportMessage> errors)
    {
        var parser = new LegacyAreaParser();
        var jsonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in areaNames)
        {
            var path = Path.Combine(sourceDirectory, name);
            if (!File.Exists(path))
            {
                errors.Add(new ImportMessage(name, 0, 0, "Area file was listed but does not exist."));
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(new ImportMessage(name, 0, 0, $"Could not read area file: {exception.Message}"));
                continue;
            }

            try
            {
                var result = parser.Parse(text, name);
                parsed.Add(result);
                foreach (var warning in result.Warnings)
                {
                    warnings.Add(new ImportMessage(name, warning.Position.Line, warning.Position.Column, warning.Message));
                }

                var area = result.Area;
                var areaErrors = area.Validate();
                if (areaErrors.Count > 0)
                {
                    foreach (var error in areaErrors)
                    {
                        errors.Add(new ImportMessage(name, 0, 0, error));
                    }

                    continue;
                }

                var json = AreaJson.Serialize(area);
                var schemaErrors = JsonSchemaValidator.Validate(areaSchema, json);
                if (schemaErrors.Count > 0)
                {
                    foreach (var error in schemaErrors)
                    {
                        errors.Add(new ImportMessage(name, 0, 0, error));
                    }

                    continue;
                }

                var jsonFile = JsonFileName(name);
                if (!jsonNames.Add(jsonFile))
                {
                    errors.Add(new ImportMessage(name, 0, 0, $"Duplicate JSON file name '{jsonFile}'."));
                    continue;
                }

                documents[jsonFile] = json;
                areas.Add(new ImportedArea(
                    name,
                    jsonFile,
                    area.Helps.Count,
                    area.Mobiles.Count,
                    area.Objects.Count,
                    area.Rooms.Count,
                    area.Resets.Count,
                    area.Shops.Count,
                    area.Specials.Count,
                    area.Socials.Count));
            }
            catch (LegacyFormatException exception)
            {
                errors.Add(new ImportMessage(
                    exception.FileName,
                    exception.Line,
                    exception.Column,
                    StripLocation(exception)));
            }
            catch (DataFormatException exception)
            {
                foreach (var error in exception.Errors)
                {
                    errors.Add(new ImportMessage(name, 0, 0, error));
                }
            }
            catch (JsonException exception)
            {
                errors.Add(new ImportMessage(name, 0, 0, exception.Message));
            }
        }
    }

    private static List<string> LoadAreaNames(
        ImportRequest request,
        string sourceDirectory,
        ICollection<ImportMessage> errors)
    {
        var names = new List<string>();
        if (request.SingleFile is not null)
        {
            names.Add(request.SingleFile);
        }
        else
        {
            var listPath = Path.Combine(sourceDirectory, "area.lst");
            if (!File.Exists(listPath))
            {
                errors.Add(new ImportMessage("area.lst", 0, 0, "area.lst was not found in the source directory."));
                return names;
            }

            try
            {
                foreach (var line in File.ReadLines(listPath))
                {
                    var value = line.Trim();
                    if (value.Length == 0 || value.StartsWith('*') || value.StartsWith(';'))
                    {
                        continue;
                    }

                    if (value == "$")
                    {
                        break;
                    }

                    names.Add(value);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(new ImportMessage("area.lst", 0, 0, $"Could not read area.lst: {exception.Message}"));
                return names;
            }
        }

        if (names.Count == 0)
        {
            errors.Add(new ImportMessage("area.lst", 0, 0, "No area files were listed."));
        }

        foreach (var name in names)
        {
            if (Path.GetFileName(name) != name
                || name.Contains('/')
                || name.Contains('\\')
                || name.Contains(Path.DirectorySeparatorChar)
                || name.Contains(Path.AltDirectorySeparatorChar))
            {
                errors.Add(new ImportMessage(name, 0, 0, "Area names must be plain file names without a path."));
                continue;
            }

            var stem = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
            if (!IsValidJsonStem(stem))
            {
                errors.Add(new ImportMessage(
                    name,
                    0,
                    0,
                    "Area names must map to a lowercase JSON file name using letters, digits, '-' or '_'."));
            }
        }

        var duplicates = names
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (var duplicate in duplicates)
        {
            errors.Add(new ImportMessage(duplicate, 0, 0, "Area file is listed more than once."));
        }

        return names;
    }

    private static string JsonFileName(string legacyName)
    {
        var baseName = Path.GetFileNameWithoutExtension(legacyName).ToLowerInvariant();
        return baseName + ".json";
    }

    private static bool IsValidJsonStem(string stem)
    {
        if (stem.Length == 0 || !char.IsAsciiLetterOrDigit(stem[0]))
        {
            return false;
        }

        return stem.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static (string File, string Message) SplitReferenceMessage(string message)
    {
        var separator = message.IndexOf(": ", StringComparison.Ordinal);
        if (separator > 0 && message[..separator].EndsWith(".are", StringComparison.OrdinalIgnoreCase))
        {
            return (message[..separator], message[(separator + 2)..]);
        }

        return ("", message);
    }

    private static string StripLocation(LegacyFormatException exception)
    {
        var prefix = $"{exception.FileName}:{exception.Line}:{exception.Column}: ";
        return exception.Message.StartsWith(prefix, StringComparison.Ordinal)
            ? exception.Message[prefix.Length..]
            : exception.Message;
    }

    private static string? FindSchemaDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                foreach (var candidate in new[]
                         {
                             Path.Combine(directory.FullName, "Data", "schema"),
                             Path.Combine(directory.FullName, "schema")
                         })
                {
                    if (File.Exists(Path.Combine(candidate, "area.schema.json"))
                        && File.Exists(Path.Combine(candidate, "index.schema.json")))
                    {
                        return candidate;
                    }
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static void WriteOutput(string outputDirectory, IReadOnlyDictionary<string, string> documents)
    {
        var parent = Path.GetDirectoryName(outputDirectory);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var temporary = outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(temporary);
            foreach (var (name, content) in documents.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                File.WriteAllText(
                    Path.Combine(temporary, name),
                    content,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            ReplaceDirectory(outputDirectory, temporary);
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    private static void ReplaceDirectory(string outputDirectory, string temporary)
    {
        string? backup = null;
        if (Directory.Exists(outputDirectory))
        {
            backup = outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + ".bak-" + Guid.NewGuid().ToString("N");
            Directory.Move(outputDirectory, backup);
        }

        try
        {
            Directory.Move(temporary, outputDirectory);
        }
        catch
        {
            if (backup is not null && !Directory.Exists(outputDirectory))
            {
                Directory.Move(backup, outputDirectory);
                backup = null;
            }

            throw;
        }
        finally
        {
            if (backup is not null && Directory.Exists(backup))
            {
                try
                {
                    Directory.Delete(backup, recursive: true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Logger.Warn(exception, "Could not remove backup directory {BackupDirectory}.", backup);
                }
            }
        }
    }

    private static IReadOnlyList<(string File, string Message)> CompareOutput(
        string outputDirectory,
        IReadOnlyDictionary<string, string> documents)
    {
        var drift = new List<(string File, string Message)>();
        if (!Directory.Exists(outputDirectory))
        {
            drift.Add(("", $"Output directory '{outputDirectory}' does not exist."));
            return drift;
        }

        foreach (var (name, content) in documents)
        {
            var path = Path.Combine(outputDirectory, name);
            if (!File.Exists(path))
            {
                drift.Add((name, "File is missing."));
                continue;
            }

            var expected = Encoding.UTF8.GetBytes(content);
            var actual = File.ReadAllBytes(path);
            if (!expected.AsSpan().SequenceEqual(actual))
            {
                drift.Add((name, "File differs from the imported data."));
            }
        }

        var expectedNames = new HashSet<string>(documents.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.GetFiles(outputDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(path);
            if (!expectedNames.Contains(name))
            {
                drift.Add((name, "Unexpected JSON file in output directory."));
            }
        }

        return drift;
    }

    private static ImportResult Finish(
        ImportRequest request,
        string sourceDirectory,
        IReadOnlyList<string> messages,
        IReadOnlyList<ImportedArea> areas,
        IReadOnlyList<ImportMessage> warnings,
        IReadOnlyList<ImportMessage> errors)
    {
        var report = new ImportReport(
            GameInfo.RomProject,
            GameInfo.RomVersionNumber,
            GameInfo.RomReleaseDate,
            sourceDirectory,
            areas,
            warnings,
            errors);

        var exitCode = errors.Count == 0 ? 0 : 1;
        if (request.ReportPath is not null)
        {
            try
            {
                WriteReport(request.ReportPath, report);
            }
            catch (Exception exception) when (exception is IOException
                                               or UnauthorizedAccessException
                                               or ArgumentException
                                               or NotSupportedException
                                               or PathTooLongException)
            {
                messages = messages.Concat([$"Could not write report: {exception.Message}"]).ToArray();
                exitCode = 1;
            }
        }

        return new ImportResult(exitCode, report, messages);
    }

    private static void WriteReport(string path, ImportReport report)
    {
        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, ImportJson.SerializeReport(report), new UTF8Encoding(false));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
