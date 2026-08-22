using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class NewGameSetupTests
{
    [Fact]
    public void Setup_creates_a_ready_at_inn_state_with_the_selected_seed_and_character()
    {
        var player = new PlayerState
        {
            Id = PlayerId,
            Attributes = new PlayerAttributes(3, 4, 5, 6, 7, 8),
            Alive = true
        };
        var versions = new GameVersions("simulation-setup", "generator-setup", "content-setup");

        var result = NewGameSetupResolver.Create(
            new NewGameSetupRequest(
                9876,
                GameMode.Legacy,
                new CharacterCreationResult(player)),
            versions);

        Assert.Equal(9876, result.State.WorldSeed);
        Assert.Equal(versions, result.State.Versions);
        Assert.Equal(GameMode.Legacy, result.State.CurrentMode);
        Assert.Equal(player, result.State.Player);
        Assert.True(result.State.Inn.IsAtInn);
        Assert.False(result.State.Expedition.Active);
        Assert.Null(result.State.Expedition.ExpeditionId);
        Assert.Equal(new ExpeditionState(), result.State.Expedition);
        Assert.Equal(new KnowledgeState(), result.State.Knowledge);
        Assert.Equal(new LegacyState { PersistentMap = new PersistentMapState() }, result.State.Legacy);
        Assert.Equal(new SecuredProgressState(), result.State.SecuredProgress);
        Assert.Equal(new SettingsState(), result.State.Settings);
        var created = Assert.IsType<NewGameCreatedEvent>(Assert.Single(result.Events));
        Assert.Equal(PlayerId, created.PlayerId);
        Assert.Equal(GameMode.Legacy, created.Mode);
        Assert.Equal(9876, created.WorldSeed);
    }

    [Fact]
    public void Setup_round_trips_and_can_enter_the_existing_dungeon()
    {
        var result = NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState { Id = PlayerId })));

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));
        var layout = new FloorLayoutGenerator().Generate(
            restored.WorldSeed,
            restored.Versions.GeneratorVersion,
            1);
        var entered = DungeonWalkingResolver.Enter(restored, new EnterDungeonCommand(), layout);

        Assert.Equal(SaveGameSerializer.Serialize(result.State), SaveGameSerializer.Serialize(restored));
        Assert.True(entered.State.Expedition.Active);
        Assert.False(entered.State.Inn.IsAtInn);
        Assert.Equal(layout.StairsUp, entered.State.Player.Position);
    }

    [Fact]
    public void Equal_setup_inputs_replay_to_equal_state_and_events()
    {
        var request = new NewGameSetupRequest(
            0,
            GameMode.Adventure,
            new CharacterCreationResult(new PlayerState
            {
                Id = PlayerId,
                Attributes = new PlayerAttributes(9, 8, 7, 6, 5, 4)
            }));

        var first = NewGameSetupResolver.Create(request);
        var second = NewGameSetupResolver.Create(request);

        Assert.Equal(0, first.State.WorldSeed);
        Assert.Equal(0, Assert.IsType<NewGameCreatedEvent>(Assert.Single(first.Events)).WorldSeed);
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Invalid_setup_inputs_are_rejected_before_state_creation()
    {
        var validCharacter = new CharacterCreationResult(new PlayerState { Id = PlayerId });

        Assert.Throws<ArgumentNullException>(() => NewGameSetupResolver.Create(null!));
        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(
            new NewGameSetupRequest(null, GameMode.Classic, validCharacter)));
        Assert.Throws<ArgumentOutOfRangeException>(() => NewGameSetupResolver.Create(
            new NewGameSetupRequest(1234, (GameMode)999, validCharacter)));
        Assert.Throws<ArgumentNullException>(() => NewGameSetupResolver.Create(
            new NewGameSetupRequest(1234, GameMode.Classic, null!)));
        Assert.Throws<InvalidOperationException>(() => NewGameSetupResolver.Create(
            new NewGameSetupRequest(
                1234,
                GameMode.Classic,
                new CharacterCreationResult(new PlayerState { Id = PlayerId, Alive = false }))));
    }

    [Fact]
    public void Setup_rejects_character_state_that_cannot_be_persisted()
    {
        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState { Id = PlayerId, Attributes = null! }))));

        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState { Id = PlayerId, CarriedGold = 1 }))));

        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState { Id = PlayerId, Inventory = ["potion"] }))));

        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState
            {
                Id = PlayerId,
                HitPoints = 11,
                MaxHitPoints = 10
            }))));

        Assert.Throws<ArgumentException>(() => NewGameSetupResolver.Create(new NewGameSetupRequest(
            1234,
            GameMode.Classic,
            new CharacterCreationResult(new PlayerState { Id = PlayerId, Level = 2 }))));
    }

    private static readonly Guid PlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
