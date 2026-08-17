using System.Reflection;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class EntryPointCoverageTests
{
    [Fact]
    public void Solution_entry_points_cover_valid_and_invalid_arguments()
    {
        var harness = Assembly.Load("Telengard.TestHarness").EntryPoint!;

        Assert.Equal(2, InvokeHarness(harness, []));
        Assert.Equal(2, InvokeHarness(harness, ["--seed"]));
        Assert.Equal(2, InvokeHarness(harness, ["--seed", "1", "unexpected"]));
        Assert.Equal(2, InvokeHarness(harness, ["wrong", "1"]));
        Assert.Equal(2, InvokeHarness(harness, ["--seed", "not-a-number"]));
        Assert.Equal(0, InvokeHarness(harness, ["--seed", "1"]));
        Assert.Equal(0, InvokeHarness(harness, ["--seed", "1", "--deterministic"]));

        var terminal = Assembly.Load("Telengard.Terminal").EntryPoint!;
        terminal.Invoke(null, null);
        Assert.NotNull(Assembly.Load("Telengard.Content"));
    }

    private static int InvokeHarness(MethodInfo entryPoint, string[] args) =>
        (int)entryPoint.Invoke(null, [args])!;
}
