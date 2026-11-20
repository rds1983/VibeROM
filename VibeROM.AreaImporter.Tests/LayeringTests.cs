using System.Reflection;

namespace VibeROM.AreaImporter.Tests;

public sealed class LayeringTests
{
    [Fact]
    public void Importer_DoesNotReference_Server()
    {
        var references = ReferencedAssemblyNames(typeof(global::VibeROM.AreaImporter.Program).Assembly);

        Assert.DoesNotContain("VibeROM.Server", references);
    }

    private static string[] ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
}
