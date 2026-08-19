using Telengard.Core.Rng;
using Xunit;
using System.Globalization;
using System.Text;

namespace Telengard.Architecture.Tests;

public sealed class DeterministicRngTests
{
    [Fact]
    public void Same_seed_version_and_scope_reproduce_the_same_sequence()
    {
        var first = new DeterministicRng(1234, "generator-1").CreateStream("loot", "floor:3", "x:4", "y:5");
        var second = new DeterministicRng(1234, "generator-1").CreateStream("loot", "floor:3", "x:4", "y:5");

        Assert.Equal(first.NextUInt(), second.NextUInt());
        Assert.Equal(first.NextInt(-10, 10), second.NextInt(-10, 10));
        Assert.Equal(first.NextDouble(), second.NextDouble());
    }

    [Fact]
    public void Changing_a_scope_does_not_reuse_the_same_stream()
    {
        var rng = new DeterministicRng(1234, "generator-1");

        Assert.NotEqual(
            rng.CreateStream("loot", "floor:3").NextUInt(),
            rng.CreateStream("loot", "floor:4").NextUInt());
        Assert.NotEqual(
            rng.CreateStream("loot", "floor:3").NextUInt(),
            new DeterministicRng(1234, "generator-2").CreateStream("loot", "floor:3").NextUInt());
    }

    [Fact]
    public void Bounded_values_are_inside_the_requested_range()
    {
        var stream = new DeterministicRng(1234, "generator-1").CreateStream("encounter");

        for (var i = 0; i < 1000; i++)
        {
            var value = stream.NextInt(-3, 7);
            Assert.InRange(value, -3, 6);
        }
    }

    [Fact]
    public void Long_values_are_deterministic_and_inside_the_requested_range()
    {
        var first = new DeterministicRng(1234, "generator-1").CreateStream("loot");
        var second = new DeterministicRng(1234, "generator-1").CreateStream("loot");

        var firstValue = first.NextLong(-10000000000, 10000000000);
        var secondValue = second.NextLong(-10000000000, 10000000000);

        Assert.Equal(firstValue, secondValue);
        Assert.InRange(firstValue, -10000000000, 9999999999);
    }

    [Fact]
    public void Invalid_stream_inputs_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DeterministicRng(1, " "));
        Assert.Throws<ArgumentException>(() => new DeterministicRng(1, "v1").CreateStream(" "));
        Assert.Throws<ArgumentNullException>(() => new DeterministicRng(1, "v1").CreateStream("test", (string[])null!));
        Assert.Throws<ArgumentNullException>(() => new DeterministicRng(1, "v1").CreateStream("test", new string[] { null! }));
        Assert.Throws<EncoderFallbackException>(() => new DeterministicRng(1, "v1").CreateStream("\uD800"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeterministicRng(1, "v1").CreateStream("test").NextInt(1, 1));
    }

    [Fact]
    public void Canonical_stream_encoding_is_invariant_to_current_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var first = new DeterministicRng(-1234567890, "0.2")
                .CreateStream("culture", "scope")
                .NextUInt();

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fa-IR");
            var second = new DeterministicRng(-1234567890, "0.2")
                .CreateStream("culture", "scope")
                .NextUInt();

            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Canonical_stream_encoding_distinguishes_embedded_nul_from_scope_boundaries()
    {
        var embeddedNul = new DeterministicRng(1234, "0.2").CreateStream("a\0b");
        var separateScope = new DeterministicRng(1234, "0.2").CreateStream("a", "b");

        Assert.NotEqual(embeddedNul.NextUInt(), separateScope.NextUInt());
    }

    [Fact]
    public void Canonical_stream_encoding_preserves_scope_cardinality_and_order()
    {
        var rng = new DeterministicRng(1234, "0.2");

        Assert.NotEqual(
            rng.CreateStream("scope").NextUInt(),
            rng.CreateStream("scope", "one").NextUInt());
        Assert.NotEqual(
            rng.CreateStream("scope", "one").NextUInt(),
            rng.CreateStream("scope", "one", "two").NextUInt());
        Assert.NotEqual(
            rng.CreateStream("scope", "one", "two").NextUInt(),
            rng.CreateStream("scope", "two", "one").NextUInt());
    }

    [Fact]
    public void Fixed_seed_stream_has_a_stable_sequence_and_double_projection()
    {
        var sequence = new DeterministicRng(1234, "0.2").CreateStream("loot", "floor:3", "x:4", "y:5");

        Assert.Equal(2187073981u, sequence.NextUInt());
        Assert.Equal(265811292u, sequence.NextUInt());
        Assert.Equal(775556425u, sequence.NextUInt());

        var stream = new DeterministicRng(1234, "0.2").CreateStream("loot", "floor:3", "x:4", "y:5");
        Assert.Equal(2187073981u, stream.NextUInt());
        Assert.Equal(2, stream.NextInt(-10, 10));
        Assert.Equal(0.18057329952716827d, stream.NextDouble());
    }
}
