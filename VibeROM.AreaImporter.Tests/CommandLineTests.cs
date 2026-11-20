namespace VibeROM.AreaImporter.Tests;

public sealed class CommandLineTests
{
    [Fact]
    public void ParsesAllOptions()
    {
        var parsed = CommandLine.Parse(
            ["--source", "src", "--output", "out", "--file", "area.are", "--report", "r.json", "--strict", "--check"]);

        Assert.Empty(parsed.Errors);
        Assert.False(parsed.HelpRequested);
        Assert.NotNull(parsed.Request);
        Assert.Equal("src", parsed.Request.SourceDirectory);
        Assert.Equal("out", parsed.Request.OutputDirectory);
        Assert.Equal("area.are", parsed.Request.SingleFile);
        Assert.Equal("r.json", parsed.Request.ReportPath);
        Assert.True(parsed.Request.Strict);
        Assert.True(parsed.Request.Check);
    }

    [Fact]
    public void SupportsEqualsSyntax()
    {
        var parsed = CommandLine.Parse(["--source=src", "--output=out"]);

        Assert.Empty(parsed.Errors);
        Assert.Equal("src", parsed.Request!.SourceDirectory);
        Assert.Equal("out", parsed.Request.OutputDirectory);
    }

    [Fact]
    public void HelpWinsOverOtherArguments()
    {
        var parsed = CommandLine.Parse(["--bogus", "--help"]);

        Assert.True(parsed.HelpRequested);
        Assert.Contains(parsed.Errors, error => error.Contains("bogus", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequiredOptionsAreErrors()
    {
        var parsed = CommandLine.Parse(["--source", "src"]);

        Assert.Null(parsed.Request);
        Assert.Contains(parsed.Errors, error => error.Contains("--output", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingOptionValueIsAnError()
    {
        var parsed = CommandLine.Parse(["--source", "--output", "out"]);

        Assert.Null(parsed.Request);
        Assert.Contains(parsed.Errors, error => error.Contains("--source", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownOptionIsAnError()
    {
        var parsed = CommandLine.Parse(["--source", "src", "--output", "out", "--bogus"]);

        Assert.Null(parsed.Request);
        Assert.Contains(parsed.Errors, error => error.Contains("--bogus", StringComparison.Ordinal));
    }
}
