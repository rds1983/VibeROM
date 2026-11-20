using VibeROM.Common.Text;

namespace VibeROM.Common.Tests.Text;

public sealed class ActFormatterTests
{
    private static readonly TestActor Actor = new("Alander", "the mage", false, ActorSex.Male);
    private static readonly TestActor Victim = new("Guard", "the guard", true, ActorSex.Female);
    private static readonly TestActor NeutralVictim = new("Blob", "the blob", true, ActorSex.Neutral);
    private static readonly TestObject Sword = new("a long sword");
    private static readonly TestObject Shield = new("a wooden shield");

    private static ActMessageContext Context => new(
        Actor,
        Victim,
        Sword,
        Shield,
        "hello there",
        "goodbye",
        "north gate");

    [Theory]
    [InlineData("$n", "You")]
    [InlineData("$N", "The guard")]
    [InlineData("$e", "He")]
    [InlineData("$E", "She")]
    [InlineData("$m", "Him")]
    [InlineData("$M", "Her")]
    [InlineData("$s", "His")]
    [InlineData("$S", "Her")]
    [InlineData("$p", "A long sword")]
    [InlineData("$P", "A wooden shield")]
    [InlineData("$t", "Hello there")]
    [InlineData("$T", "Goodbye")]
    [InlineData("$d", "North")]
    [InlineData("$n gives $p to $N.", "You gives a long sword to the guard.")]
    [InlineData("$n says '$t'.", "You says 'hello there'.")]
    [InlineData("$N says '$T'.", "The guard says 'goodbye'.")]
    [InlineData("The $d is closed.", "The north is closed.")]
    [InlineData("$n hugs $M.", "You hugs her.")]
    [InlineData("$n hugs $m.", "You hugs him.")]
    public void Format_ExpandsKnownTokens(string format, string expected)
    {
        Assert.Equal(expected, ActFormatter.Format(format, Context, Actor));
    }

    [Fact]
    public void Format_SelfUsesYou()
    {
        Assert.Equal("You", ActFormatter.Format("$n", Context, Actor));
        Assert.Equal("You", ActFormatter.Format("$N", new ActMessageContext(Actor, Actor), Actor));
    }

    [Fact]
    public void Format_NpcActorUsesShortDescription()
    {
        var context = new ActMessageContext(Victim);

        Assert.Equal("The guard", ActFormatter.Format("$n", context, Actor));
    }

    [Fact]
    public void Format_NeutralPronouns()
    {
        var context = new ActMessageContext(Actor, NeutralVictim);

        Assert.Equal("It", ActFormatter.Format("$E", context, Actor));
        Assert.Equal("It", ActFormatter.Format("$M", context, Actor));
        Assert.Equal("Its", ActFormatter.Format("$S", context, Actor));
    }

    [Fact]
    public void Format_HiddenActor_BecomesSomeone()
    {
        var visibility = new ActVisibility((_, _) => false, (_, _) => true);

        Assert.Equal("Someone", ActFormatter.Format("$n", Context, Victim, visibility));
    }

    [Fact]
    public void Format_HiddenVictim_BecomesSomeone()
    {
        var visibility = new ActVisibility((_, target) => !ReferenceEquals(target, Victim), (_, _) => true);

        Assert.Equal("Someone", ActFormatter.Format("$N", Context, Actor, visibility));
    }

    [Fact]
    public void Format_HiddenObject_BecomesSomething()
    {
        var visibility = new ActVisibility((_, _) => true, (_, obj) => !ReferenceEquals(obj, Sword));

        Assert.Equal("Something", ActFormatter.Format("$p", Context, Actor, visibility));
        Assert.Equal("A wooden shield", ActFormatter.Format("$P", Context, Actor, visibility));
    }

    [Fact]
    public void Format_MissingArguments_UsePlaceholder()
    {
        var context = new ActMessageContext(Actor);

        Assert.Equal(ActFormatter.Placeholder, ActFormatter.Format("$N", context, Actor));
        Assert.Equal(ActFormatter.Placeholder, ActFormatter.Format("$E", context, Actor));
        Assert.Equal(ActFormatter.Placeholder, ActFormatter.Format("$p", context, Actor));
        Assert.Equal(ActFormatter.Placeholder, ActFormatter.Format("$t", context, Actor));
    }

    [Theory]
    [InlineData("$z", ActFormatter.Placeholder)]
    [InlineData("$", ActFormatter.Placeholder)]
    [InlineData("hello $", "Hello " + ActFormatter.Placeholder)]
    public void Format_UnknownOrTruncatedCode_UsesPlaceholder(string format, string expected)
    {
        Assert.Equal(expected, ActFormatter.Format(format, Context, Actor));
    }

    [Fact]
    public void Format_DoubleDollar_IsLiteralDollar()
    {
        Assert.Equal("$", ActFormatter.Format("$$", Context, Actor));
    }

    [Fact]
    public void Format_EmptyFormat_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ActFormatter.Format(string.Empty, Context, Actor));
    }

    [Fact]
    public void Format_EmptyDoor_FallsBackToDoor()
    {
        var context = new ActMessageContext(Actor, Door: null);

        Assert.Equal("Door", ActFormatter.Format("$d", context, Actor));
    }

    [Fact]
    public void Format_UppercasesFirstCharacterOnly()
    {
        Assert.Equal("Hello alander", ActFormatter.Format("hello alander", Context, Actor));
    }

    [Fact]
    public void FormatLine_AppendsTelnetLineEnding()
    {
        Assert.Equal("You\r\n", ActFormatter.FormatLine("$n", Context, Actor));
    }

    private sealed record TestActor(
        string Name,
        string ShortDescription,
        bool IsNpc,
        ActorSex Sex) : IActActor;

    private sealed record TestObject(string ShortDescription) : IActObject;
}
