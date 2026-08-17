using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class GameStateTests
{
    [Fact]
    public void Create_initializes_authoritative_state_and_versions()
    {
        var playerId = Guid.NewGuid();
        var state = GameState.Create(1234, playerId: playerId);

        Assert.Equal(1234, state.WorldSeed);
        Assert.Equal(GameMode.Classic, state.CurrentMode);
        Assert.Equal(playerId, state.Player.Id);
        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Equal(GameVersions.Current, state.Versions);
        Assert.False(state.Expedition.Active);
    }

    [Fact]
    public void Create_rejects_unknown_game_modes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GameState.Create(1234, mode: (GameMode)999));
    }

    [Fact]
    public void DungeonPosition_rejects_floors_outside_the_dungeon()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonPosition(51, 0, 0));
    }

    [Fact]
    public void DungeonPosition_preserves_stable_floor_and_coordinates()
    {
        var position = new DungeonPosition(50, -12, 34);

        Assert.Equal(50, position.Floor);
        Assert.Equal(-12, position.X);
        Assert.Equal(34, position.Y);
        Assert.Equal(position, new DungeonPosition(50, -12, 34));
    }

    [Fact]
    public void Create_is_reproducible_when_the_player_identity_is_fixed()
    {
        var playerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        Assert.Equal(
            GameState.Create(1234, playerId: playerId),
            GameState.Create(1234, playerId: playerId));
    }

    [Fact]
    public void Versions_reject_missing_values_and_map_state_has_value_semantics()
    {
        Assert.Throws<ArgumentNullException>(() => new GameVersions(null!, "generator", "content"));
        Assert.Throws<ArgumentException>(() => new GameVersions("simulation", "", "content"));
        Assert.Throws<ArgumentException>(() => new GameVersions("simulation", "generator", " "));

        var first = new PersistentMapState(
            [new DungeonPosition(2, 3, 4)],
            [new DungeonPosition(2, 3, 4)]);
        var second = new PersistentMapState(
            [new DungeonPosition(2, 3, 4)],
            [new DungeonPosition(2, 3, 4)]);

        Assert.True(first.Equals(second));
        Assert.False(first.Equals((PersistentMapState?)null));
        Assert.False(first.Equals(new object()));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Persistent_map_order_and_hash_include_every_coordinate()
    {
        var first = new PersistentMapState(
            [new DungeonPosition(1, 2, 3), new DungeonPosition(1, 2, 4)],
            [new DungeonPosition(1, 2, 3)]);
        var second = new PersistentMapState(
            [new DungeonPosition(1, 2, 4), new DungeonPosition(1, 2, 3)],
            [new DungeonPosition(1, 2, 3)]);
        var different = new PersistentMapState(
            [new DungeonPosition(1, 2, 3)],
            [new DungeonPosition(1, 2, 3)]);

        Assert.Equal([new DungeonPosition(1, 2, 3), new DungeonPosition(1, 2, 4)], first.ObservedPositions);
        Assert.Equal(first, second);
        Assert.NotEqual(first.GetHashCode(), different.GetHashCode());

        var sameObservedDifferentVisited = new PersistentMapState(
            [new DungeonPosition(1, 2, 3), new DungeonPosition(1, 2, 4)],
            [new DungeonPosition(1, 2, 4)]);
        Assert.NotEqual(first.GetHashCode(), sameObservedDifferentVisited.GetHashCode());
    }
}
