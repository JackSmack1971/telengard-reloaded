using Telengard.Core.Simulation;

namespace Telengard.Core.World.Generation;

public enum DungeonTile
{
    Wall,
    Floor,
    Door,
    StairsUp,
    StairsDown
}

public sealed record FloorLayoutOptions
{
    public FloorLayoutOptions(
        int width = 64,
        int height = 64,
        int roomCount = 8,
        int minimumRoomSize = 5,
        int maximumRoomSize = 12)
    {
        if (width < 16) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 16) throw new ArgumentOutOfRangeException(nameof(height));
        if (roomCount < 2) throw new ArgumentOutOfRangeException(nameof(roomCount));
        if (minimumRoomSize < 3) throw new ArgumentOutOfRangeException(nameof(minimumRoomSize));
        if (maximumRoomSize < minimumRoomSize) throw new ArgumentOutOfRangeException(nameof(maximumRoomSize));
        if (maximumRoomSize >= width - 1) throw new ArgumentOutOfRangeException(nameof(maximumRoomSize));
        if (maximumRoomSize >= height - 1) throw new ArgumentOutOfRangeException(nameof(maximumRoomSize));

        Width = width;
        Height = height;
        RoomCount = roomCount;
        MinimumRoomSize = minimumRoomSize;
        MaximumRoomSize = maximumRoomSize;
    }

    public int Width { get; }
    public int Height { get; }
    public int RoomCount { get; }
    public int MinimumRoomSize { get; }
    public int MaximumRoomSize { get; }
}

public sealed record DungeonRoom(int Id, int X, int Y, int Width, int Height);

public sealed class FloorLayout
{
    private readonly DungeonTile[,] _tiles;

    internal FloorLayout(
        int floor,
        DungeonTile[,] tiles,
        IReadOnlyList<DungeonRoom> rooms,
        DungeonPosition stairsUp,
        DungeonPosition stairsDown)
    {
        Floor = floor;
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(rooms);
        var roomCopy = rooms.ToArray();
        if (roomCopy.Any(room => room is null))
        {
            throw new ArgumentException("Rooms cannot contain null values.", nameof(rooms));
        }

        _tiles = (DungeonTile[,])tiles.Clone();
        Rooms = Array.AsReadOnly(roomCopy);
        StairsUp = stairsUp;
        StairsDown = stairsDown;
    }

    public int Floor { get; }
    public int Width => _tiles.GetLength(0);
    public int Height => _tiles.GetLength(1);
    public IReadOnlyList<DungeonRoom> Rooms { get; }
    public DungeonPosition StairsUp { get; }
    public DungeonPosition StairsDown { get; }

    public DungeonTile GetTile(DungeonPosition position)
    {
        ValidatePosition(position);
        return _tiles[position.X, position.Y];
    }

    public bool IsWalkable(DungeonPosition position) => GetTile(position) is not DungeonTile.Wall;

    private void ValidatePosition(DungeonPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Floor != Floor)
        {
            throw new ArgumentException("Position belongs to a different floor.", nameof(position));
        }

        if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position is outside the generated layout.");
        }
    }
}
