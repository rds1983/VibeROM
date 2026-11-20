using System.Text.Json;
using NLog;
using VibeROM.Common.Configuration;
using VibeROM.Common.Domain;
using VibeROM.Common.Rom;
using VibeROM.Common.Security;
using VibeROM.Common.Serialization;
using VibeROM.Server.Network;

namespace VibeROM.Server.Login;

/// <summary>
/// ROM-style login state machine: greeting/credits/MOTD, name, password, new
/// character email confirmation and entry into the world.
/// </summary>
public sealed class LoginService : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string LoginScreen =
        "\r\n{W}Log in{x}\r\n" +
        "Original DikuMUD (1990-1991): Sebastian Hammer, Michael Seifert,\r\n" +
        "  Hans Henrik Staerfeldt, Tom Madsen and Katja Nyboe.\r\n" +
        "Merc DikuMUD (1992-1993): Michael Chastain, Michael Quan and Mitchell Tse.\r\n" +
        "ROM 2.4 (1993-1998): Russ Taylor, Gabrielle Taylor and Brian Moore.\r\n" +
        "\r\nMOTD: Welcome to VibeROM.\r\n" +
        "Rules: Be considerate; no cheating or harassment.\r\n\r\n" +
        "By what name do you wish to be known? ";

    private readonly TelnetServer _server;
    private readonly World _world;
    private readonly PlayerStore _store;
    private readonly GameplayOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, LoginContext> _contexts = [];
    private readonly Dictionary<string, Descriptor> _online = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public LoginService(
        TelnetServer server,
        World world,
        PlayerStore store,
        GameplayOptions options,
        TimeProvider? timeProvider = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;

        _server.ConnectionOpened += OnOpened;
        _server.ConnectionClosed += OnClosed;
        _server.LineReceived += OnLine;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _server.ConnectionOpened -= OnOpened;
        _server.ConnectionClosed -= OnClosed;
        _server.LineReceived -= OnLine;
    }

    private void OnOpened(Descriptor descriptor)
    {
        if (IsBannedSite(descriptor.Host))
        {
            _ = _server.SendAsync(descriptor, "Your site has been banned.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        descriptor.State = ConnectionState.GetName;
        _contexts[descriptor.Id] = new LoginContext();
        _ = _server.SendAsync(descriptor, LoginScreen);
    }

    private void OnLine(Descriptor descriptor, string line)
    {
        if (!_contexts.TryGetValue(descriptor.Id, out var context))
        {
            return;
        }

        var value = line.Trim();
        switch (descriptor.State)
        {
            case ConnectionState.GetName:
                HandleName(descriptor, context, value);
                break;

            case ConnectionState.ConfirmNewName:
                if (IsYes(value))
                {
                    descriptor.State = ConnectionState.GetNewPassword;
                    _ = _server.SendAsync(descriptor, "Choose a password: ");
                }
                else if (IsNo(value))
                {
                    descriptor.State = ConnectionState.GetName;
                    _ = _server.SendAsync(descriptor, "By what name do you wish to be known? ");
                }
                else
                {
                    _ = _server.SendAsync(descriptor, "Please answer yes or no (y/n): ");
                }

                break;

            case ConnectionState.GetOldPassword:
                HandleOldPassword(descriptor, context, value);
                break;

            case ConnectionState.GetNewPassword:
                if (value.Length is < 5 or > 80)
                {
                    _ = _server.SendAsync(descriptor, "Passwords must be 5 to 80 characters. Password: ");
                    return;
                }

                context.Password = value;
                descriptor.State = ConnectionState.ConfirmNewPassword;
                _ = _server.SendAsync(descriptor, "Please type your password again: ");
                break;

            case ConnectionState.ConfirmNewPassword:
                if (!string.Equals(value, context.Password, StringComparison.Ordinal))
                {
                    context.Password = null;
                    descriptor.State = ConnectionState.GetNewPassword;
                    _ = _server.SendAsync(descriptor, "Passwords do not match. Choose a password: ");
                    return;
                }

                descriptor.State = ConnectionState.GetNewEmail;
                _ = _server.SendAsync(descriptor, "Email address: ");
                break;

            case ConnectionState.GetNewEmail:
                if (!IsPlausibleEmail(value))
                {
                    _ = _server.SendAsync(descriptor, "That does not look like an email address. Email address: ");
                    return;
                }

                context.Email = value;
                descriptor.State = ConnectionState.ConfirmNewEmail;
                _ = _server.SendAsync(descriptor, "Is that email address correct? (y/n): ");
                break;

            case ConnectionState.ConfirmNewEmail:
                if (IsYes(value))
                {
                    BeginRaceSelection(descriptor);
                }
                else if (IsNo(value))
                {
                    descriptor.State = ConnectionState.GetNewEmail;
                    _ = _server.SendAsync(descriptor, "Email address: ");
                }
                else
                {
                    _ = _server.SendAsync(descriptor, "Please answer yes or no (y/n): ");
                }

                break;

            case ConnectionState.GetNewRace:
                HandleNewRace(descriptor, context, value);
                break;

            case ConnectionState.GetNewSex:
                HandleNewSex(descriptor, context, value);
                break;

            case ConnectionState.GetNewClass:
                HandleNewClass(descriptor, context, value);
                break;

            case ConnectionState.GetAlignment:
                HandleAlignment(descriptor, context, value);
                break;

            case ConnectionState.DefaultChoice:
                HandleDefaultChoice(descriptor, context, value);
                break;

            case ConnectionState.PickWeapon:
                HandleWeaponChoice(descriptor, context, value);
                break;
        }
    }

    private void HandleName(Descriptor descriptor, LoginContext context, string name)
    {
        if (!IsValidName(name))
        {
            _ = _server.SendAsync(
                descriptor,
                "Names must start with a letter and contain only letters and digits (2-20). Name: ");
            return;
        }

        if (_options.BannedNames.Any(candidate =>
                string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)))
        {
            _ = _server.SendAsync(descriptor, "That name has been banned.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        context.Name = name;
        if (_store.Exists(name))
        {
            descriptor.State = ConnectionState.GetOldPassword;
            _ = _server.SendAsync(descriptor, "Password: ");
            return;
        }

        if (_options.Wizlock)
        {
            _ = _server.SendAsync(descriptor, "The game is wizlocked. Try again later.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        if (_options.MaxOnline > 0 && _online.Count >= _options.MaxOnline)
        {
            _ = _server.SendAsync(descriptor, "The game is full. Try again later.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        descriptor.State = ConnectionState.ConfirmNewName;
        _ = _server.SendAsync(descriptor, $"Did I get that right, {name}? (y/n): ");
    }

    private void BeginRaceSelection(Descriptor descriptor)
    {
        descriptor.State = ConnectionState.GetNewRace;
        _ = _server.SendAsync(descriptor, RacePrompt());
    }

    private static string RacePrompt() =>
        "The following races are available:\r\n  "
        + string.Join(' ', CharacterCreation.RaceNames)
        + "\r\nWhat is your race (help for more information)? ";

    private static string RaceRetryPrompt() =>
        "The following races are available:\r\n  "
        + string.Join(' ', CharacterCreation.RaceNames)
        + "\r\nWhat is your race? (help for more information) ";

    private void HandleNewRace(Descriptor descriptor, LoginContext context, string value)
    {
        if (value.Equals("help", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("help ", StringComparison.OrdinalIgnoreCase))
        {
            _ = _server.SendAsync(descriptor, RaceRetryPrompt());
            return;
        }

        if (!CharacterCreation.TryLookupRace(value, out var race))
        {
            _ = _server.SendAsync(
                descriptor,
                "That is not a valid race.\r\n" + RaceRetryPrompt());
            return;
        }

        context.Race = race;
        descriptor.State = ConnectionState.GetNewSex;
        _ = _server.SendAsync(descriptor, "What is your sex (M/F)? ");
    }

    private void HandleNewSex(Descriptor descriptor, LoginContext context, string value)
    {
        var sex = value.Length == 0 ? '\0' : char.ToUpperInvariant(value[0]);
        switch (sex)
        {
            case 'M':
                context.Sex = Sex.Male;
                break;

            case 'F':
                context.Sex = Sex.Female;
                break;

            default:
                _ = _server.SendAsync(descriptor, "That's not a sex.\r\nWhat IS your sex? ");
                return;
        }

        descriptor.State = ConnectionState.GetNewClass;
        _ = _server.SendAsync(
            descriptor,
            $"Select a class [{string.Join(' ', CharacterCreation.ClassNames)}]: ");
    }

    private void HandleNewClass(Descriptor descriptor, LoginContext context, string value)
    {
        if (!CharacterCreation.TryLookupClass(value, out var playerClass))
        {
            _ = _server.SendAsync(descriptor, "That's not a class.\r\nWhat IS your class? ");
            return;
        }

        context.Class = playerClass;
        descriptor.State = ConnectionState.GetAlignment;
        _ = _server.SendAsync(
            descriptor,
            "\r\nYou may be good, neutral, or evil.\r\nWhich alignment (G/N/E)? ");
    }

    private void HandleAlignment(Descriptor descriptor, LoginContext context, string value)
    {
        var alignment = value.Length == 0 ? '\0' : char.ToUpperInvariant(value[0]);
        context.Alignment = alignment switch
        {
            'G' => 750,
            'N' => 0,
            'E' => -750,
            _ => int.MinValue
        };

        if (context.Alignment == int.MinValue)
        {
            _ = _server.SendAsync(
                descriptor,
                "That's not a valid alignment.\r\nWhich alignment (G/N/E)? ");
            return;
        }

        descriptor.State = ConnectionState.DefaultChoice;
        _ = _server.SendAsync(
            descriptor,
            "\r\nDo you wish to customize this character?\r\n"
            + "Customization takes time, but allows a wider range of skills and abilities.\r\n"
            + "Customize (Y/N)? ");
    }

    private void HandleDefaultChoice(Descriptor descriptor, LoginContext context, string value)
    {
        if (IsYes(value))
        {
            // ROM opens the point-buy menu here. It is tracked by T029; until
            // then Y is accepted but the default package is used.
            _ = _server.SendAsync(
                descriptor,
                "Customization is not available yet; using the default package.\r\n");
        }
        else if (!IsNo(value))
        {
            _ = _server.SendAsync(descriptor, "Please answer (Y/N)? ");
            return;
        }

        BeginWeaponChoice(descriptor, context);
    }

    private void BeginWeaponChoice(Descriptor descriptor, LoginContext context)
    {
        var choices = CharacterCreation.DefaultWeaponChoices(context.Race, context.Class);
        if (choices.Count == 0)
        {
            Logger.Error(
                "No weapon choices for race {Race} class {Class}.",
                context.Race,
                context.Class);
            _ = _server.SendAsync(descriptor, "No weapon choices are available.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        descriptor.State = ConnectionState.PickWeapon;
        _ = _server.SendAsync(
            descriptor,
            "Please pick a weapon from the following choices:\r\n"
            + WeaponChoices(choices)
            + "\r\nYour choice? ");
    }

    private void HandleWeaponChoice(Descriptor descriptor, LoginContext context, string value)
    {
        var choices = CharacterCreation.DefaultWeaponChoices(context.Race, context.Class);
        var weapon = CharacterCreation.FindWeapon(value);
        if (weapon is null || choices.All(choice => choice.Name != weapon.Name))
        {
            _ = _server.SendAsync(
                descriptor,
                "That's not a valid selection. Choices are:\r\n"
                + WeaponChoices(choices)
                + "\r\nYour choice? ");
            return;
        }

        context.Weapon = weapon.Name;
        CompleteNewPlayer(descriptor, context);
    }

    private static string WeaponChoices(IReadOnlyList<WeaponDefinition> choices) =>
        string.Join(' ', choices.Select(choice => choice.Name)) + " ";

    private void HandleOldPassword(Descriptor descriptor, LoginContext context, string password)
    {
        string? hash;
        try
        {
            hash = context.Name is null ? null : _store.GetPasswordHash(context.Name);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            Logger.Error(exception, "Could not read the save for {Player}.", context.Name);
            _ = _server.SendAsync(
                descriptor,
                "Your save could not be read. Please contact an administrator.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        if (hash is null || !PasswordHasher.Verify(password, hash))
        {
            context.PasswordAttempts++;
            if (context.PasswordAttempts >= 3)
            {
                _ = _server.SendAsync(descriptor, "Too many wrong passwords.\r\n");
                _server.DisconnectAfterFlush(descriptor);
            }
            else
            {
                _ = _server.SendAsync(descriptor, "Wrong password. Password: ");
            }

            return;
        }

        CompleteReturningPlayer(descriptor, context);
    }

    private void CompleteReturningPlayer(Descriptor descriptor, LoginContext context)
    {
        var name = context.Name!;
        if (_online.TryGetValue(name, out var oldDescriptor) && oldDescriptor != descriptor)
        {
            SaveAndDetach(oldDescriptor);
            _server.Disconnect(oldDescriptor);
        }

        try
        {
            var character = _store.Load(name, _world);
            Attach(descriptor, character);
        }
        catch (Exception exception) when (exception is DataFormatException or IOException or WorldDataException)
        {
            Logger.Error(exception, "Could not load player {Player}.", name);
            _ = _server.SendAsync(descriptor, "Your save could not be loaded. Please contact an administrator.\r\n");
            _server.DisconnectAfterFlush(descriptor);
        }
    }

    private void CompleteNewPlayer(Descriptor descriptor, LoginContext context)
    {
        var name = context.Name!;
        if (_options.MaxOnline > 0 && _online.Count >= _options.MaxOnline)
        {
            _ = _server.SendAsync(descriptor, "The game is full. Try again later.\r\n");
            _server.DisconnectAfterFlush(descriptor);
            return;
        }

        try
        {
            var player = new PlayerData
            {
                PasswordHash = PasswordHasher.Hash(context.Password!),
                Email = context.Email!,
                ColourEnabled = true,
                LastSaved = _timeProvider.GetUtcNow()
            };
            var character = _world.CreatePlayer(
                player,
                name,
                context.Race,
                context.Class,
                context.Sex);
            character.Alignment = context.Alignment;
            CharacterCreation.ApplyRace(character);
            CharacterCreation.ApplyDefaultPackage(character);
            if (!CharacterCreation.TryChooseWeapon(character, context.Weapon!, out _))
            {
                throw new InvalidOperationException(
                    $"Weapon '{context.Weapon}' was not available for {name}.");
            }

            CharacterCreation.Complete(_world, character, _options.StartingRoomVnum);
            _store.Save(character);
            _ = _server.SendAsync(descriptor, "You have been equipped by Mota.\r\n");
            Attach(descriptor, character);
        }
        catch (Exception exception) when (exception is IOException or DataFormatException or WorldDataException)
        {
            Logger.Error(exception, "Could not create player {Player}.", name);
            _ = _server.SendAsync(descriptor, "Your character could not be created.\r\n");
            _server.DisconnectAfterFlush(descriptor);
        }
    }

    private void Attach(Descriptor descriptor, Character character)
    {
        descriptor.Character = character;
        descriptor.State = ConnectionState.Playing;
        _online[character.Name] = descriptor;
        character.Player!.LastLogoff = null;
        _ = _server.SendAsync(
            descriptor,
            $"\r\nWelcome, {character.Name}. You are in {character.InRoom?.Prototype.Name ?? "the world"}.\r\n");
    }

    private void OnClosed(Descriptor descriptor)
    {
        _contexts.Remove(descriptor.Id);
        if (descriptor.Character is not { Player: not null } character
            || !_online.TryGetValue(character.Name, out var current)
            || current != descriptor)
        {
            return;
        }

        SaveAndDetach(descriptor);
    }

    private void SaveAndDetach(Descriptor descriptor)
    {
        if (descriptor.Character is not { Player: not null } character
            || !_online.TryGetValue(character.Name, out var current)
            || current != descriptor)
        {
            return;
        }

        try
        {
            character.Player.LastLogoff = _timeProvider.GetUtcNow();
            character.Player.LastSaved = _timeProvider.GetUtcNow();
            _store.Save(character);
        }
        catch (Exception exception) when (exception is IOException or DataFormatException)
        {
            Logger.Error(exception, "Could not save player {Player} on logout.", character.Name);
        }

        _online.Remove(character.Name);
        _world.DestroyCharacter(character);
        descriptor.Character = null;
    }

    private bool IsBannedSite(string host) =>
        _options.BannedSites.Any(site => host.StartsWith(site, StringComparison.OrdinalIgnoreCase));

    private static bool IsValidName(string name) =>
        name.Length is >= 2 and <= 20
        && char.IsAsciiLetter(name[0])
        && name.All(character => char.IsAsciiLetterOrDigit(character));

    private static bool IsPlausibleEmail(string email) =>
        email.Contains('@', StringComparison.Ordinal)
        && email.Contains('.', StringComparison.Ordinal)
        && !email.Contains(' ', StringComparison.Ordinal);

    private static bool IsYes(string value) =>
        value.Equals("y", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase);

    private static bool IsNo(string value) =>
        value.Equals("n", StringComparison.OrdinalIgnoreCase)
        || value.Equals("no", StringComparison.OrdinalIgnoreCase);

    private sealed class LoginContext
    {
        public string? Name { get; set; }

        public string? Password { get; set; }

        public string? Email { get; set; }

        public int PasswordAttempts { get; set; }

        public Race Race { get; set; } = Race.Human;

        public Sex Sex { get; set; } = Sex.Neutral;

        public PlayerClass Class { get; set; }

        public int Alignment { get; set; }

        public string? Weapon { get; set; }
    }
}
