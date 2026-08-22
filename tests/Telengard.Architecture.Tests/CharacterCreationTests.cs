using Telengard.Core.Simulation;
using Telengard.Core.Items;
using Telengard.Core.Rng;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CharacterCreationTests
{
    [Theory]
    [InlineData(CharacterCreationMode.Rolled)]
    [InlineData(CharacterCreationMode.PointAllocation)]
    [InlineData(CharacterCreationMode.DailySeed)]
    public void Each_named_mode_commits_the_provider_result_and_event(CharacterCreationMode mode)
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var player = new PlayerState
        {
            Id = PlayerId,
            Attributes = new PlayerAttributes(10, 11, 12, 13, 14, 15),
            Level = 1,
            HitPoints = 20,
            MaxHitPoints = 20
        };
        var provider = new RecordingProvider(mode, player);

        var result = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(mode)),
            provider);

        Assert.Equal(player, result.State.Player);
        var created = Assert.IsType<CharacterCreatedEvent>(Assert.Single(result.Events));
        Assert.Equal(PlayerId, created.PlayerId);
        Assert.Equal(mode, created.Mode);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public void Invalid_mode_is_rejected_before_provider_call_or_state_change()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var provider = new RecordingProvider(CharacterCreationMode.Rolled, state.Player);
        var command = new CreateCharacterCommand(
            new CharacterCreationRequest((CharacterCreationMode)999));
        var before = SaveGameSerializer.Serialize(state);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CharacterCreationResolver.Resolve(state, command, provider));

        Assert.Equal(before, SaveGameSerializer.Serialize(state));
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void Provider_mode_must_match_the_request_before_state_change()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var provider = new RecordingProvider(CharacterCreationMode.DailySeed, state.Player);
        var before = SaveGameSerializer.Serialize(state);

        Assert.Throws<InvalidOperationException>(() =>
            CharacterCreationResolver.Resolve(
                state,
                new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
                provider));

        Assert.Equal(before, SaveGameSerializer.Serialize(state));
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void Dispatcher_publishes_creation_event_after_state_commit()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var player = state.Player with
        {
            Attributes = new PlayerAttributes(1, 2, 3, 4, 5, 6),
            HitPoints = 10,
            MaxHitPoints = 10
        };
        var provider = new RecordingProvider(CharacterCreationMode.PointAllocation, player);
        var dispatcher = new CommandDispatcher(state, new Telengard.Core.Events.DomainEventBus());
        PlayerState? observedPlayer = null;
        dispatcher.EventBus!.Subscribe<CharacterCreatedEvent>(_ => observedPlayer = dispatcher.CurrentState.Player);
        dispatcher.Register<CreateCharacterCommand>((current, command) =>
            CharacterCreationResolver.Resolve(current, command, provider));

        dispatcher.Dispatch(new CreateCharacterCommand(
            new CharacterCreationRequest(CharacterCreationMode.PointAllocation)));

        Assert.Equal(player, observedPlayer);
    }

    [Fact]
    public void Provider_receives_mode_specific_input_without_boundary_interpretation()
    {
        var input = new TestInput("2026-08-20");
        var provider = new RecordingProvider(
            CharacterCreationMode.DailySeed,
            GameState.Create(1234).Player);
        var state = GameState.Create(1234);

        CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.DailySeed, input)),
            provider);

        Assert.Same(input, provider.LastRequest!.Input);
    }

    [Fact]
    public void Equal_provider_inputs_replay_to_equal_state_and_events_and_save_round_trip()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var request = new CharacterCreationRequest(CharacterCreationMode.Rolled);
        var player = state.Player with
        {
            Attributes = new PlayerAttributes(7, 8, 9, 10, 11, 12),
            HitPoints = 18,
            MaxHitPoints = 18
        };

        var first = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(request),
            new RecordingProvider(request.Mode, player));
        var second = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(request),
            new RecordingProvider(request.Mode, player));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        var serialized = SaveGameSerializer.Serialize(first.State);
        var roundTrip = SaveGameSerializer.Deserialize(serialized);
        Assert.Equal(serialized, SaveGameSerializer.Serialize(roundTrip));
        Assert.Equal(first.State.Player.Attributes, roundTrip.Player.Attributes);
        Assert.Equal(first.State.Player.HitPoints, roundTrip.Player.HitPoints);
        Assert.Equal(GameState.CurrentSaveVersion, first.State.SaveVersion);
    }

    [Fact]
    public void Rolled_provider_generates_six_attributes_through_the_boundary()
    {
        var state = GameState.Create(1234, playerId: PlayerId) with
        {
            Expedition = new ExpeditionState { CarriedGold = 123 },
            Player = new PlayerState
            {
                Id = PlayerId,
                Attributes = new PlayerAttributes(20, 21, 22, 23, 24, 25),
                Level = 3,
                Experience = 456,
                HitPoints = 20,
                MaxHitPoints = 30,
                SpellPower = 7,
                MaxSpellPower = 11,
                Position = new DungeonPosition(1, 0, 0),
                Inventory = ["potion"],
                EquipmentSlots = [new EquipmentSlotState("weapon", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))],
                Talents = ["talent"],
                Spells = ["spell"],
                Injuries = ["injury"],
                TemporaryEffects = ["effect"],
                CarriedGold = 123,
                Alive = true
            }
        };
        var configuration = new RolledCharacterCreationConfiguration(
            "bounds-v1",
            [
                new RolledAttributeRange(3, 3),
                new RolledAttributeRange(18, 18),
                new RolledAttributeRange(3, 3),
                new RolledAttributeRange(18, 18),
                new RolledAttributeRange(3, 3),
                new RolledAttributeRange(18, 18)
            ]);
        var provider = new RolledCharacterCreationProvider(configuration);

        var result = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            provider);

        var values = new[]
        {
            result.State.Player.Attributes.Strength,
            result.State.Player.Attributes.Intelligence,
            result.State.Player.Attributes.Wisdom,
            result.State.Player.Attributes.Constitution,
            result.State.Player.Attributes.Dexterity,
            result.State.Player.Attributes.Charisma
        };
        Assert.Equal(6, values.Length);
        Assert.Equal(new PlayerAttributes(3, 18, 3, 18, 3, 18), result.State.Player.Attributes);
        Assert.All(values, value => Assert.InRange(value, 3, 18));
        Assert.Equal(
            state.Player with { Attributes = result.State.Player.Attributes },
            result.State.Player);
        var created = Assert.IsType<CharacterCreatedEvent>(Assert.Single(result.Events));
        Assert.Equal(PlayerId, created.PlayerId);
        Assert.Equal(CharacterCreationMode.Rolled, created.Mode);

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));
        Assert.Equal(result.State.Player.Attributes, roundTrip.Player.Attributes);
    }

    [Fact]
    public void Rolled_provider_replays_from_its_named_deterministic_stream()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var configuration = new RolledCharacterCreationConfiguration(
            "stream-v1",
            [
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18)
            ]);
        var first = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var second = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var expectedStream = new DeterministicRng(1234, state.Versions.SimulationVersion)
            .CreateStream("character-creation", "mode:rolled", $"player:{PlayerId}", "policy:stream-v1");

        var expected = new PlayerAttributes(
            (int)expectedStream.NextLong(3, 19),
            (int)expectedStream.NextLong(3, 19),
            (int)expectedStream.NextLong(3, 19),
            (int)expectedStream.NextLong(3, 19),
            (int)expectedStream.NextLong(3, 19),
            (int)expectedStream.NextLong(3, 19));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(expected, first.State.Player.Attributes);
    }

    [Fact]
    public void Rolled_provider_rejects_a_non_rolled_request_before_drawing()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var configuration = new RolledCharacterCreationConfiguration(
            "stream-v1",
            [
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18)
            ]);

        var provider = new RolledCharacterCreationProvider(configuration);

        Assert.Throws<ArgumentNullException>(() => provider.Create(null!,
            new CharacterCreationRequest(CharacterCreationMode.Rolled)));
        Assert.Throws<ArgumentNullException>(() => provider.Create(state, null!));
        Assert.Throws<InvalidOperationException>(() => provider.Create(state,
            new CharacterCreationRequest(CharacterCreationMode.DailySeed)));
    }

    [Fact]
    public void Rolled_provider_is_scoped_to_simulation_version_not_generator_version()
    {
        var versions = new GameVersions("simulation-a", "generator-a", "content-a");
        var state = GameState.Create(1234, versions, playerId: PlayerId);
        var configuration = new RolledCharacterCreationConfiguration(
            "stream-v1",
            [
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18)
            ]);

        var original = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var generatorChanged = CharacterCreationResolver.Resolve(
            state with { Versions = new GameVersions("simulation-a", "generator-b", "content-a") },
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var simulationChanged = CharacterCreationResolver.Resolve(
            state with { Versions = new GameVersions("simulation-b", "generator-a", "content-a") },
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));

        Assert.Equal(original.State.Player.Attributes, generatorChanged.State.Player.Attributes);
        Assert.NotEqual(original.State.Player.Attributes, simulationChanged.State.Player.Attributes);
    }

    [Fact]
    public void Rolled_configuration_rejects_invalid_boundaries()
    {
        var validRanges = new[]
        {
            new RolledAttributeRange(1, 1),
            new RolledAttributeRange(1, 1),
            new RolledAttributeRange(1, 1),
            new RolledAttributeRange(1, 1),
            new RolledAttributeRange(1, 1),
            new RolledAttributeRange(1, 1)
        };

        var emptyPolicy = Assert.Throws<ArgumentException>(() => new RolledCharacterCreationConfiguration(
            "",
            validRanges));
        Assert.Equal("policyVersion", emptyPolicy.ParamName);

        var nullRanges = Assert.Throws<ArgumentNullException>(() =>
            new RolledCharacterCreationConfiguration("v1", null!));
        Assert.Equal("attributeRanges", nullRanges.ParamName);

        Assert.Throws<ArgumentException>(() => new RolledCharacterCreationConfiguration(
            "v1",
            [new RolledAttributeRange(1, 1)]));
        Assert.Throws<ArgumentException>(() => new RolledAttributeRange(19, 18));
        Assert.Throws<ArgumentException>(() => new RolledCharacterCreationConfiguration(
            "v1",
            [
                new RolledAttributeRange(1, 1),
                new RolledAttributeRange(1, 1),
                new RolledAttributeRange(1, 1),
                new RolledAttributeRange(1, 1),
                new RolledAttributeRange(1, 1),
                null!
            ]));
    }

    [Fact]
    public void Null_boundaries_are_rejected()
    {
        var state = GameState.Create(1234);
        var provider = new RecordingProvider(CharacterCreationMode.Rolled, state.Player);

        Assert.Throws<ArgumentNullException>(() => new RolledCharacterCreationProvider(null!));
        Assert.Throws<ArgumentNullException>(() =>
            CharacterCreationResolver.Resolve(null!, new CreateCharacterCommand(
                new CharacterCreationRequest(CharacterCreationMode.Rolled)), provider));
        Assert.Throws<ArgumentNullException>(() =>
            CharacterCreationResolver.Resolve(state, null!, provider));
        Assert.Throws<ArgumentNullException>(() =>
            CharacterCreationResolver.Resolve(state, new CreateCharacterCommand(null!), provider));
        Assert.Throws<ArgumentNullException>(() =>
            CharacterCreationResolver.Resolve(state, new CreateCharacterCommand(
                new CharacterCreationRequest(CharacterCreationMode.Rolled)), null!));
        Assert.Throws<ArgumentNullException>(() => new CharacterCreationResult(null!));
    }

    private static readonly Guid PlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private sealed record TestInput(string Value) : ICharacterCreationInput;

    private sealed class RecordingProvider(CharacterCreationMode mode, PlayerState player) : ICharacterCreationProvider
    {
        public int CallCount { get; private set; }
        public CharacterCreationRequest? LastRequest { get; private set; }
        public CharacterCreationMode Mode { get; } = mode;

        public CharacterCreationResult Create(GameState state, CharacterCreationRequest request)
        {
            CallCount++;
            LastRequest = request;
            return new CharacterCreationResult(player);
        }
    }
}
