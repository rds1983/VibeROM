using VibeROM.AreaImporter;

namespace VibeROM.AreaImporter.Tests;

public sealed class UsageTests
{
    [Fact]
    public void AreaImporterUsage_DocumentsCoreOptions()
    {
        Assert.Contains("--source", Usage.Text, StringComparison.Ordinal);
        Assert.Contains("--output", Usage.Text, StringComparison.Ordinal);
        Assert.Contains("--check", Usage.Text, StringComparison.Ordinal);
        Assert.Contains("--help", Usage.Text, StringComparison.Ordinal);
    }
}
