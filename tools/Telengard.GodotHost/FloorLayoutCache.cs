using Telengard.Core.World.Generation;

namespace Telengard.GodotHost;

public sealed class FloorLayoutCache
{
    private readonly long _worldSeed;
    private readonly string _generatorVersion;
    private readonly int _maximumFloor;
    private readonly FloorLayoutGenerator _generator = new();
    private readonly Dictionary<int, FloorLayout> _layouts = [];

    public FloorLayoutCache(long worldSeed, string generatorVersion, int maximumFloor = 5)
    {
        if (maximumFloor < 1) throw new ArgumentOutOfRangeException(nameof(maximumFloor));
        _worldSeed = worldSeed;
        _generatorVersion = generatorVersion ?? throw new ArgumentNullException(nameof(generatorVersion));
        _maximumFloor = maximumFloor;
    }

    public FloorLayout Get(int floor)
    {
        if (floor is < 1 || floor > _maximumFloor)
            throw new InvalidOperationException($"The hosted MVP session supports floors 1 through {_maximumFloor}.");

        if (!_layouts.TryGetValue(floor, out var layout))
        {
            layout = _generator.Generate(_worldSeed, _generatorVersion, floor);
            _layouts.Add(floor, layout);
        }

        return layout;
    }
}
