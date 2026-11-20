using VibeROM.Common.Security;

namespace VibeROM.Common.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashVerifiesAndRejectsWrongPassword()
    {
        var hash = PasswordHasher.Hash("correct horse");

        Assert.True(PasswordHasher.IsHash(hash));
        Assert.True(PasswordHasher.Verify("correct horse", hash));
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void HashesAreSalted()
    {
        var first = PasswordHasher.Hash("same");
        var second = PasswordHasher.Hash("same");

        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("same", first));
        Assert.True(PasswordHasher.Verify("same", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plaintext")]
    [InlineData("pbkdf2-sha256$bad$salt$hash")]
    public void InvalidHashesAreRejected(string hash)
    {
        Assert.False(PasswordHasher.IsHash(hash));
        Assert.False(PasswordHasher.Verify("password", hash));
    }
}
