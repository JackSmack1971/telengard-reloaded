using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Core.World.Visibility;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class TileVisibilityTests
{
    [Fact]
    public void Resolver_exposes_only_the_configured_neighborhood()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var observer = layout.StairsUp;
        var map = TileVisibilityMap.Resolve(layout, observer, options: new TileVisibilityOptions(1));

        Assert.Equal(TileVisibility.CurrentlyVisible, map.GetVisibility(observer));
        Assert.Equal(TileVisibility.CurrentlyVisible, map.GetVisibility(new DungeonPosition(1, observer.X + 1, observer.Y)));
        Assert.Equal(TileVisibility.Unknown, map.GetVisibility(new DungeonPosition(1, layout.Width - 1, layout.Height - 1)));
    }

    [Fact]
    public void Previously_known_visibility_is_ranked_without_revealing_unknown_tiles()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var observer = layout.StairsUp;
        var visited = new DungeonPosition(1, layout.Width - 1, layout.Height - 1);
        var observed = new DungeonPosition(1, layout.Width - 2, layout.Height - 1);
        var map = TileVisibilityMap.Resolve(layout, observer, [observed], [visited]);

        Assert.Equal(TileVisibility.Visited, map.GetVisibility(visited));
        Assert.Equal(TileVisibility.Observed, map.GetVisibility(observed));
        Assert.Equal(TileVisibility.Unknown, map.GetVisibility(new DungeonPosition(1, 0, 0)));
    }

    [Fact]
    public void Resolver_rejects_invalid_positions_and_radius()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TileVisibilityOptions(-1));
        Assert.Throws<ArgumentException>(() => TileVisibilityMap.Resolve(layout, new DungeonPosition(2, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TileVisibilityMap.Resolve(layout, new DungeonPosition(1, layout.Width, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TileVisibilityMap.Resolve(
            layout,
            layout.StairsUp,
            visited: [new DungeonPosition(1, layout.Width, 1)]));

        Assert.Throws<ArgumentNullException>(() => TileVisibilityMap.Resolve(null!, layout.StairsUp));
        Assert.Throws<ArgumentNullException>(() => TileVisibilityMap.Resolve(layout, null!));
        Assert.Throws<ArgumentNullException>(() => TileVisibilityMap.Resolve(layout, layout.StairsUp, observed: [null!]));
        Assert.Throws<ArgumentNullException>(() => TileVisibilityMap.Resolve(layout, layout.StairsUp, visited: [null!]));
    }

    [Fact]
    public void Resolver_clips_visibility_to_each_layout_corner()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var radius = new TileVisibilityOptions(1);

        var topLeft = TileVisibilityMap.Resolve(layout, new DungeonPosition(1, 0, 0), options: radius);
        var bottomRight = TileVisibilityMap.Resolve(layout, new DungeonPosition(1, layout.Width - 1, layout.Height - 1), options: radius);

        Assert.Equal(4, topLeft.CurrentlyVisiblePositions.Count);
        Assert.Contains(new DungeonPosition(1, 0, 0), topLeft.CurrentlyVisiblePositions);
        Assert.Contains(new DungeonPosition(1, 1, 1), topLeft.CurrentlyVisiblePositions);
        Assert.Equal(4, bottomRight.CurrentlyVisiblePositions.Count);
        Assert.Contains(new DungeonPosition(1, layout.Width - 2, layout.Height - 2), bottomRight.CurrentlyVisiblePositions);
    }

    [Fact]
    public void Same_inputs_produce_the_same_visibility_snapshot()
    {
        var generator = new FloorLayoutGenerator();
        var firstLayout = generator.Generate(1234, "generator-1", 1);
        var secondLayout = generator.Generate(1234, "generator-1", 1);
        var first = TileVisibilityMap.Resolve(firstLayout, firstLayout.StairsUp, options: new TileVisibilityOptions(2));
        var second = TileVisibilityMap.Resolve(secondLayout, secondLayout.StairsUp, options: new TileVisibilityOptions(2));

        Assert.Equal(first.CurrentlyVisiblePositions, second.CurrentlyVisiblePositions);
        for (var x = 0; x < first.Layout.Width; x++)
        {
            for (var y = 0; y < first.Layout.Height; y++)
            {
                var position = new DungeonPosition(1, x, y);
                Assert.Equal(first.GetVisibility(position), second.GetVisibility(position));
            }
        }
    }

    [Fact]
    public void Fog_of_war_records_only_observed_and_visited_positions()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var visited = layout.StairsUp;
        var observed = new DungeonPosition(1, visited.X + 1, visited.Y);
        var map = FogOfWarMap.Create(layout).Observe([observed]).Visit(visited);

        Assert.Equal(new HashSet<DungeonPosition> { observed, visited }, map.ObservedPositions);
        Assert.Equal(new HashSet<DungeonPosition> { visited }, map.VisitedPositions);
        var visibility = map.Resolve(new DungeonPosition(1, 0, 0), new TileVisibilityOptions(0));
        Assert.Equal(TileVisibility.Observed, visibility.GetVisibility(observed));
        Assert.Equal(TileVisibility.Unknown, visibility.GetVisibility(new DungeonPosition(1, layout.Width - 1, layout.Height - 1)));
    }

    [Fact]
    public void Creating_fog_of_war_promotes_visited_positions_to_observed()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var visited = layout.StairsUp;

        var map = FogOfWarMap.Create(layout, visited: [visited]);

        Assert.Contains(visited, map.ObservedPositions);
        Assert.Contains(visited, map.VisitedPositions);
    }

    [Fact]
    public void Fog_of_war_updates_are_immutable_and_validate_positions()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var map = FogOfWarMap.Create(layout);

        var updated = map.Visit(layout.StairsUp);

        Assert.Empty(map.VisitedPositions);
        Assert.Contains(layout.StairsUp, updated.VisitedPositions);
        Assert.Throws<ArgumentException>(() => map.Visit(new DungeonPosition(2, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Observe([new DungeonPosition(1, layout.Width, 1)]));
        Assert.Throws<ArgumentNullException>(() => map.Observe(null!));
        Assert.Throws<ArgumentNullException>(() => FogOfWarMap.Create(null!));
        Assert.Throws<ArgumentNullException>(() => FogOfWarMap.Create(layout, (PersistentMapState)null!));
        Assert.Throws<ArgumentNullException>(() => map.Resolve(null!));
        Assert.Throws<ArgumentNullException>(() => map.Visit(null!));
        Assert.Throws<ArgumentNullException>(() => map.Resolve(layout.StairsUp).GetVisibility(null!));
        Assert.Throws<ArgumentNullException>(() => FogOfWarMap.Create(layout, observed: [null!]));
        Assert.Throws<ArgumentNullException>(() => FogOfWarMap.Create(layout, visited: [null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Visit(new DungeonPosition(1, -1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Visit(new DungeonPosition(1, 0, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Visit(new DungeonPosition(1, 0, layout.Height)));
        Assert.Contains(new DungeonPosition(1, 0, 0), map.Visit(new DungeonPosition(1, 0, 0)).VisitedPositions);
        Assert.Contains(new DungeonPosition(1, 0, 0), map.Visit(new DungeonPosition(1, 0, 0)).ObservedPositions);

        var resolved = map.Resolve(layout.StairsUp);
        Assert.Throws<ArgumentOutOfRangeException>(() => resolved.GetVisibility(new DungeonPosition(1, 0, layout.Height)));
        Assert.Throws<ArgumentOutOfRangeException>(() => resolved.GetVisibility(new DungeonPosition(1, -1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => resolved.GetVisibility(new DungeonPosition(1, 0, -1)));
    }

    [Fact]
    public void Fog_of_war_round_trips_through_persistent_map_state()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var map = FogOfWarMap.Create(layout).Observe([new DungeonPosition(1, 2, 2)]).Visit(layout.StairsUp);

        var restored = FogOfWarMap.Create(layout, map.ToPersistentState());

        Assert.Equal(map.ObservedPositions, restored.ObservedPositions);
        Assert.Equal(map.VisitedPositions, restored.VisitedPositions);
    }

    [Fact]
    public void Persistent_map_state_keeps_visited_positions_observed_and_is_deterministically_ordered()
    {
        var state = new PersistentMapState(
            [new DungeonPosition(2, 4, 1), new DungeonPosition(1, 3, 2)],
            [new DungeonPosition(1, 3, 2), new DungeonPosition(2, 0, 0)]);

        Assert.Equal(
            [new DungeonPosition(1, 3, 2), new DungeonPosition(2, 0, 0), new DungeonPosition(2, 4, 1)],
            state.ObservedPositions);
        Assert.Equal(
            [new DungeonPosition(1, 3, 2), new DungeonPosition(2, 0, 0)],
            state.VisitedPositions);
    }
}
