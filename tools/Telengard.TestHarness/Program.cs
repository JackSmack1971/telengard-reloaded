using Telengard.Core.Simulation;

namespace Telengard.TestHarness;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length is < 2 or > 3 || args[0] != "--seed" || !long.TryParse(args[1], out var seed) ||
            (args.Length == 3 && args[2] != "--deterministic"))
        {
            Console.Error.WriteLine("Usage: Telengard.TestHarness --seed <seed> [--deterministic]");
            return 2;
        }

        if (args.Length == 3)
        {
            SimulationTestHarness.AssertDeterministic(seed, _ => { }, []);
        }

        var state = GameState.Create(seed);
        Console.WriteLine($"seed={state.WorldSeed} simulation_version={state.Versions.SimulationVersion} generator_version={state.Versions.GeneratorVersion} content_version={state.Versions.ContentVersion}");
        return 0;
    }
}
