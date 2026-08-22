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

        Assert.Throws<ArgumentException>(() => CharacterCreationResolver.Resolve(
            state,
            new CreateCharacterCommand(new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                input)),
            provider));

        Assert.Equal(before, SaveGameSerializer.Serialize(state));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Create(
            state,
            new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                new PointAllocationCharacterCreationInput(
                    new PlayerAttributes(19, 17, 16, 15, 6, 5)))));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PointAllocationCharacterCreationConfiguration(-1, 3, 18));
        Assert.Throws<ArgumentException>(() =>
            new PointAllocationCharacterCreationConfiguration(78, 18, 3));
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
