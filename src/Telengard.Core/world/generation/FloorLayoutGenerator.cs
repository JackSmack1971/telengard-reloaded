using Telengard.Core.Rng;
using Telengard.Core.Simulation;

namespace Telengard.Core.World.Generation;

public sealed class FloorLayoutGenerator
{
    public FloorLayout Generate(
        long worldSeed,
        string generatorVersion,
        int floor,
        FloorLayoutOptions? options = null)
    {
        if (floor is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(floor));

        var settings = options ?? new FloorLayoutOptions();
        var tiles = new DungeonTile[settings.Width, settings.Height];
        for (var x = 0; x < settings.Width; x++)
        {
            for (var y = 0; y < settings.Height; y++) tiles[x, y] = DungeonTile.Wall;
        }

        var rng = new DeterministicRng(worldSeed, generatorVersion).CreateStream("layout", $"floor:{floor}");
        var rooms = CreateRooms(settings, rng);
        for (var i = 0; i < rooms.Count; i++) CarveRoom(tiles, rooms[i]);

        for (var i = 1; i < rooms.Count; i++) CarveCorridor(tiles, rooms[i - 1], rooms[i], rng.NextInt(0, 2) == 0);
        if (rooms.Count > 2) CarveCorridor(tiles, rooms[0], rooms[^1], rng.NextInt(0, 2) == 0);

        var stairsUp = PositionFor(rooms[0], floor);
        var stairsDown = PositionFor(rooms[^1], floor);
        AddDoors(tiles, rooms);
        tiles[stairsUp.X, stairsUp.Y] = DungeonTile.StairsUp;
        tiles[stairsDown.X, stairsDown.Y] = DungeonTile.StairsDown;
        ValidateConnectivity(tiles, stairsUp, stairsDown);

        return new FloorLayout(floor, tiles, rooms, stairsUp, stairsDown);
    }

    private static void ValidateConnectivity(DungeonTile[,] tiles, DungeonPosition stairsUp, DungeonPosition stairsDown)
    {
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        var visited = new bool[width, height];
        var pending = new Queue<(int X, int Y)>([(stairsUp.X, stairsUp.Y)]);
        var walkableCount = 0;
        var reachableCount = 0;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (tiles[x, y] is not DungeonTile.Wall) walkableCount++;
            }
        }

        while (pending.TryDequeue(out var current))
        {
            if (current.X < 0 || current.X >= width || current.Y < 0 || current.Y >= height ||
                visited[current.X, current.Y] || tiles[current.X, current.Y] is DungeonTile.Wall)
            {
                continue;
            }

            visited[current.X, current.Y] = true;
            reachableCount++;
            pending.Enqueue((current.X - 1, current.Y));
            pending.Enqueue((current.X + 1, current.Y));
            pending.Enqueue((current.X, current.Y - 1));
            pending.Enqueue((current.X, current.Y + 1));
        }

        if (reachableCount != walkableCount || !visited[stairsDown.X, stairsDown.Y])
        {
            throw new InvalidOperationException("The generated floor contains disconnected walkable space.");
        }
    }

    private static List<DungeonRoom> CreateRooms(FloorLayoutOptions settings, DeterministicRngStream rng)
    {
        var rooms = new List<DungeonRoom>(settings.RoomCount);
        for (var attempt = 0; attempt < settings.RoomCount * 100 && rooms.Count < settings.RoomCount; attempt++)
        {
            var width = rng.NextInt(settings.MinimumRoomSize, settings.MaximumRoomSize + 1);
            var height = rng.NextInt(settings.MinimumRoomSize, settings.MaximumRoomSize + 1);
            var room = new DungeonRoom(
                rooms.Count,
                rng.NextInt(1, settings.Width - width),
                rng.NextInt(1, settings.Height - height),
                width,
                height);

            if (rooms.All(existing => !OverlapsWithMargin(existing, room))) rooms.Add(room);
        }

        if (rooms.Count < settings.RoomCount)
        {
            throw new InvalidOperationException("The configured layout could not place all rooms.");
        }

        return rooms;
    }

    private static bool OverlapsWithMargin(DungeonRoom first, DungeonRoom second) =>
        first.X - 1 < second.X + second.Width &&
        first.X + first.Width + 1 > second.X &&
        first.Y - 1 < second.Y + second.Height &&
        first.Y + first.Height + 1 > second.Y;

    private static void CarveRoom(DungeonTile[,] tiles, DungeonRoom room)
    {
        for (var x = room.X; x < room.X + room.Width; x++)
        {
            for (var y = room.Y; y < room.Y + room.Height; y++)
            {
                tiles[x, y] = DungeonTile.Floor;
            }
        }
    }

    private static void CarveCorridor(DungeonTile[,] tiles, DungeonRoom first, DungeonRoom second, bool horizontalFirst)
    {
        var start = (X: first.X + first.Width / 2, Y: first.Y + first.Height / 2);
        var end = (X: second.X + second.Width / 2, Y: second.Y + second.Height / 2);
        if (horizontalFirst)
        {
            CarveHorizontal(tiles, start.Y, start.X, end.X);
            CarveVertical(tiles, end.X, start.Y, end.Y);
        }
        else
        {
            CarveVertical(tiles, start.X, start.Y, end.Y);
            CarveHorizontal(tiles, end.Y, start.X, end.X);
        }
    }

    private static void CarveHorizontal(DungeonTile[,] tiles, int y, int firstX, int secondX)
    {
        for (var x = Math.Min(firstX, secondX); x <= Math.Max(firstX, secondX); x++) tiles[x, y] = DungeonTile.Floor;
    }

    private static void CarveVertical(DungeonTile[,] tiles, int x, int firstY, int secondY)
    {
        for (var y = Math.Min(firstY, secondY); y <= Math.Max(firstY, secondY); y++) tiles[x, y] = DungeonTile.Floor;
    }

    private static void AddDoors(DungeonTile[,] tiles, IReadOnlyList<DungeonRoom> rooms)
    {
        for (var i = 1; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var previous = rooms[i - 1];
            var x = room.X + room.Width / 2;
            var y = room.Y + room.Height / 2;
            if (Math.Abs(previous.X - room.X) > Math.Abs(previous.Y - room.Y)) x = previous.X < room.X ? room.X : room.X + room.Width - 1;
            else y = previous.Y < room.Y ? room.Y : room.Y + room.Height - 1;
            tiles[x, y] = DungeonTile.Door;
        }
    }

    private static DungeonPosition PositionFor(DungeonRoom room, int floor) => new(floor, room.X + room.Width / 2, room.Y + room.Height / 2);
}
