using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FloorLayoutGeneratorTests
{
    [Fact]
    public void Same_seed_version_and_floor_reproduce_the_layout()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 7);
        var second = generator.Generate(1234, "generator-1", 7);

        Assert.Equal(first.Rooms, second.Rooms);
        for (var x = 0; x < first.Width; x++)
        {
            for (var y = 0; y < first.Height; y++)
            {
                Assert.Equal(first.GetTile(new DungeonPosition(7, x, y)), second.GetTile(new DungeonPosition(7, x, y)));
            }
        }
    }

    [Fact]
    public void Generated_layout_has_one_connected_walkable_region()
    {
        var generator = new FloorLayoutGenerator();
        var layouts = Enumerable.Range(0, 32).Select(seed => generator.Generate(seed, "generator-1", 1)).ToArray();

        foreach (var layout in layouts)
        {
            Assert.Equal(8, layout.Rooms.Count);
            Assert.Equal(DungeonTile.StairsUp, layout.GetTile(layout.StairsUp));
            Assert.Equal(DungeonTile.StairsDown, layout.GetTile(layout.StairsDown));
            Assert.Contains(Enumerable.Range(0, layout.Width).SelectMany(x => Enumerable.Range(0, layout.Height).Select(y => new DungeonPosition(1, x, y))), position => layout.GetTile(position) == DungeonTile.Door);
            Assert.True(IsReachable(layout, layout.StairsUp, layout.StairsDown));
            Assert.All(layout.Rooms, room => Assert.True(IsReachable(layout, layout.StairsUp, new DungeonPosition(1, room.X + room.Width / 2, room.Y + room.Height / 2))));
        }
    }

    [Fact]
    public void Invalid_floor_and_options_are_rejected()
    {
        Assert.Equal(3, new FloorLayoutOptions(minimumRoomSize: 3).MinimumRoomSize);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutGenerator().Generate(1, "generator-1", 51));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutGenerator().Generate(1, "generator-1", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(width: 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(height: 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(width: 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(height: 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(roomCount: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(minimumRoomSize: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(minimumRoomSize: 8, maximumRoomSize: 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(width: 16, maximumRoomSize: 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FloorLayoutOptions(height: 16, maximumRoomSize: 15));
        Assert.Throws<InvalidOperationException>(() => new FloorLayoutGenerator().Generate(
            1,
            "generator-1",
            1,
            new FloorLayoutOptions(width: 16, height: 16, roomCount: 2, minimumRoomSize: 14, maximumRoomSize: 14)));
    }

    [Fact]
    public void Layout_validates_floor_and_position_boundaries()
    {
        var layout = new FloorLayoutGenerator().Generate(1, "generator-1", 1, new FloorLayoutOptions(roomCount: 2));

        Assert.Throws<ArgumentException>(() => layout.GetTile(new DungeonPosition(2, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetTile(new DungeonPosition(1, layout.Width, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetTile(new DungeonPosition(1, 0, layout.Height)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetTile(new DungeonPosition(1, -1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetTile(new DungeonPosition(1, 0, -1)));
        Assert.Throws<ArgumentNullException>(() => layout.GetTile(null!));
        Assert.True(layout.IsWalkable(layout.StairsUp));
    }

    [Fact]
    public void Generated_rooms_preserve_geometry_invariants()
    {
        var generator = new FloorLayoutGenerator();
        foreach (var seed in Enumerable.Range(0, 32))
        {
            var layout = generator.Generate(seed, "generator-1", 1);
            Assert.All(layout.Rooms, room =>
            {
                Assert.InRange(room.X, 1, layout.Width - room.Width - 1);
                Assert.InRange(room.Y, 1, layout.Height - room.Height - 1);
                Assert.InRange(room.Width, 5, 12);
                Assert.InRange(room.Height, 5, 12);
            });
            Assert.DoesNotContain(layout.Rooms, room => layout.Rooms.Any(other =>
                other.Id > room.Id &&
                room.X - 1 < other.X + other.Width &&
                room.X + room.Width + 1 > other.X &&
                room.Y - 1 < other.Y + other.Height &&
                room.Y + room.Height + 1 > other.Y));
        }
    }

    [Fact]
    public void Generated_rooms_are_not_mutable_through_the_read_only_view()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);

        Assert.Throws<NotSupportedException>(() => ((IList<DungeonRoom>)layout.Rooms).Add(new DungeonRoom(99, 1, 1, 3, 3)));
        Assert.Equal(8, layout.Rooms.Count);
    }

    [Fact]
    public void Generator_output_is_stable_for_compatibility_version_inputs()
    {
        var generator = new FloorLayoutGenerator();

        Assert.Equal(
            "8B7284D51DF66CE9F57CD3E5BF809F0B775B9E7A1D6DEB6A1F2AC47CA49A5B74",
            Fingerprint(generator.Generate(1234, "generator-1", 1)));
        Assert.Equal(
            "BA734ABE26C0004B607BEE891437BB41F3BDEB788543901D9185E61B9B8A8CE1",
            Fingerprint(generator.Generate(99, "generator-1", 3)));
        Assert.Equal(
            "95471DBC305D480AED040AD1C64D7ECBC14EC1A5BD3822A08538C43E6027AA16",
            Fingerprint(generator.Generate(42, "generator-1", 1, new FloorLayoutOptions(roomCount: 2))));
        Assert.Equal(
            "2F8EC9375BDA2BC96B86C18CF37A80EB0B2B1795C297BEF948DA0C226FB11F8A",
            AggregateFingerprint(string.Join(';', Enumerable.Range(0, 32).Select(seed => Fingerprint(generator.Generate(seed, "generator-1", 1))))));
        Assert.Equal(50, generator.Generate(1, "generator-1", 50).Floor);
    }

    [Fact]
    public void Connectivity_guard_rejects_disconnected_walkable_space_and_stairs()
    {
        var validate = typeof(FloorLayoutGenerator).GetMethod(
            "ValidateConnectivity",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var stairsUp = new DungeonPosition(1, 0, 0);
        var stairsDown = new DungeonPosition(1, 3, 3);

        var disconnected = new DungeonTile[4, 4];
        disconnected[0, 0] = DungeonTile.StairsUp;
        disconnected[3, 3] = DungeonTile.StairsDown;
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, [disconnected, stairsUp, stairsDown]));

        var unreachableStairs = new DungeonTile[4, 4];
        unreachableStairs[0, 0] = DungeonTile.StairsUp;
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, [unreachableStairs, stairsUp, stairsDown]));

        var connected = new DungeonTile[2, 2];
        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++) connected[x, y] = DungeonTile.Floor;
        }
        Assert.Null(Record.Exception(() => validate.Invoke(
            null,
            [connected, new DungeonPosition(1, 0, 0), new DungeonPosition(1, 1, 1)])));
    }

    private static bool IsReachable(FloorLayout layout, DungeonPosition start, DungeonPosition target)
    {
        var pending = new Queue<DungeonPosition>([start]);
        var visited = new HashSet<DungeonPosition>();
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current)) continue;
            if (current == target) return true;
            foreach (var next in Neighbors(layout, current))
            {
                if (layout.IsWalkable(next)) pending.Enqueue(next);
            }
        }
        return false;
    }

    private static IEnumerable<DungeonPosition> Neighbors(FloorLayout layout, DungeonPosition position)
    {
        if (position.X > 0) yield return new(position.Floor, position.X - 1, position.Y);
        if (position.X + 1 < layout.Width) yield return new(position.Floor, position.X + 1, position.Y);
        if (position.Y > 0) yield return new(position.Floor, position.X, position.Y - 1);
        if (position.Y + 1 < layout.Height) yield return new(position.Floor, position.X, position.Y + 1);
    }

    private static string Fingerprint(FloorLayout layout)
    {
        var text = new StringBuilder();
        foreach (var room in layout.Rooms)
        {
            text.Append($"{room.Id},{room.X},{room.Y},{room.Width},{room.Height};");
        }

        for (var x = 0; x < layout.Width; x++)
        {
            for (var y = 0; y < layout.Height; y++)
            {
                text.Append((int)layout.GetTile(new DungeonPosition(layout.Floor, x, y))).Append(',');
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private static string AggregateFingerprint(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
