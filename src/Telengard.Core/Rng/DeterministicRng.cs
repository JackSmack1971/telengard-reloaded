using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Telengard.Core.Rng;

public sealed class DeterministicRng
{
    private readonly long _worldSeed;
    private readonly string _generatorVersion;

    public DeterministicRng(long worldSeed, string generatorVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        _worldSeed = worldSeed;
        _generatorVersion = generatorVersion;
    }

    public DeterministicRngStream CreateStream(string name, params string[] scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(scope);

        var input = new StringBuilder()
            .Append(_worldSeed)
            .Append('\0')
            .Append(_generatorVersion)
            .Append('\0')
            .Append(name);

        foreach (var part in scope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(part);
            input.Append('\0').Append(part);
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(input.ToString()));
        return new DeterministicRngStream(BinaryPrimitives.ReadUInt64LittleEndian(digest));
    }
}

public sealed class DeterministicRngStream
{
    private ulong _state;

    internal DeterministicRngStream(ulong seed) => _state = seed;

    public uint NextUInt()
    {
        _state += 0x9E3779B97F4A7C15;
        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EB;
        return (uint)((value ^ (value >> 31)) >> 32);
    }

    public int NextInt(int minimumInclusive, int maximumExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minimumInclusive, maximumExclusive);

        var range = (uint)((long)maximumExclusive - minimumInclusive);
        var limit = uint.MaxValue - (uint.MaxValue % range);
        uint value;
        do
        {
            value = NextUInt();
        }
        while (value >= limit);

        return (int)(value % range) + minimumInclusive;
    }

    public long NextLong(long minimumInclusive, long maximumExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minimumInclusive, maximumExclusive);

        var range = (ulong)maximumExclusive - (ulong)minimumInclusive;
        var limit = ulong.MaxValue - (ulong.MaxValue % range);
        ulong value;
        do
        {
            value = ((ulong)NextUInt() << 32) | NextUInt();
        }
        while (value >= limit);

        return (long)((ulong)minimumInclusive + (value % range));
    }

    public double NextDouble() => (NextUInt() >> 5) / (double)(1u << 27);
}
