namespace VibeROM.Common.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), "viberom-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string GetPath(params string[] parts) => Path.Combine([Root, .. parts]);

    public void Dispose()
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; a failed test should not fail again here.
        }
        catch (UnauthorizedAccessException)
        {
            // Same.
        }
    }
}

internal static class TestEnvironment
{
    public static IReadOnlyDictionary<string, string?> Empty { get; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, string?> Of(params (string Key, string? Value)[] values)
    {
        var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            environment[key] = value;
        }

        return environment;
    }
}
