using Telengard.Core.Events;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
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
    public void Rolled_provider_generates_six_configured_attributes_and_round_trips()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        state = state with
        {
            Player = state.Player with
            {
                Attributes = new PlayerAttributes(20, 21, 22, 23, 24, 25),
                Level = 3,
                Experience = 456,
                HitPoints = 20,
                MaxHitPoints = 30,
                Inventory = ["potion"]
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

        var result = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));

        Assert.Equal(new PlayerAttributes(3, 18, 3, 18, 3, 18), result.State.Player.Attributes);
        Assert.Equal(state.Player with { Attributes = result.State.Player.Attributes }, result.State.Player);
        var created = Assert.IsType<CharacterCreatedEvent>(Assert.Single(result.Events));
        Assert.Equal(CharacterCreationMode.Rolled, created.Mode);
        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));
        Assert.Equal(result.State.Player.Attributes, roundTrip.Player.Attributes);
    }

    [Fact]
    public void Rolled_provider_replays_from_named_simulation_stream()
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
        var first = CharacterCreationResolver.Resolve(state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var second = CharacterCreationResolver.Resolve(state,
            new CreateCharacterCommand(new CharacterCreationRequest(CharacterCreationMode.Rolled)),
            new RolledCharacterCreationProvider(configuration));
        var stream = new DeterministicRng(1234, state.Versions.SimulationVersion)
            .CreateStream("character-creation", "mode:rolled", $"player:{PlayerId}", "policy:stream-v1");

        var expected = new PlayerAttributes(
            (int)stream.NextLong(3, 19), (int)stream.NextLong(3, 19),
            (int)stream.NextLong(3, 19), (int)stream.NextLong(3, 19),
            (int)stream.NextLong(3, 19), (int)stream.NextLong(3, 19));
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(expected, first.State.Player.Attributes);
    }

    [Fact]
    public void Rolled_provider_rejects_invalid_configuration_and_mode_before_commit()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var before = SaveGameSerializer.Serialize(state);
        Assert.Throws<ArgumentException>(() => new RolledAttributeRange(19, 18));
        Assert.Throws<ArgumentException>(() => new RolledCharacterCreationConfiguration("v1", []));
        var configuration = new RolledCharacterCreationConfiguration("v1",
            [
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18)
            ]);
        var provider = new RolledCharacterCreationProvider(configuration);
        Assert.Throws<InvalidOperationException>(() => provider.Create(
            state, new CharacterCreationRequest(CharacterCreationMode.DailySeed)));
        Assert.Equal(before, SaveGameSerializer.Serialize(state));
    }

    [Fact]
    public void Rolled_provider_uses_simulation_version_not_generator_version()
    {
        var versions = new GameVersions("simulation-a", "generator-a", "content-a");
        var state = GameState.Create(1234, versions, playerId: PlayerId);
        var configuration = new RolledCharacterCreationConfiguration("v1",
            [
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18),
                new RolledAttributeRange(3, 18)
            ]);
        PlayerAttributes Create(GameState input) => new RolledCharacterCreationProvider(configuration)
            .Create(input, new CharacterCreationRequest(CharacterCreationMode.Rolled)).Player.Attributes;

        Assert.Equal(Create(state), Create(state with
        {
            Versions = new GameVersions("simulation-a", "generator-b", "content-a")
        }));
        Assert.NotEqual(Create(state), Create(state with
        {
            Versions = new GameVersions("simulation-b", "generator-a", "content-a")
        }));
    }

    [Fact]
    public void Daily_seed_provider_replays_across_players_and_preserves_state()
    {
        var firstState = GameState.Create(1234, playerId: PlayerId) with
        {
            Player = new PlayerState
            {
                Id = PlayerId,
                Attributes = new PlayerAttributes(20, 21, 22, 23, 24, 25),
                Level = 3,
                Experience = 456,
                HitPoints = 20,
                MaxHitPoints = 30,
                Inventory = ["potion"]
            }
        };
        var secondState = GameState.Create(9876, playerId: OtherPlayerId);
        var request = new CreateCharacterCommand(new CharacterCreationRequest(
            CharacterCreationMode.DailySeed,
            new DailySeedCharacterCreationInput("2026-08-22")));
        var configuration = new DailySeedCharacterCreationConfiguration("policy-1", 3, 18);

        var first = CharacterCreationResolver.Resolve(
            firstState,
            request,
            new DailySeedCharacterCreationProvider(configuration));
        var second = CharacterCreationResolver.Resolve(
            secondState,
            request,
            new DailySeedCharacterCreationProvider(configuration));

        Assert.Equal(first.State.Player.Attributes, second.State.Player.Attributes);
        Assert.Equal(firstState.Player with { Attributes = first.State.Player.Attributes }, first.State.Player);
        Assert.All(
            new[]
            {
                first.State.Player.Attributes.Strength,
                first.State.Player.Attributes.Intelligence,
                first.State.Player.Attributes.Wisdom,
                first.State.Player.Attributes.Constitution,
                first.State.Player.Attributes.Dexterity,
                first.State.Player.Attributes.Charisma
            },
            value => Assert.InRange(value, 3, 18));
        var created = Assert.IsType<CharacterCreatedEvent>(Assert.Single(first.Events));
        Assert.Equal(PlayerId, created.PlayerId);
        Assert.Equal(CharacterCreationMode.DailySeed, created.Mode);

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));
        Assert.Equal(first.State.Player.Attributes, roundTrip.Player.Attributes);
    }

    [Fact]
    public void Different_daily_seeds_produce_different_initial_attributes()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var provider = new DailySeedCharacterCreationProvider(
            new DailySeedCharacterCreationConfiguration("policy-1", 0, 100));

        var first = provider.Create(
            state,
            new CharacterCreationRequest(
                CharacterCreationMode.DailySeed,
                new DailySeedCharacterCreationInput("seed-a")));
        var second = provider.Create(
            state,
            new CharacterCreationRequest(
                CharacterCreationMode.DailySeed,
                new DailySeedCharacterCreationInput("seed-b")));

        Assert.NotEqual(first.Player.Attributes, second.Player.Attributes);
    }

    [Fact]
    public void Invalid_daily_seed_input_is_rejected_before_mutation_or_event()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var before = SaveGameSerializer.Serialize(state);
        var bus = new DomainEventBus();
        var published = 0;
        bus.Subscribe<CharacterCreatedEvent>(_ => published++);
        var dispatcher = new CommandDispatcher(state, bus);
        var provider = new DailySeedCharacterCreationProvider(
            new DailySeedCharacterCreationConfiguration("policy-1", 3, 18));
        dispatcher.Register<CreateCharacterCommand>((current, command) =>
            CharacterCreationResolver.Resolve(current, command, provider));

        Assert.Throws<ArgumentException>(() => new DailySeedCharacterCreationInput("  "));
        Assert.Throws<ArgumentException>(() => provider.Create(
            state,
            new CharacterCreationRequest(CharacterCreationMode.DailySeed)));
        Assert.Throws<ArgumentException>(() => provider.Create(
            state,
            new CharacterCreationRequest(
                CharacterCreationMode.DailySeed,
                new TestInput("wrong"))));
        Assert.Throws<InvalidOperationException>(() => provider.Create(
            state,
            new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                new DailySeedCharacterCreationInput("seed"))));
        Assert.Throws<ArgumentException>(() => new DailySeedCharacterCreationConfiguration("policy-1", 18, 3));

        Assert.Throws<ArgumentException>(() => dispatcher.Dispatch(
            new CreateCharacterCommand(new CharacterCreationRequest(
                CharacterCreationMode.DailySeed,
                new TestInput("wrong")))));
        Assert.Equal(before, SaveGameSerializer.Serialize(dispatcher.CurrentState));
        Assert.Equal(0, published);
    }

    [Fact]
    public void Point_allocation_provider_commits_exact_budget_and_preserves_other_state()
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
                Inventory = ["potion"],
                CarriedGold = 123,
                Alive = true
            }
        };
        var allocation = new PointAllocationCharacterCreationInput(
            new PlayerAttributes(18, 17, 16, 15, 7, 5));
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(78, 3, 18));

        var result = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                allocation)),
            provider);

        Assert.Equal(allocation.Attributes, result.State.Player.Attributes);
        Assert.Equal(state.Player with { Attributes = allocation.Attributes }, result.State.Player);
        var created = Assert.IsType<CharacterCreatedEvent>(Assert.Single(result.Events));
        Assert.Equal(PlayerId, created.PlayerId);
        Assert.Equal(CharacterCreationMode.PointAllocation, created.Mode);

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));
        Assert.Equal(
            SaveGameSerializer.Serialize(result.State),
            SaveGameSerializer.Serialize(roundTrip));
        Assert.Equal(result.State.Player.Attributes, roundTrip.Player.Attributes);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    public void Point_allocation_provider_rejects_under_or_over_budget_before_mutation(int finalAttribute)
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var before = SaveGameSerializer.Serialize(state);
        var input = new PointAllocationCharacterCreationInput(
            new PlayerAttributes(12, 13, 14, 15, 16, finalAttribute));
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(78, 3, 18));
        var bus = new DomainEventBus();
        var published = 0;
        bus.Subscribe<CharacterCreatedEvent>(_ => published++);
        var dispatcher = new CommandDispatcher(state, bus);
        dispatcher.Register<CreateCharacterCommand>((current, command) =>
            CharacterCreationResolver.Resolve(current, command, provider));

        Assert.Throws<ArgumentException>(() => dispatcher.Dispatch(
            new CreateCharacterCommand(new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                input))));

        Assert.Equal(before, SaveGameSerializer.Serialize(dispatcher.CurrentState));
        Assert.Equal(0, published);
    }

    [Fact]
    public void Point_allocation_provider_rejects_bounds_and_malformed_inputs()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(78, 3, 18));

        Assert.Throws<ArgumentException>(() => provider.Create(
            state,
            new CharacterCreationRequest(CharacterCreationMode.PointAllocation)));
        Assert.Throws<ArgumentException>(() => provider.Create(
            state,
            new CharacterCreationRequest(CharacterCreationMode.PointAllocation, new TestInput("wrong"))));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PointAllocationCharacterCreationConfiguration(-1, 3, 18));
        Assert.Throws<ArgumentException>(() =>
            new PointAllocationCharacterCreationConfiguration(78, 18, 3));
    }

    [Theory]
    [InlineData(3, 18, 18, 18, 18, 3, false)]
    [InlineData(18, 3, 18, 18, 18, 3, false)]
    [InlineData(2, 18, 18, 18, 17, 5, true)]
    [InlineData(19, 17, 16, 15, 6, 5, true)]
    public void Point_allocation_provider_enforces_inclusive_attribute_bounds(
        int strength,
        int intelligence,
        int wisdom,
        int constitution,
        int dexterity,
        int charisma,
        bool invalid)
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var attributes = new PlayerAttributes(
            strength,
            intelligence,
            wisdom,
            constitution,
            dexterity,
            charisma);
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(78, 3, 18));

        if (invalid)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Create(
                state,
                new CharacterCreationRequest(
                    CharacterCreationMode.PointAllocation,
                    new PointAllocationCharacterCreationInput(attributes))));
        }
        else
        {
            var result = provider.Create(
                state,
                new CharacterCreationRequest(
                    CharacterCreationMode.PointAllocation,
                    new PointAllocationCharacterCreationInput(attributes)));
            Assert.Equal(attributes, result.Player.Attributes);
        }
    }

    [Fact]
    public void Invalid_point_allocation_does_not_commit_or_publish_an_event()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var before = SaveGameSerializer.Serialize(state);
        var bus = new DomainEventBus();
        var published = 0;
        bus.Subscribe<CharacterCreatedEvent>(_ => published++);
        var dispatcher = new CommandDispatcher(state, bus);
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(78, 3, 18));
        dispatcher.Register<CreateCharacterCommand>((current, command) =>
            CharacterCreationResolver.Resolve(current, command, provider));

        Assert.Throws<ArgumentOutOfRangeException>(() => dispatcher.Dispatch(
            new CreateCharacterCommand(new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                new PointAllocationCharacterCreationInput(
                    new PlayerAttributes(19, 17, 16, 15, 6, 5))))));

        Assert.Equal(before, SaveGameSerializer.Serialize(dispatcher.CurrentState));
        Assert.Equal(0, published);
    }

    [Fact]
    public void Equal_point_allocations_replay_to_equal_state_and_events()
    {
        var state = GameState.Create(1234, playerId: PlayerId);
        var request = new CharacterCreationRequest(
            CharacterCreationMode.PointAllocation,
            new PointAllocationCharacterCreationInput(
                new PlayerAttributes(18, 17, 16, 15, 7, 5)));
        var configuration = new PointAllocationCharacterCreationConfiguration(78, 3, 18);

        var first = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(request),
            new PointAllocationCharacterCreationProvider(configuration));
        var second = CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(request),
            new PointAllocationCharacterCreationProvider(configuration));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Null_boundaries_are_rejected()
    {
        var state = GameState.Create(1234);
        var provider = new RecordingProvider(CharacterCreationMode.Rolled, state.Player);

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
    private static readonly Guid OtherPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000002");

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
