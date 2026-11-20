using VibeROM.Common;

namespace VibeROM.Common.Tests;

public sealed class GameInfoTests
{
    [Fact]
    public void GameInfo_IdentifiesRomRelease()
    {
        Assert.Equal("VibeROM", GameInfo.Name);
        Assert.Equal("ROM 2.4b6", GameInfo.RomVersion);
        Assert.Equal("1998-05-29", GameInfo.RomReleaseDate);
        Assert.Equal(1, GameInfo.DataFormatVersion);
    }

    [Fact]
    public void Describe_IncludesVersionAndDataFormat()
    {
        Assert.Equal(
            "VibeROM ROM 2.4b6 (1998-05-29), data format v1",
            GameInfo.Describe());
    }
}
