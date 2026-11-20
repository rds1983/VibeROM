using System.Text.Json.Nodes;
using VibeROM.Common.Serialization;

namespace VibeROM.AreaImporter.Tests;

public sealed class ImportRunnerTests
{
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly string SchemaDirectory = Path.Combine(AppContext.BaseDirectory, "schema");

    [Fact]
    public void ImportsFixturesWritesIndexAndIsByteIdenticalOnRerun()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        var reportPath = Path.Combine(temp.Path, "report.json");
        WriteAreaList(source, "modern.are", "socials.are", "oldformat.are");
        CopyFixture(source, "modern.are");
        CopyFixture(source, "socials.are");
        CopyFixture(source, "oldformat.are");

        var first = AreaImportRunner.Run(new ImportRequest(source, output, null, reportPath, false, false));

        Assert.Equal(0, first.ExitCode);
        Assert.Empty(first.Report.Errors);
        Assert.Equal(3, first.Report.Areas.Count);
        Assert.True(File.Exists(Path.Combine(output, "index.json")));
        Assert.True(File.Exists(Path.Combine(output, "modern.json")));
        Assert.True(File.Exists(Path.Combine(output, "socials.json")));
        Assert.True(File.Exists(Path.Combine(output, "oldformat.json")));
        Assert.True(File.Exists(reportPath));

        var index = JsonNode.Parse(File.ReadAllText(Path.Combine(output, "index.json")))!.AsObject();
        Assert.Equal(1, index["formatVersion"]!.GetValue<int>());
        Assert.Equal("ROM", index["source"]!["project"]!.GetValue<string>());
        Assert.Equal("2.4b6", index["source"]!["version"]!.GetValue<string>());
        Assert.Equal("1998-05-29", index["source"]!["releaseDate"]!.GetValue<string>());
        Assert.Equal(
            ["modern.json", "socials.json", "oldformat.json"],
            index["areas"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());

        AssertSchemaValid("area.schema.json", Path.Combine(output, "modern.json"));
        AssertSchemaValid("area.schema.json", Path.Combine(output, "socials.json"));
        AssertSchemaValid("area.schema.json", Path.Combine(output, "oldformat.json"));
        AssertSchemaValid("index.schema.json", Path.Combine(output, "index.json"));

        var report = JsonNode.Parse(File.ReadAllText(reportPath))!.AsObject();
        Assert.Equal("ROM", report["source"]!.GetValue<string>());
        Assert.Equal(3, report["areas"]!.AsArray().Count);
        Assert.Empty(report["errors"]!.AsArray());

        var snapshot = Snapshot(output);
        var second = AreaImportRunner.Run(new ImportRequest(source, output, null, reportPath, false, false));

        Assert.Equal(0, second.ExitCode);
        AssertSnapshotEqual(snapshot, Snapshot(output));
    }

    [Fact]
    public void RepairsExitKeysThatPointAtRooms()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        CopyFixture(source, "modern.are");
        var area = File.ReadAllText(Path.Combine(source, "modern.are"))
            .Replace("0 -1 1001", "0 7777 1001", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(source, "modern.are"), area);
        WriteAreaList(source, "modern.are");

