using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Telengard.Core.Rng;

public sealed class DeterministicRng
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
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

        var input = new ArrayBufferWriter<byte>();
        WriteInt64(input, _worldSeed);
        WriteString(input, _generatorVersion);
        WriteString(input, name);
        WriteUInt32(input, checked((uint)scope.Length));

        foreach (var part in scope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(part);
            WriteString(input, part);
        }

        var digest = SHA256.HashData(input.WrittenSpan);
        return new DeterministicRngStream(BinaryPrimitives.ReadUInt64LittleEndian(digest));
    }

    // Canonical stream framing: Int64 and UInt32 values are little-endian;
    // strings are UTF-8 preceded by their UInt32 byte length; scope fields
    // follow an explicit UInt32 count in caller order.
    private static void WriteInt64(ArrayBufferWriter<byte> buffer, long value)
    {
        var destination = buffer.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(destination, value);
        buffer.Advance(sizeof(long));
    }

    private static void WriteUInt32(ArrayBufferWriter<byte> buffer, uint value)
    {
        var destination = buffer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        buffer.Advance(sizeof(uint));
    }

    private static void WriteString(ArrayBufferWriter<byte> buffer, string value)
    {
        var bytes = Utf8.GetBytes(value);
        WriteUInt32(buffer, checked((uint)bytes.Length));
        bytes.AsSpan().CopyTo(buffer.GetSpan(bytes.Length));
        buffer.Advance(bytes.Length);
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
