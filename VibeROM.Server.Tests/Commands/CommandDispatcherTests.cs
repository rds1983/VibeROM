using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Server.Commands;
using VibeROM.Server.Communication;

namespace VibeROM.Server.Tests.Commands;

public sealed class CommandDispatcherTests
{
    [Fact]
    public void Say_ReachesAwakeCharactersInTheRoom()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var listener = harness.AddPlayer("Bob");
        var sleeper = harness.AddPlayer("Sleepy", position: Position.Sleeping);
        var elsewhere = harness.AddPlayer("Far", room: harness.World.GetRoom(3002));

        harness.Run(actor, "say hello");

        Assert.Contains("You say 'hello'", harness.Output.Lines[actor]);
        Assert.Contains("Alander says 'hello'", harness.Output.Lines[listener]);
        Assert.False(harness.Output.Lines.ContainsKey(sleeper));
        Assert.False(harness.Output.Lines.ContainsKey(elsewhere));
    }

    [Fact]
    public void Say_RequiresAnArgument()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "say");

        Assert.Contains("Say what?", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Tell_DeliversToVictimAndRecordsReplyTarget()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "tell Bob hello there");

        Assert.Contains("You tell Bob 'hello there'", harness.Output.Lines[actor]);
        Assert.Contains("Alander tells you 'hello there'", harness.Output.Lines[victim]);
        Assert.Same(actor, victim.Reply);

        harness.Run(victim, "reply and hello to you");

        Assert.Contains("Bob tells you 'and hello to you'", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Tell_RespectsQuietDeafAndAfkRecipients()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var victim = harness.AddPlayer("Bob");
        victim.CommunicationFlags |= RomFlags.Communication.Get("Quiet").Bit;

        harness.Run(actor, "tell Bob hello");

        Assert.Contains("He is not receiving tells.", harness.Output.Lines[actor]);

        victim.CommunicationFlags &= ~RomFlags.Communication.Get("Quiet").Bit;
        victim.CommunicationFlags |= RomFlags.Communication.Get("Afk").Bit;
        harness.Run(actor, "tell Bob later");

        Assert.Contains(
            "He is AFK, but your tell will go through when he returns.",
            harness.Output.Lines[actor]);
        Assert.Contains(
            "Alander tells you 'later'",
            victim.TellBuffer);

        harness.Run(victim, "replay");
        Assert.Contains("Alander tells you 'later'", harness.Output.Lines[victim]);
        Assert.Empty(victim.TellBuffer);

        victim.CommunicationFlags |= RomFlags.Communication.Get("Deaf").Bit;
        harness.Run(actor, "tell Bob deaf");
        Assert.Contains("He is not receiving tells.", harness.Output.Lines[actor]);

        actor.CommunicationFlags |= RomFlags.Communication.Get("Deaf").Bit;
        harness.Run(actor, "tell Bob again");
        Assert.Contains("Your message didn't get through.", harness.Output.Lines[actor]);
    }

    [Theory]
    [InlineData("gossip", "You gossip 'hello'", "Alander gossips 'hello'", "NoGossip")]
    [InlineData("quote", "You quote 'hello'", "Alander quotes 'hello'", "NoQuote")]
    [InlineData("question", "You question 'hello'", "Alander questions 'hello'", "NoQuestion")]
    [InlineData("answer", "You answer 'hello'", "Alander answers 'hello'", "NoQuestion")]
    [InlineData("music", "You MUSIC: 'hello'", "Alander MUSIC: 'hello'", "NoMusic")]
    [InlineData("auction", "You auction 'hello'", "Alander auctions 'hello'", "NoAuction")]
    [InlineData("grats", "You grats 'hello'", "Alander grats 'hello'", "NoGrats")]
    public void Channels_DeliverAndHonourTheIgnoreFlag(
        string command,
        string senderLine,
        string receiverLine,
        string ignoreFlag)
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var listener = harness.AddPlayer("Bob");
        var ignoring = harness.AddPlayer("Carol");
        ignoring.CommunicationFlags |= RomFlags.Communication.Get(ignoreFlag).Bit;

        harness.Run(actor, $"{command} hello");

        Assert.Contains(senderLine, harness.Output.Lines[actor]);
        Assert.Contains(receiverLine, harness.Output.Lines[listener]);
        Assert.False(harness.Output.Lines.ContainsKey(ignoring));
    }

    [Fact]
    public void Gossip_TogglesAndBlocksQuietOrNoChannelsSenders()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "gossip");
        Assert.Contains("Gossip channel is now OFF.", harness.Output.Lines[actor]);
        harness.Run(actor, "gossip");
        Assert.Contains("Gossip channel is now ON.", harness.Output.Lines[actor]);

        actor.CommunicationFlags |= RomFlags.Communication.Get("Quiet").Bit;
        harness.Run(actor, "gossip hello");
        Assert.Contains("You must turn off quiet mode first.", harness.Output.Lines[actor]);

        actor.CommunicationFlags &= ~RomFlags.Communication.Get("Quiet").Bit;
        actor.CommunicationFlags |= RomFlags.Communication.Get("NoChannels").Bit;
        harness.Run(actor, "gossip hello");
        Assert.Contains(
            "The gods have revoked your channel priviliges.",
            harness.Output.Lines[actor]);
    }

    [Fact]
    public void ChannelCommandsSupportPrefixes()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var listener = harness.AddPlayer("Bob");

        harness.Run(actor, "gos hello");

        Assert.Contains("You gossip 'hello'", harness.Output.Lines[actor]);
        Assert.Contains("Alander gossips 'hello'", harness.Output.Lines[listener]);
    }

    [Fact]
    public void Social_NoArgumentAndVictimAudiences()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "smi");
        Assert.Contains("You smile happily.", harness.Output.Lines[actor]);
        Assert.Contains("Alander smiles happily.", harness.Output.Lines[victim]);

        harness.Run(actor, "smile Bob");
        Assert.Contains("You smile at Bob.", harness.Output.Lines[actor]);
        Assert.Contains("Alander smiles at you.", harness.Output.Lines[victim]);

        harness.Run(actor, "smile Nobody");
        Assert.Contains("They aren't here.", harness.Output.Lines[actor]);

        harness.Run(actor, "smile Alander");
        Assert.Contains("You smile at yourself.", harness.Output.Lines[actor]);
        Assert.Contains("Alander smiles at himself.", harness.Output.Lines[victim]);
    }

    [Fact]
    public void Social_RespectsNoEmoteAndSleeping()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        actor.CommunicationFlags |= RomFlags.Communication.Get("NoEmote").Bit;

        harness.Run(actor, "smile");
        Assert.Contains("You are anti-social!", harness.Output.Lines[actor]);

        actor.CommunicationFlags &= ~RomFlags.Communication.Get("NoEmote").Bit;
        actor.Position = Position.Sleeping;
        harness.Run(actor, "smile");
        Assert.Contains("In your dreams, or what?", harness.Output.Lines[actor]);

        harness.Run(actor, "snore");
        Assert.Contains("You snore loudly.", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Shout_ReachesEveryoneExceptIgnorersAndTheQuiet()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: 3);
        var listener = harness.AddPlayer("Bob", room: harness.World.GetRoom(3101));
        var ignoring = harness.AddPlayer("Carol");
        ignoring.CommunicationFlags |= RomFlags.Communication.Get("ShoutsOff").Bit;
        var quiet = harness.AddPlayer("Dave");
        quiet.CommunicationFlags |= RomFlags.Communication.Get("Quiet").Bit;

        harness.Run(actor, "shout hello");

        Assert.Contains("You shout 'hello'", harness.Output.Lines[actor]);
        Assert.Contains("Alander shouts 'hello'", harness.Output.Lines[listener]);
        Assert.False(harness.Output.Lines.ContainsKey(ignoring));
        Assert.False(harness.Output.Lines.ContainsKey(quiet));
    }

    [Fact]
    public void Shout_RequiresTrustThree()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "shout hello");

        Assert.Contains("Huh?", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Yell_OnlyReachesTheSameArea()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var sameArea = harness.AddPlayer("Bob", room: harness.World.GetRoom(3002));
        var otherArea = harness.AddPlayer("Carol", room: harness.World.GetRoom(3101));

        harness.Run(actor, "yell hello");

        Assert.Contains("You yell 'hello'", harness.Output.Lines[actor]);
        Assert.Contains("Alander yells 'hello'", harness.Output.Lines[sameArea]);
        Assert.False(harness.Output.Lines.ContainsKey(otherArea));
    }

    [Fact]
    public void Pmote_PersonalizesEachViewerName()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "pmote grins at Bob.");

        Assert.Contains("You grins at Bob.", harness.Output.Lines[actor]);
        Assert.Contains("Alander grins at you.", harness.Output.Lines[victim]);
    }

    [Fact]
    public void Smote_RequiresTheSpeakersNameAndPersonalizesVictims()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: 52);
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "smote waves at Bob.");
        Assert.Contains(
            "You must include your name in an smote.",
            harness.Output.Lines[actor]);

        harness.Run(actor, "smote Alander waves at Bob.");
        Assert.Contains("Alander waves at Bob.", harness.Output.Lines[actor]);
        Assert.Contains("Alander waves at you.", harness.Output.Lines[victim]);
    }

    [Fact]
    public void Pose_UsesTheClassMessages()
    {
        var harness = CreateHarness(random: new ZeroRandom());
        var actor = harness.AddPlayer("Alander", PlayerClass.Warrior);
        var listener = harness.AddPlayer("Bob");

        harness.Run(actor, "pose");

        Assert.Contains("You show your bulging muscles.", harness.Output.Lines[actor]);
        Assert.Contains("Alander shows his bulging muscles.", harness.Output.Lines[listener]);
    }

    [Fact]
    public void ImmortalTalk_ReachesOnlyImmortalsWithoutNoWiz()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: WiznetService.ImmortalTrust);
        var immortal = harness.AddPlayer("Bob", trust: WiznetService.ImmortalTrust);
        var mortal = harness.AddPlayer("Carol");

        harness.Run(actor, "immtalk hello");

        Assert.Contains("You: hello", harness.Output.Lines[actor]);
        Assert.Contains("Alander: hello", harness.Output.Lines[immortal]);
        Assert.False(harness.Output.Lines.ContainsKey(mortal));

        immortal.CommunicationFlags |= RomFlags.Communication.Get("NoWiz").Bit;
        harness.Run(actor, "immtalk again");
        Assert.DoesNotContain("Alander: again", harness.Output.Lines[immortal]);
    }

    [Fact]
    public void Wiznet_TogglesAndShowsStatus()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: WiznetService.ImmortalTrust);

        harness.Run(actor, "wiznet");
        Assert.Contains("Welcome to Wiznet!", harness.Output.Lines[actor]);

        harness.Run(actor, "wiznet status");
        Assert.Contains(
            "Wiznet status:\r\non",
            harness.Output.Lines[actor]);

        harness.Run(actor, "wiznet off");
        Assert.Contains("Signing off of Wiznet.", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Snoop_ForwardsInputToTheSnooper()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: 55);
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "snoop Bob");
        Assert.Contains("Ok.", harness.Output.Lines[actor]);

        harness.Run(victim, "say hello");

        Assert.Contains("% say hello", harness.Output.Lines[actor]);
    }

    [Fact]
    public void Snoop_FailsAgainstEqualTrust()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: 55);
        var victim = harness.AddPlayer("Bob", trust: 55);

        harness.Run(actor, "snoop Bob");

        Assert.Contains("You failed.", harness.Output.Lines[actor]);
    }

    [Fact]
    public void NoChannels_TogglesTheVictimFlag()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", trust: 55);
        var victim = harness.AddPlayer("Bob");

        harness.Run(actor, "nochannels Bob");

        Assert.True(
            (victim.CommunicationFlags & RomFlags.Communication.Get("NoChannels").Bit) != 0);
        Assert.Contains("NOCHANNELS set.", harness.Output.Lines[actor]);

        harness.Run(actor, "nochannels Bob");
        Assert.False(
            (victim.CommunicationFlags & RomFlags.Communication.Get("NoChannels").Bit) != 0);
    }

    [Fact]
    public void Channels_ListsTheStatusTable()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "channels");

        var text = harness.Output.Text(actor);
        Assert.Contains("channel     status", text);
        Assert.Contains("gossip         ON", text);
        Assert.Contains("tells          ON", text);
        Assert.Contains("quiet mode     OFF", text);
    }

    [Fact]
    public void Socials_ListsAvailableSocials()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "socials");

        var text = harness.Output.Text(actor);
        Assert.Contains("smile", text);
        Assert.Contains("snore", text);
    }

    [Fact]
    public void UnknownCommand_ShowsHuh()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander");

        harness.Run(actor, "frobnicate");

        Assert.Contains("Huh?", harness.Output.Lines[actor]);
    }

    [Fact]
    public void PositionCheck_ReportsTheRomMessage()
    {
        var harness = CreateHarness();
        var actor = harness.AddPlayer("Alander", position: Position.Sleeping);

        harness.Run(actor, "say hello");

        Assert.Contains("In your dreams, or what?", harness.Output.Lines[actor]);
    }

    private static Harness CreateHarness(Random? random = null) => new(random);

    private sealed class Harness
    {
        public Harness(Random? random)
        {
            World = CreateWorld();
            Online = new OnlinePlayers();
            Output = new RecordingOutput();
            var wiznet = new WiznetService(Online, Output);
            Dispatcher = new CommandDispatcher(World, Online, Output, wiznet, random);
        }

        public World World { get; }

        public OnlinePlayers Online { get; }

        public RecordingOutput Output { get; }

        public CommandDispatcher Dispatcher { get; }

        public Character AddPlayer(
            string name,
            PlayerClass playerClass = PlayerClass.Warrior,
            int trust = 1,
            Room? room = null,
            Position position = Position.Standing)
        {
            var character = World.CreatePlayer(
                new PlayerData(),
                name,
                Race.Human,
                playerClass,
                Sex.Male);
            character.Trust = trust;
            character.Position = position;
            var descriptor = new Descriptor
            {
                Character = character,
                State = ConnectionState.Playing
            };
            character.Descriptor = descriptor;
            World.MoveCharacterToRoom(character, room ?? World.GetRoom(3001));
            Online.Add(descriptor);
            return character;
        }

        public void Run(Character character, string input) =>
            Dispatcher.Execute(character, input);
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }

    private sealed class RecordingOutput : IPlayerOutput
    {
        public Dictionary<Character, List<string>> Lines { get; } = [];

        public void Send(Character character, string text)
        {
            if (!Lines.TryGetValue(character, out var lines))
            {
                lines = [];
                Lines[character] = lines;
            }

            lines.Add(text.TrimEnd('\r', '\n'));
        }

        public void Send(Descriptor descriptor, string text)
        {
            if (descriptor.Character is { } character)
            {
                Send(character, text);
            }
        }

        public string Text(Character character) =>
            Lines.TryGetValue(character, out var lines)
                ? string.Concat(lines)
                : string.Empty;
    }

    private static World CreateWorld()
    {
        var area = new AreaData(
            "Midgaard",
            "tests",
            3000,
            3099,
            9,
            "tests",
            "midgaard.are",
            [],
            [],
            [],
            [
                Room(3001, "The Temple"),
                Room(3002, "The Square")
            ],
            [],
            [],
            [],
            Socials());

        var otherArea = new AreaData(
            "Elsewhere",
            "tests",
            3100,
            3199,
            9,
            "tests",
            "elsewhere.are",
            [],
            [],
            [],
            [Room(3101, "A Far Room")],
            [],
            [],
            [],
            []);

        return new World([area, otherArea], new DateTimeOffset(2026, 11, 21, 0, 0, 0, TimeSpan.Zero));
    }

    private static RoomPrototype Room(int vnum, string name) =>
        new(
            vnum,
            name,
            "A test room.",
            0,
            Sector.Inside,
            100,
            100,
            0,
            new Dictionary<Direction, ExitPrototype>(),
            []);

    private static IReadOnlyList<SocialData> Socials() =>
    [
        new(
            "smile",
            "You smile happily.",
            "$n smiles happily.",
            "You smile at $N.",
            "$n smiles at $N.",
            "$n smiles at you.",
            "$n smiles at nobody.",
            "You smile at yourself.",
            "$n smiles at $mself."),
        new(
            "snore",
            "You snore loudly.",
            "$n snores loudly.",
            "You snore at $N.",
            "$n snores at $N.",
            "$n snores at you.",
            "$n snores at nobody.",
            "You snore at yourself.",
            "$n snores at $mself."),
        new(
            "wave",
            "You wave.",
            "$n waves.",
            "You wave at $N.",
            "$n waves at $N.",
            "$n waves at you.",
            "$n waves at nobody.",
            "You wave at yourself.",
            "$n waves at $mself.")
    ];
}
