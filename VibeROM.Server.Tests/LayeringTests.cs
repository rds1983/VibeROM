using System.Reflection;

namespace VibeROM.Server.Tests;

public sealed class LayeringTests
{
    [Fact]
    public void Server_DoesNotReference_AreaImporter()
    {
        var references = ReferencedAssemblyNames(typeof(global::VibeROM.Server.Program).Assembly);

        Assert.DoesNotContain("VibeROM.AreaImporter", references);
    }

    [Fact]
    public void Server_References_Common()
    {
        var references = ReferencedAssemblyNames(typeof(global::VibeROM.Server.Program).Assembly);

        Assert.Contains("VibeROM.Common", references);
    }

    private static string[] ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
}