        var result = AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, false));

        Assert.Equal(0, result.ExitCode);
        var repair = Assert.Single(result.Report.Repairs);
        Assert.Contains("7777", repair.Message, StringComparison.Ordinal);
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(output, "modern.json")))!.AsObject();
        var exit = json["rooms"]!.AsArray()[0]!["exits"]!["north"]!.AsObject();
        Assert.False(exit.ContainsKey("keyVnum"));

        Assert.Equal(
            0,
            AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, true)).ExitCode);
    }

    [Fact]
    public void CheckPassesThenDetectsDriftWithoutWriting()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        WriteAreaList(source, "modern.are");
        CopyFixture(source, "modern.are");

        Assert.Equal(
            0,
            AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, false)).ExitCode);

        var checkedResult = AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, true));
        Assert.Equal(0, checkedResult.ExitCode);
        Assert.Empty(checkedResult.Report.Errors);
        Assert.Contains(checkedResult.Messages, message => message.Contains("Check passed", StringComparison.Ordinal));

        var areaPath = Path.Combine(output, "modern.json");
        File.AppendAllText(areaPath, " ");
        var before = File.ReadAllBytes(areaPath);

        var drift = AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, true));

        Assert.Equal(1, drift.ExitCode);
        Assert.Contains(drift.Report.Errors, error => error.Message.Contains("differs", StringComparison.Ordinal));
        Assert.Equal(before, File.ReadAllBytes(areaPath));
    }

    [Fact]
    public void StrictPromotesWarningsAndLeavesExistingOutputUntouched()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        CopyFixture(source, "modern.are");
        var warningArea = File.ReadAllText(Path.Combine(source, "modern.are"))
            .Replace("ABV D 500 0", "ZBV D 500 0", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(source, "warning.are"), warningArea);
        WriteAreaList(source, "warning.are");
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "keep me");

        var nonStrict = AreaImportRunner.Run(new ImportRequest(source, temp.CreateDirectory("out-nonstrict"), null, null, false, false));
        Assert.Equal(0, nonStrict.ExitCode);
        Assert.Single(nonStrict.Report.Warnings);

        var strict = AreaImportRunner.Run(new ImportRequest(source, output, null, null, true, false));

        Assert.Equal(1, strict.ExitCode);
        Assert.NotEmpty(strict.Report.Errors);
        Assert.True(File.Exists(Path.Combine(output, "sentinel.txt")));
        Assert.False(File.Exists(Path.Combine(output, "warning.json")));
    }

    [Fact]
    public void MissingListedAreaFailsAndLeavesExistingOutputUntouched()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        WriteAreaList(source, "missing.are");
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "keep me");

        var result = AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, false));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Report.Errors, error => error.Message.Contains("does not exist", StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(output, "sentinel.txt")));
        Assert.False(File.Exists(Path.Combine(output, "missing.json")));
    }

    [Fact]
    public void SingleFileBypassesAreaList()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = temp.CreateDirectory("output");
        CopyFixture(source, "socials.are");

        var result = AreaImportRunner.Run(new ImportRequest(source, output, "socials.are", null, false, false));

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(output, "socials.json")));
        var index = JsonNode.Parse(File.ReadAllText(Path.Combine(output, "index.json")))!.AsObject();
        Assert.Equal(["socials.json"], index["areas"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());
    }

    [Fact]
    public void AreaNamesWithPathsAreRejected()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateDirectory("source");
        var output = Path.Combine(temp.Path, "output");
        WriteAreaList(source, "../modern.are");

        var result = AreaImportRunner.Run(new ImportRequest(source, output, null, null, false, false));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Report.Errors, error => error.Message.Contains("without a path", StringComparison.Ordinal));
        Assert.False(Directory.Exists(output));
    }

    private static void AssertSchemaValid(string schemaFile, string jsonFile)
    {
        var schema = JsonSchemaValidator.Load(Path.Combine(SchemaDirectory, schemaFile));
        var errors = JsonSchemaValidator.Validate(schema, File.ReadAllText(jsonFile));
        Assert.Empty(errors);
    }

    private static void CopyFixture(string destinationDirectory, string name) =>
        File.Copy(
            Path.Combine(FixtureDirectory, name),
            Path.Combine(destinationDirectory, name),
            overwrite: true);

    private static void WriteAreaList(string directory, params string[] names) =>
        File.WriteAllText(
            Path.Combine(directory, "area.lst"),
            string.Join("\n", names) + "\n$\n");

    private static Dictionary<string, byte[]> Snapshot(string directory) =>
        Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .ToDictionary(
                path => Path.GetFileName(path)!,
                File.ReadAllBytes,
                StringComparer.Ordinal);

    private static void AssertSnapshotEqual(
        IReadOnlyDictionary<string, byte[]> expected,
        IReadOnlyDictionary<string, byte[]> actual)
    {
        Assert.Equal(expected.Keys.Order().ToArray(), actual.Keys.Order().ToArray());
        foreach (var (name, bytes) in expected)
        {
            Assert.True(bytes.AsSpan().SequenceEqual(actual[name]), $"{name} changed.");
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "viberom-import-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup on Windows if a handle is still open.
            }
        }
    }
}
