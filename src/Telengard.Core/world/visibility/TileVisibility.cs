using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;

namespace Telengard.Core.World.Visibility;

public enum TileVisibility
{
    Unknown,
    Observed,
    Visited,
    CurrentlyVisible
}

public sealed record TileVisibilityOptions
{
    public TileVisibilityOptions(int viewRadius = 4)
    {
        if (viewRadius < 0) throw new ArgumentOutOfRangeException(nameof(viewRadius));

        ViewRadius = viewRadius;
    }

    public int ViewRadius { get; }
}

public sealed class TileVisibilityMap
{
    private readonly HashSet<DungeonPosition> _observed;
    private readonly HashSet<DungeonPosition> _visited;
    private readonly HashSet<DungeonPosition> _currentlyVisible;

    private TileVisibilityMap(
        FloorLayout layout,
        IEnumerable<DungeonPosition> observed,
        IEnumerable<DungeonPosition> visited,
        IEnumerable<DungeonPosition> currentlyVisible)
    {
        Layout = layout;
        _observed = observed.ToHashSet();
        _visited = visited.ToHashSet();
        _currentlyVisible = currentlyVisible.ToHashSet();
    }

    public FloorLayout Layout { get; }
    public IReadOnlySet<DungeonPosition> CurrentlyVisiblePositions => _currentlyVisible;

    public static TileVisibilityMap Resolve(
        FloorLayout layout,
        DungeonPosition observer,
        IEnumerable<DungeonPosition>? observed = null,
        IEnumerable<DungeonPosition>? visited = null,
        TileVisibilityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidatePosition(layout, observer, nameof(observer));

        var knownObserved = ValidatePositions(layout, observed ?? [], nameof(observed));
        var knownVisited = ValidatePositions(layout, visited ?? [], nameof(visited));
        var settings = options ?? new TileVisibilityOptions();
        var currentlyVisible = new List<DungeonPosition>();

        for (var x = Math.Max(0, observer.X - settings.ViewRadius); x <= Math.Min(layout.Width - 1, observer.X + settings.ViewRadius); x++)
        {
            for (var y = Math.Max(0, observer.Y - settings.ViewRadius); y <= Math.Min(layout.Height - 1, observer.Y + settings.ViewRadius); y++)
            {
                currentlyVisible.Add(new DungeonPosition(layout.Floor, x, y));
            }
        }

        return new TileVisibilityMap(layout, knownObserved, knownVisited, currentlyVisible);
    }

    public TileVisibility GetVisibility(DungeonPosition position)
    {
        ValidatePosition(Layout, position, nameof(position));
        if (_currentlyVisible.Contains(position)) return TileVisibility.CurrentlyVisible;
        if (_visited.Contains(position)) return TileVisibility.Visited;
        if (_observed.Contains(position)) return TileVisibility.Observed;
        return TileVisibility.Unknown;
    }

    private static HashSet<DungeonPosition> ValidatePositions(
        FloorLayout layout,
        IEnumerable<DungeonPosition> positions,
        string parameterName)
    {
        var result = new HashSet<DungeonPosition>();
        foreach (var position in positions)
        {
            ValidatePosition(layout, position, parameterName);
            result.Add(position);
        }

        return result;
    }

    private static void ValidatePosition(FloorLayout layout, DungeonPosition position, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Floor != layout.Floor)
        {
            throw new ArgumentException("Position belongs to a different floor.", parameterName);
        }

        if (position.X < 0 || position.X >= layout.Width || position.Y < 0 || position.Y >= layout.Height)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Position is outside the generated layout.");
        }
    }
}

public sealed class FogOfWarMap
{
    private readonly HashSet<DungeonPosition> _observed;
    private readonly HashSet<DungeonPosition> _visited;

    private FogOfWarMap(FloorLayout layout, IEnumerable<DungeonPosition> observed, IEnumerable<DungeonPosition> visited)
    {
        Layout = layout;
        _observed = observed.ToHashSet();
        _visited = visited.ToHashSet();
    }

    public FloorLayout Layout { get; }
    public IReadOnlySet<DungeonPosition> ObservedPositions => _observed;
    public IReadOnlySet<DungeonPosition> VisitedPositions => _visited;

    public static FogOfWarMap Create(FloorLayout layout, PersistentMapState persistentMap)
    {
        ArgumentNullException.ThrowIfNull(persistentMap);
        return Create(layout, persistentMap.ObservedPositions, persistentMap.VisitedPositions);
    }

    public static FogOfWarMap Create(
        FloorLayout layout,
        IEnumerable<DungeonPosition>? observed = null,
        IEnumerable<DungeonPosition>? visited = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var knownObserved = ValidatePositions(layout, observed ?? [], nameof(observed));
        var knownVisited = ValidatePositions(layout, visited ?? [], nameof(visited));
        knownObserved.UnionWith(knownVisited);
        return new FogOfWarMap(layout, knownObserved, knownVisited);
    }

    public FogOfWarMap Observe(IEnumerable<DungeonPosition> positions)
    {
        var observed = new HashSet<DungeonPosition>(_observed);
        observed.UnionWith(ValidatePositions(Layout, positions, nameof(positions)));
        return new FogOfWarMap(Layout, observed, _visited);
    }

    public FogOfWarMap Visit(DungeonPosition position)
    {
        ValidatePosition(Layout, position, nameof(position));
        var visited = new HashSet<DungeonPosition>(_visited) { position };
        var observed = new HashSet<DungeonPosition>(_observed) { position };
        return new FogOfWarMap(Layout, observed, visited);
    }

    public PersistentMapState ToPersistentState() => new(_observed, _visited);

    public TileVisibilityMap Resolve(DungeonPosition observer, TileVisibilityOptions? options = null) =>
        TileVisibilityMap.Resolve(Layout, observer, _observed, _visited, options);

    private static HashSet<DungeonPosition> ValidatePositions(
        FloorLayout layout,
        IEnumerable<DungeonPosition> positions,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var result = new HashSet<DungeonPosition>();
        foreach (var position in positions)
        {
            ValidatePosition(layout, position, parameterName);
            result.Add(position);
        }

        return result;
    }

    private static void ValidatePosition(FloorLayout layout, DungeonPosition position, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Floor != layout.Floor)
        {
            throw new ArgumentException("Position belongs to a different floor.", parameterName);
        }

        if (position.X < 0 || position.X >= layout.Width || position.Y < 0 || position.Y >= layout.Height)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Position is outside the generated layout.");
        }
    }
}
