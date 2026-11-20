using System.Reflection;
using VibeROM.Common;

namespace VibeROM.Common.Tests;

public sealed class LayeringTests
{
    [Fact]
    public void Common_DoesNotReference_ServerOrImporter()
    {
        var references = ReferencedAssemblyNames(typeof(GameInfo).Assembly);

        Assert.DoesNotContain("VibeROM.Server", references);
        Assert.DoesNotContain("VibeROM.AreaImporter", references);
    }

    private static string[] ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
}
