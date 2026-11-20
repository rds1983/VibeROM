namespace VibeROM.AreaImporter;

public static class Usage
{
    public const string Text =
        """
        VibeROM.AreaImporter - migrates ROM 2.4b6 .are files to JSON.

        Usage:
          VibeROM.AreaImporter --source <dir> --output <dir> [options]

        Options:
          --source <dir>    Directory containing area.lst and the .are files.
          --output <dir>    Destination for area JSON and index.json.
          --file <name>     Import a single area, bypassing area.lst.
          --report <path>   Write a JSON import report.
          --check           Re-import to a temporary directory and compare.
          --strict          Treat every warning as an error and write no output.
          -h, --help        Show this help.
        """;
}
